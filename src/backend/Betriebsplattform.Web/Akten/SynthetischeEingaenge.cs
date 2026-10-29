using System.Text;
using Betriebsplattform.Web.Sicherheit;
namespace Betriebsplattform.Web.Akten;
public static class SynthetischeEingaenge
{
    public static readonly string[] Schluessel=["regel-versicherung","rechnung","versicherung","behoerde","kunde","wiederholung","unklar","wartung","wartung-pdf"];
    public static (string Name,byte[] Bytes) Datei(string key)
    {
        if(key=="wartung-pdf")return("Wartungsrechnung Oktober 2026.pdf",Betriebsplattform.Web.DemoDokumente.TestPdf(["MBOP-SYNTHETISCH - keine Zahlungsanweisung","Aussteller: Nordlicht Haustechnik GmbH","Empfaenger: Elbequartier Objektservice GmbH","Rechnungsnummer: RE-2026-1042","Betrag: 1249.50","Dokumentdatum: 2026-10-21","Kundennummer: 0042","Referenz: Kontorhaus Suedhafen - Lueftungswartung","Antwort bis: 2026-10-30"]));
        var text=key switch{
            "regel-versicherung"=>"MBOP-SYNTHETISCH – ausschließlich interne Prüfdaten, keine echte Versicherung.\nDokumentart: Versicherungsschreiben\nDokumentdatum: 2026-10-22\nVertragsnummer: TEST-INTERN-781\nAntwort bis: 2026-11-12\nSynthetische Betriebsversicherung 781\nAussteller: Fiktive Binnenstern Versicherung\nEmpfänger: Fiktiver Kieselbüro Betrieb\nBitte die aufgeführten Unterlagen intern prüfen und eine Antwort vorbereiten. Genannter Termin ohne rechtliche Fristberechnung. Kein Versand.",
            "wartung"=>"Vollständig erfundener Geschäftsvorgang – keine echte Rechnung.\nMBOP-SYNTHETISCH\nDokumentart: Eingangsrechnung\nDokumentdatum: 2026-10-20\nRechnungsnummer: RE-2026-1042\nBetrag: 1249.50\nAntwort bis: 2026-10-30\nReferenz: Kontorhaus Südhafen – Lüftungswartung 2026\nAussteller: Nordlicht Haustechnik GmbH\nEmpfänger: Elbequartier Objektservice GmbH\nAnsprechpartnerin: Mara Falk, service@nordlicht.example\nLeistung: Wartung der Lüftungsanlage im Kontorhaus Südhafen, Oktober 2026.\nPrüfung bis 30.10.2026 vorgesehen. Keine Zahlungsanweisung oder rechtliche Fristberechnung.",
            "rechnung" or "wiederholung"=>"MBOP-SYNTHETISCH\nDokumentart: Eingangsrechnung\nDokumentdatum: 2026-10-20\nRechnungsnummer: BEISPIEL-2026-104\nBetrag: 125.50\nReferenz: Wartung Oktober\nKeine echte Rechnung. Absender und Akte müssen geprüft werden.",
            "versicherung"=>"MBOP-SYNTHETISCH\nDokumentart: Versicherungsschreiben\nDokumentdatum: 2026-10-20\nAntwort bis: 2026-11-06\nReferenz: Vertrag BEISPIEL-42\nBitte Unterlagen bis zum genannten Datum senden. Ungeprüfter Termin, keine rechtliche Fristberechnung.",
            "behoerde"=>"MBOP-SYNTHETISCH\nDokumentart: Behördenbrief\nDokumentdatum: 2026-10-20\nReferenz: AZ-BEISPIEL-12\nEine mögliche Antwortfrist ist im Original zu prüfen; kein Termin wird berechnet.",
            "kunde"=>"MBOP-SYNTHETISCH\nDokumentart: Kundenkorrespondenz\nDokumentdatum: 2026-10-20\nReferenz: Rückfrage zum Angebot\nBitte Rückruf vorbereiten. Keine verbindliche Fälligkeit angegeben.",
            "unklar"=>"MBOP-SYNTHETISCH\nUnterlagen ohne eindeutigen Absender oder Vorgangsbezug. Bitte manuell klären.",
            _=>throw new Arbeitsfehler("Beispiel nicht vorhanden.",404)};
        return (key=="wartung"?"Wartungsrechnung Lüftungsanlage – Oktober 2026.txt":(key=="wiederholung"?"rechnung":key)+"-beispiel.txt",Encoding.UTF8.GetBytes(text));
    }
    // This is a closed trusted fixture allowlist, expressly NOT a malware scanner.
    public sealed class NurKatalog : IMalwarepruefung
    {
        public Task Freigeben(byte[] bytes,CancellationToken ct){ct.ThrowIfCancellationRequested();if(!Schluessel.Any(k=>Datei(k).Bytes.AsSpan().SequenceEqual(bytes)))throw new Arbeitsfehler("Nur unveränderte synthetische Katalogdateien sind hier freigegeben.",503);return Task.CompletedTask;}
    }
}
