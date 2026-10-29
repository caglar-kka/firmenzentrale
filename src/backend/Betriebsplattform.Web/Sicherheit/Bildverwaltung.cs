using System.Security.Claims;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Sicherheit;

// Deliberately closed preparation boundary. No decoder, file store or writable
// image metadata exists yet. Configuration cannot turn this into an upload.
public static class Bildverwaltung
{
    public const long MaxBytes = 2_000_000;
    public const string Grund = "Bildannahme gesperrt: Ein geprüfter Rasterbild-Decoder mit vollständiger Dekodierung, Größen-/Pixelgrenzen, Metadatenentfernung und Neuencodierung sowie die Malware-Freigabe und private Bildablage sind noch nicht eingerichtet.";
    public static bool Bildroute(PathString p) => p == "/api/bildverwaltung/profil" || p == "/api/bildverwaltung/firmenlogo";

    private static async Task<Benutzerkonto> Konto(HttpContext c, FirmenDbContext db)
    {
        var id = c.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await db.Benutzer.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id && u.Aktiv && u.Eingerichtet && (u.Rolle == "Administrator" || u.Rolle == "Mitarbeiter"), c.RequestAborted)
            ?? throw new Arbeitsfehler("Bitte erneut anmelden.", 401);
    }
    public static object Status(bool administrator) => new
    {
        uploadAktiv = false,
        status = "Gesperrt",
        grund = Grund,
        profilbildVerwalten = true,
        firmenlogoVerwalten = administrator,
        bildVorhanden = false,
        organisationsgrenze = "Aktueller Host und seine Datenbank",
        // Design limits, not a claim of an implemented safe decoder.
        vorgeseheneFormate = new[] { "PNG", "JPEG" },
        maxBytes = MaxBytes, maxDimension = 2048, maxPixel = 4_000_000
    };

    public static void Endpunkte(WebApplication app)
    {
        var g = app.MapGroup("/api/bildverwaltung").RequireAuthorization();
        g.AddEndpointFilter(async (c, next) =>
        {
            try { return await next(c); }
            catch (Arbeitsfehler e) { return Results.Json(new ApiFehler(e.Message), statusCode: e.Status); }
        });
        g.MapGet("/status", async (HttpContext c, FirmenDbContext db) =>
        {
            var u = await Konto(c, db);
            if (c.Request.Query.Count != 0) throw new Arbeitsfehler("Nur der eigene Profil- und Organisationskontext ist zulässig.");
            return Results.Json(Status(u.Rolle == "Administrator"));
        });
        foreach (var art in new[] { "profil", "firmenlogo" })
        {
            // Resource identity is the authenticated user / existing host, never
            // an arbitrary user, tenant, URL, storage key or client filename.
            async Task<IResult> Gesperrt(HttpContext c, FirmenDbContext db)
            {
                var u = await Konto(c, db);
                if (art == "firmenlogo" && u.Rolle != "Administrator") throw new Arbeitsfehler("Das Firmenlogo darf nur ein Administrator verwalten.", 403);
                if (c.Request.Query.Count != 0) throw new Arbeitsfehler("Fremde Benutzer-, Firmen- oder Dateipfade sind nicht zulässig.");
                // Do not read, decode, quarantine, log or save ANY body. Even a
                // syntactically valid PNG and an approving scanner cannot bypass
                // the missing full decoder/re-encoder/storage security chain.
                return Results.Json(new ApiFehler(Grund), statusCode: 503);
            }
            g.MapPost("/" + art, Gesperrt).RequireRateLimiting("bildverwaltung");
            g.MapPost("/" + art + "/entfernen", Gesperrt).RequireRateLimiting("bildverwaltung");
            g.MapGet("/" + art + "/datei", async (HttpContext c, FirmenDbContext db) =>
            {
                await Konto(c, db);
                if (c.Request.Query.Count != 0) throw new Arbeitsfehler("Nur der eigene Profil- und Organisationskontext ist zulässig.");
                // No bytes or filename are exposed, no store/path is resolved.
                return Results.Json(new ApiFehler("Kein freigegebenes Bild vorhanden."), statusCode: 404);
            });
        }
    }
}
