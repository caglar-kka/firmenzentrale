using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Betriebsplattform.Rechnungen;

namespace Betriebsplattform.Import;

internal static class Zeilenpruefung
{
    private sealed class Zeile(Originalzelle[] zellen)
    {
        internal Originalzelle[] Zellen { get; } = zellen;
        internal List<Pruefmeldung> Meldungen { get; } = [];
        internal Quellschluessel? Schluessel { get; set; }
        internal Rechnungskennung? Rechnungskennung { get; set; }
        internal Rechnung? Rechnung { get; set; }
        internal Zahlung? Zahlung { get; set; }
        internal bool HatFehler => Meldungen.Any(m => m.Stufe == Meldungsstufe.Fehler);
        internal void Fehler(string code, string text, int spalte) =>
            Meldungen.Add(new(Meldungsstufe.Fehler, code, text, Zellen[spalte].Quelle));
        internal void Warnung(string code, string text, int spalte) =>
            Meldungen.Add(new(Meldungsstufe.Warnung, code, text, Zellen[spalte].Quelle));
    }

    internal static Importbericht Pruefen(IReadOnlyList<Originalzelle[]> originalzeilen, string hash, Importprofil profil)
    {
        var zeilen = originalzeilen.Select(z => Pruefen(z, profil)).ToArray();
        foreach (var gruppe in zeilen.Where(z => z.Schluessel is not null).GroupBy(z => z.Schluessel))
            if (gruppe.Count() > 1 || profil.BekannteQuellschluessel.Contains(gruppe.Key!))
                foreach (var zeile in gruppe)
                    zeile.Fehler("QUELL_DUBLETTE", "Quellsystem, Art und Datensatz-ID sind mehrfach vorhanden oder bereits bekannt. Auch widersprüchliche Wiederholungen benötigen eine Prüfung.", 0);
        foreach (var gruppe in zeilen.Where(z => z.Rechnungskennung is not null).GroupBy(z => z.Rechnungskennung))
            if (gruppe.Count() > 1 || profil.BekannteRechnungen.Contains(gruppe.Key!))
                foreach (var zeile in gruppe)
                    zeile.Fehler("RECHNUNGS_DUBLETTE", "Aussteller und Rechnungsnummer sind mehrfach vorhanden oder bereits bekannt.", 2);

        var meldungen = new List<Pruefmeldung>
        {
            new(Meldungsstufe.Warnung, "ISD_UNGEKLAERT", "ISD wird vorläufig als Geschäftspartner behandelt. Vollständiger Firmenname und konkrete Rolle müssen je Vorgang geprüft werden; Originalbezeichnungen bleiben erhalten."),
            new(Meldungsstufe.Warnung, "NUR_VORSCHAU", "Keine Buchung und keine automatische Zahlungsbestätigung. Mapping und Belege müssen menschlich geprüft werden.")
        };
        if (zeilen.Length == 0)
            meldungen.Add(new(Meldungsstufe.Warnung, "LEER", "Die Vorlage enthält keine Datenzeilen."));
        return new(hash, zeilen.Select(z => new Vorschauzeile(z.Zellen[0].Quelle.Zeile, z.Zellen, z.Meldungen,
            z.Schluessel, z.HatFehler ? null : z.Rechnung, z.HatFehler ? null : z.Zahlung)), meldungen);
    }

    private static Zeile Pruefen(Originalzelle[] zellen, Importprofil profil)
    {
        var zeile = new Zeile(zellen);
        for (var i = 0; i < zellen.Length; i++)
        {
            if (zellen[i].Formel is not null)
                zeile.Fehler("FORMEL", "Formeln werden weder ausgeführt noch anhand zwischengespeicherter Ergebnisse übernommen.", i);
            if (zellen[i].Datentyp is not ("inlineStr" or "s" or "n" or "d" or "fehlend"))
                zeile.Fehler("DATENTYP", "Dieser Excel-Zelltyp ist für die Vorlage nicht erlaubt.", i);
            if (zellen[i].Formatindex is { } stil && stil != "0")
                zeile.Warnung("FORMATIERUNG", "Excel-Anzeigeformatierung wird nicht interpretiert. Rohwert und Formatindex bleiben erhalten und müssen geprüft werden.", i);
        }

        string Text(int index, bool pflicht = true, bool datum = false)
        {
            var zelle = zellen[index];
            var text = zelle.Text.Trim();
            if (text.Length == 0)
            {
                if (pflicht) zeile.Fehler("PFLICHTWERT", "Ein Pflichtwert fehlt.", index);
                return "";
            }
            if (zelle.Datentyp is not ("inlineStr" or "s") && !(datum && zelle.Datentyp == "d"))
                zeile.Fehler("DATENTYP", "Hier wird ein Textwert erwartet; Kennungen und Referenzen dürfen nicht als Zahl interpretiert werden.", index);
            if (zelle.Text != text)
                zeile.Warnung("LEERZEICHEN", "Rand-Leerzeichen wurden nur für die Prüfung entfernt; der Originalwert bleibt erhalten.", index);
            if (text.Length > 256)
                zeile.Fehler("TEXTLAENGE", "Der fachliche Textwert überschreitet 256 Zeichen.", index);
            return text;
        }

        var id = Text(0);
        var art = Text(1);
        var nummer = Text(2, pflicht: art != "Zahlung");
        var von = Text(3);
        var an = Text(4);
        var datumstext = Text(5, datum: true);
        var waehrung = Text(7);
        var beleg = Text(8);
        if (art is not ("Rechnung" or "Zahlung"))
            zeile.Fehler("ART", "Art muss ausdrücklich Rechnung oder Zahlung sein. ISD ist keine bestätigte Rechnungsart.", 1);
        else if (id.Length > 0) zeile.Schluessel = new(profil.Quellsystem, art, id);
        if (waehrung != "EUR")
            zeile.Fehler("WAEHRUNG", "Nur ausdrücklich angegebenes EUR wird unterstützt. Keine automatische Währungsannahme oder Umrechnung.", 7);

        var vonBekannt = profil.Unternehmen.TryGetValue(von, out var vonId);
        var anBekannt = profil.Unternehmen.TryGetValue(an, out var anId);
        if (!vonBekannt) zeile.Fehler("UNTERNEHMEN", "Für Von fehlt eine explizite Unternehmenszuordnung.", 3);
        if (!anBekannt) zeile.Fehler("UNTERNEHMEN", "Für An fehlt eine explizite Unternehmenszuordnung.", 4);
        if (art == "Rechnung" && vonBekannt && nummer.Length > 0)
            zeile.Rechnungskennung = new(vonId!, nummer);
        if (!DateOnly.TryParseExact(datumstext, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datum)
            || datum == default)
            zeile.Fehler("DATUM", "Ein gültiges ISO-Datum JJJJ-MM-TT ist erforderlich. Excel-Seriendaten werden nicht geraten.", 5);
        var betrag = Betrag(zeile);

        if (!zeile.HatFehler)
        {
            // Fachobjekte sind ausschließlich ungespeicherte Vorschaukandidaten.
            // Es gibt absichtlich keinen Rechnungskern-Parameter und keinen schreibenden Aufruf.
            var fachId = new Guid(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(zeile.Schluessel)).AsSpan(0, 16));
            var quelle = zellen[8].Quelle;
            var nachweis = new Nachweis(beleg, $"SHA256:{quelle.DateiHash}/{quelle.Blatt}!{quelle.Zelle}");
            try
            {
                if (art == "Rechnung")
                    zeile.Rechnung = new(fachId, nummer, vonId!, anId!, datum, betrag, nachweis);
                else
                {
                    zeile.Zahlung = new(fachId, vonId!, anId!, datum, betrag, nachweis);
                    zeile.Warnung("ZAHLUNG_UNBESTAETIGT", "Zahlung bleibt zur Prüfung und reduziert keinen offenen Betrag.", 1);
                }
            }
            catch (ArgumentException)
            {
                zeile.Fehler("FACHREGEL", "Die Zeile verletzt eine Eingaberegel des vorhandenen Rechnungskerns.", 0);
            }
        }
        return zeile;
    }

    private static decimal Betrag(Zeile zeile)
    {
        var zelle = zeile.Zellen[6];
        var text = zelle.Text.Trim();
        var istText = zelle.Datentyp is "inlineStr" or "s";
        var muster = istText ? @"\A[0-9]+(?:,[0-9]{1,2})?\z" : @"\A[0-9]+(?:\.[0-9]{1,2})?\z";
        if (text.Length is 0 or > 64 || (!istText && zelle.Datentyp != "n")
            || !Regex.IsMatch(text, muster, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking))
        {
            zeile.Fehler("BETRAG", "Positiver centgenauer Betrag erforderlich: Zahlenzelle mit Dezimalpunkt oder Text mit Dezimalkomma, ohne Tausendertrennzeichen und Exponenten.", 6);
            return 0m;
        }
        var trennzeichen = istText ? ',' : '.';
        var position = text.IndexOf(trennzeichen);
        var nachkommastellen = position < 0 ? 0 : text.Length - position - 1;
        var cent = BigInteger.Parse(text.Replace(trennzeichen.ToString(), ""), CultureInfo.InvariantCulture)
            * (nachkommastellen == 0 ? 100 : nachkommastellen == 1 ? 10 : 1);
        if (cent <= BigInteger.Zero || cent > new BigInteger(decimal.MaxValue))
        {
            zeile.Fehler("BETRAG", "Betrag muss positiv und innerhalb der centgenauen Dezimalgrenze liegen.", 6);
            return 0m;
        }
        if (istText) zeile.Warnung("BETRAG_TEXT", "Betrag wurde ausdrücklich als deutscher Dezimaltext geprüft; Original und Zelltyp bleiben erhalten.", 6);
        if (text != zelle.Text) zeile.Warnung("LEERZEICHEN", "Rand-Leerzeichen wurden nur für die Betragsprüfung entfernt.", 6);
        return (decimal)cent / 100m;
    }
}
