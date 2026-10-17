namespace Betriebsplattform.Rechnungen;

public sealed class Zahlung(Guid id, string zahler, string empfaenger,
    DateOnly datum, decimal betrag, Nachweis nachweis)
{
    public Guid Id { get; } = Pruefung.Id(id);
    public string Zahler { get; } = Pruefung.Text(zahler, nameof(zahler));
    public string Empfaenger { get; } = Pruefung.Text(empfaenger, nameof(empfaenger));
    public DateOnly Datum { get; } = Pruefung.Datum(datum);
    public decimal Betrag { get; } = Pruefung.Betrag(betrag);
    public Nachweis Nachweis { get; } = nachweis ?? throw new ArgumentNullException(nameof(nachweis));
}
