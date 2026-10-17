using System.Security.Cryptography;
using System.Xml;

namespace Betriebsplattform.Import;

public sealed class ExcelImportvorschau
{
    public Importbericht Pruefen(Stream datei, string dateiname, Importprofil profil)
    {
        ArgumentNullException.ThrowIfNull(datei);
        ArgumentNullException.ThrowIfNull(profil);
        if (string.IsNullOrWhiteSpace(dateiname) || !dateiname.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return Fehler(null, "FORMAT", "Nur makrofreie XLSX-Testvorlagen werden unterstützt.");
        if (!datei.CanRead)
            return Fehler(null, "LESEN", "Der Eingabestream ist nicht lesbar oder bereits geschlossen.");
        string? hash = null;
        try
        {
            // Ab aktueller Streamposition bis zum Ende; der Aufrufer behält den Stream.
            var bytes = XlsxLeser.BegrenztLesen(datei, XlsxLeser.MaxDateiBytes);
            hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (bytes.Length < 4 || bytes[0] != 'P' || bytes[1] != 'K' || bytes[2] != 3 || bytes[3] != 4)
                return Fehler(hash, "PAKET", "Die Datei beginnt nicht mit einer unterstützten ZIP-/XLSX-Signatur.");
            if (profil.BekannteDateiHashes.Contains(hash))
                return Fehler(hash, "DATEI_DUBLETTE", "Dieser Dateiinhalt ist bereits in der übergebenen Vergleichshistorie vorhanden.");
            var originalzeilen = XlsxLeser.Lesen(bytes, hash);
            return Zeilenpruefung.Pruefen(originalzeilen, hash, profil);
        }
        catch (ImportdateiException ex) { return Fehler(hash, ex.Code, ex.Message); }
        catch (XmlException) { return Fehler(hash, "XML", "XML ist beschädigt oder enthält nicht erlaubte DTD-/Entitätsdefinitionen."); }
        catch (InvalidDataException) { return Fehler(hash, "PAKET", "Die Datei ist kein lesbares, unverschlüsseltes XLSX-Paket."); }
        catch (IOException) { return Fehler(hash, "LESEN", "Die Datei konnte nicht vollständig gelesen werden."); }
        catch (NotSupportedException) { return Fehler(hash, "LESEN", "Der Eingabestream oder das ZIP-Verfahren wird nicht unterstützt."); }
    }

    private static Importbericht Fehler(string? hash, string code, string text) =>
        new(hash, [], [new(Meldungsstufe.Fehler, code, text)]);
}
