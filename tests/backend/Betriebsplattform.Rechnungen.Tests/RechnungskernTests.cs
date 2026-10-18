using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class RechnungskernTests
{
    private readonly Rechnungskern kern = new();
    private static readonly DateOnly Datum = new(2026, 10, 17);
    private static readonly DateTimeOffset Zeitpunkt = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    private static Nachweis Quelle(string beleg = "SYNTH-BELEG-01") => new(beleg, "synthetisch/Fall-01");

    private Rechnung Rechnung(decimal betrag = 100m, string aussteller = "FIKTIV-LIEFERANT-A")
    {
        var rechnung = new Rechnung(Guid.NewGuid(), "SYNTH-RE-01", aussteller,
            "FIKTIV-KUNDE-A", Datum, betrag, Quelle());
        kern.RechnungErfassen(rechnung);
        return rechnung;
    }

    private Zahlung Zahlung(decimal betrag = 100m, bool bestaetigt = true,
        string empfaenger = "FIKTIV-LIEFERANT-A")
    {
        var zahlung = new Zahlung(Guid.NewGuid(), "FIKTIV-KUNDE-A", empfaenger, Datum, betrag, Quelle());
        kern.ZahlungErfassen(zahlung);
        if (bestaetigt)
            kern.ZahlungBestaetigen(zahlung.Id, Quelle("SYNTH-BANK-01"), "TEST-PRUEFER", Zeitpunkt);
        return zahlung;
    }

    private Zahlungszuordnung Zuordnen(Rechnung rechnung, Zahlung zahlung, decimal betrag, Guid? id = null) =>
        kern.Zuordnen(id ?? Guid.NewGuid(), rechnung.Id, zahlung.Id, betrag,
            Quelle("SYNTH-ZUORDNUNG-01"), "TEST-PRUEFER", Zeitpunkt);

    [Fact]
    public void NeueRechnungIstVollstaendigOffen() => Assert.Equal(100m, kern.OffenerBetrag(Rechnung().Id));

    [Theory]
    [InlineData("100.00", "0.00")]
    [InlineData("30.25", "69.75")]
    [InlineData("0.01", "99.99")]
    public void ZahlungReduziertOffenenBetragExakt(string zuordnung, string rest)
    {
        var rechnung = Rechnung();
        var betrag = decimal.Parse(zuordnung, System.Globalization.CultureInfo.InvariantCulture);
        var zahlung = Zahlung(betrag);
        Zuordnen(rechnung, zahlung, betrag);
        Assert.Equal(decimal.Parse(rest, System.Globalization.CultureInfo.InvariantCulture), kern.OffenerBetrag(rechnung.Id));
        Assert.Equal(0m, kern.VerfuegbarerBetrag(zahlung.Id));
    }

    [Fact]
    public void MehrereZahlungenBegleichenEineRechnung()
    {
        var rechnung = Rechnung();
        Zuordnen(rechnung, Zahlung(30.10m), 30.10m);
        Zuordnen(rechnung, Zahlung(69.90m), 69.90m);
        Assert.Equal(0m, kern.OffenerBetrag(rechnung.Id));
    }

    [Fact]
    public void SammelzahlungBegleichtMehrereRechnungen()
    {
        var erste = Rechnung(40m);
        var zweite = Rechnung(60m);
        var zahlung = Zahlung();
        Zuordnen(erste, zahlung, 40m);
        Zuordnen(zweite, zahlung, 60m);
        Assert.Equal(0m, kern.OffenerBetrag(erste.Id));
        Assert.Equal(0m, kern.OffenerBetrag(zweite.Id));
        Assert.Equal(0m, kern.VerfuegbarerBetrag(zahlung.Id));
    }

    [Fact]
    public void UeberzahlungBleibtAlsNichtZugeordneterRestErhalten()
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung(120m);
        Zuordnen(rechnung, zahlung, 100m);
        Assert.Equal(0m, kern.OffenerBetrag(rechnung.Id));
        Assert.Equal(20m, kern.VerfuegbarerBetrag(zahlung.Id));
        Assert.Equal(120m, zahlung.Betrag);
    }

    [Fact]
    public void UnbestaetigteZahlungVeraendertKeineSalden()
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung(bestaetigt: false);
        Assert.Throws<InvalidOperationException>(() => Zuordnen(rechnung, zahlung, 50m));
        Assert.Equal(100m, kern.OffenerBetrag(rechnung.Id));
        Assert.Equal(0m, kern.VerfuegbarerBetrag(zahlung.Id));
        Assert.Empty(kern.Zuordnungen);
        kern.ZahlungBestaetigen(zahlung.Id, Quelle("SYNTH-BANK-02"), "TEST-PRUEFER", Zeitpunkt);
        Assert.Equal(100m, kern.OffenerBetrag(rechnung.Id));
        Zuordnen(rechnung, zahlung, 50m);
        Assert.Equal(50m, kern.OffenerBetrag(rechnung.Id));
    }

    [Fact]
    public void DoppeltesPaarAuchMitNeuerZuordnungsIdWirdAbgewiesen()
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung();
        Zuordnen(rechnung, zahlung, 20m);
        Assert.Throws<InvalidOperationException>(() => Zuordnen(rechnung, zahlung, 20m));
        Assert.Equal(80m, kern.OffenerBetrag(rechnung.Id));
        Assert.Equal(80m, kern.VerfuegbarerBetrag(zahlung.Id));
        Assert.Single(kern.Zuordnungen);
    }

    [Fact]
    public void WiederverwendeteZuordnungsIdWirdAuchBeiAnderemPaarAbgewiesen()
    {
        var erste = Rechnung();
        var zweite = Rechnung();
        var zahlung = Zahlung();
        var id = Guid.NewGuid();
        Zuordnen(erste, zahlung, 20m, id);
        Assert.Throws<InvalidOperationException>(() => Zuordnen(zweite, zahlung, 20m, id));
        Assert.Equal(100m, kern.OffenerBetrag(zweite.Id));
        Assert.Equal(80m, kern.VerfuegbarerBetrag(zahlung.Id));
    }

    [Theory]
    [InlineData(101, 200)]
    [InlineData(51, 50)]
    public void ZuordnungUeberRechnungsOderZahlungsrestIstAtomarAbgewiesen(int betrag, int zahlbetrag)
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung(zahlbetrag);
        Assert.Throws<InvalidOperationException>(() => Zuordnen(rechnung, zahlung, betrag));
        Assert.Equal(100m, kern.OffenerBetrag(rechnung.Id));
        Assert.Equal(zahlbetrag, kern.VerfuegbarerBetrag(zahlung.Id));
        Assert.Empty(kern.Zuordnungen);
    }

    [Fact]
    public void SammelzahlungKannNichtMehrfachAusgegebenWerden()
    {
        var erste = Rechnung(60m);
        var zweite = Rechnung(60m);
        var zahlung = Zahlung();
        Zuordnen(erste, zahlung, 60m);
        Assert.Throws<InvalidOperationException>(() => Zuordnen(zweite, zahlung, 60m));
        Assert.Equal(60m, kern.OffenerBetrag(zweite.Id));
        Assert.Equal(40m, kern.VerfuegbarerBetrag(zahlung.Id));
    }

    [Fact]
    public void FremderZahlungsempfaengerWirdAbgewiesen()
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung(empfaenger: "FIKTIV-ANDERER-LIEFERANT");
        Assert.Throws<InvalidOperationException>(() => Zuordnen(rechnung, zahlung, 10m));
        Assert.Empty(kern.Zuordnungen);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("0.001")]
    public void UngueltigeBetraegeWerdenNichtGerundetOderUebernommen(string text)
    {
        var betrag = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentOutOfRangeException>(() => Rechnung(betrag));
        Assert.Throws<ArgumentOutOfRangeException>(() => Zahlung(betrag));
        var rechnung = Rechnung();
        var zahlung = Zahlung();
        Assert.Throws<ArgumentOutOfRangeException>(() => Zuordnen(rechnung, zahlung, betrag));
        Assert.Empty(kern.Zuordnungen);
    }

    [Fact]
    public void BetraegeOhnePlatzFuerExakteCentWerdenAbgewiesen()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rechnung(decimal.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => Zahlung(decimal.MaxValue));
    }

    [Fact]
    public void ObergrenzeBehaeltAuchBeiEinemCentTeilzahlungExakteSalden()
    {
        const decimal maximum = 792_281_625_142_643_375_935_439_503.35m;
        var rechnung = Rechnung(maximum);
        var zahlung = Zahlung(maximum);
        var zweiteRechnung = Rechnung(0.01m);
        Zuordnen(rechnung, Zahlung(0.01m), 0.01m);
        Assert.Equal(792_281_625_142_643_375_935_439_503.34m, kern.OffenerBetrag(rechnung.Id));
        Zuordnen(rechnung, zahlung, maximum - 0.01m);
        Assert.Equal(0.01m, kern.VerfuegbarerBetrag(zahlung.Id));
        Zuordnen(zweiteRechnung, zahlung, 0.01m);
        Assert.Equal(0m, kern.VerfuegbarerBetrag(zahlung.Id));
        Assert.Equal(0m, kern.OffenerBetrag(rechnung.Id));
    }

    [Fact]
    public void WeitereZahlungDarfBereitsTeilbezahlteRechnungNichtUeberzahlen()
    {
        var rechnung = Rechnung();
        Zuordnen(rechnung, Zahlung(70m), 70m);
        var zweite = Zahlung(40m);
        Assert.Throws<InvalidOperationException>(() => Zuordnen(rechnung, zweite, 40m));
        Assert.Equal(30m, kern.OffenerBetrag(rechnung.Id));
        Assert.Equal(40m, kern.VerfuegbarerBetrag(zweite.Id));
        Assert.Single(kern.Zuordnungen);
    }

    [Fact]
    public void ZahlungDurchDrittenBleibtMitAbweichendemZahlerNachvollziehbar()
    {
        var rechnung = Rechnung();
        var zahlung = new Zahlung(Guid.NewGuid(), "FIKTIV-DRITTER", rechnung.Aussteller, Datum, 100m, Quelle());
        kern.ZahlungErfassen(zahlung);
        kern.ZahlungBestaetigen(zahlung.Id, Quelle("SYNTH-BANK-DRITTER"), "TEST", Zeitpunkt);
        Zuordnen(rechnung, zahlung, 100m);
        Assert.Equal(0m, kern.OffenerBetrag(rechnung.Id));
        Assert.NotEqual(rechnung.Empfaenger, zahlung.Zahler);
    }

    [Fact]
    public void DoppelteErfassungUndBestaetigungUeberschreibenKeineOriginale()
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung();
        Assert.Throws<InvalidOperationException>(() => kern.RechnungErfassen(rechnung));
        Assert.Throws<InvalidOperationException>(() => kern.ZahlungErfassen(zahlung));
        Assert.Throws<InvalidOperationException>(() => kern.ZahlungBestaetigen(zahlung.Id, Quelle("ANDERER-BELEG"), "ANDERER-PRUEFER", Zeitpunkt));
        Assert.Equal("SYNTH-BANK-01", kern.Bestaetigungen.Single().Nachweis.Beleg);
        Assert.Equal(100m, kern.OffenerBetrag(rechnung.Id));
    }

    [Fact]
    public void HerkunftUndAuditSindNachvollziehbarUndListenSindSnapshots()
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung();
        var vorher = kern.Zuordnungen;
        var zuordnung = Zuordnen(rechnung, zahlung, 25m);
        Assert.Empty(vorher);
        Assert.Equal(rechnung.Id, zuordnung.RechnungId);
        Assert.Equal(zahlung.Id, zuordnung.ZahlungId);
        Assert.Equal(25m, zuordnung.Betrag);
        Assert.Equal("TEST-PRUEFER", zuordnung.ErfasstVon);
        Assert.Equal(Zeitpunkt, zuordnung.ErfasstAm);
        Assert.Equal("synthetisch/Fall-01", zuordnung.Nachweis.Quellenort);
        Assert.Equal("SYNTH-RE-01", rechnung.Nummer);
        Assert.Equal(Datum, rechnung.Datum);
        Assert.Equal(Datum, zahlung.Datum);
        Assert.Equal(rechnung.Empfaenger, zahlung.Zahler);
        Assert.Equal(rechnung.Aussteller, zahlung.Empfaenger);
        var bestaetigung = Assert.Single(kern.Bestaetigungen);
        Assert.Equal(zahlung.Id, bestaetigung.ZahlungId);
        Assert.Equal("TEST-PRUEFER", bestaetigung.ErfasstVon);
        Assert.Equal(Zeitpunkt, bestaetigung.ErfasstAm);
        Assert.Equal("SYNTH-BANK-01", bestaetigung.Nachweis.Beleg);
    }

    [Fact]
    public void UnbekannteReferenzenWerdenAbgewiesen()
    {
        var rechnung = Rechnung();
        var zahlung = Zahlung();
        Assert.Throws<KeyNotFoundException>(() => kern.OffenerBetrag(Guid.NewGuid()));
        Assert.Throws<KeyNotFoundException>(() => kern.VerfuegbarerBetrag(Guid.NewGuid()));
        Assert.Throws<KeyNotFoundException>(() => kern.ZahlungBestaetigen(Guid.NewGuid(), Quelle(), "TEST", Zeitpunkt));
        Assert.Throws<KeyNotFoundException>(() => kern.Zuordnen(Guid.NewGuid(), Guid.NewGuid(), zahlung.Id, 1m, Quelle(), "TEST", Zeitpunkt));
        Assert.Throws<KeyNotFoundException>(() => kern.Zuordnen(Guid.NewGuid(), rechnung.Id, Guid.NewGuid(), 1m, Quelle(), "TEST", Zeitpunkt));
        Assert.Empty(kern.Zuordnungen);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void FehlendePflichttexteWerdenAbgewiesen(string? text)
    {
        Assert.Throws<ArgumentException>(() => new Nachweis(text!, "synthetisch"));
        Assert.Throws<ArgumentException>(() => new Nachweis("BELEG", text!));
        Assert.Throws<ArgumentException>(() => new Rechnung(Guid.NewGuid(), text!, "A", "B", Datum, 1m, Quelle()));
        Assert.Throws<ArgumentException>(() => new Rechnung(Guid.NewGuid(), "RE", text!, "B", Datum, 1m, Quelle()));
        Assert.Throws<ArgumentException>(() => new Rechnung(Guid.NewGuid(), "RE", "A", text!, Datum, 1m, Quelle()));
        Assert.Throws<ArgumentException>(() => new Zahlung(Guid.NewGuid(), text!, "B", Datum, 1m, Quelle()));
        Assert.Throws<ArgumentException>(() => new Zahlung(Guid.NewGuid(), "A", text!, Datum, 1m, Quelle()));
        var rechnung = Rechnung();
        var zahlung = Zahlung(bestaetigt: false);
        Assert.Throws<ArgumentException>(() => kern.ZahlungBestaetigen(zahlung.Id, Quelle(), text!, Zeitpunkt));
        Assert.Empty(kern.Bestaetigungen);
        kern.ZahlungBestaetigen(zahlung.Id, Quelle(), "TEST", Zeitpunkt);
        Assert.Throws<ArgumentException>(() => kern.Zuordnen(Guid.NewGuid(), rechnung.Id, zahlung.Id, 1m, Quelle(), text!, Zeitpunkt));
        Assert.Empty(kern.Zuordnungen);
    }

    [Fact]
    public void FehlendeIdsDatenNachweiseUndZeitpunkteWerdenAbgewiesen()
    {
        Assert.Throws<ArgumentException>(() => new Rechnung(Guid.Empty, "RE", "A", "B", Datum, 1m, Quelle()));
        Assert.Throws<ArgumentException>(() => new Zahlung(Guid.Empty, "A", "B", Datum, 1m, Quelle()));
        Assert.Throws<ArgumentException>(() => new Rechnung(Guid.NewGuid(), "RE", "A", "B", default, 1m, Quelle()));
        Assert.Throws<ArgumentException>(() => new Zahlung(Guid.NewGuid(), "A", "B", default, 1m, Quelle()));
        Assert.Throws<ArgumentNullException>(() => new Rechnung(Guid.NewGuid(), "RE", "A", "B", Datum, 1m, null!));
        Assert.Throws<ArgumentNullException>(() => new Zahlung(Guid.NewGuid(), "A", "B", Datum, 1m, null!));
        Assert.Throws<ArgumentNullException>(() => kern.RechnungErfassen(null!));
        Assert.Throws<ArgumentNullException>(() => kern.ZahlungErfassen(null!));
        var rechnung = Rechnung();
        var zahlung = Zahlung(bestaetigt: false);
        Assert.Throws<ArgumentNullException>(() => kern.ZahlungBestaetigen(zahlung.Id, null!, "TEST", Zeitpunkt));
        Assert.Throws<ArgumentException>(() => kern.ZahlungBestaetigen(zahlung.Id, Quelle(), "TEST", default));
        Assert.Empty(kern.Bestaetigungen);
        kern.ZahlungBestaetigen(zahlung.Id, Quelle(), "TEST", Zeitpunkt);
        Assert.Throws<ArgumentException>(() => Zuordnen(rechnung, zahlung, 1m, Guid.Empty));
        Assert.Throws<ArgumentNullException>(() => kern.Zuordnen(Guid.NewGuid(), rechnung.Id, zahlung.Id, 1m, null!, "TEST", Zeitpunkt));
        Assert.Throws<ArgumentException>(() => kern.Zuordnen(Guid.NewGuid(), rechnung.Id, zahlung.Id, 1m, Quelle(), "TEST", default));
        Assert.Empty(kern.Zuordnungen);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BestaetigteTeilzahlungenFunktionierenBeiBeidenRollenDesEigenenUnternehmens(bool ausgehend)
    {
        // Perspektive ist im Test explizit, im Kern noch kein eigenes Attribut.
        var kern = new Rechnungskern();
        var aussteller = ausgehend ? "SYNTH-EIGEN" : "SYNTH-LIEFERANT";
        var empfaenger = ausgehend ? "SYNTH-KUNDE" : "SYNTH-EIGEN";
        var datum = new DateOnly(2026, 10, 17);
        var rechnung = new Rechnung(Guid.NewGuid(), "SYNTH-BEIDE-RICHTUNGEN", aussteller, empfaenger, datum, 240m, new("SYNTH-RE", "synthetisch/Rechnung"));
        kern.RechnungErfassen(rechnung);
        void Zuordnen(decimal betrag, bool bestaetigt)
        {
            var z = new Zahlung(Guid.NewGuid(), empfaenger, aussteller, datum, betrag, new("SYNTH-ZA", "synthetisch/Zahlung"));
            kern.ZahlungErfassen(z);
            if (bestaetigt)
            {
                kern.ZahlungBestaetigen(z.Id, z.Nachweis, "SYNTH-PRUEFER", new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
                kern.Zuordnen(Guid.NewGuid(), rechnung.Id, z.Id, betrag, z.Nachweis, "SYNTH-PRUEFER", new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
            }
            else Assert.Throws<InvalidOperationException>(() => kern.Zuordnen(Guid.NewGuid(), rechnung.Id, z.Id, betrag, z.Nachweis, "SYNTH-PRUEFER", new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero)));
        }
        Zuordnen(60m, true);
        Zuordnen(40m, true);
        Assert.Equal(140m, kern.OffenerBetrag(rechnung.Id));
        Zuordnen(140m, false);
        Assert.Equal(140m, kern.OffenerBetrag(rechnung.Id));
        Zuordnen(140m, true);
        Assert.Equal(0m, kern.OffenerBetrag(rechnung.Id));
        Assert.Equal(3, kern.Zuordnungen.Count);
    }
}
