using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;

namespace Betriebsplattform.Web;

public static class ApiAnwendung
{
    public static WebApplication Erstellen(string[] args, int port = 5180, string? frontendVerzeichnis = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Keine Windows-Ereignisprotokoll-Schreibrechte für einen lokalen Prototyp verlangen.
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
        // Für diesen lokalen Prototyp keine konfigurierbaren zusätzlichen Kestrel-Endpunkte.
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Configure(new ConfigurationBuilder().Build());
            options.Listen(IPAddress.Loopback, port);
            options.Limits.MaxRequestBodySize = 2 * 1024 * 1024;
        });
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        var app = builder.Build();
        var uebersicht = DemoDaten.UebersichtErstellen();
        app.UseExceptionHandler(fehler => fehler.Run(async context =>
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new ApiFehler("Die Beispieldaten konnten nicht geladen werden. Bitte erneut versuchen."));
        }));
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";
            var origin = context.Request.Headers.Origin.ToString();
            var eigen = $"http://127.0.0.1:{context.Connection.LocalPort}";
            if (context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != context.Connection.LocalPort
                || (origin.Length > 0 && origin != eigen && origin != "http://127.0.0.1:5173"))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new ApiFehler("Dieser Prototyp ist nur über seine lokale Adresse erreichbar."));
                return;
            }
            await next(context);
        });
        app.MapGet("/api/uebersicht", () => uebersicht);
        app.MapGet("/api/vorgaenge/{vorgangId}/dokumente", (string vorgangId) =>
            DemoDokumente.Liste(vorgangId) is { } liste ? Results.Ok(liste)
                : Results.NotFound(new ApiFehler("Für diesen Beispielvorgang sind keine Dokumente verfügbar.")));
        app.MapGet("/api/vorgaenge/{vorgangId}/dokumente/{id}", (string vorgangId, string id, HttpContext context) =>
        {
            var meta = DemoDokumente.Metadaten(vorgangId, id);
            var bytes = DemoDokumente.Datei(vorgangId, id);
            if (meta is null || bytes is null)
                return Results.Content("<!doctype html><html lang=\"de\"><meta charset=\"utf-8\"><title>PDF nicht verfügbar</title><main><h1>PDF nicht verfügbar</h1><p>Dieses synthetische Dokument ist nicht vorhanden oder gehört nicht zu dieser Rechnung.</p><p>Schließen Sie diesen Tab. Die zuvor geöffnete Rechnung bleibt im anderen Tab erhalten.</p></main></html>", "text/html; charset=utf-8", statusCode: 404);
            context.Response.Headers.ContentDisposition = $"inline; filename=\"{meta.Dateiname}\"";
            return Results.Bytes(bytes, "application/pdf");
        });
        app.MapGet("/api/importvorschau/{beispiel}", (string beispiel) => beispiel is "fehler" or "gueltig" or "dublette"
            ? Results.Ok(DemoDaten.Importvorschau(beispiel))
            : Results.BadRequest(new ApiFehler("Bitte eines der vorhandenen künstlichen Beispiele auswählen.")));

        var frontend = frontendVerzeichnis ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../frontend/dist"));
        var index = Path.Combine(frontend, "index.html");
        if (Directory.Exists(frontend))
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
        return app;
    }
}
