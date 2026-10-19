using System.Net;
using System.Text.Json.Serialization;
using Betriebsplattform.Web.Akten;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Betriebsplattform.Web.Sicherheit;
using System.Security.Cryptography.X509Certificates;

namespace Betriebsplattform.Web.Persistenz;

public static class PostgreSqlHost
{
    public static WebApplication Erstellen(WebApplicationBuilder builder,Persistenzkonfiguration config,int port,string? frontend,Arbeitsmodule? module,ILesezugriff? rechte,IArbeitsrechte? schreibrechte,X509Certificate2? testZertifikat=null)
    {
        var aktiv=module??new Arbeitsmodule();var lesen=rechte??new LokalerDemozugriff();var schreiben=schreibrechte??new LokaleArbeitsrechte();
        builder.Logging.ClearProviders();builder.Logging.AddSimpleConsole();builder.Logging.AddFilter("Microsoft.EntityFrameworkCore",LogLevel.None);builder.Logging.AddFilter("Npgsql",LogLevel.None);
        builder.Logging.AddFilter("Microsoft.AspNetCore",LogLevel.Warning);builder.Logging.AddFilter("Microsoft.AspNetCore.DataProtection",LogLevel.Warning);
        var pilot=Pilotoptionen.Lesen(builder.Configuration);Anmeldung.Dienste(builder,pilot,config.Dateiablage);
        if(builder.Configuration["FirmenZentrale:Network:Address"] is string address&&address!="127.0.0.1")throw new InvalidOperationException("LAN binding requires a separately approved deployment gate. Loopback only.");
        port=builder.Configuration.GetValue<int?>("FirmenZentrale:Network:Port")??port;if(port is <0 or >65535)throw new InvalidOperationException("Invalid local application port.");
        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton<IMalwarepruefung,NichtKonfigurierterScanner>();
        builder.WebHost.ConfigureKestrel(o=>{o.Configure(new ConfigurationBuilder().Build());o.Listen(IPAddress.Loopback,port,l=>{if(pilot.Aktiv){var cert=testZertifikat??(pilot.Zertifikat is not null?X509CertificateLoader.LoadPkcs12FromFile(pilot.Zertifikat,pilot.Zertifikatpasswort):throw new InvalidOperationException("Pilot requires an explicitly provisioned HTTPS certificate."));l.UseHttps(cert);}});o.Limits.MaxRequestBodySize=Dokumentpruefung.MaxBytes;});
        builder.Services.ConfigureHttpJsonOptions(o=>{o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());o.SerializerOptions.MaxDepth=16;o.SerializerOptions.UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow;});
        builder.Services.AddDbContextFactory<FirmenDbContext>(o=>o.UseNpgsql(config.Verbindung!,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false));
        builder.Services.AddScoped<FirmenDaten>();builder.Services.AddScoped<FinanzDaten>();
        builder.Services.AddSingleton(new LokaleDokumentdateien(config.Dateiablage,builder.Environment.ContentRootPath,builder.Environment.WebRootPath??Path.Combine(builder.Environment.ContentRootPath,"wwwroot"),frontend??Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../frontend/dist"))));
        builder.Services.AddSingleton<IDokumentdateien>(s=>s.GetRequiredService<LokaleDokumentdateien>());
        builder.Services.AddHostedService<DatenbankStartpruefung>();builder.Services.AddHostedService<Betriebsplattform.Web.Betrieb.Betriebssperre>();
        builder.Services.AddSingleton<IHostedService>(s=>s.GetRequiredService<LokaleDokumentdateien>());builder.Services.AddHostedService<Betriebsplattform.Web.Betrieb.PilotStartpruefung>();
        var app=builder.Build();Webschutz.Konfigurieren(app,aktiv);Anmeldung.Pipeline(app,pilot);Anmeldung.Endpunkte(app,pilot);
        var api=app.MapGroup("/api");
        if(pilot.Aktiv)api.RequireAuthorization();
        bool DarfVerwalten(string aktion)=>schreiben.Darf(aktion)&&(!pilot.Aktiv||aktion is not ("Kategorien.Verwalten" or "Diagnose.Lesen")||app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext!.User.IsInRole("Administrator"));
        void Darf(string aktion){if(!DarfVerwalten(aktion))throw new Arbeitsfehler("Für diese Aktion fehlt die Berechtigung.",403);}
        void Zugriff(string art,string? id){if(id is null)return;FirmenDaten.Id(id);if(!lesen.Erlaubt(art,id))throw new Arbeitsfehler("Für diesen Eintrag fehlt die Berechtigung.",403);}
        async Task AkteZugriff(FirmenDaten d,string? id,CancellationToken ct){
            if(id is null)return;Zugriff("Vorgang",id);var a=await d.Db.Akten.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id,ct)??throw new Arbeitsfehler("Akte nicht gefunden.",404);Zugriff("Firma",a.FirmaId);
            if(!aktiv.Aktiv("Finanzen")&&await d.Db.Rechnungen.AnyAsync(r=>r.AkteId==id,ct))throw new Arbeitsfehler("Akte nicht verfügbar.",404);
        }
        api.AddEndpointFilter(async(context,next)=>{
            try{
                var rule=context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<Ressource>();
                if(rule is not null)Zugriff(rule.Art,context.HttpContext.Request.RouteValues[rule.Parameter]?.ToString());
                if(rule is not null&&context.HttpContext.Request.RouteValues[rule.Parameter]?.ToString() is string key){
                    var d=context.HttpContext.RequestServices.GetRequiredService<FirmenDaten>();
                    if(rule.Art=="Vorgang")await AkteZugriff(d,key,context.HttpContext.RequestAborted);
                    if(rule.Art=="Dokument"){
                        Zugriff("Firma",await d.Db.Dokumente.Where(x=>x.Id==key).Select(x=>x.AbsenderId).SingleOrDefaultAsync(context.HttpContext.RequestAborted));
                        var refs=await (from z in d.Db.Dokumentzuordnungen join a in d.Db.Akten on z.AkteId equals a.Id where z.DokumentId==key select new{a.Id,a.FirmaId}).Take(2).ToArrayAsync(context.HttpContext.RequestAborted);
                        foreach(var r in refs)await AkteZugriff(d,r.Id,context.HttpContext.RequestAborted);
                    }
                }
                if(context.HttpContext.Request.Method!="GET"&&rule?.Schreibrecht is not null)Darf(rule.Schreibrecht);
                return await next(context);
            }catch(Arbeitsfehler e){return Results.Json(new{meldung=e.Message,hinweise=e.Hinweise.Where(h=>lesen.Erlaubt(h.Art=="Akte"?"Vorgang":"Firma",h.Id)).ToArray()},statusCode:e.Status);}
            catch(Exception e) when(e is DbUpdateException or NpgsqlException or TimeoutException or IOException or UnauthorizedAccessException){var f=Persistenzfehler.Beschreiben(e);return Results.Json(new ApiFehler(f.Meldung),statusCode:f.Status);}
        });
        api.MapGet("/persistenz",()=>new{provider="PostgreSQL",fluechtig=false,authentifizierung=pilot.Aktiv?"Individuelle Cookie-Anmeldung":"Keine produktive Authentifizierung",seeding="Nicht aktiviert"});
        api.MapGet("/arbeitsumgebung",(HttpContext c)=>new{beispielkennzeichnung=!pilot.Aktiv||app.Environment.IsDevelopment(),diagnose=false,benutzer=pilot.Aktiv?c.User.Identity!.Name:Arbeitskontext.Benutzer,authentifiziert=pilot.Aktiv,administrator=pilot.Aktiv&&c.User.IsInRole("Administrator"),fluechtigeDaten=false,kategorienVerwalten=DarfVerwalten("Kategorien.Verwalten"),kontakteVerwalten=DarfVerwalten("Kontakte.Verwalten"),aktenVerwalten=DarfVerwalten("Akten.Verwalten")});
        api.MapGet("/module",()=>new{module=aktiv.Liste,umgebung="Lokale Entwicklungsgrundlage",persistenz="PostgreSQL"});
        api.MapGet("/akten-status",()=>Vorgangsbearbeitung.Werte);
        api.MapGet("/kategorien",async(FirmenDaten d,CancellationToken ct)=>(await d.Db.Kategorien.AsNoTracking().OrderBy(k=>k.ElternId).ThenBy(k=>k.Reihenfolge).ThenBy(k=>k.Name).Take(100).ToArrayAsync(ct)).Select(FirmenDaten.KategorieAnsicht));
        api.MapPost("/kategorien",async(Kategorieeingabe e,FirmenDaten d,CancellationToken ct)=>{Darf("Kategorien.Verwalten");return await d.KategorieSpeichern(null,e,ct);});
        api.MapPost("/kategorien/{id}",(string id,Kategorieeingabe e,FirmenDaten d,CancellationToken ct)=>d.KategorieSpeichern(id,e,ct)).WithMetadata(new Ressource("Kategorie","id","Kategorien.Verwalten"));
        api.MapPost("/kategorien/{id}/archivieren",(string id,Versionsentscheidung e,FirmenDaten d,CancellationToken ct)=>d.KategorieArchivieren(id,e.Version,ct)).WithMetadata(new Ressource("Kategorie","id","Kategorien.Verwalten"));
        api.MapPost("/kategorien/{id}/verschieben",(string id,Kategorieverschiebung e,FirmenDaten d,CancellationToken ct)=>d.KategorieVerschieben(id,e,ct)).WithMetadata(new Ressource("Kategorie","id","Kategorien.Verwalten"));
        api.MapGet("/kontakte",async(HttpContext c,FirmenDaten d,CancellationToken ct)=>{
            if(lesen is not LokalerDemozugriff)throw new Arbeitsfehler("Für diese Liste fehlt ein Datenbank-Rechtefilter.",403);
            var result=await d.KontakteSuchen(Listenfilter.Lesen(c),Page(c),ct);
            return c.Request.Query.ContainsKey("seite")?Results.Json(result):Results.Json(result.Eintraege);
        });
        api.MapPost("/kontakte",async(Kontakteingabe e,FirmenDaten d,CancellationToken ct)=>{Darf("Kontakte.Verwalten");Zugriff("Firma",e.FirmaId);return await d.KontaktSpeichern(null,e,ct);});
        api.MapGet("/kontakte/{id}",async(string id,FirmenDaten d,CancellationToken ct)=>(await d.KontaktAnsichten([await d.Db.Kontakte.SingleOrDefaultAsync(k=>k.Id==FirmenDaten.Id(id),ct)??throw new Arbeitsfehler("Kontakt nicht gefunden.",404)],ct)).Single()).WithMetadata(new Ressource("Firma","id"));
        api.MapPost("/kontakte/{id}",async(string id,Kontakteingabe e,FirmenDaten d,CancellationToken ct)=>{Zugriff("Firma",e.FirmaId);return await d.KontaktSpeichern(id,e,ct);}).WithMetadata(new Ressource("Firma","id","Kontakte.Verwalten"));
        api.MapPost("/kontakte/{id}/archivieren",(string id,Versionsentscheidung e,FirmenDaten d,CancellationToken ct)=>d.KontaktArchivieren(id,e.Version,ct)).WithMetadata(new Ressource("Firma","id","Kontakte.Verwalten"));
        api.MapGet("/akten",(HttpContext c,FirmenDaten d,CancellationToken ct)=>d.AktenSuchen(Text(c),c.Request.Query["status"],c.Request.Query["kategorie"],c.Request.Query["sort"],c.Request.Query["ab"]=="1",Page(c),c.Request.Query.ContainsKey("seite"),lesen,ct,aktiv));
        api.MapPost("/akten",async(Vorgangseingabe e,FirmenDaten d,CancellationToken ct)=>{Darf("Akten.Verwalten");Zugriff("Firma",e.FirmaId);return await d.AkteAnlegen(e,ct);});
        api.MapGet("/akten/{akte}",async(string akte,FirmenDaten d,CancellationToken ct)=>{
            var a=await d.AkteLesen(akte,ct);var docs=a.Dokumente.Where(x=>lesen.Erlaubt("Dokument",x.Id)&&(x.AbsenderId is null||lesen.Erlaubt("Firma",x.AbsenderId))).ToArray();
            return a with{Vorgang=a.Vorgang with{Dokumentanzahl=docs.Length},Dokumente=docs,Kontaktvorschlaege=a.Kontaktvorschlaege.Where(v=>lesen.Erlaubt("Firma",v.FirmaId)&&lesen.Erlaubt("Dokument",v.DokumentId)).ToArray(),Korrespondenz=a.Korrespondenz.Where(k=>k.KontaktId is null||lesen.Erlaubt("Firma",k.KontaktId)).Select(k=>k with{DokumentIds=k.DokumentIds?.Where(id=>lesen.Erlaubt("Dokument",id)).ToArray()}).ToArray()};
        }).WithMetadata(new Ressource("Vorgang","akte"));
        api.MapPost("/akten/{akte}/bearbeitungsstatus",(string akte,Bearbeitungsentscheidung e,FirmenDaten d,CancellationToken ct)=>d.AkteStatus(akte,e,ct)).WithMetadata(new Ressource("Vorgang","akte","Akten.Verwalten"));
        api.MapGet("/posteingang",async(HttpContext c,FirmenDaten d,CancellationToken ct)=>{
            if(lesen is not LokalerDemozugriff)throw new Arbeitsfehler("Für diese Liste fehlt ein Datenbank-Rechtefilter.",403);
            var result=await d.EingangSuchen(Listenfilter.Lesen(c),Page(c),ct,aktiv);
            return new{dokumente=result.Eintraege,anzahl=result.Anzahl,scanner=new{aktiv=false,status="Scan-Ordner ist im PostgreSQL-Modus noch nicht angebunden. Direkte Aktenablage und Upload sind möglich.",uebernommen=0,fehler=0,wartend=0}};
        });
        api.MapGet("/posteingang/{id}",async(string id,FirmenDaten d,CancellationToken ct)=>(await d.DokumentAnsichten([await d.DokumentMetadaten().SingleOrDefaultAsync(x=>x.Id==FirmenDaten.Id(id),ct)??throw new Arbeitsfehler("Dokument nicht gefunden.",404)],ct)).Single()).WithMetadata(new Ressource("Dokument","id"));
        api.MapPost("/posteingang/{id}/zuordnen",async(string id,Zuordnung e,FirmenDaten d,CancellationToken ct)=>{await AkteZugriff(d,e.VorgangId,ct);Zugriff("Firma",e.FirmaId);return (await d.DokumentAnsichten([await d.Erstzuordnen(id,e,ct)],ct)).Single();}).WithMetadata(new Ressource("Dokument","id","Akten.Verwalten"));
        api.MapPost("/dokumente/{id}/umordnen",async(string id,Umzuordnung e,FirmenDaten d,CancellationToken ct)=>{await AkteZugriff(d,e.BisherigerVorgangId,ct);await AkteZugriff(d,e.NeuerVorgangId,ct);Zugriff("Firma",e.FirmaId);return (await d.DokumentAnsichten([await d.Umordnen(id,e,ct)],ct)).Single();}).WithMetadata(new Ressource("Dokument","id","Akten.Verwalten"));
        api.MapPost("/kontaktvorschlaege/{id}",async(string id,Kontaktfreigabe e,FirmenDaten d,CancellationToken ct)=>{
            Darf("Kontakte.Verwalten");var v=await d.Db.Kontaktvorschlaege.AsNoTracking().SingleOrDefaultAsync(v=>v.Id==FirmenDaten.Id(id),ct)??throw new Arbeitsfehler("Vorschlag nicht gefunden.",404);Zugriff("Firma",v.KontaktId);Zugriff("Dokument",v.DokumentId);return await d.KontaktvorschlagEntscheiden(id,e,ct);
        });
        api.MapPost("/akten/{akte}/notizen",async(string akte,Notizeingabe e,FirmenDaten d,CancellationToken ct)=>await d.NotizAnsicht(await d.NotizSpeichern(akte,null,e,ct:ct),ct)).WithMetadata(new Ressource("Vorgang","akte","Akten.Verwalten"));
        api.MapPost("/akten/{akte}/notizen/{id}",async(string akte,string id,Notizeingabe e,FirmenDaten d,CancellationToken ct)=>await d.NotizAnsicht(await d.NotizSpeichern(akte,id,e,ct:ct),ct)).WithMetadata(new Ressource("Vorgang","akte","Akten.Verwalten"));
        foreach(var pfad in new[]{"entfernen","wiederherstellen"}){
            var archiv=pfad=="entfernen";
            api.MapPost("/akten/{akte}/notizen/{id}/"+pfad,async(string akte,string id,Versionsentscheidung e,FirmenDaten d,CancellationToken ct)=>await d.NotizAnsicht(await d.NotizSpeichern(akte,id,new(null,null,e.Version),archiv,ct),ct)).WithMetadata(new Ressource("Vorgang","akte","Akten.Verwalten"));
        }
        api.MapPost("/akten/{akte}/korrespondenz",async(string akte,Korrespondenzeingabe e,FirmenDaten d,CancellationToken ct)=>{if(!aktiv.Aktiv("Kommunikation"))throw new Arbeitsfehler("Kommunikation ist nicht aktiviert.",404);Zugriff("Firma",e.KontaktId);foreach(var id in e.DokumentIds??[])Zugriff("Dokument",id);return await d.KorrespondenzSpeichern(akte,e,ct);}).WithMetadata(new Ressource("Vorgang","akte","Akten.Verwalten"));
        api.MapPost("/dokumenteingang",async(HttpContext c,FirmenDaten d,IDokumentdateien dateien,CancellationToken ct)=>{
            Darf("Akten.Verwalten");if(c.Request.ContentType!="application/octet-stream")throw new Arbeitsfehler("Bitte die Dateiauswahl verwenden.",415);
            var akte=c.Request.Query["akte"].ToString();await AkteZugriff(d,akte.Length==0?null:akte,ct);
            using var bytes=new MemoryStream();var puffer=new byte[81920];int n;while((n=await c.Request.Body.ReadAsync(puffer,ct))>0){if(bytes.Length+n>Dokumentpruefung.MaxBytes)throw new Arbeitsfehler("Die Datei überschreitet 20 MB.",413);bytes.Write(puffer,0,n);}
            string name;try{name=Uri.UnescapeDataString(c.Request.Headers["X-Dateiname"].ToString());}catch(UriFormatException){throw new Arbeitsfehler("Ungültiger Dateiname.");}
            return (await d.DokumentAnsichten([await d.DokumentHinzufuegen(bytes.ToArray(),name,c.Request.Headers["X-Dateityp"].ToString(),akte.Length==0?null:akte,dateien,ct)],ct)).Single();
        });
        api.MapGet("/dokumente/{id}/text",async(string id,FirmenDaten d,CancellationToken ct)=>new{text=await d.Db.Dokumente.Where(x=>x.Id==FirmenDaten.Id(id)).Select(x=>x.Text).SingleOrDefaultAsync(ct)??throw new Arbeitsfehler("Dokument nicht gefunden.",404)}).WithMetadata(new Ressource("Dokument","id"));
        async Task<IResult> Originaldatei(string id,HttpContext c,FirmenDaten d,IDokumentdateien dateien,CancellationToken ct){
            var meta=await d.Db.Dokumente.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==FirmenDaten.Id(id),ct)??throw new Arbeitsfehler("Dokument nicht gefunden.",404);
            if(c.Request.RouteValues["vorgangId"]?.ToString() is string akte){await AkteZugriff(d,akte,ct);if(!await d.Db.Dokumentzuordnungen.AnyAsync(z=>z.DokumentId==id&&z.AkteId==akte,ct))throw new Arbeitsfehler("Dokument gehört nicht zu dieser Akte.",404);}
            var stream=await dateien.Oeffnen(meta.StorageKey,meta.Groesse,meta.Hash,ct);
            var inline=meta.Vorschau is "pdf" or "bild" or "text";c.Response.Headers.ContentSecurityPolicy="default-src 'none'; sandbox; frame-ancestors 'self'; object-src 'none'";c.Response.Headers.ContentDisposition=$"{(inline?"inline":"attachment")}; filename*=UTF-8''{Uri.EscapeDataString(meta.Dateiname)}";
            return Results.Stream(stream,inline?meta.Inhaltstyp:"application/octet-stream");
        }
        api.MapGet("/dokumente/{id}/datei",Originaldatei).WithMetadata(new Ressource("Dokument","id"));
        api.MapGet("/vorgaenge/{vorgangId}/dokumente/{id}",Originaldatei).WithMetadata(new Ressource("Dokument","id"));
        api.MapGet("/vorgaenge/{vorgangId}/dokumente",async(string vorgangId,FirmenDaten d,CancellationToken ct)=>{
            await AkteZugriff(d,vorgangId,ct);var firma=await d.Db.Akten.Where(a=>a.Id==vorgangId).Select(a=>a.FirmaId).SingleAsync(ct);
            var ids=d.Db.Dokumentzuordnungen.Where(z=>z.AkteId==vorgangId).Select(z=>z.DokumentId);var daten=await d.DokumentMetadaten().Where(x=>ids.Contains(x.Id)).OrderBy(x=>x.Id).Take(200).ToArrayAsync(ct);
            return new Dokumentenliste(true,vorgangId,daten.Where(x=>lesen.Erlaubt("Dokument",x.Id)).Select(x=>new Dokumentansicht(x.Id,firma??"nicht-zugeordnet",vorgangId,x.Art,x.Dateiname,x.ErstelltAm.UtcDateTime.ToString("yyyy-MM-dd"),x.Version,(int)x.Groesse,x.Hash,x.Quelle,null,x.Inhaltstyp,x.Vorschau,x.Titel)).ToArray());
        }).WithMetadata(new Ressource("Vorgang","vorgangId"));
        api.MapGet("/uebersicht",(HttpContext c,FirmenDaten d,CancellationToken ct)=>d.Rechnungsuebersicht(Page(c),lesen,ct,Listenfilter.Lesen(c)));
        api.MapGet("/suche",(string? text,FirmenDaten d,CancellationToken ct)=>d.Suchen(text??"",lesen,aktiv,ct));
        api.MapGet("/kontakte/{id}/arbeitszahlen",(string id,FirmenDaten d,CancellationToken ct)=>d.Arbeitszahlen(id,lesen,aktiv,ct)).WithMetadata(new Ressource("Firma","id"));
        api.MapGet("/kontakte/{id}/arbeit",(string id,HttpContext c,FirmenDaten d,CancellationToken ct)=>d.Firmenarbeit(id,c.Request.Query["bereich"],Page(c),lesen,aktiv,ct)).WithMetadata(new Ressource("Firma","id"));
        // The old importer is explicitly synthetic and depends on demo identities: never silently reuse it here.
        api.MapGet("/importe",()=>Array.Empty<DemoImportansicht>());
        api.MapPost("/importe/{**pfad}",()=>Results.Json(new ApiFehler("Die strukturierte Übernahme ist noch auf den getrennten Demo-Modus begrenzt."),statusCode:409));
        Webschutz.Oberflaeche(app,frontend);return app;
    }
    private sealed record Ressource(string Art,string Parameter,string? Schreibrecht=null);
    private static int Page(HttpContext c)=>c.Request.Query.TryGetValue("seite",out var s)?int.TryParse(s,out var n)?FirmenDaten.Seite(n):throw new Arbeitsfehler("Ungültige Ergebnisseite."):0;
    private static string Text(HttpContext c){var text=c.Request.Query["text"].ToString();if(text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");return text;}
}
