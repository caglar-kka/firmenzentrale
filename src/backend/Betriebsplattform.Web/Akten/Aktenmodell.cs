namespace Betriebsplattform.Web.Akten;

public sealed class Arbeitsfehler(string text, int status = 400, IReadOnlyList<Dublettenhinweis>? hinweise=null) : Exception(text) { public int Status { get; } = status; public IReadOnlyList<Dublettenhinweis> Hinweise {get;}=hinweise??[]; }
public sealed record Dublettenhinweis(string Id,string Name,string Art,string Anschrift,IReadOnlyList<string> Angaben,IReadOnlyList<string> Gruende);
public sealed record Vorgang(string Id, string Titel, string Kategorie, string? FirmaId, string? Rechnungsnummer = null,
    string? KategorieId=null,string Firma="",string Status="Neu",DateTimeOffset? LetztesEreignis=null,int Dokumentanzahl=0,string Kurzinfo="",int Version=1);
public sealed record Vorgangseingabe(string? Titel,string? Kategorie,string? FirmaId,bool DublettenBestaetigt=false);
public sealed record Bearbeitungsentscheidung(string? Status,int Version);
public static class Vorgangsbearbeitung
{
    public static IReadOnlyList<string> Werte {get;}=Array.AsReadOnly(new[]{"Neu","In Bearbeitung","Wartet auf Rückmeldung","Erledigt","Archiviert"});
}
public sealed record Kategorieverschiebung(string? Richtung,int Version,string? ZielId=null,int ZielVersion=0);
public sealed record Notizrevision(int Version, string Betreff, string Inhalt, string Autor, DateTimeOffset Zeitpunkt, string Aktion);
public sealed record Aktennotiz(string Id, string VorgangId, string Betreff, string Inhalt, string Autor,
    DateTimeOffset ErstelltAm, DateTimeOffset GeaendertAm, int Version, bool Entfernt, IReadOnlyList<Notizrevision> Historie);
public sealed record Notizeingabe(string? Betreff, string? Inhalt, int Version = 0);
public sealed record Versionsentscheidung(int Version);
public sealed record Zuordnung(string? VorgangId, string? NeuerTitel, string? Kategorie, string? FirmaId, int Version,bool DublettenBestaetigt=false);
public sealed record Korrespondenzeingabe(string? Betreff, string? Inhalt, string? Art, string? DokumentId=null,IReadOnlyList<string>? DokumentIds=null);
public sealed record Korrespondenz(string Id, string VorgangId, string Betreff, string Inhalt, string Art, string Autor,
    DateTimeOffset ErfasstAm, string? DokumentId,IReadOnlyList<string>? DokumentIds=null);
public sealed record Kontaktwert(string Id, string Art, string Wert, bool Aktiv,bool Primaer=false,string Typ="");
public sealed record Kontaktentscheidung(string Aktion, string Vorher, string Nachher, string DokumentId, string Benutzer, DateTimeOffset Zeitpunkt);
public sealed record Firmenkontakt(string Id, string Firma, int Version, IReadOnlyList<Kontaktwert> Werte, IReadOnlyList<Kontaktentscheidung> Historie,
    string Art="Firma",string? FirmaId=null,string Anschrift="",string Abteilung="",string Rolle="",bool Archiviert=false,DateTimeOffset ErstelltAm=default,DateTimeOffset GeaendertAm=default,
    string Strasse="",string Plz="",string Ort="",string Land="",string Hausnummer="",string Vorname="",string Nachname="");
public sealed record Kontaktvorschlag(string Id, string FirmaId, string DokumentId, string Art, string Bisher, string Neu,
    string Quelle, string Status, int Kontaktversion, string Firma);
public sealed record Kontaktfreigabe(string? Aktion, int Version);
public sealed record Dateiergebnis(string Inhaltstyp, string Vorschau, string Text, string Textmethode);
public sealed record Aktendokument(string Id, string Titel, string Dateiname, string Art, string Kategorie, string Kanal,
    DateTimeOffset ErfasstAm, int Version, string Inhaltstyp, string Vorschau, string Textstatus, IReadOnlyList<string> Vorgaenge,
    string? KategorieId=null,string Status="Zur Prüfung",string? AbsenderId=null,string Absender="Nicht angegeben",string? Vorgangsvorschlag=null,bool Pruefbedarf=true,
    string Kurzfassung="",string Handlungsbedarf="Inhalt und Zuordnung prüfen. Es liegen keine verlässlich ermittelten Fristen vor.",string Auswertungsquelle="Keine automatische Inhaltsauswertung",IReadOnlyList<Eingangsaktivitaet>? Aktivitaeten=null,DateTimeOffset? LetzterEingangAm=null);
public sealed record Eingangsaktivitaet(string Text,DateTimeOffset Zeitpunkt,string Benutzer,string? VorgangId=null,string? Kanal=null);
public sealed record Dokumenttechnik(string Id, string DateiHash, int Bytezahl, string Quelle, string Textmethode, IReadOnlyList<string> Vorgaenge);
public sealed record Aktenansicht(Vorgang Vorgang, IReadOnlyList<Aktendokument> Dokumente, IReadOnlyList<Aktennotiz> Notizen,
    IReadOnlyList<Korrespondenz> Korrespondenz, IReadOnlyList<Kontaktvorschlag> Kontaktvorschlaege);
public sealed record Suchtreffer(string Id, string Art, string Titel, string Zusatz, string Ziel, string? VorgangId = null);

public static class Arbeitsregeln
{
    public static readonly string[] Kategorien = ["Zur Prüfung", "Rechnung", "Versicherung", "Krankenkasse / Sozialversicherung", "Behörde / Finanzamt", "Mahnung", "Vertrag", "Allgemeines Schreiben", "Sonstiges"];
    public static string Text(string? wert, string feld, int maximum)
    {
        var t = wert?.Trim() ?? "";
        if (t.Length is 0 || t.Length > maximum || t.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t'))
            throw new Arbeitsfehler($"{feld}: Bitte 1 bis {maximum} Zeichen ohne Steuerzeichen eingeben.");
        return t;
    }
    public static void Version(int erwartet, int aktuell)
    {
        if (erwartet != aktuell) throw new Arbeitsfehler("Der Eintrag wurde inzwischen geändert. Bitte neu laden und erneut prüfen.", 409);
    }
}

// Aktivierung, Ressourcenrechte und spätere Produktlizenz bleiben getrennte Ebenen.
public interface ILesezugriff { bool Erlaubt(string art, string id); }
public sealed class LokalerDemozugriff : ILesezugriff { public bool Erlaubt(string art, string id) => true; }
public sealed class Arbeitsmodule(IEnumerable<string>? aktive = null)
{
    private readonly HashSet<string> aktiv = new(aktive ?? ["Akten", "Dokumente", "Finanzen", "Import", "Kommunikation"], StringComparer.Ordinal);
    public bool Aktiv(string modul) => aktiv.Contains(modul);
    public IReadOnlyList<string> Liste => aktiv.Order().ToArray();
}

public interface IDocumentTextExtractor { Dateiergebnis Pruefen(byte[] bytes, string dateiname, string mime); }
public interface IScannerAdapter
{
    // Ein künftiger Companion muss die lokale Sitzung authentisieren und Nutzerwahl/Abbruch respektieren.
    Task<IReadOnlyList<string>> GeraeteAsync(CancellationToken token);
    Task<byte[]> ScannenAsync(string geraetId, CancellationToken token);
}
