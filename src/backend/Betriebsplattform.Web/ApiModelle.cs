using Betriebsplattform.Import;

namespace Betriebsplattform.Web;

public sealed record Zahlungsansicht(string Datum, string Zahler, string Empfaenger, string Betrag,
    string Zugeordnet, string Status, string Beleg, string Quellenort, string? Id = null);
public sealed record Unternehmensperspektive(string Unternehmen, string Rolle);
// Optionale Demo-Anzeigeinformation; weder CRM-Identität noch Finanzzustand.
public sealed record Kontaktansicht(string? Ansprechpartner, string? Email, string? Telefon, string Quelle,
    IReadOnlyList<string>? Emails = null, IReadOnlyList<string>? Telefone = null, IReadOnlyList<string>? AnsprechpartnerListe = null);
public sealed record Rechnungsansicht(string Nummer, string Datum, string Aussteller, string Empfaenger,
    string Betrag, string OffenerBetrag, string BestaetigtZugeordnet, string Status, string Beleg, string Quellenort,
    IReadOnlyList<Zahlungsansicht> Zahlungen, Kontaktansicht? AusstellerKontakt = null, Kontaktansicht? EmpfaengerKontakt = null,
    string? VorgangId = null, Unternehmensperspektive? Perspektive = null,string? AusstellerId=null,string? EmpfaengerId=null);
public sealed record Uebersicht(bool Synthetisch, string Gesamtbetrag, string OffenerBetrag,
    IReadOnlyList<Rechnungsansicht> Rechnungen,string? OffeneForderungen=null,string? OffeneVerbindlichkeiten=null);
public sealed record Importzeilenansicht(int Zeile, bool HatFehler, string? Art, string? Betrag,
    string? Datum, string? Von, string? An, IReadOnlyList<Originalzelle> Zellen, IReadOnlyList<Pruefmeldung> Meldungen);
public sealed record Importansicht(bool Synthetisch, string? DateiHash, bool HatFehler, int Zeilenanzahl,
    int Fehlerzeilen, string Bericht, IReadOnlyList<Pruefmeldung> Meldungen, IReadOnlyList<Importzeilenansicht> Zeilen);
public sealed record ApiFehler(string Meldung);
