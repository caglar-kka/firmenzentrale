# Architektur und Projektstruktur

## Zielbild und implementierter Ausschnitt

Ein React-Frontend spricht mit einem ASP.NET-Core-Backend. Das Backend bündelt die fachlichen Module in einem modularen Monolithen und greift über EF Core und Npgsql auf PostgreSQL zu. Geschäftsdaten bleiben in der vorgesehenen lokalen Kundenumgebung. Ein späterer mobiler oder hybrider Zugang benötigt eine eigene Sicherheits- und Betriebsentscheidung.

Der Stack ist festgelegt: React, TypeScript (strict), Vite; C# und .NET 10; PostgreSQL 18 und EF Core 10. Zusätzliche Infrastruktur und Bibliotheken werden erst bei belegtem Bedarf ausgewählt.

Implementiert sind `Betriebsplattform.Rechnungen`, `Betriebsplattform.Import` und der lokale ASP.NET-Core-Host `Betriebsplattform.Web`, jeweils für .NET 10, sowie das React-/TypeScript-strict-/Vite-Frontend. Ein `Rechnungskern` hält Rechnungen, Zahlungen, Bestätigungen und Zuordnungen im Speicher. Alle fachlichen Änderungen laufen über diesen Kern; zurückgegebene Datensätze sind unveränderlich, Listen sind unabhängige Momentaufnahmen. Aufrufe des Kerns erfolgen seriell und innerhalb eines einzigen fachlichen Kontexts. Die Importvorschau konstruiert nur ungespeicherte Fachobjekte und ruft keine schreibenden Kernoperationen auf. Persistenz folgt später. [Entscheidung und Fachregeln](adr/0001-rechnungs-und-zahlungskern.md).

Aktuelle ISD-Arbeitsannahme: vorläufig Firma/Geschäftspartner ohne nachgewiesenen vollständigen Firmennamen. Die konkrete Rolle muss aus dem Vorgang hervorgehen; ISD ist keine Rechnungsart. Frühere Quellbezeichnungen bleiben erhalten.

## Fachliche Grenzen

Rechnung, Zahlung und Zuordnung haben getrennte Verantwortlichkeiten. Aussteller, Empfänger, Zahler, Zahlungsempfänger, Datum, Betrag, Beleg und Herkunft werden erfasst. Teil-, Sammel- und Überzahlungen sind im engen MVP-01-Umfang implementiert. Storno ist noch nicht implementiert.

Excel-Dateien sind nicht vertrauenswürdig. Die begrenzte Vorschau prüft Format und Größen, bewahrt Originalwerte und Quellen, verlangt explizites Mapping und erzeugt einen Prüfbericht. Sie führt keinerlei Makros, Formeln oder Buchungen aus. Dauerhafter Import, Mappingfreigabe und Wiederholungsschutz in einer Datenbank sind noch nicht implementiert.

## MVP-02 – Verwendung und Datenvertrag

```csharp
using Betriebsplattform.Import;

var profil = new Importprofil("SYNTH-ISD", new Dictionary<string, string>
{
    ["Firma A"] = "FIKTIV-A",
    ["Firma B"] = "FIKTIV-B"
});
using var datei = File.OpenRead("synthetisch.xlsx"); // ausschließlich künstliche Testdatei
var bericht = new ExcelImportvorschau().Pruefen(datei, "synthetisch.xlsx", profil);
Console.WriteLine(bericht.AlsText());
```

Der Leser öffnet selbst keine Pfade und hält keine Historie. Er liest ab der aktuellen Streamposition bis zum Ende und lässt den Stream offen. `Importprofil` kopiert das Unternehmensmapping und optionale Vergleichsmengen `bekannteDateiHashes`, `bekannteQuellschluessel` und `bekannteRechnungen`. Nicht zugeordnete Unternehmen werden gesperrt; Namen werden nicht geraten. Quellschlüssel bestehen aus Quellsystem, ausdrücklicher Art und Datensatz-ID. Rechnungskennungen bestehen zusätzlich aus Ausstellerreferenz und Rechnungsnummer. Bei einer Dublette innerhalb der Datei werden alle betroffenen Zeilen gesperrt. Ein veränderter Betrag mit gleicher Quellkennung gilt ebenfalls als prüfpflichtige Wiederholung. Fachliche Zahlungsdubletten mit verschiedenen externen IDs werden noch nicht erkannt.

Die Vorlage besteht genau aus `xl/worksheets/sheet1.xml`, einem sichtbaren Blatt `ISD-Testdaten` und neun festen Spalten in Zeile 1: `DatensatzId`, `Art`, `Nummer`, `Von`, `An`, `Datum`, `Betrag`, `Waehrung`, `Beleg`. Die tatsächliche Blattbeziehung wird geprüft. Nummer ist nur bei Rechnung Pflicht; Datensatz-ID und Beleg sind bei beiden Arten Pflicht. Typen `inlineStr` und `s` liefern Texte, `n` Beträge, `d` ein ISO-Datum. Gemeinsame Texte und einfache Rich-Text-Läufe werden unterstützt. Führende Nullen in textuellen Kennungen bleiben erhalten; numerische Kennungen werden nicht als Text geraten. Datum verlangt `JJJJ-MM-TT`; Excel-Seriendaten, Uhrzeiten und das 1900-/1904-Datumssystem werden nicht interpretiert.

Beträge sind strikt positiv und centgenau: Zahlenzellen mit Punkt, Textbeträge mit Komma, jeweils höchstens zwei Nachkommastellen, keine Gruppierungszeichen oder Exponenten. Eine ganzzahlige Cent-Prüfung verhindert Rundung beim Einlesen übergroßer Werte. Anschließend werden die vorhandenen Rechnung-/Zahlungskonstruktoren verwendet. Nur ausdrückliches `EUR` ist zulässig. Rand-Leerzeichen werden nur für die Validierung entfernt und als Warnung gemeldet. Formatindizes bleiben erhalten; nichttriviale Excel-Anzeigeformatierung wird nicht interpretiert und erzeugt eine Warnung.

`Importbericht.Zeilen` enthält unveränderliche Originalzellen und adressierte Meldungen. Jede Zelle hat Datei-SHA-256, Blatt, Zeile, Zelladresse, OOXML-Datentyp, unveränderten logischen Rohwert, aufgelösten Text, gegebenenfalls Formel und Formatindex. Bei Shared Strings bleiben Originalindex und aufgelöster Text getrennt erhalten. Fehlende Zellen erhalten eine explizite leere Quellposition. Die Originaldatei selbst wird nicht gespeichert; XML-Entity-Schreibweisen und visuelle Excel-Darstellung sind keine rekonstruierten Originalwerte. Der Hash identifiziert die vollständig gelesenen Dateibytes, nicht deren Echtheit.

Nur Zeilen ohne Fehler enthalten einen ungespeicherten `Rechnung`-/`Zahlung`-Kandidaten mit Beleg und Quellenort. Die Kandidaten-ID ist deterministisch aus dem Quellschlüssel abgeleitet und ersetzt keine künftige Datenbankbedingung. Kandidaten anderer Zeilen können auch bei einem insgesamt fehlerhaften Bericht sichtbar sein: Das ist keine Teilimportfreigabe. Dateistrukturfehler liefern dagegen keine Teilvorschau. Jede Zahlung bleibt unbestätigt. Der Textbericht nennt Status, Zeilenzahlen, geprüfte Beträge und adressierte Fehler/Warnungen; freie Zelltexte werden nicht automatisch in Terminal oder Logs ausgegeben.

## MVP-02 – Sicherheitsgrenzen

Limits: 2 MiB Eingabedatei, 16 ZIP-Einträge, 2 MiB pro entpacktem Teil, 8 MiB insgesamt, Kompressionsverhältnis höchstens 100:1, XML-Tiefe höchstens 32, 1000 Datenzeilen bis einschließlich Zeile 1001, neun Spalten, 10000 Shared Strings und 32767 Zeichen pro Zelle. Fachliche Pflichttexte sind auf 256 Zeichen begrenzt. Zu große Dateien erhalten keinen irreführenden Hash über einen bloß gelesenen Ausschnitt.

Es wird nichts auf das Dateisystem entpackt oder ausgeführt. ZIP-Pfade und Paketbestandteile folgen einer Positivliste; aktive/zusätzliche Inhalte, externe Beziehungen, DTDs, externe XML-Entitäten, Makroformate, verbundene Zellen und versteckte Daten werden abgewiesen. Formeln einschließlich gültig aussehender Cachewerte sperren die jeweilige Zeile. Doppelte ZIP-Namen, Zelladressen und Inhaltstypdeklarationen werden abgewiesen. XML-Lesen und tatsächliche dekomprimierte Größen sind zusätzlich begrenzt. Fehlerberichte enthalten keine internen Ausnahme-/Dateipfaddetails.

Dies ist keine vollständige OpenXML-Schemavalidierung, kein Virenscanner und kein allgemeiner Excel-Reader. Regulär in Excel erzeugte Dateien mit weiteren Paket-/Blattmerkmalen können abgewiesen werden. Keine Excel-Anwendung, Makroengine, Netzwerkabfrage, Datenbank oder Unternehmensdaten wurde zum Testen benötigt. Ressourcen- und Negativtests sind keine allgemeine Sicherheitsgarantie. Vor breiter XLSX-Unterstützung oder realen Daten ist ein erneutes Format-/Bibliotheks- und Securityreview erforderlich.

## Vorhandene und geplante Verzeichnisstruktur

```text
README.md
CONTRIBUTING.md
docs/public/       Öffentliche Produkt- und Architekturdokumentation
src/backend/       Solution, Rechnungskern, Importvorschau und lokaler Webhost
src/frontend/      React/TypeScript/Vite, Designregeln und DOM-Tests
tests/repository/  Prüfungen der öffentlichen Ablage
tests/backend/     xUnit-Fach-, XLSX-, Security- und HTTP-Prüfungen
```

Es gibt keine Datenbankintegration und keine leeren Infrastrukturprojekte.

## MVP-03 – lokaler vertikaler Ausschnitt

Der Host erzeugt beim Start einen eigenen synthetischen Kern, berechnet daraus eine unveränderliche Rechnungsansicht und teilt ausschließlich diese Ansicht zwischen parallelen GET-Anfragen. Kein geteilter veränderlicher Rechnungskern und keine schreibenden Endpunkte. Drei Rechnungen zeigen Vollzahlung (1280 EUR), zwei Teilzahlungen (300 und 200 EUR auf 960 EUR) und eine unbestätigte Zahlung (450 EUR). Offener Gesamtbetrag: 910 EUR. ISD ist je nach Vorgang Empfänger beziehungsweise Aussteller; daraus wird keine allgemeine Rolle abgeleitet.

`GET /api/uebersicht` liefert Rechnungen, saldenwirksame Zuordnungen, unbestätigte Zahlungen, Beteiligte, Datum, Beleg und Herkunft. Geldwerte werden serverseitig als exakte formatierte Texte ausgegeben; im Frontend gibt es weder Geldberechnung noch Konvertierung in JavaScript-Fließkommazahlen.

`GET /api/importvorschau/{beispiel}` erlaubt nur `gueltig`, `fehler` und `dublette`. Der Host erzeugt jeweils eine tatsächliche künstliche XLSX-Datei im Speicher und verwendet denselben geprüften Importleser. Die Antwort enthält Hash, Zeilen, Originalwerte, Datentypen, normalisierte Beteiligte/Beträge und Meldungen. Keine fremden Pfade, Uploads, Buchungen, automatische Bestätigungen oder gespeicherten Importhistorien.

Kestrel bindet fest an `127.0.0.1:5180`; zusätzliche konfigurierbare Kestrel-Endpunkte werden für diesen Prototyp nicht übernommen. Host und Origin werden geprüft. Der gebaute Frontendstand kommt vom selben Origin; nur für den lokalen Vite-Proxy ist Origin `http://127.0.0.1:5173` zusätzlich erlaubt, ohne allgemeine CORS-Freigabe. Statische Dateien werden ausschließlich aus dem Frontend-`dist` des Repository-Layouts ausgeliefert. Fehlender Build liefert eine verständliche 503-Antwort. Konsolenlogging benötigt keine Windows-Ereignisprotokollrechte. Keine externen Fonts, Analyse- oder Bildressourcen. CSP ohne Inline-Skripte, `nosniff`, `no-store` und Referrer-Begrenzung ergänzen die lokale Grenze.

Das Frontend validiert den JSON-Vertrag samt Kennzeichnung `synthetisch`, zeigt Lade-/Fehler-/Leerzustände und bietet einen erneuten Versuch. Requests werden beim Ansichtswechsel abgebrochen; ein Timeout begrenzt Wartezeiten. Rechnungsübersicht und Importvorschau sind per Navigation erreichbar. Originalwerte werden als React-Text dargestellt, nicht als HTML oder ausführbare Formel. Bei 375 px bleiben breite Quelltabellen in einem eigenen Scrollbereich. Gestaltungsregeln stehen in `src/frontend/DESIGN.md`.

Dieser Stand ist ein lokaler Entwicklungsprototyp ohne Anmeldung und ohne echte Daten. Loopback und Originprüfung ersetzen kein produktives Rechte-/Mandantenkonzept. Kein abgesicherter Smartphone-Fernzugriff, kein Deployment, keine allgemeine Excel-Kompatibilität und keine vollständige Barrierefreiheitszertifizierung. Eine Datenbank-/Upload-/Authentifizierungsstrecke benötigt eigene Entscheidungen und Tests.

## Öffentliche und private Ablage

Öffentliche Produktquellen und Tests bleiben von privaten lokalen Arbeitsunterlagen getrennt. Statt persönliche Playbooks zu bereinigen und mitzuveröffentlichen, werden sie vollständig von Git ausgeschlossen. Dadurch bleiben lokale Arbeitsabläufe erhalten; ein öffentlicher Klon enthält diese optionalen Unterlagen nicht.

Diese Trennung folgt dem Auftrag zur öffentlichen Vorbereitung. Sie benötigt erneut eine Prüfung, sobald öffentliche Tests von privaten Dateien abhängen, neue Root-Verzeichnisse hinzukommen oder gemeinsame Entwickleranweisungen veröffentlicht werden sollen. Dann sind eigenständige, neutrale Anweisungen zu erstellen und ohne private Unterlagen zu testen.

## Vor einem produktiven Einsatz

Benutzer- und Rollenrechte, Audit, sichere Updates, Monitoring, Backups mit nachgewiesenem Restore-Test sowie Betriebs- und Krisenabläufe sind erforderlich. Diese Fähigkeiten sind bisher nicht nachgewiesen.

## Design-/UX-Sprint – bestehende Ansichten

Die verbindliche UI-Quelle ist das [Designsystem](../../src/frontend/DESIGN.md), verknüpft mit den öffentlichen Entwicklungsregeln. Entscheidung: Petrol/kühle neutrale Flächen statt Graphit/warmer Sand; rationale Alternativen und Neuprüfungs-Trigger stehen dort. Die Modulgrenzen und Geldregeln bleiben unverändert.

Der Rechnungsanzeigevertrag enthält zusätzlich `BestaetigtZugeordnet`: exakter serverseitiger Dezimalbetrag aus Rechnungsbetrag minus dem im Kern berechneten offenen Betrag, formatiert wie die übrigen Geldwerte. UI-Filter und Text-/Datumsortierung verändern ausschließlich die angezeigte Liste, niemals Salden. Beide Partnerrollen bleiben ausdrücklich benannt; keine feste ISD-Rolle. Ohne Fälligkeitsdaten kein Überfälligkeitsstatus.

Rechnungsdetails und adressierte Importquellen werden nach der zugehörigen Tabellenzeile zugänglich aufgeklappt. Importfehler, Warnungen und neutrale Zeilenanzeige sind von Zahlungsstatus/Bestätigung getrennt. Technische Herkunft bleibt vollständig erreichbar, ohne die Hauptansicht zu dominieren. Auf Tabletbreite eigener seitlicher Tabellenbereich mit Links-/Rechts-Tasten; bei ≤680 px gestapelte beschriftete Datensätze. Verschachtelte Quelltabellen behalten ihr zweidimensionales Layout.

`darstellung.ts` enthält reine Suche-/Sortier-/Status-/Datumsdarstellung und regionale Metadaten für Deutschland, Österreich und Schweiz. Aktive Oberfläche de-DE/EUR; keine CHF-Verarbeitung, keine Umrechnung, keine Steuerregeln. Regionale Geldformatierung benötigt später einen erweiterten serverseitigen Darstellungsvertrag. Keine zusätzlichen Pakete, Endpunkte, Datenbankobjekte oder externen Ressourcen.
