using System.Net;
using Betriebsplattform.Web.Akten;

namespace Betriebsplattform.Web;

// The executable defaults to personal authentication. Demo requires an explicit launch argument.
public static class Startmodus
{
    public static string[] Argumente(string[] args)=>args.Any(a=>a=="--FirmenZentrale:Startmodus"||a.StartsWith("--FirmenZentrale:Startmodus=",StringComparison.Ordinal))
        ?args:[..args,"--FirmenZentrale:Startmodus=Anmeldung"];

    public static WebApplication? Vorbereiten(WebApplicationBuilder builder,int port,string? frontend)
    {
        var modus=builder.Configuration["FirmenZentrale:Startmodus"];
        if(modus is null)return null; // In-process test harnesses supply their own isolated host configuration.
        if(modus is not ("Demo" or "Vorschau" or "Anmeldung"))throw new InvalidOperationException("Unbekannter Startmodus.");
        var sicher=builder.Configuration.GetValue<bool>("FirmenZentrale:Security:Enabled");
        var provider=builder.Configuration["FirmenZentrale:Persistence:Provider"]??"Demo";
        if(modus is "Demo" or "Vorschau")
        {
            if(!builder.Environment.IsDevelopment())throw new InvalidOperationException("Vorschau nur in Development.");
            if(sicher||provider!="Demo")throw new InvalidOperationException("Demo muss vom authentifizierten Datenbestand getrennt bleiben.");
            return null;
        }
        if(sicher&&provider=="PostgreSql"&&File.Exists(builder.Configuration["FirmenZentrale:Security:CertificatePath"]))return null;
        // No database connection, no business endpoints and no automatic provisioning in setup mode.
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o=>{o.Configure(new ConfigurationBuilder().Build());o.Listen(IPAddress.Loopback,port);});
        var app=builder.Build();Webschutz.Konfigurieren(app,new Arbeitsmodule());
        app.MapGet("/api/auth/session",()=>new{aktiv=true,angemeldet=false,bereit=false,meldung="Die persönliche Anmeldung ist noch nicht betriebsbereit. PostgreSQL, HTTPS, Schlüsselablage und Benutzerkonten müssen lokal freigegeben eingerichtet sein. Es wurde kein Demobetrieb gestartet und keine Datenbank verändert."});
        app.Map("/api/{**pfad}",()=>Results.Json(new ApiFehler("Anmeldung noch nicht eingerichtet. Kein Zugriff auf Geschäftsdaten."),statusCode:503));
        Webschutz.Oberflaeche(app,frontend);
        return app;
    }
}
