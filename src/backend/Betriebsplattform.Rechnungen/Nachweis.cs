namespace Betriebsplattform.Rechnungen;

// Referenzen, keine Dateioperationen und keine automatische Echtheitsprüfung.
public sealed class Nachweis(string beleg, string quellenort)
{
    public string Beleg { get; } = Pruefung.Text(beleg, nameof(beleg));
    public string Quellenort { get; } = Pruefung.Text(quellenort, nameof(quellenort));
}
