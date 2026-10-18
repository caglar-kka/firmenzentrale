using System.Globalization;
using Betriebsplattform.Import;
using Betriebsplattform.Rechnungen;

namespace Betriebsplattform.Web;

public static class DemoDaten
{
    private static readonly DateOnly Datum = new(2026, 10, 17);
    private static readonly DateTimeOffset ErfasstAm = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    internal static readonly IReadOnlyDictionary<string, string> Namen = new Dictionary<string, string>
    {
        ["DEMO-NORD"] = "Fjordfalter Objektpflege GmbH", ["DEMO-ISD"] = "Elbpfad Büroausstattung GmbH", ["DEMO-WEST"] = "Fennbogen Gewerbeservice GmbH", ["DEMO-VERS"] = "Kieselanker Versicherung AG", ["DEMO-KASSE"] = "Fennquell Gesundheitskasse", ["DEMO-AMT"] = "Verwaltungsstelle Musterhafen", ["DEMO-MOBIL"] = "Tannufer Kommunikation GmbH"
    };
    internal static string Geld(decimal wert) => wert.ToString("N2", CultureInfo.GetCultureInfo("de-DE")) + " €";

    // Ausschließlich erfundene Kontakte. Maildomain .invalid; Telefon aus dem
    // BNetzA-Filmrufnummernbereich Berlin (030) 23125 000–999. Nicht erreichbar.
    private static Kontaktansicht? Kontakt(string unternehmen) => unternehmen switch
    {
        "DEMO-NORD" => new("Lina Fennbrück", "buero@fjordfalter.invalid", "+49 30 23125 001", "synthetisch/Kontakt-DEMO-NORD"),
        "DEMO-ISD" => new(null, "kontakt@elbpfad.invalid", null, "synthetisch/Kontakt-DEMO-ISD"),
        _ => null
    };

    internal static Rechnung[] Grundrechnungen() =>
        [
            new Rechnung(Guid.NewGuid(), "RE-2026-1039", "DEMO-NORD", "DEMO-ISD", Datum, 1280m, new("SYNTH-RE-001", "synthetisch/Rechnung-001")),
            new Rechnung(Guid.NewGuid(), "RE-2026-1048", "DEMO-ISD", "DEMO-NORD", Datum, 960m, new("SYNTH-RE-002", "synthetisch/Rechnung-002")),
            new Rechnung(Guid.NewGuid(), "RE-2026-1051", "DEMO-NORD", "DEMO-WEST", Datum, 450m, new("SYNTH-RE-003", "synthetisch/Rechnung-003"))
        ];

    public static Uebersicht UebersichtErstellen(IEnumerable<Rechnung>? importiert = null)
    {
        // Den nicht nebenläufigen Kern nur lokal aufbauen. Danach ausschließlich unveränderliche Ansichten teilen.
        var kern = new Rechnungskern();
        var rechnungen = Grundrechnungen().Concat(importiert ?? []).ToArray();
        foreach (var rechnung in rechnungen) kern.RechnungErfassen(rechnung);
        var zahlungen = new Dictionary<Guid, List<Zahlungsansicht>>();
        void ZahlungAnzeigen(Rechnung rechnung, decimal betrag, bool bestaetigt, string beleg)
        {
            var zahlung = new Zahlung(Guid.NewGuid(), rechnung.Empfaenger, rechnung.Aussteller, Datum, betrag,
                new(beleg, "synthetisch/" + beleg));
            kern.ZahlungErfassen(zahlung);
            if (bestaetigt)
            {
                kern.ZahlungBestaetigen(zahlung.Id, new("SYNTH-BANK-" + beleg, "synthetisch/Bankbeleg"), "SYNTH-PRUEFER", ErfasstAm);
                kern.Zuordnen(Guid.NewGuid(), rechnung.Id, zahlung.Id, betrag, zahlung.Nachweis, "SYNTH-PRUEFER", ErfasstAm);
            }
            if (!zahlungen.TryGetValue(rechnung.Id, out var liste)) zahlungen[rechnung.Id] = liste = [];
            liste.Add(new(Datum.ToString("yyyy-MM-dd"), Namen[zahlung.Zahler], Namen[zahlung.Empfaenger], Geld(zahlung.Betrag),
                Geld(bestaetigt ? betrag : 0m), bestaetigt ? "Bestätigt" : "Zur Prüfung", zahlung.Nachweis.Beleg, zahlung.Nachweis.Quellenort,
                beleg switch { "SYNTH-ZA-001" => "demo-zahlung-001", "SYNTH-ZA-002" => "demo-zahlung-002", "SYNTH-ZA-003" => "demo-zahlung-003", "SYNTH-ZA-004" => "demo-zahlung-004", _ => null }));
        }
        ZahlungAnzeigen(rechnungen[0], 1280m, true, "SYNTH-ZA-001");
        ZahlungAnzeigen(rechnungen[1], 300m, true, "SYNTH-ZA-002");
        ZahlungAnzeigen(rechnungen[1], 200m, true, "SYNTH-ZA-003");
        ZahlungAnzeigen(rechnungen[2], 450m, false, "SYNTH-ZA-004");
        var ansichten = rechnungen.Select(r =>
        {
            var offen = kern.OffenerBetrag(r.Id);
            return new Rechnungsansicht(r.Nummer, r.Datum.ToString("yyyy-MM-dd"), Namen[r.Aussteller], Namen[r.Empfaenger],
                Geld(r.Betrag), Geld(offen), Geld(r.Betrag - offen), offen == 0m ? "Bezahlt" : offen == r.Betrag ? "Offen" : "Teilzahlung",
                r.Nachweis.Beleg, r.Nachweis.Quellenort, zahlungen.TryGetValue(r.Id, out var liste) ? liste.AsReadOnly() : Array.Empty<Zahlungsansicht>(), Kontakt(r.Aussteller), Kontakt(r.Empfaenger),
                r.Nummer switch { "RE-2026-1039" => "demo-nord-rechnung-001", "RE-2026-1048" => "demo-nord-rechnung-002", "RE-2026-1051" => "demo-nord-rechnung-003", _ => "import-" + r.Id.ToString("N") },
                r.Aussteller == "DEMO-NORD" ? new(Namen["DEMO-NORD"], "Aussteller") : r.Empfaenger == "DEMO-NORD" ? new(Namen["DEMO-NORD"], "Empfänger") : null,r.Aussteller,r.Empfaenger);
        }).ToArray();
        var richtungVollstaendig=rechnungen.All(r=>(r.Aussteller=="DEMO-NORD")!=(r.Empfaenger=="DEMO-NORD"));
        return new(true, Geld(rechnungen.Sum(r => r.Betrag)), Geld(rechnungen.Sum(r => kern.OffenerBetrag(r.Id))), Array.AsReadOnly(ansichten),
            richtungVollstaendig?Geld(rechnungen.Where(r=>r.Aussteller=="DEMO-NORD").Sum(r=>kern.OffenerBetrag(r.Id))):null,
            richtungVollstaendig?Geld(rechnungen.Where(r=>r.Empfaenger=="DEMO-NORD").Sum(r=>kern.OffenerBetrag(r.Id))):null);
    }

    public static Importansicht Importvorschau(string beispiel)
    {
        using var datei = new MemoryStream(DemoArbeitsmappe.Erstellen(beispiel));
        var profil = new Importprofil("SYNTH-ISD", new Dictionary<string, string>
        {
            [Namen["DEMO-NORD"]] = "DEMO-NORD", [Namen["DEMO-ISD"]] = "DEMO-ISD"
        });
        var bericht = new ExcelImportvorschau().Pruefen(datei, "synthetisch.xlsx", profil);
        return ImportDarstellen(bericht);
    }

    internal static Importansicht ImportDarstellen(Importbericht bericht)
    {
        string? Name(string? referenz) => referenz is null ? null : Namen.GetValueOrDefault(referenz, referenz);
        var zeilen = bericht.Zeilen.Select(z => new Importzeilenansicht(z.Zeile, z.HatFehler,
            z.Rechnung is not null ? "Rechnung" : z.Zahlung is not null ? "Zahlung" : null,
            z.Rechnung is not null ? Geld(z.Rechnung.Betrag) : z.Zahlung is not null ? Geld(z.Zahlung.Betrag) : null,
            (z.Rechnung?.Datum ?? z.Zahlung?.Datum)?.ToString("yyyy-MM-dd"),
            Name(z.Rechnung?.Aussteller ?? z.Zahlung?.Zahler), Name(z.Rechnung?.Empfaenger ?? z.Zahlung?.Empfaenger), z.Zellen, z.Meldungen)).ToArray();
        return new(true, bericht.DateiHash, bericht.HatFehler, zeilen.Length, zeilen.Count(z => z.HatFehler),
            bericht.AlsText(), bericht.Meldungen, Array.AsReadOnly(zeilen));
    }
}
