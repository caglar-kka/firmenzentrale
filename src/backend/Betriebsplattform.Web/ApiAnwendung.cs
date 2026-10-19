using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Betriebsplattform.Web.Akten;

namespace Betriebsplattform.Web;

public static class ApiAnwendung
{
    public static WebApplication Erstellen(string[] args, int port = 5180, string? frontendVerzeichnis = null,
        Arbeitsmodule? module = null, ILesezugriff? leserechte = null, Arbeitsumgebung? umgebung = null, IArbeitsrechte? arbeitsrechte = null)
    {
        var builder = WebApplication.CreateBuilder(args.Where(a => a is not ("--scan-demo" or "--diagnose")).ToArray());
        var persistenz=Persistenz.Persistenzkonfiguration.Lesen(builder.Configuration);
        if(persistenz.Provider=="PostgreSql")return Persistenz.PostgreSqlHost.Erstellen(builder,persistenz,port,frontendVerzeichnis,module,leserechte,arbeitsrechte);
        var akten = new Aktenbestand(beispielszenario:true);
        var arbeitsumgebung=umgebung??new Arbeitsumgebung(Diagnose:args.Contains("--diagnose"));
        var aktionsrechte=arbeitsrechte??new LokaleArbeitsrechte();
        var aktiveModule = module ?? new Arbeitsmodule();
        var rechte = leserechte ?? new LokalerDemozugriff();
        ScanOrdnerAdapter? scanner = null;
        if (args.Contains("--scan-demo"))
        {
            // Nur explizit aktivierter, eigener synthetischer Demoordner, kein frei wählbarer Systempfad.
            var ordner = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../private/scan-eingang"));
            Directory.CreateDirectory(ordner);
            scanner = new ScanOrdnerAdapter(ordner, akten);
            builder.Services.AddSingleton<IHostedService>(new ScanOrdnerDienst(scanner));
        }
        // Keine Windows-Ereignisprotokoll-Schreibrechte für einen lokalen Prototyp verlangen.
        builder.Logging.ClearProviders();
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics",LogLevel.Warning);
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
        // Für diesen lokalen Prototyp keine konfigurierbaren zusätzlichen Kestrel-Endpunkte.
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Configure(new ConfigurationBuilder().Build());
            options.Listen(IPAddress.Loopback, port);
            options.Limits.MaxRequestBodySize = Dokumentpruefung.MaxBytes;
        });
        builder.Services.ConfigureHttpJsonOptions(o => {o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());o.SerializerOptions.MaxDepth=16;o.SerializerOptions.UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow;});
        var app = builder.Build();
        var bestand = new DemoImportbestand();
        Webschutz.Konfigurieren(app,aktiveModule);
        Uebersicht Uebersicht() => akten.KontakteAnwenden(bestand.Uebersicht());
        // Gesamtsalden dürfen keine nicht lesbaren Rechnungen verraten. Teilbestände benötigen
        // später eine eigene berechtigte Kernabfrage statt formatierte Geldtexte neu zu berechnen.
        app.MapGet("/api/uebersicht", () =>
        {
            var u = Uebersicht(); return u.Rechnungen.All(r => rechte.Erlaubt("Rechnung", r.VorgangId ?? r.Nummer)) ? Results.Ok(u) : Results.StatusCode(403);
        });
        app.MapGet("/api/vorgaenge/{vorgangId}/dokumente", (string vorgangId) =>
        {
            if (!rechte.Erlaubt("Vorgang", vorgangId)) return Results.StatusCode(403);
            foreach (var r in Uebersicht().Rechnungen) akten.Registrieren(r);
            try { var liste = akten.Dokumentenliste(vorgangId); return Results.Ok(liste with { Dokumente = liste.Dokumente.Where(d => rechte.Erlaubt("Dokument", d.Id)).ToArray() }); }
            catch (Arbeitsfehler) { return Results.NotFound(new ApiFehler("Für diesen Vorgang sind keine Dokumente verfügbar.")); }
        });
        app.MapGet("/api/vorgaenge/{vorgangId}/dokumente/{id}", (string vorgangId, string id, HttpContext context) =>
        {
            if (!rechte.Erlaubt("Dokument", id) || !rechte.Erlaubt("Vorgang", vorgangId)) return Results.StatusCode(403);
            Aktendokument? meta = null; byte[]? bytes = null;
            try { meta = akten.Dokument(id); bytes = akten.Bytes(id, vorgangId); } catch (Arbeitsfehler) { }
            if (meta is null || bytes is null)
                return Results.Content("<!doctype html><html lang=\"de\"><meta charset=\"utf-8\"><title>PDF nicht verfügbar</title><main><h1>PDF nicht verfügbar</h1><p>Dieses synthetische Dokument ist nicht vorhanden oder gehört nicht zu dieser Rechnung.</p><p>Schließen Sie diesen Tab. Die zuvor geöffnete Rechnung bleibt im anderen Tab erhalten.</p></main></html>", "text/html; charset=utf-8", statusCode: 404);
            var inline = meta.Vorschau is "pdf" or "bild" or "text";
            context.Response.Headers.ContentDisposition = $"{(inline ? "inline" : "attachment")}; filename*=UTF-8''{Uri.EscapeDataString(meta.Dateiname)}";
            // Nur feste, selbst erzeugte PDF-Testbytes dürfen im eigenen Arbeitsraum erscheinen.
            // Die Anwendung und alle übrigen Antworten bleiben gegen fremdes Framing gesperrt.
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; object-src 'none'; base-uri 'none'; frame-ancestors 'self'; form-action 'none'";
            return Results.Bytes(bytes, inline ? meta.Inhaltstyp : "application/octet-stream");
        });
        app.MapGet("/api/importvorschau/{beispiel}", (string beispiel) => beispiel is "fehler" or "gueltig" or "dublette"
            ? Results.Ok(DemoDaten.Importvorschau(beispiel))
            : Results.BadRequest(new ApiFehler("Bitte eines der vorhandenen künstlichen Beispiele auswählen.")));

        // Rohdatei statt Multipart: kein temporärer Dateipfad, keine Antiforgery-Deaktivierung.
        // Browser-Schreibzugriffe benötigen expliziten Origin und einen nicht als HTML-Form sendbaren Header.
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/importe") && context.Request.Method != "GET"
                && (context.Request.Headers["X-Demo-Import"] != "1" || string.IsNullOrEmpty(context.Request.Headers.Origin)))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new ApiFehler("Importaktionen benötigen den bestätigten lokalen Demoablauf."));
                return;
            }
            await next(context);
        });
        DemoImportansicht MitFirmenhinweisen(DemoImportansicht v)=>v with{Firmenhinweise=v.Firmen.ToDictionary(n=>n,n=>(IReadOnlyList<Dublettenhinweis>)akten.Kontakte().Where(k=>k.Art=="Firma"&&!k.Archiviert&&rechte.Erlaubt("Firma",k.Id)&&Datenvergleich.NameAehnlich(k.Firma,n)).Take(5).Select(k=>new Dublettenhinweis(k.Id,k.Firma,"Firma",k.Anschrift,k.Werte.Where(w=>w.Aktiv).Select(w=>w.Art+": "+w.Wert).ToArray(),["Ähnlicher Firmenname – Zuordnung bitte selbst bestätigen"])).ToArray())};
        IResult Importaktion(Func<object> aktion)
        {
            try { var ergebnis=aktion();return Results.Ok(ergebnis is DemoImportansicht v?MitFirmenhinweisen(v):ergebnis); }
            catch (KeyNotFoundException) { return Results.NotFound(new ApiFehler("Dieser Demoimport ist nicht mehr verfügbar. Bitte die Datei erneut auswählen.")); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ApiFehler(ex.Message)); }
            catch (ArgumentException) { return Results.BadRequest(new ApiFehler("Die Firmenzuordnung ist ungültig. Bitte prüfen.")); }
        }
        app.MapGet("/api/importe", () => bestand.Liste().Select(MitFirmenhinweisen).ToArray());
        app.MapGet("/api/importe/{id}", (string id) => Importaktion(() => bestand.Lesen(id)));
        app.MapGet("/api/importvorlage/{beispiel}", (string beispiel) => beispiel is "gueltig" or "fehler" or "dublette"
            ? Results.File(DemoArbeitsmappe.Erstellen(beispiel), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Rechnungsimport-" + beispiel + ".xlsx")
            : Results.NotFound());
        app.MapPost("/api/importe/vorschau", async (HttpContext context) =>
        {
            if (context.Request.ContentType is not "application/octet-stream")
                return Results.BadRequest(new ApiFehler("Bitte eine synthetische XLSX-Datei über die Dateiauswahl übertragen."));
            try
            {
                using var bytes = new MemoryStream();
                var puffer = new byte[8192];
                int gelesen;
                while ((gelesen = await context.Request.Body.ReadAsync(puffer, context.RequestAborted)) > 0)
                {
                    if (bytes.Length + gelesen > DemoImportbestand.MaxDateiBytes)
                        return Results.Json(new ApiFehler("Die Datei überschreitet 2 MiB."), statusCode: 413);
                    bytes.Write(puffer, 0, gelesen);
                }
                var name = Uri.UnescapeDataString(context.Request.Headers["X-Dateiname"].ToString());
                return Importaktion(() => bestand.Hochladen(bytes.ToArray(), name));
            }
            catch (BadHttpRequestException) { return Results.Json(new ApiFehler("Datei oder Anfrage überschreitet die erlaubte Größe."), statusCode: 413); }
            catch (UriFormatException) { return Results.BadRequest(new ApiFehler("Der Dateiname ist ungültig.")); }
        });
        app.MapPost("/api/importe/{id}/mapping", (string id, Mappingentscheidung entscheidung) =>
            Importaktion(() => bestand.Zuordnen(id, entscheidung)));
        app.MapPost("/api/importe/{id}/uebernehmen", (string id, Uebernahmeentscheidung entscheidung) => Importaktion(() =>
        {
            bestand.Uebernehmen(id, entscheidung.Zeilen, entscheidung.Bestaetigt);
            return bestand.Lesen(id);
        }));
        AktenEndpoints.Registrieren(app, akten, Uebersicht, aktiveModule, rechte, scanner, arbeitsumgebung, aktionsrechte);

        Webschutz.Oberflaeche(app,frontendVerzeichnis);
        return app;
    }
}
