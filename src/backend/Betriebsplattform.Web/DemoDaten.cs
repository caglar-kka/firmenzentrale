using System.Globalization;
using Betriebsplattform.Import;
using Betriebsplattform.Rechnungen;

namespace Betriebsplattform.Web;

public static class DemoDaten
{
    private static readonly DateOnly Datum = new(2026, 10, 17);
    private static readonly DateTimeOffset ErfasstAm = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<string, string> Namen = new Dictionary<string, string>
    {
        ["DEMO-NORD"] = "Beispielbetrieb Nord", ["DEMO-ISD"] = "ISD (synthetischer Geschäftspartner)", ["DEMO-WEST"] = "Musterbetrieb West"
    };
    private static string Geld(decimal wert) => wert.ToString("N2", CultureInfo.GetCultureInfo("de-DE")) + " €";

    public static Uebersicht UebersichtErstellen()
    {
        // Den nicht nebenläufigen Kern nur lokal aufbauen. Danach ausschließlich unveränderliche Ansichten teilen.
        var kern = new Rechnungskern();
        var rechnungen = new[]
        {
            new Rechnung(Guid.NewGuid(), "SYNTH-2026-001", "DEMO-NORD", "DEMO-ISD", Datum, 1280m, new("SYNTH-RE-001", "synthetisch/Rechnung-001")),
            new Rechnung(Guid.NewGuid(), "SYNTH-2026-002", "DEMO-ISD", "DEMO-NORD", Datum, 960m, new("SYNTH-RE-002", "synthetisch/Rechnung-002")),
            new Rechnung(Guid.NewGuid(), "SYNTH-2026-003", "DEMO-NORD", "DEMO-WEST", Datum, 450m, new("SYNTH-RE-003", "synthetisch/Rechnung-003"))
        };
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
                Geld(bestaetigt ? betrag : 0m), bestaetigt ? "Bestätigt (synthetisch)" : "Zur Prüfung", zahlung.Nachweis.Beleg, zahlung.Nachweis.Quellenort));
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
                r.Nachweis.Beleg, r.Nachweis.Quellenort, zahlungen[r.Id].AsReadOnly());
        }).ToArray();
        return new(true, Geld(rechnungen.Sum(r => r.Betrag)), Geld(rechnungen.Sum(r => kern.OffenerBetrag(r.Id))), Array.AsReadOnly(ansichten));
    }

    public static Importansicht Importvorschau(string beispiel)
    {
        using var datei = new MemoryStream(DemoArbeitsmappe.Erstellen(beispiel));
        var profil = new Importprofil("SYNTH-ISD", new Dictionary<string, string>
        {
            ["Beispielbetrieb Nord"] = "DEMO-NORD", ["ISD"] = "DEMO-ISD"
        });
        var bericht = new ExcelImportvorschau().Pruefen(datei, "synthetisch.xlsx", profil);
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
