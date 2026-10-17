using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Betriebsplattform.Rechnungen.Tests;

internal static class SynthetischeArbeitsmappe
{
    internal const string S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    internal const string R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    internal const string P = "http://schemas.openxmlformats.org/package/2006/relationships";
    internal const string C = "http://schemas.openxmlformats.org/package/2006/content-types";
    internal static readonly string[] Spalten = ["DatensatzId", "Art", "Nummer", "Von", "An", "Datum", "Betrag", "Waehrung", "Beleg"];

    internal static XElement Text(string adresse, string wert) =>
        new(XName.Get("c", S), new XAttribute("r", adresse), new XAttribute("t", "inlineStr"),
            new XElement(XName.Get("is", S), new XElement(XName.Get("t", S), new XAttribute(XNamespace.Xml + "space", "preserve"), wert)));

    internal static XElement Wert(string adresse, string wert, string typ = "n") =>
        new(XName.Get("c", S), new XAttribute("r", adresse), new XAttribute("t", typ), new XElement(XName.Get("v", S), wert));

    internal static XElement Zeile(int nummer = 2, string art = "Rechnung", string id = "SYNTH-01", string betrag = "100.25") =>
        new(XName.Get("row", S), new XAttribute("r", nummer),
            Text($"A{nummer}", id), Text($"B{nummer}", art), Text($"C{nummer}", "SYNTH-RE-01"),
            Text($"D{nummer}", "Firma A"), Text($"E{nummer}", "Firma B"), Text($"F{nummer}", "2026-10-17"),
            Wert($"G{nummer}", betrag), Text($"H{nummer}", "EUR"), Text($"I{nummer}", "SYNTH-BELEG-01"));

    internal static Dictionary<string, string> Teile(params XElement[] zeilen)
    {
        XNamespace s = S, r = R, p = P, c = C;
        var kopf = new XElement(s + "row", new XAttribute("r", 1), Spalten.Select((v, i) => Text($"{(char)('A' + i)}1", v)));
        return new(StringComparer.Ordinal)
        {
            ["[Content_Types].xml"] = new XElement(c + "Types",
                new XElement(c + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(c + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(c + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(c + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))).ToString(),
            ["_rels/.rels"] = new XElement(p + "Relationships", new XElement(p + "Relationship", new XAttribute("Id", "r1"), new XAttribute("Type", R + "/officeDocument"), new XAttribute("Target", "xl/workbook.xml"))).ToString(),
            ["xl/workbook.xml"] = new XElement(s + "workbook", new XElement(s + "sheets", new XElement(s + "sheet", new XAttribute("name", "ISD-Testdaten"), new XAttribute("sheetId", 1), new XAttribute(r + "id", "r1")))).ToString(),
            ["xl/_rels/workbook.xml.rels"] = new XElement(p + "Relationships", new XElement(p + "Relationship", new XAttribute("Id", "r1"), new XAttribute("Type", R + "/worksheet"), new XAttribute("Target", "worksheets/sheet1.xml"))).ToString(),
            ["xl/worksheets/sheet1.xml"] = new XElement(s + "worksheet", new XElement(s + "sheetData", kopf, zeilen)).ToString()
        };
    }

    internal static byte[] Bauen(Dictionary<string, string> teile)
    {
        using var ausgabe = new MemoryStream();
        using (var zip = new ZipArchive(ausgabe, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, xml) in teile)
            {
                var eintrag = zip.CreateEntry(name, CompressionLevel.Fastest);
                eintrag.LastWriteTime = new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero);
                using var writer = new StreamWriter(eintrag.Open(), new UTF8Encoding(false));
                writer.Write(xml);
            }
        return ausgabe.ToArray();
    }
}
