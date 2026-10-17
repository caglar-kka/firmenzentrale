namespace Betriebsplattform.Rechnungen;

public sealed class Rechnung(Guid id, string nummer, string aussteller, string empfaenger,
    DateOnly datum, decimal betrag, Nachweis nachweis)
{
    public Guid Id { get; } = Pruefung.Id(id);
    public string Nummer { get; } = Pruefung.Text(nummer, nameof(nummer));
    public string Aussteller { get; } = Pruefung.Text(aussteller, nameof(aussteller));
    public string Empfaenger { get; } = Pruefung.Text(empfaenger, nameof(empfaenger));
    public DateOnly Datum { get; } = Pruefung.Datum(datum);
    public decimal Betrag { get; } = Pruefung.Betrag(betrag);
    public Nachweis Nachweis { get; } = nachweis ?? throw new ArgumentNullException(nameof(nachweis));
}
