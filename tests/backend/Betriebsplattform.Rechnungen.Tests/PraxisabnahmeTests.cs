using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Betriebsplattform.Rechnungen;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Betrieb;
using Betriebsplattform.Web.Leistungen;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

// Explicit operator gate for the supplied fictional corpus. Never registered in the product host.
public sealed class PraxisabnahmeTests
{
    sealed class PraxisFact : FactAttribute
    {
        public PraxisFact() { if (Environment.GetEnvironmentVariable("FZ_PRAXIS_APPLY") != "1") Skip = "Explicit existing isolated acceptance database and fictional corpus required."; }
    }
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    static string Key(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("praxis-20261025/" + value)))[..32].ToLowerInvariant();
    static string ResultId(object result) => JsonSerializer.SerializeToElement(result, Json).GetProperty("id").GetString()!;
    sealed class Korpusfreigabe(HashSet<string> hashes) : IMalwarepruefung
    {
        public Task Freigeben(byte[] bytes, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!hashes.Contains(Convert.ToHexString(SHA256.HashData(bytes)))) throw new Arbeitsfehler("Kein freigegebener Korpusinhalt.", 503);
            return Task.CompletedTask;
        }
    }

    [PraxisFact]
    public async Task VorhandenenIsoliertenBestandErgaenzenOhneOriginaleOderKontenZuUeberschreiben()
    {
        var repo = Path.GetFullPath(Environment.GetEnvironmentVariable("FZ_PRAXIS_WORKSPACE") ?? throw new InvalidOperationException("Explicit repository required."));
        var evidence = Path.Combine(repo, "private", "pruefungen", "praxisabnahme-20261025");
        var corpus = Path.Combine(repo, "private", "pruefmaterial");
        using var ready = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(repo, "private", "arbeitsumgebung", "ready.json")));
        var database = IsolierteArbeitsumgebung.Datenbankname(ready.RootElement.GetProperty("database").GetString()!);
        Assert.Equal(database, Environment.GetEnvironmentVariable("FZ_PRAXIS_DATABASE"));
        var storage = ready.RootElement.GetProperty("storage").GetString()!;
        var expectedStorage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FirmenZentrale", "Development", "browser-gate", database["fz_browser_test_".Length..], "documents");
        Assert.Equal(Path.GetFullPath(expectedStorage), Path.GetFullPath(storage));
        var config = new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly, optional: true).Build();
        var cs = new NpgsqlConnectionStringBuilder(config.GetConnectionString("FirmenZentrale")) { Database = database, Pooling = false, IncludeErrorDetail = false, LogParameters = false };
        Assert.Contains(cs.Host, new[] { "127.0.0.1", "localhost" }); Assert.Equal(5432, cs.Port);
        await using var db = new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(cs.ConnectionString, n => n.SetPostgresVersion(18, 0)).Options);
        await db.Database.OpenConnectionAsync();
        await using (var query = new NpgsqlCommand("select current_database()", (NpgsqlConnection)db.Database.GetDbConnection())) Assert.Equal(database, await query.ExecuteScalarAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync()); Assert.False(db.Database.HasPendingModelChanges());
        var backupRecord = Path.Combine(evidence, "backup-before.json");
        if (!File.Exists(backupRecord))
        {
            var backup = await new Sicherung(@"C:\Program Files\PostgreSQL\18\bin").Erstellen(db, cs.ConnectionString, storage, Path.Combine(Path.GetDirectoryName(storage)!, "praxis-backups"));
            await File.WriteAllTextAsync(backupRecord, JsonSerializer.Serialize(new { database, backup }, Json));
        }
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(corpus, "PRUEFSUMMEN.json")));
        var originals = new Dictionary<string, byte[]>();
        foreach (var entry in manifest.RootElement.GetProperty("sha256").EnumerateObject())
        {
            var path = Path.GetFullPath(Path.Combine(corpus, entry.Name));
            Assert.StartsWith(Path.GetFullPath(corpus) + Path.DirectorySeparatorChar, path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, (int)(File.GetAttributes(path) & FileAttributes.ReparsePoint));
            var bytes = await File.ReadAllBytesAsync(path); Assert.InRange(bytes.Length, 1, Dokumentpruefung.MaxBytes);
            Assert.Equal(entry.Value.GetString()!.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(bytes)));
            originals.Add(entry.Name, bytes);
        }
        var accounts = await db.Benutzer.AsNoTracking().ToDictionaryAsync(x => x.Id, x => new { x.PasswortHash, x.MfaGeheimnis, x.Rolle, x.Aktiv });
        var priorInvoices = await db.Rechnungen.AsNoTracking().Select(x => x.Id).ToArrayAsync();
        var priorObjects = await db.Leistungsobjekte.AsNoTracking().Select(x => x.Id).ToArrayAsync();
        var admin = (await db.Benutzer.AsNoTracking().SingleAsync(u => u.Login == "admin" && u.Aktiv)).Id;
        var team = (await db.Benutzer.AsNoTracking().SingleAsync(u => u.Login == "anna.berg" && u.Aktiv)).Id;
        var rights = new AufgabenDienst(db, TimeProvider.System, TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
        var files = new LokaleDokumentdateien(storage); await files.StartAsync(default);
        var data = new FirmenDaten(db, new(true, null, null), new Korpusfreigabe(originals.Values.Select(b => Convert.ToHexString(SHA256.HashData(b))).ToHashSet()));
        var services = new Leistungsdienst(db, rights, TimeProvider.System, config, files);
        var documentReport = new List<object>();

        async Task Kontakt(string code, string name, string street, string city = "Hamburg")
        {
            var id = Key(code); if (await db.Kontakte.AnyAsync(k => k.Id == id)) return;
            db.Kontakte.Add(new() { Id = id, Name = name, Suchname = name.ToLowerInvariant(), Strasse = street, Hausnummer = code == "KD-1048" ? "26" : code == "KD-1052" ? "7" : code == "KD-1067" ? "11" : "1", Plz = code == "KD-1052" ? "20095" : code == "KD-1067" ? "22085" : "20457", Ort = city, Land = "DE", Anschrift = street + ", " + city });
            db.Kontaktwerte.AddRange(new KontaktwertDatensatz { Id = Key(code + "/mail"), KontaktId = id, Art = "E-Mail", Wert = code.ToLowerInvariant() + "@buero.example", Vergleichswert = code.ToLowerInvariant() + "@buero.example", Primaer = true }, new KontaktwertDatensatz { Id = Key(code + "/tel"), KontaktId = id, Art = "Telefon", Wert = "+49 40 5550 100", Vergleichswert = "+49405550100", Primaer = true });
            db.Sicherheitsereignisse.Add(new() { BenutzerId = admin, Ressource = id, Aktion = "Fiktiver Praxisbestand: Kontakt intern erstellt" });
            await db.SaveChangesAsync();
        }
        var names = new Dictionary<string, string> { ["betrieb"] = "Küstenklar Gebäudeservice GmbH", ["KD-1048"] = "Elbquartier Verwaltung GmbH", ["KD-1052"] = "Nordwerft Technologie GmbH", ["KD-1067"] = "Fleetwerk Objektverwaltung", ["hygiene"] = "HanseHygiene Fachhandel GmbH", ["telefon"] = "NordTel Geschäftskunden", ["energie"] = "AlsterEnergie Geschäftskunden" };
        foreach (var (code, name) in names) await Kontakt(code, name, code == "KD-1048" ? "Brookufer" : code == "KD-1052" ? "Ankerkai" : code == "KD-1067" ? "Fährsteg" : "Hafenweg");
        foreach(var (code,first,last,email) in new[]{("KD-1048","Elena","Martens","elena.martens@elbquartier.example"),("KD-1052","Robert","Heller","r.heller@nordwerft.example"),("KD-1067","Leonie","Brandt","l.brandt@fleetwerk.example")}){
            if(await db.Kontakte.AnyAsync(x=>x.Id==Key(code+"/person")))continue;
            db.Kontakte.Add(new(){Id=Key(code+"/person"),Art="Person",Name=first+" "+last,Vorname=first,Nachname=last,Suchname=(first+" "+last).ToLowerInvariant(),FirmaId=Key(code),Rolle="Objektkontakt"});
            db.Kontaktwerte.Add(new(){Id=Key(code+"/person-mail"),KontaktId=Key(code+"/person"),Art="E-Mail",Wert=email,Vergleichswert=email,Primaer=true});await db.SaveChangesAsync();
        }
        var category = Key("kategorie");
        if (!await db.Kategorien.AnyAsync(x => x.Id == category)) { db.Kategorien.Add(new() { Id = category, Name = "Objektservice", Suchname = "objektservice" }); await db.SaveChangesAsync(); }
        async Task Akte(string code, string title, string customer)
        {
            if (!await db.Akten.AnyAsync(x => x.Id == Key(code))) { db.Akten.Add(new() { Id = Key(code), Titel = title, Suchtext = title.ToLowerInvariant(), KategorieId = category, FirmaId = Key(customer) }); await db.SaveChangesAsync(); }
        }
        await Akte("vertrag", "LV-KS-2026-014 · Kontorhaus Südhafen", "KD-1048");
        await Akte("reklamation", "BM-2026-1024 · Zugang und Nacharbeit", "KD-1048");
        await Akte("eingang", "Dokumenteingang Oktober 2026", "betrieb");
        foreach (var customer in names.Keys)
            if (!await db.Freigaben.AnyAsync(g => g.BenutzerId == team && g.KontaktId == Key(customer))) db.Freigaben.Add(new() { BenutzerId = team, KontaktId = Key(customer) });
        await db.SaveChangesAsync();
        var invoiceFiles = originals.Keys.Where(k => k.StartsWith("Rechnungen/", StringComparison.Ordinal)).ToDictionary(k => Path.GetFileName(k).Split('_')[0], k => k);
        foreach (var line in Encoding.UTF8.GetString(originals["Import/Rechnungsuebersicht_Oktober_2026.csv"]).TrimStart('\uFEFF').Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var c = line.Trim().Split(';'); var number = c[0]; var direction = c[3]; var partner = names.Single(x => x.Value == c[2]).Key;
            await Akte(number, number + " · " + c[2], direction == "Ausgang" ? partner : "betrieb");
            if (!await db.Rechnungen.AnyAsync(x => x.Id == Key(number)))
            {
                var invoice = new Rechnung(Guid.ParseExact(Key(number), "N"), number, Key(direction == "Ausgang" ? "betrieb" : partner), Key(direction == "Ausgang" ? partner : "betrieb"), DateOnly.ParseExact(c[1], "dd.MM.yyyy", CultureInfo.InvariantCulture), decimal.Parse(c[6], CultureInfo.GetCultureInfo("de-DE")), new(invoiceFiles[number], "MBOP-Praxisdaten-2026-10-25/" + invoiceFiles[number]));
                await new FinanzDaten(db).RechnungErfassen(invoice, Key(number), direction);
            }
        }
        // Explicit additional fictional settlement scenario; an advice/status alone is not bank evidence.
        foreach (var (number, amount, partner) in new[] { ("RE-2026-0917", 1000m, "KD-1048"), ("HH-4405", 303.45m, "hygiene"), ("NT-2026-1012", 111.86m, "telefon") })
        {
            var paymentId = Key("zahlung/" + number); var outgoing = number != "RE-2026-0917";
            var proof = new Nachweis("Zusätzlicher fiktiver Zahlungsnachweis " + number, "Isolierte Praxisabnahme; frei erfundener Prüfvorgang, kein echter Banknachweis. Avis und Journal bleiben getrennte Originalquellen.");
            var finance = new FinanzDaten(db);
            if (!await db.Zahlungen.AnyAsync(x => x.Id == paymentId)) await finance.ZahlungErfassen(new(Guid.ParseExact(paymentId, "N"), Key(outgoing ? "betrieb" : partner), Key(outgoing ? partner : "betrieb"), new(2026, 10, 16), amount, proof));
            if (!await db.Zahlungsbestaetigungen.AnyAsync(x => x.ZahlungId == paymentId)) await finance.Bestaetigen(paymentId, (await db.Zahlungen.SingleAsync(x => x.Id == paymentId)).Version, proof, admin);
            if (!await db.Zahlungszuordnungen.AnyAsync(x => x.RechnungId == Key(number) && x.ZahlungId == paymentId)) await finance.Zuordnen(Key(number), paymentId, (await db.Rechnungen.SingleAsync(x => x.Id == Key(number))).Version, (await db.Zahlungen.SingleAsync(x => x.Id == paymentId)).Version, amount, proof, admin);
        }
        foreach (var (relative, bytes) in originals.Where(x => !x.Key.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (await db.Dokumente.AnyAsync(d => d.Hash == hash)) { documentReport.Add(new { relative, hash, status = "Bereits vorhanden; unverändert" }); continue; }
            try
            {
                string? akte = relative.StartsWith("Scans/") ? Key("HH-4405") : relative.StartsWith("Rechnungen/") ? Key(Path.GetFileName(relative).Split('_')[0]) : relative.StartsWith("Vertraege/") ? Key("vertrag") : relative.Contains("BM-") || relative.Contains("NA-") ? Key("reklamation") : null;
                var d = await data.DokumentHinzufuegen(bytes, Path.GetFileName(relative), "", akte, files, benutzer: "Interne Praxisdatenbereitstellung");
                d.Quelle = "MBOP-Praxisdaten-2026-10-25/" + relative; await db.SaveChangesAsync();
                documentReport.Add(new { relative, hash, status = "Intern bereitgestellt; regulärer Upload weiterhin gesperrt", d.Id, d.Vorschau, d.Textmethode });
            }
            catch (Arbeitsfehler e) { db.ChangeTracker.Clear(); documentReport.Add(new { relative, hash, status = "Nicht übernommen", reason = e.Message }); }
        }
        // Explicit source-index links, not inferred approval or financial import.
        foreach(var (source,target) in new[]{("Schriftverkehr/ZA-2026-088_Zahlungsavis.pdf","RE-2026-0917"),("Schriftverkehr/ZE-2026-0917_Zahlungserinnerung.pdf","RE-2026-0917"),("Schriftverkehr/BR-2026-210_Buerozugang.pdf","reklamation"),("Leistungsnachweise/LN-2026-0109_Leistungsnachweis.pdf","vertrag"),("Leistungsnachweise/EB-2026-1023_Einsatzbericht.pdf","vertrag")}){
            var h=Convert.ToHexString(SHA256.HashData(originals[source]));var document=await db.Dokumente.SingleAsync(x=>x.Hash==h);var link=await db.Dokumentzuordnungen.AsNoTracking().SingleOrDefaultAsync(x=>x.DokumentId==document.Id);
            if(link==null)await data.Erstzuordnen(document.Id,new(Key(target),null,category,null,document.Version));else Assert.Equal(Key(target),link.AkteId);
        }
        var catalog = new[] { "Büro- und Hartbodenreinigung", "Saugen", "Papierkörbe und Mülltrennung", "Sanitär und WC", "Verbrauchsmaterial ergänzen", "Spiegel und Armaturen", "Glas und Eingangsbereich", "Treppen und Handläufe", "Teeküche", "Besprechungsräume", "Türen und Kontaktflächen", "Grund- und Sonderreinigung" };
        foreach (var (name, i) in catalog.Select((name, i) => (name, i))) await services.KatalogSpeichern(admin, null, new(Key("katalog/" + i), 0, name, "Nach vereinbartem Leistungsverzeichnis ausführen; Abweichungen frei beschreiben.", "Reinigung", i * 10));
        var objectRows = new[] { ("OB-2104", "KD-1048", "Kontorhaus Südhafen", "Neuer Hafendamm 18"), ("OB-2105", "KD-1048", "Speicherhaus Fleettor", "Fleetsteg 21"), ("OB-2201", "KD-1052", "Technologiecampus Nord", "Ankerkai 7"), ("OB-2209", "KD-1067", "Wohnanlage Fährsteg", "Fährsteg 11") };
        var objectReport = new List<object>();
        foreach (var (code, customer, name, address) in objectRows)
        {
            var o = ResultId(await services.ObjektAnlegen(admin, new(Key(code), Key(customer), name, address + ", Hamburg", admin, customer == "KD-1048" ? Key("vertrag") : null)));
            var positions = catalog.Take(code == "OB-2104" ? 12 : code == "OB-2105" ? 3 : 6).Select((n, i) => new Sollposition(n, i < 3 ? "Büro" : "Objekt", i == 11 ? "Einmalig" : "Bestimmte Wochentage", true, "Zugangsvorgaben beachten", i == 11 ? null : [1, 3, 5])).ToArray();
            var plan = ResultId(await services.PlanAnlegen(admin, o, new(Key(code + "/plan"), 1, "Leistungsverzeichnis " + code, new(2026, 10, 1), positions)));
            var e = ResultId(await services.EinsatzAnlegen(admin, o, new(Key(code + "/einsatz"), plan, new(2026, 10, 23), team)));
            if (code == "OB-2104")
            {
                await services.Entscheiden(team, e, new(Key(code + "/check"), 1, "checkliste", positions.Select((_, i) => new Istposition(i, i == 1 ? "Teilweise erledigt" : "Erledigt", i == 1 ? "Raum 3.14 verschlossen; Nacharbeit nach Zugang erforderlich." : "Nach Leistungsverzeichnis ausgeführt.")).ToArray(), "Zugangsbeschränkung aus BR-2026-210; keine Kundenunterschrift."));
                await services.Entscheiden(team, e, new(Key(code + "/melden"), 2, "melden"));
                await services.Entscheiden(admin, e, new(Key(code + "/pruefen"), 3, "pruefen"));
                await services.Entscheiden(admin, e, new(Key(code + "/freigeben"), 4, "freigeben"));
                await services.Entscheiden(admin, e, new(Key(code + "/reklamation"), 5, "rueckmeldung", Bemerkung: "Frei erfundene Beanstandung BM-2026-1024 aus dem gelieferten Korpus: Raum 3.14 nach Zugang nacharbeiten. Geplant für 26.10.; noch nicht erledigt.", Kundenstatus: "Beanstandet", Position: 1, Nacharbeit: true));
            }
            objectReport.Add(new { code, o, plan, einsatz = e });
        }
        foreach (var (code, title, status) in new[] { ("mahnung", "ZE-2026-0917 · Restbetrag 2.213,00 € prüfen", "Offen"), ("zugang", "Zugang Raum 3.14 vor Nacharbeit klären", "In Bearbeitung"), ("angebot", "ANG-2026-038 · Angebotsrückfrage beantwortet", "Erledigt") })
        {
            if (await db.Aufgaben.AnyAsync(a => a.Id == Key(code))) continue;
            db.Aufgaben.Add(new() { Id = Key(code), Betreff = title, Beschreibung = "Fiktiver Bürofall aus MBOP-Praxisdaten-2026-10-25; kein automatischer Versand.", ErstellerId = admin, ZustaendigId = admin, Status = status, FaelligAm = new(2026, 10, 26), FirmaId = Key("KD-1048"), AkteId = Key("vertrag") }); await db.SaveChangesAsync();
        }
        var receivableTask=await db.Aufgaben.AsNoTracking().SingleAsync(x=>x.Id==Key("mahnung"));
        if(receivableTask.RechnungId==null)await rights.Speichern(admin,receivableTask.Id,new(receivableTask.Betreff,receivableTask.Beschreibung,receivableTask.ZustaendigId,receivableTask.Prioritaet,receivableTask.Status,receivableTask.FaelligAm,receivableTask.FaelligUm,receivableTask.Version,FirmaId:Key("KD-1048"),BezugArt:"Rechnung",BezugId:Key("RE-2026-0917")));
        var berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"); var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow.AddMinutes(2), berlin);
        var appointment = new Termineingabe("Objektbegehung · Kontorhaus Südhafen", local.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture), local.AddMinutes(30).ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture), false, "Kurzfristige In-App-Erinnerung der isolierten Praxisabnahme. Kein System-Push.", "Kontorhaus Südhafen", AnlageId: Key("termin"), Strasse: "Neuer Hafendamm", Hausnummer: "18", Plz: "20457", Stadt: "Hamburg", Land: "DE", ErinnerungMinuten: 0);
        if (!await db.Termine.AnyAsync(t => t.Id == appointment.AnlageId)) await new Termindienst(db, TimeProvider.System).Speichern(admin, null, appointment);
        var teamAppointment=appointment with{AnlageId=Key("termin-team"),Titel="Übergabe · Kontorhaus Südhafen"};
        if(!await db.Termine.AnyAsync(t=>t.Id==teamAppointment.AnlageId))await new Termindienst(db,TimeProvider.System).Speichern(team,null,teamAppointment);
        var foreign=(await db.Benutzer.AsNoTracking().SingleAsync(u=>u.Login=="leon.kruse")).Id;
        var firstJob=JsonSerializer.SerializeToElement(objectReport,Json)[0].GetProperty("einsatz").GetString()!;
        var beforeDenied=(await db.Leistungseinsaetze.AsNoTracking().SingleAsync(e=>e.Id==firstJob)).Daten;
        Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>services.Entscheiden(foreign,firstJob,new(Key("denied"),1,"checkliste",[])))).Status);
        Assert.Equal(beforeDenied,(await db.Leistungseinsaetze.AsNoTracking().SingleAsync(e=>e.Id==firstJob)).Daten);
        foreach (var (id, old) in accounts) { var current = await db.Benutzer.AsNoTracking().SingleAsync(x => x.Id == id); Assert.Equal(old.PasswortHash, current.PasswortHash); Assert.Equal(old.MfaGeheimnis, current.MfaGeheimnis); Assert.Equal(old.Rolle, current.Rolle); Assert.Equal(old.Aktiv, current.Aktiv); }
        Assert.All(priorInvoices, id => Assert.Contains(id, db.Rechnungen.Select(x => x.Id)));
        Assert.All(priorObjects, id => Assert.Contains(id, db.Leistungsobjekte.Select(x => x.Id)));
        var totals = new { contacts = await db.Kontakte.CountAsync(), invoices = await db.Rechnungen.CountAsync(), documents = await db.Dokumente.CountAsync(), objects = await db.Leistungsobjekte.CountAsync(), plans = await db.Leistungsplaene.CountAsync(), jobs = await db.Leistungseinsaetze.CountAsync(), proofs = await db.Leistungsnachweise.CountAsync(), tasks = await db.Aufgaben.CountAsync() };
        var repeatBaseline=Path.Combine(evidence,"repeat-baseline.json");
        if(File.Exists(repeatBaseline)){using var previous=JsonDocument.Parse(await File.ReadAllTextAsync(repeatBaseline));var expected=previous.RootElement.GetProperty("totals").EnumerateObject().ToDictionary(x=>x.Name,x=>x.Value.GetInt32());var actual=JsonSerializer.SerializeToElement(totals,Json).EnumerateObject().ToDictionary(x=>x.Name,x=>x.Value.GetInt32());Assert.Equal(expected.OrderBy(x=>x.Key),actual.OrderBy(x=>x.Key));}
        await File.WriteAllTextAsync(Path.Combine(evidence, "praxisbestand.json"), JsonSerializer.Serialize(new { database, totals, documents = documentReport, objects = objectReport, appointmentId = appointment.AnlageId, note = "Existing isolated normal profile; accounts unchanged. Internal manifest-bound fixture is not a production malware or import approval." }, Json));
    }
}
