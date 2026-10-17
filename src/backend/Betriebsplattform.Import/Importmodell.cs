using System.Collections.Frozen;
using Betriebsplattform.Rechnungen;

namespace Betriebsplattform.Import;

public enum Meldungsstufe { Warnung, Fehler }
public sealed record Zellquelle(string DateiHash, string Blatt, int Zeile, string Zelle);
public sealed record Originalzelle(Zellquelle Quelle, string Datentyp, string? Originalwert,
    string Text, string? Formel, string? Formatindex = null);
public sealed record Pruefmeldung(Meldungsstufe Stufe, string Code, string Text, Zellquelle? Quelle = null);
public sealed record Quellschluessel(string Quellsystem, string Art, string DatensatzId);
public sealed record Rechnungskennung(string Aussteller, string Nummer);

public sealed class Importprofil
{
    public Importprofil(string quellsystem, IReadOnlyDictionary<string, string> unternehmen,
        IEnumerable<string>? bekannteDateiHashes = null,
        IEnumerable<Quellschluessel>? bekannteQuellschluessel = null,
        IEnumerable<Rechnungskennung>? bekannteRechnungen = null)
    {
        if (string.IsNullOrWhiteSpace(quellsystem)) throw new ArgumentException("Quellsystem fehlt.", nameof(quellsystem));
        ArgumentNullException.ThrowIfNull(unternehmen);
        if (unternehmen.Any(p => string.IsNullOrWhiteSpace(p.Key) || string.IsNullOrWhiteSpace(p.Value)
            || p.Key != p.Key.Trim() || p.Value != p.Value.Trim()))
            throw new ArgumentException("Unternehmensmapping benötigt eindeutige, nichtleere Referenzen ohne Rand-Leerzeichen.", nameof(unternehmen));
        Quellsystem = quellsystem;
        Unternehmen = unternehmen.ToFrozenDictionary(StringComparer.Ordinal);
        BekannteDateiHashes = (bekannteDateiHashes ?? []).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        BekannteQuellschluessel = (bekannteQuellschluessel ?? []).ToFrozenSet();
        BekannteRechnungen = (bekannteRechnungen ?? []).ToFrozenSet();
    }

    public string Quellsystem { get; }
    public IReadOnlyDictionary<string, string> Unternehmen { get; }
    public IReadOnlySet<string> BekannteDateiHashes { get; }
    public IReadOnlySet<Quellschluessel> BekannteQuellschluessel { get; }
    public IReadOnlySet<Rechnungskennung> BekannteRechnungen { get; }
}

public sealed class Vorschauzeile
{
    internal Vorschauzeile(int zeile, IEnumerable<Originalzelle> zellen, IEnumerable<Pruefmeldung> meldungen,
        Quellschluessel? schluessel, Rechnung? rechnung, Zahlung? zahlung)
    {
        Zeile = zeile;
        Zellen = Array.AsReadOnly(zellen.ToArray());
        Meldungen = Array.AsReadOnly(meldungen.ToArray());
        Schluessel = schluessel;
        Rechnung = rechnung;
        Zahlung = zahlung;
    }
    public int Zeile { get; }
    public IReadOnlyList<Originalzelle> Zellen { get; }
    public IReadOnlyList<Pruefmeldung> Meldungen { get; }
    public Quellschluessel? Schluessel { get; }
    public Rechnung? Rechnung { get; }
    public Zahlung? Zahlung { get; }
    public bool HatFehler => Meldungen.Any(m => m.Stufe == Meldungsstufe.Fehler);
}

public sealed class Importbericht
{
    internal Importbericht(string? dateiHash, IEnumerable<Vorschauzeile> zeilen, IEnumerable<Pruefmeldung> meldungen)
    {
        DateiHash = dateiHash;
        Zeilen = Array.AsReadOnly(zeilen.ToArray());
        Meldungen = Array.AsReadOnly(meldungen.ToArray());
    }
    public string? DateiHash { get; }
    public IReadOnlyList<Vorschauzeile> Zeilen { get; }
    public IReadOnlyList<Pruefmeldung> Meldungen { get; }
    public bool HatFehler => Meldungen.Concat(Zeilen.SelectMany(z => z.Meldungen)).Any(m => m.Stufe == Meldungsstufe.Fehler);

    // Nur Meldungen und Quellen ausgeben, keine frei eingegebenen Zelltexte in Terminal/Logs übernehmen.
    public string AlsText()
    {
        var ausgabe = new List<string>
        {
            "Excel-Importvorschau – keine Buchung, keine Zahlungsbestätigung",
            "ISD: vorläufiger Geschäftspartner; vollständiger Firmenname nicht bestätigt. Die Rolle ergibt sich aus dem jeweiligen Vorgang.",
            $"SHA-256: {DateiHash ?? "nicht verfügbar (Datei nicht vollständig gelesen)"}",
            $"Zeilen: {Zeilen.Count}; ohne Zeilenfehler: {Zeilen.Count(z => !z.HatFehler)}; mit Zeilenfehler: {Zeilen.Count(z => z.HatFehler)}",
            HatFehler ? "Ergebnis: Fehler – zur Prüfung. Keine Importfreigabe." : "Ergebnis: Vorschau geprüft. Menschliche Mapping- und Belegprüfung bleibt erforderlich."
        };
        foreach (var zeile in Zeilen)
        {
            var fachwert = zeile.Rechnung?.Betrag ?? zeile.Zahlung?.Betrag;
            var art = zeile.Rechnung is not null ? "Rechnung" : zeile.Zahlung is not null ? "Zahlung (unbestätigt)" : "kein Fachobjekt";
            var betrag = fachwert is { } wert ? $"; {wert.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))} EUR" : "";
            ausgabe.Add($"Zeile {zeile.Zeile}: {(zeile.HatFehler ? "Fehler" : "ohne Zeilenfehler")}; {art}{betrag}");
        }
        foreach (var meldung in Meldungen.Concat(Zeilen.SelectMany(z => z.Meldungen)))
        {
            var ort = meldung.Quelle is { } q ? $" [{q.Blatt}!{q.Zelle}, Zeile {q.Zeile}]" : "";
            ausgabe.Add($"{meldung.Stufe}: {meldung.Code}{ort}: {meldung.Text}");
        }
        return string.Join(Environment.NewLine, ausgabe);
    }
}
