using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Aufgaben;

public sealed class Termin : VersionierterDatensatz
{
    public string EigentuemerId { get; set; } = "";
    public string Titel { get; set; } = "";
    public string Beschreibung { get; set; } = "";
    public string Ort { get; set; } = "";
    public string Strasse {get;set;}="";public string Hausnummer {get;set;}="";public string Plz {get;set;}="";public string Stadt {get;set;}="";public string Land {get;set;}="";
    public int? ErinnerungMinuten {get;set;}public DateTimeOffset ErinnerungFaelligUtc {get;set;}
    public bool ErinnerungQuittiert {get;set;}public string Erinnerungskennung {get;set;}="";
    public string Beginn { get; set; } = "";
    public string Ende { get; set; } = "";
    public bool Ganztaegig { get; set; }
    public DateTimeOffset BeginnUtc { get; set; }
    public DateTimeOffset EndeUtc { get; set; }
    public bool Entfernt { get; set; }
    public string AnlageHash { get; set; } = "";
}
public sealed class Terminverlauf
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TerminId { get; set; } = "";
    public string BenutzerId { get; set; } = "";
    public DateTimeOffset Zeitpunkt { get; set; }
    public string Vorher { get; set; } = "";
    public string Nachher { get; set; } = "";
}
public sealed record Termineingabe(string Titel, string Beginn, string Ende, bool Ganztaegig, string Beschreibung = "", string Ort = "", int Version = 0, string? AnlageId = null,string Strasse="",string Hausnummer="",string Plz="",string Stadt="",string Land="",int? ErinnerungMinuten=null);
public sealed record Terminversion(int Version);

// One existing host/database is one organisation. A personal appointment has exactly
// one owner; even an administrator receives no implicit access to another calendar.
public sealed class Termindienst(FirmenDbContext db, TimeProvider clock)
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
    private async Task Konto(string actor)
    {
        if (!await db.Benutzer.AnyAsync(u => u.Id == actor && u.Aktiv && u.Eingerichtet && (u.Rolle == "Administrator" || u.Rolle == "Mitarbeiter")))
            throw new Arbeitsfehler("Bitte erneut anmelden.", 401);
    }
    public static (DateTimeOffset Beginn, DateTimeOffset Ende) Pruefen(Termineingabe e)
    {
        if (string.IsNullOrWhiteSpace(e.Titel) || e.Titel.Trim().Length > 160 || e.Beschreibung is null || e.Beschreibung.Length > 4000 || e.Ort is null || e.Ort.Length > 200)
            throw new Arbeitsfehler("Titel (höchstens 160 Zeichen), Beschreibung und Ort prüfen.");
        if(e.Strasse is null||e.Strasse.Length>160||e.Hausnummer is null||e.Hausnummer.Length>30||e.Plz is null||e.Plz.Length>20||e.Stadt is null||e.Stadt.Length>120||e.Land is null||e.Land.Length>80||e.ErinnerungMinuten is not(null or 0 or 5 or 15 or 30 or 1440))throw new Arbeitsfehler("Adresse oder Erinnerung prüfen.");
        DateTime Zeit(string s)
        {
            if (!DateTime.TryParseExact(s, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) || d.Year is < 1900 or > 2200)
                throw new Arbeitsfehler("Datum und Uhrzeit im Kalenderformat prüfen.");
            if (e.Ganztaegig && d.TimeOfDay != TimeSpan.Zero) throw new Arbeitsfehler("Ganztägige Termine beginnen und enden um 00:00; das Enddatum ist exklusiv.");
            if (Berlin.IsInvalidTime(d)) throw new Arbeitsfehler("Diese Uhrzeit existiert durch die Zeitumstellung nicht. Bitte eine andere Uhrzeit wählen.");
            if (Berlin.IsAmbiguousTime(d)) throw new Arbeitsfehler("Diese Uhrzeit kommt durch die Zeitumstellung zweimal vor. Bitte eine eindeutige Uhrzeit außerhalb der Doppelstunde wählen.");
            return DateTime.SpecifyKind(d, DateTimeKind.Unspecified);
        }
        var start = Zeit(e.Beginn); var ende = Zeit(e.Ende);
        if (ende <= start || ende - start > TimeSpan.FromDays(366)) throw new Arbeitsfehler("Das Ende muss nach dem Beginn liegen; höchstens 366 Tage.");
        return (new(TimeZoneInfo.ConvertTimeToUtc(start, Berlin)), new(TimeZoneInfo.ConvertTimeToUtc(ende, Berlin)));
    }
    private static string Hash(Termineingabe e) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(e with { Version = 0, AnlageId = null }))));
    private static string Stand(Termin t) => JsonSerializer.Serialize(new { t.Titel, t.Beginn, t.Ende, t.Ganztaegig, t.Beschreibung, t.Ort,t.Strasse,t.Hausnummer,t.Plz,t.Stadt,t.Land,t.ErinnerungMinuten,t.ErinnerungQuittiert, t.Entfernt });
    public async Task<Termin> Lesen(string actor, string id)
    {
        await Konto(actor);
        return await db.Termine.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id && t.EigentuemerId == actor && !t.Entfernt) ?? throw new Arbeitsfehler("Termin nicht verfügbar.", 404);
    }
    public async Task<object> Liste(string actor, string? von, string? bis)
    {
        await Konto(actor); var (v, b) = Kalender.Zeitraum(von, bis);
        var start = v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00";
        var ende = b.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00";
        // ISO local strings preserve day membership across DST; UTC instants remain
        // available for future integrations. End is exclusive, including all-day spans.
        var q = db.Termine.AsNoTracking().Where(t => t.EigentuemerId == actor && !t.Entfernt && t.Beginn.CompareTo(ende) < 0 && t.Ende.CompareTo(start) > 0);
        var anzahl = await q.CountAsync();
        return new { eintraege = await q.OrderBy(t => t.Beginn).ThenBy(t => t.Id).Take(500).ToArrayAsync(), anzahl, zeitzone = "Europe/Berlin" };
    }
    public async Task<Termin> Speichern(string actor, string? id, Termineingabe e)
    {
        await Konto(actor); var zeiten = Pruefen(e);
        var neu = id is null;
        if (neu && (e.Version != 0 || !Guid.TryParseExact(e.AnlageId, "N", out _))) throw new Arbeitsfehler("Anlagekennung fehlt. Bitte den Dialog erneut öffnen.");
        var key = id ?? e.AnlageId!; var hash = Hash(e);
        if (neu && await db.Termine.AsNoTracking().SingleOrDefaultAsync(t => t.Id == key) is { } vorhanden)
        {
            if (vorhanden.EigentuemerId == actor && !vorhanden.Entfernt && vorhanden.Version == 1 && vorhanden.AnlageHash == hash) return vorhanden;
            throw new Arbeitsfehler("Diese Anlagekennung wurde bereits verwendet. Bitte neu laden.", 409);
        }
        var t = neu ? new Termin { Id = key, EigentuemerId = actor, AnlageHash = hash, ErstelltAm = clock.GetUtcNow() }
            : await db.Termine.SingleOrDefaultAsync(t => t.Id == key && t.EigentuemerId == actor && !t.Entfernt) ?? throw new Arbeitsfehler("Termin nicht verfügbar.", 404);
        if (!neu && t.Version != e.Version) throw new Arbeitsfehler("Der Termin wurde inzwischen geändert. Bitte neu laden; Ihre Eingaben bleiben erhalten.", 409);
        var vorher = neu ? "" : Stand(t);
        t.Strasse=e.Strasse.Trim();t.Hausnummer=e.Hausnummer.Trim();t.Plz=e.Plz.Trim();t.Stadt=e.Stadt.Trim();t.Land=e.Land.Trim();t.ErinnerungMinuten=e.ErinnerungMinuten;t.ErinnerungFaelligUtc=zeiten.Beginn.AddMinutes(-(e.ErinnerungMinuten??0));
        t.Titel = e.Titel.Trim(); t.Beschreibung = e.Beschreibung.Trim(); t.Ort = e.Ort.Trim(); t.Beginn = e.Beginn; t.Ende = e.Ende; t.Ganztaegig = e.Ganztaegig; t.BeginnUtc = zeiten.Beginn; t.EndeUtc = zeiten.Ende;
        if(neu||vorher!=Stand(t)){t.Erinnerungskennung=Guid.NewGuid().ToString("N");t.ErinnerungQuittiert=false;}
        if (neu) db.Termine.Add(t); else db.Entry(t).Property(x => x.Version).IsModified = true;
        Audit(actor, t, vorher, neu ? "Termin erstellt" : "Termin geändert");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new Arbeitsfehler("Der Termin wurde inzwischen geändert. Bitte neu laden; Ihre Eingaben bleiben erhalten.", 409); }
        catch (DbUpdateException) when (neu)
        {
            db.ChangeTracker.Clear();
            var wiederholung = await db.Termine.AsNoTracking().SingleOrDefaultAsync(x => x.Id == key);
            if (wiederholung?.EigentuemerId == actor && !wiederholung.Entfernt && wiederholung.Version == 1 && wiederholung.AnlageHash == hash) return wiederholung;
            if (wiederholung is not null) throw new Arbeitsfehler("Die Anlagekennung wurde bereits verwendet.", 409);
            throw;
        }
        return t;
    }
    public async Task Entfernen(string actor, string id, int version)
    {
        await Konto(actor);
        var t = await db.Termine.SingleOrDefaultAsync(t => t.Id == id && t.EigentuemerId == actor && !t.Entfernt) ?? throw new Arbeitsfehler("Termin nicht verfügbar.", 404);
        if (t.Version != version) throw new Arbeitsfehler("Der Termin wurde inzwischen geändert. Bitte neu laden.", 409);
        var vorher = Stand(t); t.Entfernt = true; Audit(actor, t, vorher, "Termin gelöscht");
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { throw new Arbeitsfehler("Der Termin wurde inzwischen geändert. Bitte neu laden.", 409); }
    }
    private void Audit(string actor, Termin t, string vorher, string aktion)
    {
        db.Terminverlauf.Add(new() { TerminId = t.Id, BenutzerId = actor, Zeitpunkt = clock.GetUtcNow(), Vorher = vorher, Nachher = Stand(t) });
        db.Sicherheitsereignisse.Add(new() { BenutzerId = actor, Ressource = t.Id, Aktion = aktion, Zeitpunkt = clock.GetUtcNow() });
    }
    public async Task<object> Erinnerungen(string actor,int seite=0){
        await Konto(actor);if(seite<0||seite>10000)throw new Arbeitsfehler("Erinnerungsseite prüfen.");var now=clock.GetUtcNow();
        var q=db.Termine.AsNoTracking().Where(t=>t.EigentuemerId==actor&&!t.Entfernt&&t.ErinnerungMinuten!=null&&!t.ErinnerungQuittiert&&t.ErinnerungFaelligUtc<=now);
        return new{anzahl=await q.CountAsync(),eintraege=await q.OrderBy(t=>t.ErinnerungFaelligUtc).ThenBy(t=>t.Id).Skip(seite*20).Take(20).Select(t=>new{t.Id,t.Titel,t.Beginn,t.Erinnerungskennung,adresse=(t.Strasse+" "+t.Hausnummer).Trim()+((t.Plz+" "+t.Stadt).Trim()==""?"":" · "+(t.Plz+" "+t.Stadt).Trim()),t.Ort}).ToArrayAsync()};
    }
    public async Task Quittieren(string actor,string id,string kennung){
        await Konto(actor);var t=await db.Termine.SingleOrDefaultAsync(t=>t.Id==id&&t.EigentuemerId==actor&&!t.Entfernt)??throw new Arbeitsfehler("Termin nicht verfügbar.",404);
        if(t.ErinnerungMinuten==null||t.Erinnerungskennung!=kennung||t.ErinnerungFaelligUtc>clock.GetUtcNow())throw new Arbeitsfehler("Erinnerung wurde geändert. Bitte neu laden.",409);
        if(t.ErinnerungQuittiert)return;var vorher=Stand(t);t.ErinnerungQuittiert=true;Audit(actor,t,vorher,"Terminerinnerung quittiert");
        try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){db.ChangeTracker.Clear();var aktuell=await Lesen(actor,id);if(aktuell.Erinnerungskennung!=kennung||!aktuell.ErinnerungQuittiert)throw new Arbeitsfehler("Termin wurde geändert. Bitte neu laden.",409);}
    }
    public static void Endpunkte(WebApplication app)
    {
        var g = app.MapGroup("/api/termine").RequireAuthorization();
        g.AddEndpointFilter(async (c, next) => { try { return await next(c); } catch (Arbeitsfehler e) { return Results.Json(new ApiFehler(e.Message), statusCode: e.Status); } });
        static string Actor(HttpContext c) => c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        g.MapGet("/erinnerungen",(HttpContext c,Termindienst d)=>d.Erinnerungen(Actor(c),int.TryParse(c.Request.Query["seite"],out var n)?n:0));
        g.MapPost("/{id}/quittieren",async(HttpContext c,Termindienst d,string id,Erinnerungsquittung e)=>{await d.Quittieren(Actor(c),id,e.Kennung);return Results.Json(new{quittiert=true});});
        g.MapGet("/", (HttpContext c, Termindienst d) => d.Liste(Actor(c), c.Request.Query["von"], c.Request.Query["bis"]));
        g.MapGet("/{id}", (HttpContext c, Termindienst d, string id) => d.Lesen(Actor(c), id));
        g.MapPost("/", (HttpContext c, Termindienst d, Termineingabe e) => d.Speichern(Actor(c), null, e));
        g.MapPost("/{id}", (HttpContext c, Termindienst d, string id, Termineingabe e) => d.Speichern(Actor(c), id, e));
        g.MapPost("/{id}/loeschen", async (HttpContext c, Termindienst d, string id, Terminversion e) => { await d.Entfernen(Actor(c), id, e.Version); return Results.Json(new { entfernt = true }); });
    }
}

public sealed record Erinnerungsquittung(string Kennung);
