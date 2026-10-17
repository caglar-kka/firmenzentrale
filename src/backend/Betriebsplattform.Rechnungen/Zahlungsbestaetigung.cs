namespace Betriebsplattform.Rechnungen;

public sealed class Zahlungsbestaetigung
{
    internal Zahlungsbestaetigung(Guid zahlungId, Nachweis nachweis, string erfasstVon, DateTimeOffset erfasstAm)
    {
        ZahlungId = Pruefung.Id(zahlungId);
        Nachweis = nachweis ?? throw new ArgumentNullException(nameof(nachweis));
        ErfasstVon = Pruefung.Text(erfasstVon, nameof(erfasstVon));
        ErfasstAm = Pruefung.Zeitpunkt(erfasstAm);
    }

    public Guid ZahlungId { get; }
    public Nachweis Nachweis { get; }
    public string ErfasstVon { get; }
    public DateTimeOffset ErfasstAm { get; }
}
