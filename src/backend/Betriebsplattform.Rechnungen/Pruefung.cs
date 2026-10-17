namespace Betriebsplattform.Rechnungen;

internal static class Pruefung
{
    internal static string Text(string wert, string name)
    {
        if (string.IsNullOrWhiteSpace(wert))
            throw new ArgumentException("Ein Pflichttext fehlt.", name);
        return wert;
    }

    internal static Guid Id(Guid wert)
    {
        if (wert == Guid.Empty)
            throw new ArgumentException("Eine leere ID ist nicht erlaubt.", nameof(wert));
        return wert;
    }

    internal static decimal Betrag(decimal wert)
    {
        // decimal hat eine endliche Stellenzahl. Diese Grenze lässt zwei Centstellen
        // für jede zulässige Addition und Subtraktion innerhalb eines Saldos frei.
        const decimal maximum = 792_281_625_142_643_375_935_439_503.35m;
        if (wert <= 0m || wert > maximum || decimal.Round(wert, 2) != wert)
            throw new ArgumentOutOfRangeException(nameof(wert), "EUR-Betrag muss positiv, centgenau und innerhalb der Dezimalgrenze sein.");
        return wert;
    }

    internal static DateOnly Datum(DateOnly wert)
    {
        if (wert == default)
            throw new ArgumentException("Ein Datum fehlt.", nameof(wert));
        return wert;
    }

    internal static DateTimeOffset Zeitpunkt(DateTimeOffset wert)
    {
        if (wert == default)
            throw new ArgumentException("Ein Erfassungszeitpunkt fehlt.", nameof(wert));
        return wert;
    }
}
