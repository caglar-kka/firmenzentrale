using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Betriebsplattform.Import;

internal sealed class ImportdateiException(string code, string meldung) : Exception(meldung)
{
    internal string Code { get; } = code;
}

// Absichtlich ein begrenzter Vorlagenleser, kein allgemeiner SpreadsheetML-Interpreter.
internal static class XlsxLeser
{
    internal const int MaxDateiBytes = 2 * 1024 * 1024;
    internal const int MaxTeilBytes = 2 * 1024 * 1024;
    internal const int MaxGesamtBytes = 8 * 1024 * 1024;
    internal const int MaxDatenzeilen = 1000;
    internal static readonly string[] Spalten = ["DatensatzId", "Art", "Nummer", "Von", "An", "Datum", "Betrag", "Waehrung", "Beleg"];
    internal const string Blatt = "ISD-Testdaten";
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace P = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace C = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string TypBasis = "application/vnd.openxmlformats-officedocument.spreadsheetml.";

    internal static void Fordern(bool bedingung, string code, string text)
    {
        if (!bedingung) throw new ImportdateiException(code, text);
    }

    internal static byte[] BegrenztLesen(Stream stream, int maximum)
    {
        using var ziel = new MemoryStream();
        var puffer = new byte[8192];
        int anzahl;
        while ((anzahl = stream.Read(puffer, 0, Math.Min(puffer.Length, maximum + 1 - (int)ziel.Length))) != 0)
        {
            ziel.Write(puffer, 0, anzahl);
            Fordern(ziel.Length <= maximum, "GROESSE", "Datei oder entpackter XML-Teil überschreitet die erlaubte Größe.");
        }
        return ziel.ToArray();
    }

    internal static IReadOnlyList<Originalzelle[]> Lesen(byte[] daten, string hash)
    {
        using var stream = new MemoryStream(daten, writable: false);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        Fordern(zip.Entries.Count is > 0 and <= 16, "PAKET", "Unzulässige Anzahl von ZIP-Einträgen.");
        var teile = new Dictionary<string, XDocument>(StringComparer.Ordinal);
        var namen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long gesamt = 0;
        foreach (var eintrag in zip.Entries)
        {
            var name = eintrag.FullName;
            var erlaubt = name is "[Content_Types].xml" or "_rels/.rels" or "xl/workbook.xml"
                or "xl/_rels/workbook.xml.rels" or "xl/worksheets/sheet1.xml"
                or "xl/sharedStrings.xml" or "xl/styles.xml" or "docProps/core.xml" or "docProps/app.xml";
            Fordern(erlaubt && namen.Add(name), "PAKET", "Unbekannter, aktiver, mehrfacher oder unsicherer Paketbestandteil. Nur die Testvorlage ist erlaubt.");
            gesamt += eintrag.Length;
            Fordern(eintrag.Length <= MaxTeilBytes && gesamt <= MaxGesamtBytes
                && eintrag.Length <= Math.Max(1L, eintrag.CompressedLength) * 100L,
                "GROESSE", "Entpackte Größe oder Kompressionsverhältnis überschreitet das Limit.");
            using var inhalt = eintrag.Open();
            var bytes = BegrenztLesen(inhalt, MaxTeilBytes);
            Fordern(bytes.LongLength == eintrag.Length, "PAKET", "Inkonsistente Länge eines Paketbestandteils.");
            teile.Add(name, XmlLesen(bytes));
        }

        XDocument Teil(string name)
        {
            Fordern(teile.ContainsKey(name), "PAKET", "Ein erforderlicher Paketbestandteil fehlt.");
            return teile[name];
        }

        InhaltstypenPruefen(Teil("[Content_Types].xml"));
        var hauptbeziehungen = Beziehungen(Teil("_rels/.rels"), haupt: true);
        Fordern(hauptbeziehungen.Values.Count(v => v.Typ == R.NamespaceName + "/officeDocument" && v.Ziel == "xl/workbook.xml") == 1,
            "BEZIEHUNG", "Die Arbeitsmappenbeziehung ist nicht eindeutig.");
        var beziehungen = Beziehungen(Teil("xl/_rels/workbook.xml.rels"), haupt: false);
        var workbook = Teil("xl/workbook.xml");
        Fordern(workbook.Root?.Name == S + "workbook", "VORLAGE", "Arbeitsmappenformat wird nicht unterstützt.");
        Fordern(workbook.Root!.Elements().All(e => e.Name == S + "sheets" || e.Name == S + "workbookPr"
            || e.Name == S + "bookViews" || e.Name == S + "calcPr"), "VORLAGE", "Zusätzliche Arbeitsmappenfunktionen werden nicht unterstützt.");
        var blaetter = workbook.Root.Elements(S + "sheets").Elements(S + "sheet").ToArray();
        Fordern(blaetter.Length == 1 && (string?)blaetter[0].Attribute("name") == Blatt
            && ((string?)blaetter[0].Attribute("state") is null or "visible"), "BLATT", "Genau ein sichtbares Blatt ISD-Testdaten ist erforderlich.");
        var rid = (string?)blaetter[0].Attribute(R + "id") ?? "";
        Fordern(beziehungen.TryGetValue(rid, out var ziel) && ziel.Typ == R.NamespaceName + "/worksheet"
            && ziel.Ziel == "xl/worksheets/sheet1.xml", "BEZIEHUNG", "Blattbeziehung ist ungültig.");
        Fordern(beziehungen.Values.Count(v => v.Typ == R.NamespaceName + "/worksheet") == 1, "BLATT", "Zusätzliche Blattbeziehungen sind nicht erlaubt.");
        foreach (var (_, rel) in beziehungen)
            Fordern(teile.ContainsKey(rel.Ziel), "BEZIEHUNG", "Das Ziel einer Beziehung fehlt.");
        foreach (var name in new[] { "sharedStrings", "styles" })
            Fordern(beziehungen.Values.Count(v => v.Typ == R.NamespaceName + "/" + name) == (teile.ContainsKey("xl/" + name + ".xml") ? 1 : 0),
                "BEZIEHUNG", "Gemeinsame Texte und Formatvorlagen benötigen eindeutige Paketbeziehungen.");

        var shared = teile.TryGetValue("xl/sharedStrings.xml", out var strings) ? SharedStrings(strings) : [];
        return BlattLesen(Teil("xl/worksheets/sheet1.xml"), shared, hash);
    }

    private static XDocument XmlLesen(byte[] bytes)
    {
        var einstellungen = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxTeilBytes,
            IgnoreComments = true
        };
        using (var vorpruefung = XmlReader.Create(new MemoryStream(bytes), einstellungen))
            while (vorpruefung.Read())
                Fordern(vorpruefung.Depth <= 32, "XML", "XML-Verschachtelung überschreitet das Limit.");
        using var reader = XmlReader.Create(new MemoryStream(bytes), einstellungen);
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private static void InhaltstypenPruefen(XDocument dokument)
    {
        Fordern(dokument.Root?.Name == C + "Types", "FORMAT", "XLSX-Inhaltstypen fehlen.");
        var erlaubteTypen = new HashSet<string>(StringComparer.Ordinal)
        {
            "application/xml", "application/vnd.openxmlformats-package.relationships+xml",
            TypBasis + "sheet.main+xml", TypBasis + "worksheet+xml", TypBasis + "sharedStrings+xml", TypBasis + "styles+xml",
            "application/vnd.openxmlformats-package.core-properties+xml",
            "application/vnd.openxmlformats-officedocument.extended-properties+xml"
        };
        var deklarationen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in dokument.Root!.Elements())
        {
            Fordern((element.Name == C + "Default" || element.Name == C + "Override")
                && erlaubteTypen.Contains((string?)element.Attribute("ContentType") ?? ""),
                "FORMAT", "Nicht unterstützter Inhaltstyp, beispielsweise Makro- oder Binärinhalt.");
            var schluessel = element.Name.LocalName + ":" + ((string?)element.Attribute(element.Name == C + "Override" ? "PartName" : "Extension") ?? "");
            Fordern(deklarationen.Add(schluessel), "FORMAT", "Mehrfache oder widersprüchliche Inhaltstypdeklaration.");
            var erwartet = element.Name == C + "Default"
                ? (string?)element.Attribute("Extension") switch
                {
                    "xml" => "application/xml",
                    "rels" => "application/vnd.openxmlformats-package.relationships+xml",
                    _ => null
                }
                : (string?)element.Attribute("PartName") switch
                {
                    "/xl/workbook.xml" => TypBasis + "sheet.main+xml",
                    "/xl/worksheets/sheet1.xml" => TypBasis + "worksheet+xml",
                    "/xl/sharedStrings.xml" => TypBasis + "sharedStrings+xml",
                    "/xl/styles.xml" => TypBasis + "styles+xml",
                    "/docProps/core.xml" => "application/vnd.openxmlformats-package.core-properties+xml",
                    "/docProps/app.xml" => "application/vnd.openxmlformats-officedocument.extended-properties+xml",
                    _ => null
                };
            Fordern(erwartet is not null && (string?)element.Attribute("ContentType") == erwartet,
                "FORMAT", "Inhaltstyp und Paketpfad stimmen nicht mit der Testvorlage überein.");
        }
        Fordern(dokument.Root.Elements(C + "Override").Count(e => (string?)e.Attribute("PartName") == "/xl/workbook.xml"
            && (string?)e.Attribute("ContentType") == TypBasis + "sheet.main+xml") == 1, "FORMAT", "Keine eindeutige makrofreie XLSX-Arbeitsmappe.");
        Fordern(deklarationen.Contains("Override:/xl/worksheets/sheet1.xml"), "FORMAT", "Blatt-Inhaltstyp fehlt.");
    }

    private static Dictionary<string, (string Typ, string Ziel)> Beziehungen(XDocument dokument, bool haupt)
    {
        Fordern(dokument.Root?.Name == P + "Relationships", "BEZIEHUNG", "Ungültiges Beziehungsformat.");
        var ergebnis = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (var element in dokument.Root!.Elements())
        {
            var typ = (string?)element.Attribute("Type") ?? "";
            var target = (string?)element.Attribute("Target") ?? "";
            var id = (string?)element.Attribute("Id") ?? "";
            var ziel = target.StartsWith('/') ? target[1..] : (haupt ? "" : "xl/") + target;
            var erlaubt = haupt
                ? (typ == R.NamespaceName + "/officeDocument" && ziel == "xl/workbook.xml")
                    || (typ == "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" && ziel == "docProps/core.xml")
                    || (typ == R.NamespaceName + "/extended-properties" && ziel == "docProps/app.xml")
                : (typ == R.NamespaceName + "/worksheet" && ziel == "xl/worksheets/sheet1.xml")
                    || (typ == R.NamespaceName + "/sharedStrings" && ziel == "xl/sharedStrings.xml")
                    || (typ == R.NamespaceName + "/styles" && ziel == "xl/styles.xml");
            Fordern(element.Name == P + "Relationship" && erlaubt
                && ((string?)element.Attribute("TargetMode") is null or "Internal")
                && id.Length > 0 && ergebnis.TryAdd(id, (typ, ziel)), "BEZIEHUNG", "Externe, unsichere oder doppelte Beziehung wird abgewiesen.");
        }
        return ergebnis;
    }

    private static string[] SharedStrings(XDocument dokument)
    {
        Fordern(dokument.Root?.Name == S + "sst" && dokument.Root.Elements().All(e => e.Name == S + "si"),
            "TEXT", "Ungültige gemeinsame Texttabelle.");
        var texte = dokument.Root!.Elements(S + "si").Select(Text).ToArray();
        Fordern(texte.Length <= 10000, "GROESSE", "Zu viele gemeinsame Texte.");
        return texte;
    }

    private static string Text(XElement element)
    {
        if (element.Name == S + "c")
        {
            var inline = element.Element(S + "is");
            if (inline is null) return "";
            element = inline;
        }
        var inhalt = element.Elements().ToArray();
        var einfach = inhalt.Length == 1 && inhalt[0].Name == S + "t";
        var richtext = inhalt.Length > 0 && inhalt.All(r => r.Name == S + "r"
            && r.Elements(S + "t").Count() == 1 && r.Elements(S + "rPr").Count() <= 1
            && r.Elements().All(e => e.Name == S + "t" || e.Name == S + "rPr"));
        Fordern(einfach || richtext || inhalt.Length == 0, "TEXT", "Mehrdeutiger oder nicht unterstützter Textinhalt.");
        var text = einfach ? inhalt[0].Value : string.Concat(inhalt.Select(r => r.Element(S + "t")?.Value));
        Fordern(text.Length <= 32767, "GROESSE", "Ein Zelltext ist zu lang.");
        return text;
    }

    private static IReadOnlyList<Originalzelle[]> BlattLesen(XDocument dokument, string[] shared, string hash)
    {
        Fordern(dokument.Root?.Name == S + "worksheet", "BLATT", "Ungültiges Arbeitsblatt.");
        var erlaubteElemente = new HashSet<string> { "dimension", "sheetViews", "sheetFormatPr", "cols", "sheetData", "pageMargins" };
        Fordern(dokument.Root!.Elements().All(e => e.Name.Namespace == S && erlaubteElemente.Contains(e.Name.LocalName))
            && !dokument.Descendants().Any(e => e.Attribute("hidden") is { } hidden && hidden.Value is not ("0" or "false"))
            && dokument.Root.Elements(S + "sheetData").Count() == 1,
            "BLATT", "Versteckte Daten, verbundene Zellen oder weitere Blattfunktionen sind nicht erlaubt.");
        var zeilen = new List<Originalzelle[]>();
        var letzteZeile = 0;
        foreach (var row in dokument.Root.Element(S + "sheetData")!.Elements())
        {
            Fordern(row.Name == S + "row" && int.TryParse((string?)row.Attribute("r"), NumberStyles.None,
                CultureInfo.InvariantCulture, out var _), "ZEILE", "Ungültige Zeilennummer.");
            var nummer = int.Parse(row.Attribute("r")!.Value, CultureInfo.InvariantCulture);
            Fordern(nummer > letzteZeile && nummer <= MaxDatenzeilen + 1, "ZEILE", "Doppelte, unsortierte oder zu große Zeilennummer.");
            letzteZeile = nummer;
            var zellen = Enumerable.Range(0, Spalten.Length).Select(i => new Originalzelle(
                new(hash, Blatt, nummer, $"{(char)('A' + i)}{nummer}"), "fehlend", null, "", null)).ToArray();
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in row.Elements())
            {
                var adresse = (string?)cell.Attribute("r") ?? "";
                Fordern(cell.Name == S + "c" && gesehen.Add(adresse) && zellen.Any(c => c.Quelle.Zelle == adresse),
                    "ZELLE", "Doppelte, widersprüchliche oder außerhalb der Vorlage liegende Zelladresse.");
                var index = adresse[0] - 'A';
                var typ = (string?)cell.Attribute("t") ?? "n";
                Fordern(cell.Elements(S + "v").Count() <= 1 && cell.Elements(S + "f").Count() <= 1
                    && cell.Elements(S + "is").Count() <= 1
                    && cell.Elements().All(e => e.Name == S + "v" || e.Name == S + "f" || e.Name == S + "is")
                    && (typ == "inlineStr" ? cell.Element(S + "v") is null : cell.Element(S + "is") is null),
                    "ZELLE", "Mehrdeutiger oder nicht unterstützter Zellinhalt.");
                var rohwert = cell.Element(S + "v")?.Value;
                var text = typ == "inlineStr" ? Text(cell) : rohwert ?? "";
                if (typ == "s")
                {
                    Fordern(int.TryParse(rohwert, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                        && id >= 0 && id < shared.Length, "TEXT", "Ungültiger Verweis auf einen gemeinsamen Text.");
                    text = shared[id];
                }
                Fordern(text.Length <= 32767, "GROESSE", "Ein Zellwert ist zu lang.");
                zellen[index] = new(zellen[index].Quelle, typ, typ == "inlineStr" ? text : rohwert,
                    text, cell.Element(S + "f")?.Value, (string?)cell.Attribute("s"));
            }
            if (nummer == 1)
                Fordern(zellen.Select(c => c.Text).SequenceEqual(Spalten) && zellen.All(c => c.Formel is null && c.Datentyp is "inlineStr" or "s"),
                    "KOPF", "Die neun Spaltenüberschriften müssen der synthetischen Vorlage entsprechen.");
            else zeilen.Add(zellen);
        }
        Fordern(letzteZeile > 0 && dokument.Root.Element(S + "sheetData")!.Elements().First().Attribute("r")?.Value == "1",
            "KOPF", "Die Kopfzeile in Zeile 1 fehlt.");
        return zeilen;
    }
}
