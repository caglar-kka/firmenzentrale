namespace Betriebsplattform.Rechnungen;

public sealed class Rechnungskern
{
    private readonly Dictionary<Guid, Rechnung> rechnungen = [];
    private readonly Dictionary<Guid, Zahlung> zahlungen = [];
    private readonly Dictionary<Guid, Zahlungsbestaetigung> bestaetigungen = [];
    private readonly List<Zahlungszuordnung> zuordnungen = [];

    // Eine Instanz bildet genau einen fachlichen Kontext ab; Aufrufe erfolgen seriell.
    public IReadOnlyList<Zahlungszuordnung> Zuordnungen => zuordnungen.ToArray();
    public IReadOnlyList<Zahlungsbestaetigung> Bestaetigungen => bestaetigungen.Values.ToArray();

    public void RechnungErfassen(Rechnung rechnung)
    {
        ArgumentNullException.ThrowIfNull(rechnung);
        if (!rechnungen.TryAdd(rechnung.Id, rechnung))
            throw new InvalidOperationException("Rechnungs-ID ist bereits erfasst.");
    }

    public void ZahlungErfassen(Zahlung zahlung)
    {
        ArgumentNullException.ThrowIfNull(zahlung);
        if (!zahlungen.TryAdd(zahlung.Id, zahlung))
            throw new InvalidOperationException("Zahlungs-ID ist bereits erfasst.");
    }

    // Der Aufrufer muss den Beleg tatsächlich prüfen. Ein Text ist kein Banknachweis.
    public void ZahlungBestaetigen(Guid zahlungId, Nachweis nachweis, string erfasstVon, DateTimeOffset erfasstAm)
    {
        _ = zahlungen[zahlungId];
        var bestaetigung = new Zahlungsbestaetigung(zahlungId, nachweis, erfasstVon, erfasstAm);
        if (!bestaetigungen.TryAdd(zahlungId, bestaetigung))
            throw new InvalidOperationException("Zahlung ist bereits bestätigt.");
    }

    public decimal OffenerBetrag(Guid rechnungId) =>
        rechnungen[rechnungId].Betrag - zuordnungen.Where(z => z.RechnungId == rechnungId).Sum(z => z.Betrag);

    public decimal VerfuegbarerBetrag(Guid zahlungId)
    {
        var zahlung = zahlungen[zahlungId];
        return bestaetigungen.ContainsKey(zahlungId)
            ? zahlung.Betrag - zuordnungen.Where(z => z.ZahlungId == zahlungId).Sum(z => z.Betrag)
            : 0m;
    }

    public Zahlungszuordnung Zuordnen(Guid id, Guid rechnungId, Guid zahlungId, decimal betrag,
        Nachweis nachweis, string erfasstVon, DateTimeOffset erfasstAm)
    {
        var zuordnung = new Zahlungszuordnung(id, rechnungId, zahlungId, betrag, nachweis, erfasstVon, erfasstAm);
        var rechnung = rechnungen[rechnungId];
        var zahlung = zahlungen[zahlungId];

        if (!bestaetigungen.ContainsKey(zahlungId))
            throw new InvalidOperationException("Zahlung ist noch zur Prüfung.");
        if (!string.Equals(rechnung.Aussteller, zahlung.Empfaenger, StringComparison.Ordinal))
            throw new InvalidOperationException("Zahlungsempfänger und Rechnungsaussteller stimmen nicht überein.");
        if (zuordnungen.Any(z => z.Id == id || (z.RechnungId == rechnungId && z.ZahlungId == zahlungId)))
            throw new InvalidOperationException("Zahlungszuordnung ist bereits vorhanden.");
        if (betrag > OffenerBetrag(rechnungId) || betrag > VerfuegbarerBetrag(zahlungId))
            throw new InvalidOperationException("Zuordnung überschreitet den offenen oder verfügbaren Betrag.");

        // Erst nach allen Prüfungen mutieren: abgewiesene Aktionen verändern keinen Saldo.
        zuordnungen.Add(zuordnung);
        return zuordnung;
    }
}
