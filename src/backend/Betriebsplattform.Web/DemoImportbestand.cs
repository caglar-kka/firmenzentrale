using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Betriebsplattform.Import;
using Betriebsplattform.Rechnungen;

namespace Betriebsplattform.Web;

public sealed record Mappingentscheidung(string? Blatt, Dictionary<string, string>? Unternehmen, bool Bestaetigt);
public sealed record Uebernahmeentscheidung(int[]? Zeilen, bool Bestaetigt);
public sealed record Importabschluss(int Uebernommen, int Uebersprungen, int Fehlerhaft, int ZurPruefung);
public sealed record DemoImportansicht(string Id, bool Synthetisch, string Blatt, IReadOnlyList<string> Spalten,
    IReadOnlyList<string> Firmen, IReadOnlyDictionary<string, string> Mapping, bool MappingBestaetigt,
    Importansicht Vorschau, Importabschluss? Ergebnis, DateTimeOffset? EntschiedenAm, IReadOnlyList<int> Auswahl,IReadOnlyDictionary<string,IReadOnlyList<Akten.Dublettenhinweis>>? Firmenhinweise=null);

// Ein Prozess, ein ausschließlich synthetischer Demo-Bestand. Kein Dateisystem und keine Datenbank.
// Derselbe Lock schützt Prüfung, Dublettensets und Übernahme auch bei parallelen HTTP-Anfragen.
public sealed class DemoImportbestand
{
    public const int MaxDateiBytes = 2 * 1024 * 1024;
    private static readonly string[] Spalten = ["DatensatzId", "Art", "Nummer", "Von", "An", "Datum", "Betrag", "Waehrung", "Beleg"];
    private sealed class Vorgang(string id, byte[] bytes, string hash, Importbericht bericht)
    {
        internal string Id { get; } = id;
        internal byte[] Bytes { get; } = bytes;
        internal string Hash { get; } = hash;
        internal Importbericht Bericht { get; set; } = bericht;
        internal Dictionary<string, string> Mapping { get; set; } = [];
        internal bool Bestaetigt { get; set; }
        internal Importabschluss? Ergebnis { get; set; }
        internal DateTimeOffset? EntschiedenAm { get; set; }
        internal int[] Auswahl { get; set; } = [];
    }
    private readonly object sperre = new();
    private readonly Dictionary<string, Vorgang> vorgaenge = [];
    private readonly List<Rechnung> rechnungen = [];
    private readonly HashSet<Quellschluessel> schluessel = [];

    public Uebersicht Uebersicht() { lock (sperre) return DemoDaten.UebersichtErstellen(rechnungen); }
    public IReadOnlyList<DemoImportansicht> Liste() { lock (sperre) return vorgaenge.Values.Select(Ansicht).ToArray(); }
    public DemoImportansicht Lesen(string id) { lock (sperre) return Ansicht(vorgaenge[id]); }
    public bool HatVorgang(string id) { lock (sperre) return rechnungen.Any(r => "import-" + r.Id.ToString("N") == id); }

    public DemoImportansicht Hochladen(byte[] bytes, string dateiname)
    {
        if (bytes.Length is 0 or > MaxDateiBytes || string.IsNullOrWhiteSpace(dateiname) || dateiname.Length > 180
            || dateiname.Any(c => char.IsControl(c) || c is '/' or '\\') || !dateiname.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bitte eine makrofreie synthetische XLSX-Datei bis 2 MiB auswählen.");
        lock (sperre)
        {
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var vorhanden = vorgaenge.Values.SingleOrDefault(v => v.Hash == hash);
            if (vorhanden is not null) return Ansicht(vorhanden);
            if (vorgaenge.Count >= 20) throw new InvalidOperationException("Die Demo hält höchstens 20 Dateien bis zum Neustart. Es wurde nichts übernommen.");
            var bericht = Pruefen(bytes, new Dictionary<string, string>());
            if (bericht.Meldungen.Any(m => m.Stufe == Meldungsstufe.Fehler))
                throw new InvalidOperationException(bericht.Meldungen.First(m => m.Stufe == Meldungsstufe.Fehler).Text);
            var neu = new Vorgang(Guid.NewGuid().ToString("N"), bytes.ToArray(), hash, bericht);
            vorgaenge.Add(neu.Id, neu);
            return Ansicht(neu);
        }
    }

    public DemoImportansicht Zuordnen(string id, Mappingentscheidung entscheidung)
    {
        lock (sperre)
        {
            var v = vorgaenge[id];
            var firmen = Firmen(v);
            if (v.Ergebnis is not null || !entscheidung.Bestaetigt || entscheidung.Blatt != "ISD-Testdaten"
                || entscheidung.Unternehmen is null || entscheidung.Unternehmen.Count > 100
                || entscheidung.Unternehmen.Any(p => !firmen.Contains(p.Key) || !DemoDaten.Namen.ContainsKey(p.Value)))
                throw new InvalidOperationException("Bitte das erkannte Blatt und eine gültige Firmenzuordnung bestätigen. Abgeschlossene Importe bleiben unverändert.");
            var mapping = new Dictionary<string, string>(entscheidung.Unternehmen, StringComparer.Ordinal);
            v.Bericht = Pruefen(v.Bytes, mapping);
            v.Mapping = mapping;
            v.Bestaetigt = true;
            return Ansicht(v);
        }
    }

    public Importabschluss Uebernehmen(string id, int[]? zeilen, bool bestaetigt)
    {
        lock (sperre)
        {
            var v = vorgaenge[id];
            if (!bestaetigt || !v.Bestaetigt || zeilen is null || zeilen.Length is 0 or > 1000 || zeilen.Distinct().Count() != zeilen.Length)
                throw new InvalidOperationException("Bitte Firmenzuordnung und ausgewählte gültige Rechnungszeilen ausdrücklich bestätigen.");
            var auswahl = zeilen.Order().ToArray();
            if (v.Ergebnis is not null)
            {
                if (!v.Auswahl.SequenceEqual(auswahl)) throw new InvalidOperationException("Diese Datei wurde bereits mit einer anderen Auswahl abgeschlossen.");
                return v.Ergebnis;
            }
            // Frische Gegenprüfung gegen alle inzwischen übernommenen Rechnungen und Quellkennungen.
            var frisch = Pruefen(v.Bytes, v.Mapping);
            var kandidaten = frisch.Zeilen.Where(z => auswahl.Contains(z.Zeile)).ToArray();
            if (kandidaten.Length != auswahl.Length || kandidaten.Any(z => z.HatFehler || z.Rechnung is null))
                throw new InvalidOperationException("Die Auswahl enthält fehlerhafte, doppelte oder ungeklärte Zeilen. Nur geprüfte Rechnungen sind übernehmbar; Zahlungen bleiben zur Prüfung.");
            if (rechnungen.Count + kandidaten.Length > 5000)
                throw new InvalidOperationException("Der Demo-Bestand ist auf 5.000 importierte Rechnungen begrenzt.");
            var neu = kandidaten.Select(z => z.Rechnung!).ToArray();
            try { _ = DemoDaten.UebersichtErstellen(rechnungen.Concat(neu)); }
            catch (OverflowException) { throw new InvalidOperationException("Die Gesamtsumme überschreitet die unterstützte Betragsgrenze. Es wurde nichts übernommen."); }
            // Erst nach vollständiger Prüfung mutieren. Kein ZahlungBestaetigen/Zuordnen-Aufruf.
            rechnungen.AddRange(neu);
            foreach (var z in kandidaten) schluessel.Add(z.Schluessel!);
            v.Bericht = frisch;
            v.Auswahl = auswahl;
            v.EntschiedenAm = DateTimeOffset.UtcNow;
            var fehler = frisch.Zeilen.Count(z => z.HatFehler);
            var pruefen = frisch.Zeilen.Count(z => !z.HatFehler && z.Zahlung is not null);
            return v.Ergebnis = new(neu.Length, frisch.Zeilen.Count - neu.Length - fehler - pruefen, fehler, pruefen);
        }
    }

    private Importbericht Pruefen(byte[] bytes, IReadOnlyDictionary<string, string> mapping)
    {
        var bekannte = DemoDaten.Grundrechnungen().Concat(rechnungen)
            .Select(r => new Rechnungskennung(r.Aussteller, r.Nummer));
        return new ExcelImportvorschau().Pruefen(new MemoryStream(bytes, writable: false), "synthetisch.xlsx",
            new Importprofil("SYNTH-LOKAL", mapping, bekannteQuellschluessel: schluessel, bekannteRechnungen: bekannte));
    }
    private static string[] Firmen(Vorgang v) => v.Bericht.Zeilen.SelectMany(z => z.Zellen.Skip(3).Take(2))
        .Select(z => z.Text.Trim()).Where(s => s.Length is > 0 and <= 256).Distinct(StringComparer.Ordinal).Order().ToArray();
    private static DemoImportansicht Ansicht(Vorgang v) => new(v.Id, true, "ISD-Testdaten", Array.AsReadOnly(Spalten),
        Firmen(v), new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(v.Mapping)), v.Bestaetigt,
        DemoDaten.ImportDarstellen(v.Bericht), v.Ergebnis, v.EntschiedenAm, Array.AsReadOnly(v.Auswahl.ToArray()));
}
