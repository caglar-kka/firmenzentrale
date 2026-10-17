using Betriebsplattform.Import;
using System.Security.Cryptography;
using System.Xml.Linq;
using Xunit;
using static Betriebsplattform.Rechnungen.Tests.SynthetischeArbeitsmappe;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class ExcelImportvorschauTests
{
    [Fact]
    public void SynthetischeXlsxTeileSindWohlgeformtesXml()
    {
        using var stream = new MemoryStream(Bauen(Teile(Zeile())));
        using var zip = new System.IO.Compression.ZipArchive(stream);
        foreach (var eintrag in zip.Entries)
        {
            using var reader = eintrag.Open();
            _ = System.Xml.Linq.XDocument.Load(reader);
        }
    }
    private static Importprofil Profil() => new("SYNTH-ISD", new Dictionary<string, string> { ["Firma A"] = "FIKTIV-A", ["Firma B"] = "FIKTIV-B" });
    private static Importbericht Pruefen(byte[] bytes, Importprofil? profil = null, string name = "synthetisch.xlsx") =>
        new ExcelImportvorschau().Pruefen(new MemoryStream(bytes), name, profil ?? Profil());

    [Fact]
    public void EchteXlsxVorschauBewahrtHashOriginaleQuellenUndFachwerte()
    {
        var bytes = Bauen(Teile(Zeile()));
        var bericht = Pruefen(bytes);
        Assert.False(bericht.HatFehler, bericht.AlsText());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), bericht.DateiHash);
        var zeile = Assert.Single(bericht.Zeilen);
        Assert.Equal(100.25m, zeile.Rechnung!.Betrag);
        Assert.Equal("FIKTIV-A", zeile.Rechnung.Aussteller);
        Assert.Equal("FIKTIV-B", zeile.Rechnung.Empfaenger);
        var betrag = zeile.Zellen.Single(c => c.Quelle.Zelle == "G2");
        Assert.Equal("100.25", betrag.Originalwert);
        Assert.Equal("n", betrag.Datentyp);
        Assert.Equal(new Zellquelle(bericht.DateiHash!, "ISD-Testdaten", 2, "G2"), betrag.Quelle);
        Assert.Contains(bericht.DateiHash!, zeile.Rechnung.Nachweis.Quellenort);
        Assert.Contains("ISD-Testdaten!I2", zeile.Rechnung.Nachweis.Quellenort);
    }

    [Fact]
    public void ZahlungBleibtUnbestaetigtUndBestehenderKernUnveraendert()
    {
        var kern = new Rechnungskern();
        var rechnung = new Rechnung(Guid.NewGuid(), "SYNTH", "FIKTIV-B", "FIKTIV-A", new(2026, 10, 17), 100m, new("SYNTH", "synthetisch"));
        kern.RechnungErfassen(rechnung);
        var bericht = Pruefen(Bauen(Teile(Zeile(art: "Zahlung"))));
        Assert.False(bericht.HatFehler, bericht.AlsText());
        var zahlung = Assert.Single(bericht.Zeilen).Zahlung!;
        Assert.Equal("FIKTIV-A", zahlung.Zahler);
        Assert.Equal(100m, kern.OffenerBetrag(rechnung.Id));
        Assert.Empty(kern.Bestaetigungen);
        Assert.Empty(kern.Zuordnungen);
        kern.ZahlungErfassen(zahlung);
        Assert.Equal(0m, kern.VerfuegbarerBetrag(zahlung.Id));
    }

    [Fact]
    public void WiederholteVorschauIstDeterministischUndMarkiertDateidublette()
    {
        var bytes = Bauen(Teile(Zeile()));
        var erster = Pruefen(bytes);
        Assert.Equal(erster.Zeilen.Single().Rechnung!.Id, Pruefen(bytes).Zeilen.Single().Rechnung!.Id);
        var profil = new Importprofil("SYNTH-ISD", Profil().Unternehmen, [erster.DateiHash!]);
        var wiederholung = Pruefen(bytes, profil);
        Assert.True(wiederholung.HatFehler);
        Assert.Contains(wiederholung.Meldungen, m => m.Code == "DATEI_DUBLETTE");
    }

    [Fact]
    public void AlleZeilenEinerQuellDubletteWerdenGesperrt()
    {
        var bericht = Pruefen(Bauen(Teile(Zeile(), Zeile(3))));
        Assert.Equal(2, bericht.Zeilen.Count);
        Assert.All(bericht.Zeilen, z =>
        {
            Assert.True(z.HatFehler);
            Assert.Null(z.Rechnung);
            Assert.Contains(z.Meldungen, m => m.Code == "QUELL_DUBLETTE");
        });
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("NaN")]
    [InlineData("1E2")]
    [InlineData("792281625142643375935439503.36")]
    public void FehlerhafteBetraegeWerdenMitZellquelleAbgewiesen(string betrag)
    {
        var bericht = Pruefen(Bauen(Teile(Zeile(betrag: betrag))));
        var zeile = Assert.Single(bericht.Zeilen);
        Assert.True(zeile.HatFehler);
        Assert.Null(zeile.Rechnung);
        Assert.Contains(zeile.Meldungen, m => m.Code == "BETRAG" && m.Quelle!.Zelle == "G2");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("")]
    [InlineData("€")]
    public void WaehrungWirdNichtGeraten(string waehrung)
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "H2").ReplaceWith(Text("H2", waehrung));
        var bericht = Pruefen(Bauen(Teile(zeile)));
        Assert.Contains(bericht.Zeilen.Single().Meldungen, m => m.Code == "WAEHRUNG");
        Assert.Null(bericht.Zeilen.Single().Rechnung);
    }

    [Theory]
    [InlineData("100,25", "100.25")]
    [InlineData("0001,2", "1.20")]
    [InlineData("792281625142643375935439503,35", "792281625142643375935439503.35")]
    public void DeutscheTextbetraegeWerdenExaktUndMitWarnungGelesen(string original, string erwartet)
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "G2").ReplaceWith(Text("G2", original));
        var ergebnis = Pruefen(Bauen(Teile(zeile))).Zeilen.Single();
        Assert.False(ergebnis.HatFehler);
        Assert.Equal(decimal.Parse(erwartet, System.Globalization.CultureInfo.InvariantCulture), ergebnis.Rechnung!.Betrag);
        Assert.Equal(original, ergebnis.Zellen[6].Originalwert);
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "BETRAG_TEXT");
    }

    [Theory]
    [InlineData("1.234,56")]
    [InlineData("100.25")]
    [InlineData("1,234")]
    [InlineData("1,00 EUR")]
    public void MehrdeutigeTextbetraegeWerdenNichtNormalisiert(string text)
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "G2").ReplaceWith(Text("G2", text));
        var ergebnis = Pruefen(Bauen(Teile(zeile))).Zeilen.Single();
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "BETRAG");
        Assert.Equal(text, ergebnis.Zellen[6].Originalwert);
        Assert.Null(ergebnis.Rechnung);
    }

    [Fact]
    public void SharedStringsWerdenAufgeloestUndIhrOriginalindexBleibtErhalten()
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "D2").ReplaceWith(Wert("D2", "0", "s"));
        var teile = Teile(zeile);
        teile["xl/sharedStrings.xml"] = $"<sst xmlns='{S}'><si><r><t>Firma </t></r><r><t>A</t></r></si></sst>";
        teile["xl/_rels/workbook.xml.rels"] = teile["xl/_rels/workbook.xml.rels"].Replace("</Relationships>",
            $"<Relationship Id='r2' Type='{R}/sharedStrings' Target='sharedStrings.xml'/></Relationships>");
        teile["[Content_Types].xml"] = teile["[Content_Types].xml"].Replace("</Types>",
            "<Override PartName='/xl/sharedStrings.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml'/></Types>");
        var bericht = Pruefen(Bauen(teile));
        Assert.False(bericht.HatFehler, bericht.AlsText());
        var original = bericht.Zeilen.Single().Zellen[3];
        Assert.Equal("0", original.Originalwert);
        Assert.Equal("Firma A", original.Text);
        Assert.Equal("s", original.Datentyp);
    }

    [Fact]
    public void BekannteQuellUndRechnungskennungenWerdenAuchInAndererDateiErkannt()
    {
        var profil = new Importprofil("SYNTH-ISD", Profil().Unternehmen,
            bekannteQuellschluessel: [new("SYNTH-ISD", "Zahlung", "SYNTH-01")],
            bekannteRechnungen: [new("FIKTIV-A", "SYNTH-RE-01")]);
        var rechnung = Pruefen(Bauen(Teile(Zeile())), profil).Zeilen.Single();
        Assert.Contains(rechnung.Meldungen, m => m.Code == "RECHNUNGS_DUBLETTE");
        Assert.Null(rechnung.Rechnung);
        var zahlung = Pruefen(Bauen(Teile(Zeile(art: "Zahlung"))), profil).Zeilen.Single();
        Assert.Contains(zahlung.Meldungen, m => m.Code == "QUELL_DUBLETTE");
        Assert.Null(zahlung.Zahlung);
    }

    [Fact]
    public void RechnungsdublettenMitVerschiedenenQuellIdsSperrenBeideZeilen()
    {
        var bericht = Pruefen(Bauen(Teile(Zeile(), Zeile(3, id: "SYNTH-ANDERS"))));
        Assert.All(bericht.Zeilen, z =>
        {
            Assert.Contains(z.Meldungen, m => m.Code == "RECHNUNGS_DUBLETTE");
            Assert.Null(z.Rechnung);
        });
    }

    [Fact]
    public void WiderspruechlicheQuellDubletteMitAnderemBetragWirdNichtUeberschrieben()
    {
        var bericht = Pruefen(Bauen(Teile(Zeile(art: "Zahlung"), Zeile(3, art: "Zahlung", betrag: "50"))));
        Assert.All(bericht.Zeilen, z => Assert.Contains(z.Meldungen, m => m.Code == "QUELL_DUBLETTE"));
        Assert.Equal("100.25", bericht.Zeilen[0].Zellen[6].Originalwert);
        Assert.Equal("50", bericht.Zeilen[1].Zellen[6].Originalwert);
        Assert.All(bericht.Zeilen, z => Assert.Null(z.Zahlung));
    }

    [Theory]
    [InlineData("D2", "Unbekannte Firma", "UNTERNEHMEN")]
    [InlineData("B2", "ISD", "ART")]
    [InlineData("F2", "2026-02-30", "DATUM")]
    [InlineData("I2", "", "PFLICHTWERT")]
    [InlineData("A2", "", "PFLICHTWERT")]
    public void FachfehlerWerdenZellgenauMarkiert(string adresse, string text, string code)
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == adresse).ReplaceWith(Text(adresse, text));
        var ergebnis = Pruefen(Bauen(Teile(zeile))).Zeilen.Single();
        Assert.Contains(ergebnis.Meldungen, m => m.Code == code && m.Quelle!.Zelle == adresse);
        Assert.Null(ergebnis.Rechnung);
    }

    [Fact]
    public void FehlendeZelleUndNumerischeKennungWerdenNichtGeraten()
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "I2").Remove();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "A2").ReplaceWith(Wert("A2", "123"));
        var ergebnis = Pruefen(Bauen(Teile(zeile))).Zeilen.Single();
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "DATENTYP" && m.Quelle!.Zelle == "A2");
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "PFLICHTWERT" && m.Quelle!.Zelle == "I2");
        Assert.Equal("fehlend", ergebnis.Zellen[8].Datentyp);
    }

    [Fact]
    public void DatumstypUndFormatwarnungenBleibenNachvollziehbar()
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "F2").ReplaceWith(Wert("F2", "2026-10-17", "d"));
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "D2").ReplaceWith(Text("D2", " Firma A "));
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "G2").Add(new XAttribute("s", "1"));
        var ergebnis = Pruefen(Bauen(Teile(zeile))).Zeilen.Single();
        Assert.False(ergebnis.HatFehler);
        Assert.Equal(new DateOnly(2026, 10, 17), ergebnis.Rechnung!.Datum);
        Assert.Equal(" Firma A ", ergebnis.Zellen[3].Originalwert);
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "LEERZEICHEN");
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "FORMATIERUNG");
        Assert.Equal("1", ergebnis.Zellen[6].Formatindex);
    }

    [Fact]
    public void NumerischesExcelSeriendatumWirdNichtAlsKalenderdatumGeraten()
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "F2").ReplaceWith(Wert("F2", "46312"));
        var ergebnis = Pruefen(Bauen(Teile(zeile))).Zeilen.Single();
        Assert.Contains(ergebnis.Meldungen, m => m.Code == "DATUM");
        Assert.Null(ergebnis.Rechnung);
    }

    [Fact]
    public void MappingUndBerichtSindGegenNachtraeglicheAenderungenGeschuetzt()
    {
        var mapping = new Dictionary<string, string> { ["Firma A"] = "FIKTIV-A", ["Firma B"] = "FIKTIV-B" };
        var profil = new Importprofil("SYNTH", mapping);
        mapping["Firma A"] = "FIKTIV-FALSCH";
        var bericht = Pruefen(Bauen(Teile(Zeile())), profil);
        Assert.Equal("FIKTIV-A", bericht.Zeilen.Single().Rechnung!.Aussteller);
        Assert.Throws<NotSupportedException>(() => ((IList<Vorschauzeile>)bericht.Zeilen).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<Originalzelle>)bericht.Zeilen[0].Zellen).Clear());
    }

    [Fact]
    public void LeereVorlageErzeugtWarnungUndKeineFachobjekte()
    {
        var bericht = Pruefen(Bauen(Teile()));
        Assert.False(bericht.HatFehler);
        Assert.Empty(bericht.Zeilen);
        Assert.Contains(bericht.Meldungen, m => m.Code == "LEER");
    }

    [Theory]
    [InlineData("D2", true)]
    [InlineData("E2", false)]
    public void IsdIstVorgangsbezogenerGeschaeftspartnerUndKeinePauschaleRolle(string zelle, bool aussteller)
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == zelle).ReplaceWith(Text(zelle, "ISD"));
        var profil = new Importprofil("SYNTH", new Dictionary<string, string>
        {
            ["Firma A"] = "FIKTIV-A", ["Firma B"] = "FIKTIV-B", ["ISD"] = "FIKTIV-ISD"
        });
        var bericht = Pruefen(Bauen(Teile(zeile)), profil);
        Assert.False(bericht.HatFehler, bericht.AlsText());
        var ergebnis = bericht.Zeilen.Single();
        Assert.Equal("FIKTIV-ISD", aussteller ? ergebnis.Rechnung!.Aussteller : ergebnis.Rechnung!.Empfaenger);
        Assert.Equal("ISD", ergebnis.Zellen.Single(c => c.Quelle.Zelle == zelle).Originalwert);
        Assert.Equal("ISD-Testdaten", ergebnis.Zellen[0].Quelle.Blatt);
        Assert.Contains("Geschäftspartner", bericht.AlsText());
        Assert.Contains("Firmenname", bericht.AlsText());
    }

    [Fact]
    public void VorschauBenoetigtKeinSeekUndLaesstDenEingabestreamOffen()
    {
        using var stream = new NichtSuchbarerStream(Bauen(Teile(Zeile())));
        var bericht = new ExcelImportvorschau().Pruefen(stream, "SYNTHETISCH.XLSX", Profil());
        Assert.False(bericht.HatFehler, bericht.AlsText());
        Assert.True(stream.CanRead);
    }

    private sealed class NichtSuchbarerStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }

    [Fact]
    public void PruefberichtIstVerstaendlichUndGibtKeineUngeprueftenZelltexteAus()
    {
        var zeile = Zeile();
        zeile.Elements().Single(c => (string?)c.Attribute("r") == "D2").ReplaceWith(Text("D2", "SYNTH-UNBEKANNT\nGEFAELSCHTE-MELDUNG"));
        var bericht = Pruefen(Bauen(Teile(zeile)));
        Assert.Contains("keine Buchung", bericht.AlsText());
        Assert.Contains("ISD-Testdaten!D2", bericht.AlsText());
        Assert.DoesNotContain("SYNTH-UNBEKANNT", bericht.AlsText());
        Assert.DoesNotContain("GEFAELSCHTE-MELDUNG", bericht.AlsText());
        Assert.DoesNotContain('\u001b', bericht.AlsText());
        Assert.Contains("Keine Importfreigabe", bericht.AlsText());
    }

    [Fact]
    public void SynthetischesBeispielErzeugtNachpruefbareLokaleArtefakte()
    {
        var erste = Zeile();
        var zweite = Zeile(3, "Zahlung", "SYNTH-ZAHLUNG-01", "50");
        var dritte = Zeile(4, id: "SYNTH-FEHLER-01", betrag: "-5");
        dritte.Elements().Single(c => (string?)c.Attribute("r") == "C4").ReplaceWith(Text("C4", "SYNTH-RE-02"));
        var bytes = Bauen(Teile(erste, zweite, dritte));
        var bericht = Pruefen(bytes);
        Assert.Equal(3, bericht.Zeilen.Count);
        Assert.Equal(2, bericht.Zeilen.Count(z => !z.HatFehler));
        Assert.True(bericht.HatFehler);
        // Ausschließlich erzeugte künstliche Daten innerhalb des ignorierten bin-Verzeichnisses.
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "MVP-02-synthetisch.xlsx"), bytes);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "MVP-02-Pruefbericht.txt"), bericht.AlsText());
        Assert.Equal(bericht.AlsText(), File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MVP-02-Pruefbericht.txt")));
    }
}
