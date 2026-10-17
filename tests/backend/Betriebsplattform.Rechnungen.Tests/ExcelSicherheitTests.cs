using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Betriebsplattform.Import;
using Xunit;
using static Betriebsplattform.Rechnungen.Tests.SynthetischeArbeitsmappe;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class ExcelSicherheitTests
{
    private static Importbericht Pruefen(byte[] bytes, string name = "synthetisch.xlsx") =>
        new ExcelImportvorschau().Pruefen(new MemoryStream(bytes), name,
            new("SYNTH-ISD", new Dictionary<string, string> { ["Firma A"] = "FIKTIV-A", ["Firma B"] = "FIKTIV-B" }));

    private static void Dateifehler(Importbericht bericht, string code)
    {
        Assert.True(bericht.HatFehler, bericht.AlsText());
        Assert.Empty(bericht.Zeilen);
        Assert.Contains(bericht.Meldungen, m => m.Code == code);
    }

    [Theory]
    [InlineData("test.xls")]
    [InlineData("test.xlsm")]
    [InlineData("test.xlsb")]
    [InlineData("test.csv")]
    [InlineData("test.xlsx.exe")]
    public void AndereDateiformateWerdenAbgewiesen(string name) => Dateifehler(Pruefen(Bauen(Teile(Zeile())), name), "FORMAT");

    [Fact]
    public void UmbenannteBinaerdateiUndAbgeschnittenesZipWerdenAbgewiesen()
    {
        Dateifehler(Pruefen(Encoding.UTF8.GetBytes("KEINE EXCEL-DATEI")), "PAKET");
        var datei = Bauen(Teile(Zeile()));
        Dateifehler(Pruefen(datei[..(datei.Length / 2)]), "PAKET");
    }

    [Theory]
    [InlineData("xl/vbaProject.bin")]
    [InlineData("xl/externalLinks/externalLink1.xml")]
    [InlineData("xl/embeddings/oleObject1.bin")]
    [InlineData("../ausbruch.xml")]
    [InlineData("/absolut.xml")]
    [InlineData("xl/worksheets/sheet2.xml")]
    public void AktiveUndUnbekanntePaketbestandteileWerdenNichtUebernommen(string pfad)
    {
        var teile = Teile(Zeile());
        teile[pfad] = "SYNTHETISCH";
        Dateifehler(Pruefen(Bauen(teile)), "PAKET");
    }

    [Fact]
    public void MakroInhaltstypWirdAuchBeiXlsxDateinameAbgewiesen()
    {
        var teile = Teile(Zeile());
        teile["[Content_Types].xml"] = teile["[Content_Types].xml"].Replace(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", "application/vnd.ms-excel.sheet.macroEnabled.main+xml");
        Dateifehler(Pruefen(Bauen(teile)), "FORMAT");
    }

    [Theory]
    [InlineData("https://example.invalid/keine-abfrage")]
    [InlineData("file:///synthetisch/keine-datei")]
    [InlineData("../worksheets/sheet1.xml")]
    public void UnsichereBeziehungenWerdenOhneZugriffAbgewiesen(string ziel)
    {
        var teile = Teile(Zeile());
        teile["xl/_rels/workbook.xml.rels"] = teile["xl/_rels/workbook.xml.rels"].Replace("worksheets/sheet1.xml", ziel);
        Dateifehler(Pruefen(Bauen(teile)), "BEZIEHUNG");
    }

    [Fact]
    public void ExterneBeziehungMitScheinbarInternemZielWirdAbgewiesen()
    {
        var teile = Teile(Zeile());
        teile["xl/_rels/workbook.xml.rels"] = teile["xl/_rels/workbook.xml.rels"].Replace("Id=\"r1\"", "TargetMode=\"External\" Id=\"r1\"");
        Dateifehler(Pruefen(Bauen(teile)), "BEZIEHUNG");
    }

    [Fact]
    public void DtdUndExterneEntitaetenWerdenNichtAufgeloest()
    {
        var teile = Teile(Zeile());
        teile["xl/worksheets/sheet1.xml"] = "<!DOCTYPE worksheet [<!ENTITY fremd SYSTEM 'file:///synthetisch/niemals-lesen'>]>"
            + teile["xl/worksheets/sheet1.xml"].Replace("Firma A", "&fremd;");
        Dateifehler(Pruefen(Bauen(teile)), "XML");
    }

    [Fact]
    public void FormelnMitGueltigemCacheWerdenNichtAlsBetragUebernommen()
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "G2").AddFirst(new XElement(XName.Get("f", S), "1+1"));
        var bericht = Pruefen(Bauen(Teile(zeile)));
        var ergebnis = Assert.Single(bericht.Zeilen);
        Assert.True(ergebnis.HatFehler);
        Assert.Null(ergebnis.Rechnung);
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "FORMEL" && m.Quelle!.Zelle == "G2");
        Assert.Equal("1+1", ergebnis.Zellen[6].Formel);
        Assert.Equal("100.25", ergebnis.Zellen[6].Originalwert);
    }

    [Theory]
    [InlineData("b", "1")]
    [InlineData("e", "#VALUE!")]
    [InlineData("str", "100.25")]
    public void UnpassendeExcelDatentypenWerdenMarkiert(string typ, string wert)
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "G2").ReplaceWith(Wert("G2", wert, typ));
        var bericht = Pruefen(Bauen(Teile(zeile)));
        Assert.Contains(bericht.Zeilen.Single().Meldungen, m => m.Code == "DATENTYP");
        Assert.Null(bericht.Zeilen.Single().Rechnung);
    }

    [Fact]
    public void DoppelteZipNamenWerdenAbgewiesen()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            for (var i = 0; i < 2; i++)
            {
                using var writer = new StreamWriter(zip.CreateEntry("xl/workbook.xml").Open());
                writer.Write("<workbook />");
            }
        Dateifehler(Pruefen(stream.ToArray()), "PAKET");
    }

    [Theory]
    [InlineData("doppelteZelle")]
    [InlineData("falscheZeile")]
    [InlineData("weitereSpalte")]
    [InlineData("mehrdeutigerInhalt")]
    public void WiderspruechlicheZellquellenFuehrenZuDateifehler(string fall)
    {
        var zeile = Zeile();
        if (fall == "doppelteZelle") zeile.Add(Text("A2", "SYNTH-ZWEI"));
        if (fall == "falscheZeile") zeile.Add(Text("A3", "SYNTH-ZWEI"));
        if (fall == "weitereSpalte") zeile.Add(Text("J2", "SYNTH-ZWEI"));
        if (fall == "mehrdeutigerInhalt") zeile.Elements().Single(c => (string?)c.Attribute("r") == "G2")
            .Add(new XElement(XName.Get("is", S), new XElement(XName.Get("t", S), "9999")));
        Dateifehler(Pruefen(Bauen(Teile(zeile))), "ZELLE");
    }

    [Fact]
    public void VersteckteZeilenUndVerbundeneZellenWerdenNichtStillIgnoriert()
    {
        var zeile = Zeile();
        zeile.Add(new XAttribute("hidden", 1));
        Dateifehler(Pruefen(Bauen(Teile(zeile))), "BLATT");
        var teile = Teile(Zeile());
        var sheet = XDocument.Parse(teile["xl/worksheets/sheet1.xml"]);
        sheet.Root!.Add(new XElement(XName.Get("mergeCells", S)));
        teile["xl/worksheets/sheet1.xml"] = sheet.ToString();
        Dateifehler(Pruefen(Bauen(teile)), "BLATT");
    }

    [Fact]
    public void GroessenZeilenUndXmlTiefenlimitsWerdenDurchgesetzt()
    {
        Dateifehler(Pruefen(new byte[2 * 1024 * 1024 + 1]), "GROESSE");
        Dateifehler(Pruefen(Bauen(Teile(Zeile(1002)))), "ZEILE");
        var teile = Teile(Zeile());
        teile["docProps/core.xml"] = string.Concat(Enumerable.Repeat("<a>", 40)) + string.Concat(Enumerable.Repeat("</a>", 40));
        Dateifehler(Pruefen(Bauen(teile)), "XML");
        teile["docProps/core.xml"] = "<a>" + new string('x', 100000) + "</a>";
        Dateifehler(Pruefen(Bauen(teile)), "GROESSE");
    }

    [Fact]
    public void BeschaedigtesSpaeteresXmlGibtKeineTeilvorschauFrei()
    {
        var teile = Teile(Zeile());
        teile["docProps/core.xml"] = "<defekt>";
        Dateifehler(Pruefen(Bauen(teile)), "XML");
    }

    [Fact]
    public void LesefehlerWerdenOhneInterneDetailsGemeldet()
    {
        using var stream = new Fehlerstream();
        var bericht = new ExcelImportvorschau().Pruefen(stream, "synthetisch.xlsx", new("SYNTH", new Dictionary<string, string>()));
        Dateifehler(bericht, "LESEN");
        Assert.DoesNotContain("NICHT-IN-BERICHT", bericht.AlsText());
        Assert.Null(bericht.DateiHash);
    }

    [Fact]
    public void NichtLesbarerStreamWirdAlsDateifehlerGemeldet()
    {
        var stream = new MemoryStream();
        stream.Dispose();
        var bericht = new ExcelImportvorschau().Pruefen(stream, "synthetisch.xlsx", new("SYNTH", new Dictionary<string, string>()));
        Dateifehler(bericht, "LESEN");
    }

    [Fact]
    public void MehrdeutigeInlineTexteWerdenNichtZusammengefuegt()
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "D2")
            .Element(XName.Get("is", S))!.Add(new XElement(XName.Get("t", S), "ZWEITER-WERT"));
        Dateifehler(Pruefen(Bauen(Teile(zeile))), "TEXT");
    }

    [Fact]
    public void UngueltigeXmlSteuerzeichenWerdenAbgewiesen()
    {
        var teile = Teile(Zeile());
        teile["xl/worksheets/sheet1.xml"] = teile["xl/worksheets/sheet1.xml"].Replace("Firma A", "SYNTH\u001b[31m");
        Dateifehler(Pruefen(Bauen(teile)), "XML");
    }

    [Theory]
    [InlineData("kopf", "KOPF")]
    [InlineData("blattname", "BLATT")]
    [InlineData("fehlt", "PAKET")]
    [InlineData("sharedIndex", "TEXT")]
    [InlineData("doppelteZeile", "ZEILE")]
    [InlineData("doppelteTypen", "FORMAT")]
    public void StrukturelleFehlerWerdenOhneTeilvorschauGemeldet(string fall, string code)
    {
        var teile = Teile(Zeile());
        if (fall == "kopf") teile["xl/worksheets/sheet1.xml"] = teile["xl/worksheets/sheet1.xml"].Replace("Waehrung", "Unbekannt");
        if (fall == "blattname") teile["xl/workbook.xml"] = teile["xl/workbook.xml"].Replace("ISD-Testdaten", "AnderesBlatt");
        if (fall == "fehlt") teile.Remove("xl/workbook.xml");
        if (fall == "doppelteZeile") teile = Teile(Zeile(), Zeile());
        if (fall == "sharedIndex")
        {
            var zeile = Zeile();
            zeile.Elements().Single(c => (string?)c.Attribute("r") == "D2").ReplaceWith(Wert("D2", "999", "s"));
            teile = Teile(zeile);
        }
        if (fall == "doppelteTypen") teile["[Content_Types].xml"] = teile["[Content_Types].xml"].Replace("</Types>",
            "<Override PartName='/xl/worksheets/sheet1.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'/></Types>");
        Dateifehler(Pruefen(Bauen(teile)), code);
    }

    private sealed class Fehlerstream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("NICHT-IN-BERICHT");
    }
}
