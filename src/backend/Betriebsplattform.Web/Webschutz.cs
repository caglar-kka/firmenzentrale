using Microsoft.Extensions.FileProviders;
using Betriebsplattform.Web.Akten;

namespace Betriebsplattform.Web;

public static class Webschutz
{
    public static void Konfigurieren(WebApplication app,Arbeitsmodule aktiveModule)
    {
        app.UseExceptionHandler(fehler => fehler.Run(async context =>
        {
            var fehler=context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
            context.Response.StatusCode=fehler is BadHttpRequestException b?b.StatusCode:500;
            await context.Response.WriteAsJsonAsync(new ApiFehler(context.Response.StatusCode==413?"Die Anfrage ist zu groß.":context.Response.StatusCode==400?"Bitte Eingaben und Datenformat prüfen.":"Die Anfrage konnte nicht verarbeitet werden. Bitte erneut versuchen."));
        }));
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self' data:; frame-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";
            var origin = context.Request.Headers.Origin.ToString();
            var eigen = $"{context.Request.Scheme}://127.0.0.1:{context.Connection.LocalPort}";
            if(context.Request.IsHttps)context.Response.Headers.StrictTransportSecurity="max-age=31536000";
            if (context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != context.Connection.LocalPort
                || (origin.Length > 0 && origin != eigen && !(app.Environment.IsDevelopment() && origin == "http://127.0.0.1:5173")))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new ApiFehler("Dieser Prototyp ist nur über seine lokale Adresse erreichbar."));
                return;
            }
            await next(context);
        });
        app.Use(async (context, next) =>
        {
            var pfad = context.Request.Path;
            var erforderlich = pfad.StartsWithSegments("/api/uebersicht") ? "Finanzen"
                : pfad.Value?.StartsWith("/api/import", StringComparison.Ordinal) == true ? "Import"
                : pfad.StartsWithSegments("/api/dokumente") || pfad.StartsWithSegments("/api/dokumenteingang") || pfad.StartsWithSegments("/api/posteingang") || pfad.StartsWithSegments("/api/vorgaenge") ? "Dokumente"
                : pfad.StartsWithSegments("/api/akten") ? "Akten" : null;
            if (erforderlich is not null && !aktiveModule.Aktiv(erforderlich)) { context.Response.StatusCode = 404; return; }
            if (pfad.StartsWithSegments("/api") && context.Request.Method is not ("GET" or "HEAD")
                && (string.IsNullOrEmpty(context.Request.Headers.Origin)
                    || (context.Request.Headers["X-Arbeitsaktion"] != "1" && context.Request.Headers["X-Demo-Import"] != "1")))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new ApiFehler("Bitte die lokale Arbeitsoberfläche für Änderungen verwenden.")); return;
            }
            if(pfad.StartsWithSegments("/api")&&context.Request.Method is "POST" or "PUT" or "PATCH"
                &&pfad!="/api/dokumenteingang"&&pfad!="/api/importe/vorschau"
                &&!(context.Request.ContentType?.Split(';')[0].Trim().Equals("application/json",StringComparison.OrdinalIgnoreCase)??false))
            {context.Response.StatusCode=415;await context.Response.WriteAsJsonAsync(new ApiFehler("Bitte das vorgesehene JSON-Datenformat verwenden."));return;}
            var requestLimit=context.Request.Path=="/api/dokumenteingang"?Dokumentpruefung.MaxBytes:context.Request.Path.Value?.StartsWith("/api/import",StringComparison.Ordinal)==true?2*1024*1024:128*1024;
            var bodyFeature=context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
            if(bodyFeature is {IsReadOnly:false})bodyFeature.MaxRequestBodySize=requestLimit;
            if(context.Request.ContentLength>requestLimit){context.Response.StatusCode=413;await context.Response.WriteAsJsonAsync(new ApiFehler(context.Request.Path=="/api/dokumenteingang"?"Die Datei überschreitet 20 MB.":context.Request.Path.Value?.StartsWith("/api/import",StringComparison.Ordinal)==true?"Die Datei überschreitet 2,1 MB.":"Die Eingabe ist zu umfangreich. Bitte kürzen."));return;}
            await next(context);
        });
        app.UseStatusCodePages(async s=>{var c=s.HttpContext;if(c.Request.Path.StartsWithSegments("/api"))await c.Response.WriteAsJsonAsync(new ApiFehler(c.Response.StatusCode switch{400=>"Bitte Eingaben und Datenformat prüfen.",403=>"Für diese Aktion fehlt die Berechtigung.",404=>"Eintrag nicht gefunden.",405=>"Diese Aktion wird nicht unterstützt.",413=>"Die Anfrage ist zu groß.",415=>"Bitte das vorgesehene Datenformat verwenden.",_=>"Die Anfrage konnte nicht verarbeitet werden."}));});
    }
    public static void Oberflaeche(WebApplication app,string? frontendVerzeichnis)
    {
        var frontend = frontendVerzeichnis ?? (File.Exists(Path.Combine(AppContext.BaseDirectory,"wwwroot","index.html"))?Path.Combine(AppContext.BaseDirectory,"wwwroot"):Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/dist")));
        var index = Path.Combine(frontend, "index.html");
        if (Directory.Exists(frontend)&&File.Exists(index))
        {
            var dateien = new PhysicalFileProvider(Path.GetFullPath(frontend));
            app.Lifetime.ApplicationStopped.Register(dateien.Dispose);
            app.UseStaticFiles(new StaticFileOptions { FileProvider = dateien });
        }
        app.MapFallback(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new ApiFehler("Diese Funktion ist im lokalen Prototyp nicht vorhanden."));
            }
            else if (context.Request.Method != "GET") context.Response.StatusCode = 405;
            else if (File.Exists(index))
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync(index);
            }
            else
            {
                context.Response.StatusCode = 503;
                await context.Response.WriteAsJsonAsync(new ApiFehler("Die Oberfläche wurde noch nicht gebaut. Bitte zuerst den Frontend-Build ausführen."));
            }
        });
    }
}
