namespace Betriebsplattform.Web.Persistenz;

// Storage records are separate from transport DTOs and immutable domain objects.
public abstract class VersionierterDatensatz
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Version { get; set; } = 1;
    public DateTimeOffset ErstelltAm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset GeaendertAm { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class KontaktDatensatz : VersionierterDatensatz
{
    public string Art { get; set; } = "Firma";
    public string Name { get; set; } = "";
    public string Suchname { get; set; } = "";
    public string Vorname { get; set; } = "";
    public string Nachname { get; set; } = "";
    public string? FirmaId { get; set; }
    public string Strasse { get; set; } = "";
    public string Hausnummer { get; set; } = "";
    public string Plz { get; set; } = "";
    public string Ort { get; set; } = "";
    public string Land { get; set; } = "";
    public string Anschrift { get; set; } = ""; // Legacy value is retained, never guessed apart.
    public string Abteilung { get; set; } = "";
    public string Rolle { get; set; } = "";
    public bool Archiviert { get; set; }
}
public sealed class KontaktwertDatensatz
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string KontaktId { get; set; } = "";
    public string Art { get; set; } = "";
    public string Wert { get; set; } = "";
    public string Vergleichswert { get; set; } = "";
    public string Typ { get; set; } = "";
    public bool Primaer { get; set; }
    public bool Aktiv { get; set; } = true;
}
public sealed class KategorieDatensatz : VersionierterDatensatz
{
    public string Name { get; set; } = "";
    public string Suchname { get; set; } = "";
    public string Beschreibung { get; set; } = "";
    public string? ElternId { get; set; }
    public int Reihenfolge { get; set; }
    public bool Archiviert { get; set; }
}
public sealed class AkteDatensatz : VersionierterDatensatz
{
    public string Titel { get; set; } = "";
    public string Suchtext { get; set; } = "";
    public string KategorieId { get; set; } = "";
    public string? FirmaId { get; set; }
    public string Status { get; set; } = "Offen";
}
public sealed class DokumentDatensatz : VersionierterDatensatz
{
    public string Titel { get; set; } = "";
    public string Suchtext { get; set; } = "";
    public string Dateiname { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string Inhaltstyp { get; set; } = "";
    public long Groesse { get; set; }
    public string Hash { get; set; } = "";
    public string Art { get; set; } = "Anhang";
    public string? KategorieId { get; set; }
    public string? AbsenderId { get; set; }
    public string Kanal { get; set; } = "Upload";
    public string Quelle { get; set; } = "Upload";
    public string Status { get; set; } = "Zur Prüfung";
    public bool Pruefbedarf { get; set; } = true;
    public string Vorschau { get; set; } = "download";
    public string Text { get; set; } = "";
    public string Textmethode { get; set; } = "Keine Texterkennung verfügbar";
    public string? AnalyseJson { get; set; }
}
// Document PK enforces at most one current case. No copied binary content.
public sealed class DokumentzuordnungDatensatz
{
    public string DokumentId { get; set; } = "";
    public string AkteId { get; set; } = "";
}
public sealed class DokumentaktivitaetDatensatz
{
    public string? ScanauftragId { get; set; }
    public string? EingangsId { get; set; }
    public string? PruefAbsenderId { get; set; }
    public string? VorherigerAbsenderId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DokumentId { get; set; } = "";
    public string? VorherigeAkteId { get; set; }
    public string? AkteId { get; set; }
    public string Text { get; set; } = "";
    public string Benutzer { get; set; } = "";
    public DateTimeOffset Zeitpunkt { get; set; } = DateTimeOffset.UtcNow;
    public string? Kanal { get; set; }
}
public sealed class NotizDatensatz : VersionierterDatensatz
{
    public string AkteId { get; set; } = "";
    public string Betreff { get; set; } = "";
    public string Inhalt { get; set; } = "";
    public string Autor { get; set; } = "";
    public bool Archiviert { get; set; }
}
public sealed class NotizrevisionDatensatz
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string NotizId { get; set; } = "";
    public int Version { get; set; }
    public string Betreff { get; set; } = "";
    public string Inhalt { get; set; } = "";
    public string Autor { get; set; } = "";
    public DateTimeOffset Zeitpunkt { get; set; }
    public string Aktion { get; set; } = "";
}
public sealed class KorrespondenzDatensatz : VersionierterDatensatz
{
    public string AkteId { get; set; } = "";
    public string KontaktId { get; set; } = "";
    public string Betreff { get; set; } = "";
    public string Inhalt { get; set; } = "";
    public string Art { get; set; } = "";
    public string Richtung { get; set; } = "";
    public string Autor { get; set; } = "";
    public DateTimeOffset KommuniziertAm { get; set; }
}
public sealed class KorrespondenzdokumentDatensatz
{
    public string KorrespondenzId { get; set; } = "";
    public string DokumentId { get; set; } = "";
}
public sealed class KontaktverlaufDatensatz
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string KontaktId { get; set; } = "";
    public string? DokumentId { get; set; }
    public string Vorher { get; set; } = "";
    public string Nachher { get; set; } = "";
    public string Aktion { get; set; } = "";
    public string Benutzer { get; set; } = "";
    public DateTimeOffset Zeitpunkt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class KontaktvorschlagDatensatz : VersionierterDatensatz
{
    public string KontaktId { get; set; } = "";
    public string DokumentId { get; set; } = "";
    public string Art { get; set; } = "";
    public string Bisher { get; set; } = "";
    public string Neu { get; set; } = "";
    public string Status { get; set; } = "Zur Prüfung";
    public int Kontaktversion { get; set; }
}
public sealed class RechnungDatensatz : VersionierterDatensatz
{
    public string Nummer { get; set; } = "";
    public string AusstellerId { get; set; } = "";
    public string EmpfaengerId { get; set; } = "";
    public string AkteId { get; set; } = "";
    public string Richtung { get; set; } = "Ungeklärt";
    public DateOnly Datum { get; set; }
    public decimal Betrag { get; set; }
    public string Zahlungsstatus { get; set; } = "Offen";
    public string Beleg { get; set; } = "";
    public string Quelle { get; set; } = "";
}
public sealed class ZahlungDatensatz : VersionierterDatensatz
{
    public string ZahlerId { get; set; } = "";
    public string EmpfaengerId { get; set; } = "";
    public DateOnly Datum { get; set; }
    public decimal Betrag { get; set; }
    public string Beleg { get; set; } = "";
    public string Quelle { get; set; } = "";
}
public sealed class ZahlungsbestaetigungDatensatz
{
    public string ZahlungId { get; set; } = "";
    public string Beleg { get; set; } = "";
    public string Quelle { get; set; } = "";
    public string Benutzer { get; set; } = "";
    public DateTimeOffset Zeitpunkt { get; set; }
}
public sealed class ZahlungszuordnungDatensatz
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RechnungId { get; set; } = "";
    public string ZahlungId { get; set; } = "";
    public decimal Betrag { get; set; }
    public string Beleg { get; set; } = "";
    public string Quelle { get; set; } = "";
    public string Benutzer { get; set; } = "";
    public DateTimeOffset Zeitpunkt { get; set; }
}
