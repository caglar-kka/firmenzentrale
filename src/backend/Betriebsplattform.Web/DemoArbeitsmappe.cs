using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Betriebsplattform.Web;

internal static class DemoArbeitsmappe
{
    internal static byte[] Erstellen(string fall)
    {
        if (fall is not ("fehler" or "gueltig" or "dublette")) throw new ArgumentException("Unbekanntes synthetisches Beispiel.", nameof(fall));
        XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XElement Text(int spalte, int zeile, string wert) => new(s + "c", new XAttribute("r", $"{(char)('A' + spalte)}{zeile}"),
            new XAttribute("t", "inlineStr"), new XElement(s + "is", new XElement(s + "t", wert)));
        XElement Zeile(int zeile, string id, string art, string nummer, string von, string an, string betrag) =>
            new(s + "row", new XAttribute("r", zeile),
                new[] { id, art, nummer, von, an, "2026-10-17", betrag, "EUR", "Beleg-" + zeile }.Select((wert, i) =>
                    i == 6 ? new XElement(s + "c", new XAttribute("r", "G" + zeile), new XElement(s + "v", wert)) : Text(i, zeile, wert)));
        var kopf = new XElement(s + "row", new XAttribute("r", 1),
            new[] { "DatensatzId", "Art", "Nummer", "Von", "An", "Datum", "Betrag", "Waehrung", "Beleg" }.Select((v, i) => Text(i, 1, v)));
        var daten = new XElement(s + "sheetData", kopf,
            Zeile(2, "Zeile-01", "Rechnung", "RE-2026-1063", DemoDaten.Namen["DEMO-NORD"], DemoDaten.Namen["DEMO-ISD"], "100.25"),
            Zeile(3, "Zeile-02", "Zahlung", "", DemoDaten.Namen["DEMO-ISD"], DemoDaten.Namen["DEMO-NORD"], "50"));
        if (fall == "fehler") daten.Add(Zeile(4, "Zeile-03", "Rechnung", "RE-2026-1064", DemoDaten.Namen["DEMO-ISD"], DemoDaten.Namen["DEMO-NORD"], "-5"));
        if (fall == "dublette") daten.Add(Zeile(4, "Zeile-02", "Zahlung", "", DemoDaten.Namen["DEMO-ISD"], DemoDaten.Namen["DEMO-NORD"], "75"));
        var teile = new Dictionary<string, string>
        {
            ["[Content_Types].xml"] = """
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """,
            ["_rels/.rels"] = """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="r1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """,
            ["xl/workbook.xml"] = """
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="ISD-Testdaten" sheetId="1" r:id="r1"/></sheets>
                </workbook>
                """,
            ["xl/_rels/workbook.xml.rels"] = """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="r1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """,
            ["xl/worksheets/sheet1.xml"] = new XElement(s + "worksheet", daten).ToString()
        };
        using var ausgabe = new MemoryStream();
        using (var zip = new ZipArchive(ausgabe, ZipArchiveMode.Create, true))
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
