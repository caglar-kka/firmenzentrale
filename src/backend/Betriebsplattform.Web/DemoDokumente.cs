using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Betriebsplattform.Web;

public sealed record Dokumentansicht(string Id, string UnternehmenId, string VorgangId, string Art,
    string Dateiname, string Datum, int Version, int Bytezahl, string DateiHash, string Quelle, string? ZahlungId,
    string Inhaltstyp = "application/pdf", string Vorschau = "pdf", string? Titel = null);
public sealed record Dokumentenliste(bool Synthetisch, string VorgangId, IReadOnlyList<Dokumentansicht> Dokumente);

// Öffentlich gedachte synthetische Demo: kein Dateisystemzugriff, Upload oder produktives Archiv.
public static class DemoDokumente
{
    private sealed record Eintrag(Dokumentansicht Ansicht, byte[] Bytes);
    private const string Vorgang = "demo-nord-rechnung-002";
    private static readonly Eintrag[] Katalog =
    [
        Erstellen("original-002-v1", "Originalrechnung", "Rechnung_Oktober_2026.pdf", null,
            "ELBPFAD BUEROAUSSTATTUNG GMBH", "Originalrechnung RE-2026-1048",
            "Rechnung an: Fjordfalter Objektpflege GmbH", "Rechnungsdatum: 17.10.2026",
            "Bueromaterial und Arbeitsplatzbedarf", "Rechnungsbetrag: 960,00 EUR"),
        Erstellen("zahlung-002-v1", "Zahlungsbeleg", "Zahlungsbeleg_17-10-2026.pdf", "demo-zahlung-002",
            "ZAHLUNGSBELEG", "Rechnung RE-2026-1048", "Zahler: Fjordfalter Objektpflege GmbH",
            "Zahlungsempfaenger: Elbpfad Bueroausstattung GmbH", "Zahlungsdatum: 17.10.2026",
            "Zahlungsbetrag: 300,00 EUR", "Fuer diese Rechnung: 300,00 EUR")
    ];

    public static Dokumentenliste? Liste(string vorgangId) => vorgangId is
        "demo-nord-rechnung-001" or Vorgang or "demo-nord-rechnung-003"
        ? new(true, vorgangId, Array.AsReadOnly(Katalog.Where(d => d.Ansicht.VorgangId == vorgangId).Select(d => d.Ansicht).ToArray()))
        : null;

    public static Dokumentansicht? Metadaten(string vorgangId, string id) =>
        Katalog.SingleOrDefault(d => d.Ansicht.VorgangId == vorgangId && d.Ansicht.Id == id)?.Ansicht;

    public static byte[]? Datei(string vorgangId, string id) =>
        Katalog.SingleOrDefault(d => d.Ansicht.VorgangId == vorgangId && d.Ansicht.Id == id)?.Bytes.ToArray();

    private static Eintrag Erstellen(string id, string art, string name, string? zahlungId, params string[] zeilen)
    {
        var bytes = TestPdf(zeilen);
        return new(new(id, "DEMO-NORD", Vorgang, art, name, "2026-10-17", 1, bytes.Length,
            Convert.ToHexString(SHA256.HashData(bytes)), "synthetisch/DOK-01/" + id, zahlungId), bytes);
    }

    // Ausschließlich feste ASCII-Testtexte, eine A4-Seite, Standardschrift.
    // Kein allgemeiner PDF-Writer und keine Verarbeitung fremder PDF-Inhalte.
    internal static byte[] TestPdf(string[] zeilen)
    {
        string Esc(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var stream = "BT /F1 12 Tf 18 TL 42 790 Td\n" + string.Join("\n", zeilen.Select(z => $"({Esc(z)}) Tj T*")) + "\nET\n";
        var objekte = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objekte.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append($"{i + 1} 0 obj\n{objekte[i]}\nendobj\n");
        }
        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets) pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        pdf.Append($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
