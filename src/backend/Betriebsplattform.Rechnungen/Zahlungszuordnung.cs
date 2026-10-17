namespace Betriebsplattform.Rechnungen;

public sealed class Zahlungszuordnung
{
    internal Zahlungszuordnung(Guid id, Guid rechnungId, Guid zahlungId, decimal betrag,
        Nachweis nachweis, string erfasstVon, DateTimeOffset erfasstAm)
    {
        Id = Pruefung.Id(id);
        RechnungId = Pruefung.Id(rechnungId);
        ZahlungId = Pruefung.Id(zahlungId);
        Betrag = Pruefung.Betrag(betrag);
        Nachweis = nachweis ?? throw new ArgumentNullException(nameof(nachweis));
        ErfasstVon = Pruefung.Text(erfasstVon, nameof(erfasstVon));
        ErfasstAm = Pruefung.Zeitpunkt(erfasstAm);
    }

    public Guid Id { get; }
    public Guid RechnungId { get; }
    public Guid ZahlungId { get; }
    public decimal Betrag { get; }
    public Nachweis Nachweis { get; }
    public string ErfasstVon { get; }
    public DateTimeOffset ErfasstAm { get; }
}
