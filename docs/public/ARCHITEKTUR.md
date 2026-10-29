# Architektur und Projektstruktur

## Aktueller Quellstand – Dokumentpipeline 21.10.2026

Die folgenden MVP-Abschnitte beschreiben teilweise historische Entwicklungsstufen. Aktuell existieren neben dem Demo-Fachkern PostgreSQL-/EF-Adapter für Kontakte, Akten, Dokumente, Notizen, Korrespondenz, Rechnungen/Zahlungen, Konten/MFA und Aufgaben. Finanzielle Schreiboperationen sind interne Adapterfunktionen, keine vollständige Produktstrecke aus UI und HTTP-Endpunkten. Strukturierter Excelimport bleibt auf die flüchtige Demo beschränkt.

Posteingang 2.0 ergänzt bestehende Dokument-Eingangsereignisse um Prüfung, bestätigte Angaben, Absender-/Aktenbezug, Zuständigkeit, Historie und ausdrücklich bestätigte Folgeaufgabe. Der authentifizierte Host verwendet PostgreSQL; die Development-Vorschau simuliert den Workflow und Aufgaben im Browser und verwendet für Originaldokumente den bisherigen Demo-Host. Die ältere Demoansicht bleibt an ihren eigenen API-Vertrag gebunden. Optionale lokale PDF-/OCR-Verarbeitung und bestätigte Feldkorrekturen sind jetzt angebunden; keine KI-Verarbeitung oder automatische Buchung.

Der produktive Malware-Adapter fehlt weiterhin: reguläre Dokumentannahme im Kontenbetrieb bleibt kontrolliert gesperrt. Synthetische Katalogdateien in Tests sind kein Scannerersatz. Echte PostgreSQL-/HTTPS-/Restoretests sowie Browser–HTTPS–PostgreSQL-Prüfungen sind nachgewiesen. Die menschliche Pilotabnahme fehlt weiterhin. Details und Grenzen stehen im [Entwicklungsnachweis](MVP-01-NACHWEIS.md).


### Dokumentprüfung und kontrollierte Muster

Der bestehende sichere Dokumenteingang und Originalspeicher bleiben die einzige Eingangsstrecke. `Dokumentanalysator` verbindet optionale lokale PDF-/OCR-Adapter mit gemeinsamen Seiten, Textabschnitten, Feldern, Herkunft und Prüfstatus. Digitaler Text wird direkt verwendet; OCR läuft nur auf Seiten ohne ausreichend Text. Grundlinien aus PDF-Text und echte OCR-Rahmen sind ausdrücklich verschieden. Positionen sind auf die gerenderte Seite mit Ursprung links oben normiert; Rotation und Seitenmaße bleiben erhalten. Vorschau wird bei Bedarf erzeugt, Originale bleiben unverändert.

Die Prüfoberfläche im Posteingang verbindet Bild, Feldnavigation, Zoom und korrigierbare Angaben. Bestätigte Angaben bleiben getrennt von der wiederverwendbaren Analyse. Feldkorrekturen besitzen Audit und Quellenbezug; manuelle Eingaben dürfen ohne Position bestehen. Muster sind an die bestehende Organisationsfirma der Akte, Absender, Layout, Feldstrategie und unveränderte Prüfbelege gebunden. Ein zweiter unterschiedlicher Monatsbeleg ist für eine administrative Aktivierung erforderlich. Muster lesen Werte aus dem neuen Dokument; alte Rechnungswerte werden nicht kopiert. Layoutabweichung, Quellenänderung und Widerruf führen zurück zur Prüfung.

Automatik ist ausschließlich für die ausdrücklich administrativ freigegebene interne Klassifizierung implementiert. Keine automatische Zahlung, Aktenanlage, externe Kommunikation oder rechtliche Fristentscheidung. Stufe 4 ist noch nicht implementiert. Der persistente, standardmäßig deaktivierte Scanordner verwendet denselben Eingang mit Leasing, begrenzten Versuchen und idempotenter Wiederaufnahme. WIA-Geräteinventar ist real angebunden; Aufnahme und Browser-Companion bleiben ohne Hardwareabnahme blockiert. Produktiver Fremdeingang bleibt ohne Malwareadapter geschlossen.

Details, Alternativen und Grenzen: [ADR 0005](adr/0005-lokale-dokumentanalyse-und-kontrollierte-muster.md).

## Aktueller lokaler Ausschnitt – nach DOK-01

Die Rechnung nutzt eine gemeinsame Kontaktaktionsgruppe und interne native PDF-Vorschau. Ein `DemoImportbestand` hält kontrolliert hochgeladene synthetische XLSX-Dateien, Prüfergebnisse, bestätigte Firmenzuordnung, Zeilenauswahl und Entscheidungszeit im Prozessspeicher. Der bestehende MVP-02-Leser und die gemeinsame Anzeigeprojektion in `DemoDaten` werden wiederverwendet; importierte Rechnungen durchlaufen den vorhandenen Fachkern bei der Anzeige. Kein zusätzliches Paket, PDF-Dienst oder zweiter Frontendweg.

GET `/api/importe` und `/api/importe/{id}` liefern die Demo-Prüfhistorie. POST `/api/importe/vorschau` akzeptiert begrenzte Rohbytes mit `application/octet-stream`, Dateinamenheader und ausdrücklichem Demoheader; POST `/api/importe/{id}/mapping` bestätigt Blatt/Firmen, POST `/api/importe/{id}/uebernehmen` bestätigt ausgewählte gültige Rechnungen. Schreibaktionen verlangen zusätzlich einen vorhandenen erlaubten Origin. Loopback-/Hostgrenzen bleiben erhalten. Kein CORS-Wildcard, Dateipfad oder temporäres Uploadverzeichnis. GET `/api/importvorlage/{beispiel}` liefert ausschließlich lokal erzeugte synthetische Übungsdateien.

Maximal 2 MiB pro Datei, 20 Dateien und 5.000 importierte Rechnungen pro Prozess. Derselbe Lock schützt neue Prüfung, Dublettensets und atomare Übernahme. Bytegleiche Dateien liefern denselben Vorgang; erneute identische Bestätigung liefert denselben Abschluss. Veränderte Wiederholungen werden anhand Quellsystem/Art/Datensatz-ID und Aussteller/Rechnungsnummer erneut geprüft. Originalbytes, Zellwerte, Mapping, Auswahl und Zeit bleiben bis zum Neustart erhalten. Keine automatische Zahlungsübernahme, Bestätigung oder Zuordnung.

Die bisherige CSP-Sperre der Anwendung gegen Einbettung bleibt bestehen. Nur bekannte selbst erzeugte Demo-PDF-Antworten erhalten `frame-ancestors 'self'`; der Arbeitsraum erlaubt Same-Origin-Frames. Skripte, fremde Frames und Objekte werden dadurch nicht allgemein freigegeben. Native PDF-Werkzeuge können lokale Kopien erzeugen; der Server besitzt keinen PDF-Schreibendpunkt. Diese Ausnahme gilt nicht als Freigabe fremder PDFs.

Notizen sind begrenzter sessionStorage je Rechnung im aktuellen Browser-Tab mit synthetischem Benutzer und Zeit; keine serverseitige Historie oder Teamfunktion. Die folgenden MVP-/DOK-01-Abschnitte beschreiben die ursprünglichen Ausschnitte. Aktuelle Ergänzung und Revisionsgrenzen: [ADR 0002](adr/0002-lokaler-demoimport-und-pdf-vorschau.md).

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

## Arbeitsraum – kontrollierte UI-Integration

**Entscheidung 18.10.2026, vorläufig angenommen:** Die vom Nutzer akzeptierte Arbeitsraumrichtung ist der normale React-Einstieg. Alternative dauerhaft separater Design-App verworfen: gemeinsame Daten-/Anzeigewege sind wartbarer und leichter zu prüfen. App.tsx enthält Shell und Navigation; Rechnungen.tsx, Rechnungsdetails.tsx und Importvorschau.tsx die Ansichten. ui.tsx bündelt wiederverwendbare Daten-/Status-/Meldungs-/Titel-/Kennzahlen-/Tastaturbausteine. design-tokens.css ist die einzige aktive Palette, Arbeitsraum.css die Komponenten-/Layoutregeln, style.css die Grundregeln. Historische Vorschaupfade exportieren nur dieselbe App beziehungsweise enthalten einen Hinweis; sie erzeugen keine zweite Runtime. Frühere Arbeit ist privat als verifizierte Rückfallsicherung erhalten.

Rückkehr aus Details über sichtbaren Button oder Breadcrumb erhält Suche, Zahlungsfilter, Sortierung und Fokus auf dem Rechnungsknopf. Tabelle nennt Rechnungsbestand und Aussteller/Empfänger; keine unbelegte Einordnung als Kunde, Lieferant, Ausgangs-/Eingangsrechnung oder andere Verpflichtung. Getrennte zukünftige Vorgangstypen sind möglich, aber noch nicht modelliert oder als Navigation angeboten.

API-/Import-/Finanzverträge bleiben unverändert. Der Rechnungsanzeigevertrag enthält BestaetigtZugeordnet als formatierten serverseitigen Dezimalbetrag; das Frontend filtert/sortiert nur. Originalwerte bleiben React-Text, Quellen und Prüfbericht erreichbar. Kein neuer Endpunkt, keine Persistenz, keine Dokumentenaktion. Entscheidung bei tatsächlichen neuen Modulen, Auth/Mandanten, Detail-Deep-Links oder nach Funktionsabnahme erneut prüfen. Gestaltungsdetails ausschließlich im [Designsystem](../../src/frontend/DESIGN.md).

## Geplante Dokumenten- und Belegakte – kompakter Fachentwurf

**Status: Zielkonzept; nur der nachfolgend beschriebene synthetische DOK-01-Lesepfad ist implementiert.** Ziel ist eine vorgangsbezogene Akte für geeignete Rechnungen, Forderungen/Verpflichtungen und weitere Geschäftsvorgänge. Heute ist nur die synthetische Rechnungs-/Zahlungs-/Importstrecke vorhanden. Kein generisches Finanzmodell, Upload, Viewer, OCR oder PDF-Editor in diesem Sprint. Die aktuelle Detailansicht zeigt nur den ehrlich beschrifteten fehlenden Dateibestand.

### Fachliche Bausteine und Invarianten

| Baustein (Vorschlag, noch kein Code) | Kleinster Inhalt / Regel |
| --- | --- |
| Vorgangsreferenz | UnternehmenId, stabiler Vorgangstyp und VorgangId. Der heutige Frontendvertrag liefert keine solche allgemeine Identität; im Folgepaket explizit definieren. Keine Zuordnung allein über Dateiname oder Rechnungsnummer ohne Unternehmen. |
| Dokument | DokumentId, UnternehmenId, Dokumentart, ursprünglicher Dateiname, Herkunft/Quellenreferenz, erfasstes Dokumentdatum soweit belegt und Erfassungsdatum. Mehrere Anhänge pro Vorgang über explizite Zuordnung. |
| Unveränderliche Dateiversion | VersionId, DokumentId, Version, MIME/Format, Bytezahl, SHA-256, ursprüngliche Bytes, Eingangsquelle, Erfassungsautor/-zeit. Neue Version ersetzt keine Originalbytes. Hash identifiziert Bytes, bestätigt keine Echtheit oder Zahlung. |
| Vorgang–Dokument-Zuordnung | Unternehmen, Vorgangsreferenz, DokumentId, Zuordnungsautor/-zeit. Mehrfachverwendung eines Dokuments nur ausdrücklich und mit passenden Rechten; kein unternehmensübergreifendes Deduplizieren mit Datenoffenlegung. |
| Notiz / Kommentar | Text, Bezug auf Vorgang oder konkrete Dokumentversion, authentifizierter Autor und serverseitiger UTC-Zeitstempel. Änderungen mit Revision/Vorgänger nachvollziehen, keine heimliche Originaländerung. |
| Bearbeitung / Prüfung | Eigener Dokumentenstatus und Ereignisse wie Geprüft/Bearbeitet mit Autor/Zeit/Begründung. Keine Änderung von Zahlungsbestätigung, Zahlungszuordnung oder Rechnungssaldo. |
| Anmerkung / Stempel | Separate Annotation mit Position/Seite/Version/Autor/Zeit; optional abgeleiteter Export mit eigener Version/Hash und Bezug zum Original. „Bezahlt“ ist ein Bearbeitungsvermerk, keine Zahlung oder Bankbestätigung. |
| Audit / Export | Ereignisse für Zuordnung, Öffnung soweit erforderlich, Notizrevision, Prüfung und Export; referenzierte Version/Hashes, autorisierter Akteur, Zeit und Zweck. Exportmanifest und Herkunft erhalten; keine sensiblen Vollinhalte in technischen Logs. |

**Finanzielle Sperre:** Dokumentenvermerke, einschließlich „Bezahlt“, dürfen keinen Zahlungsvorgang, keine Bestätigung, Zuordnung, Buchung oder Saldenänderung auslösen. Die Finanzmodule bleiben alleinige Quelle dieser Zustände. Dokumentenprüfung und Zahlungsnachweis werden nebeneinander mit eigener Bedeutung angezeigt; keine Gleichsetzung. Bestehende Originaldateien werden niemals mit Stempeln oder Notizen überschrieben.

### Original, Scan und E-Rechnung

Ein digital eingegangenes Original wird mit seinen ursprünglichen Bytes/Herkunft aufbewahrt. Ein PDF-Scan bildet Papier ab und wird als Scan gekennzeichnet; Textsuche kann fehlen. Eine strukturierte E-Rechnung ist nicht auf eine PDF-Ansicht reduzierbar: beispielsweise definiert XRechnung Rechnungsinformationen als XML-Datensatz. Strukturierte Quelldaten und gegebenenfalls weitere Bestandteile bleiben erhalten; gerenderte PDF ist eine abgeleitete Darstellung, keine Ersetzung der Quelle. Format-/Schema-/Fachvalidierung ist ein späterer eigener Schritt, keine heutige Konformitätszusage. Technische Primärquelle, gelesen 18.10.2026: `https://xeinkauf.de/xrechnung/`.

OCR ist optional und nachgelagert: Original unverändert, extrahierter Text/Wert mit Quellseite/Position, Werkzeugversion und Unsicherheit, Korrekturfreigabe durch Menschen. Keine automatische Übernahme in Zahlung, Buchung, Frist oder andere finanziell wirksame Aktion. Ein OCR-Text oder sichtbarer Stempel belegt keine Echtheit.

### Rechte, Datenschutz und Dateisicherheit

- Unternehmenskontext serverseitig aus vertrauenswürdiger Sitzung bestimmen; Rechte für Liste, Dateiöffnung, Notiz, Prüfung und Export jeweils am Vorgang und Dokument prüfen. Dateikennung oder vom Browser gelieferte UnternehmenId reicht nicht. Rollen/Minimalrechte, Widerruf und Negativfälle über verschiedene Unternehmen vor realen Dateien nachweisen. Der heutige Loopback-Prototyp ohne Anmeldung erfüllt diese Produktionsvoraussetzungen nicht.
- Originale außerhalb öffentlich erreichbarer Webpfade, Speicherung/Dateinamen nicht aus beliebigen Browserpfaden; nur IDs über autorisierten Handler. Format-/Größen-/Signaturprüfung, keine alleinige MIME-/Endungsannahme. Spätere Quarantäne und geeignete Schadsoftwareprüfung vor Freigabe, keine Ausführung aktiver PDF-Inhalte oder externer Verknüpfungen. Grenzen, Ressourcenverbrauch, verschlüsselte/beschädigte Dateien und Parserfehler testen. Primärquelle: `https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html` (18.10.2026); daraus keine Sicherheitsgarantie ableiten.
- PDF-Betrachter getrennt vom Finanzkern, kontrollierter Dateiabruf, keine externen Viewer-/OCR-Dienste ohne Datenfreigabe. Suche/Zoom nur als verfügbar ausweisen, wenn der konkrete Betrachter sie nachweislich bietet. Bestehende CSP enthält object-src 'none'; sie wird hier nicht verändert. Eingebetteter Viewer oder zusätzliche Bibliothek benötigt gesonderte Sicherheits-/Abhängigkeitsprüfung. Alternative erster Lesepfad: nativer Browserbetrachter in separatem Tab ohne Einbettung, nur für feste synthetische Dateien und nach tatsächlichem Browsertest.
- Datenschutz-/Aufbewahrungstrigger sind offen: je Unternehmen/Land DE/AT/CH, Dokumentart, Inhalt, Zweck, Verantwortlichem und rechtlichem Kontext klären. Versicherungs-/Krankenkassen-/Personalunterlagen können besonders schutzbedürftig sein. Aktuell keine realen Daten, keine nationale Frist oder Steuerregel erfinden. Fachstelle/Datenschutz-/Steuerberatung bestätigt Anwendbarkeit, Aufbewahrung, Sperre, Löschregeln und Exportanforderungen vor realem Einsatz; technische Abdeckung derzeit nicht implementiert. Keine unbegrenzte Speicherung personenbezogener Notiztexte als pauschale Auditregel.
- Vor realer Nutzung: Auth/Mandantentrennung, Backups mit Restore-Test, Integritäts-/Versionsprüfung, Zugriffsaudit, Export-/Löschkonzept und abgesicherter Betrieb. Keine revisionssichere/GoBD-/DSGVO-Konformität oder Echtheitsgarantie allein aus Hash/Historie behaupten.

### Kleinster nächster vertikaler Schnitt – DOK-01 (nur Vorschlag)

**Synthetische Dokumentenliste und lesender Originalaufruf für genau einen Demo-Rechnungsvorgang**, zwei selbst erzeugte Test-PDFs mit klarer Kennzeichnung. Kein Upload, Notizschreiben, Stempeln, OCR, PDF-Verändern oder Finanzschreiben. Fixtures bei Tests lokal erzeugen statt pauschale PDF-Ausnahme in .gitignore. Fachlich stabile Demo-Vorgangs-/Dokument-/Versionskennung und Zuordnung zuerst in Tests festlegen; bestehende Finanzverträge nicht ungeprüft erweitern.

1. Kleiner read-only Dokumentendienst mit festem synthetischem Unternehmenskontext, unveränderlichen Bytes und Metadatenliste; keine beliebigen Pfade/URLs. Demo-IDs und zulässige Zuordnungen explizit. Das ist kein Ersatz produktiver Anmeldung.
2. Detailansicht listet zwei erkennbare Anhänge mit Art, Name, Größe, Datum/Quelle und Version. Öffnen lädt genau die zugeordnete Testdatei über lokalen Handler in einen nachweislich nutzbaren Browserbetrachter. Fehlender Betrachter/Datei klar melden, keine Schein-Buttons. MIME, Disposition, Ressourcenlimits und bestehende CSP gezielt prüfen; keine pauschale Lockerung.
3. Akzeptanztests: richtige Anhänge pro Vorgang/Unternehmen; fremde Unternehmens-ID/Zuordnung, unbekannte ID und Pfad-/Formatangriffe abweisen; unveränderte Byte-/Hashwerte nach Öffnen; keine zusätzlichen Zahlungs-/Zuordnungsereignisse; Tastatur, Fehlermeldung, Suche/Zoom soweit tatsächlich vorhanden und Smartphone. Geprüft-/Notiz-/Stempelfunktionen bleiben ausdrücklich später.

Nach erfolgreicher menschlicher Abnahme dieses Lesepfads erst ein getrennt freizugebendes Paket für Notizen/Bearbeitungshistorie planen. Speicherung/Viewerwahl und verbleibende Rechte-/Sicherheitsfragen vor dessen Implementierung konkretisieren. Dieser Entwurf erteilt keine automatische Freigabe des Folgepakets.

## Kontakt-Sofortaktionen und zweiseitige Vorgänge – 18.10.2026

### Implementierter kleiner Schnitt

Der reguläre Arbeitsraum bleibt unverändert die einzige App. Rechnungsansicht besitzt jetzt **optionale** AusstellerKontakt/EmpfaengerKontakt-Felder mit Ansprechpartner, E-Mail, Telefon und Quelle. Alte Antworten ohne diese Felder bleiben im Frontend gültig, alte C#-Konstruktoraufrufe durch Default null möglich; alte Felder/Importverträge bleiben unverändert. Ein alter Client kann zusätzliche JSON-Felder ignorieren. Es ist eine additive Demo-Erweiterung, keine versionierte produktive CRM-API oder persistente Kontaktidentität.

DemoDaten ordnet Kontakte über DEMO-NORD/DEMO-ISD zu, bevor Kennungen in Anzeigenamen umgesetzt werden; keine Namensähnlichkeit als Zuordnungsregel. Musterbetrieb West besitzt keine Kontaktangaben. Rollen kommen aus dem konkreten Rechnungsvorgang (Rechnungsaussteller/Rechnungsempfänger), nicht aus dem Kontakt. Dieselbe Organisation wechselt ihre Rolle zwischen Rechnung 001 und 002. Keine allgemeine Einstufung als Kunde/Lieferant und keine Ableitung eines Zahlungsempfängers aus einer E-Mail-Adresse.

Entscheidung: gemeinsame Kontaktkarte und kleine URI-Helfer statt CRM/zusätzlichem Endpoint oder im Frontend erfundener Kontaktdaten. Zwei explizite Kontakte am vorhandenen Anzeigeobjekt sind der kleinste rückwärtskompatible Lesepfad. Konsequenz: diese Projektion ist keine Kontaktverwaltung; Organisations-/Personenidentität, Zuständigkeit und verifizierte Gültigkeit fehlen noch. Bei produktiver Kontaktpflege, weiteren Vorgangsarten oder Versionierung der API diese Übergangsentscheidung neu prüfen.

Kontaktkarte zeigt Organisation/Rolle, vorhandenen Ansprechpartner, benannte Brief-/Telefonaktionen und fehlende/ungeeignete Werte als Text. Kontaktquelle ist separat aufklappbar. mailto enthält genau eine validierte ASCII-Adresse und optional den kodierten Betreff „Rechnung <Nummer>“. Keine Anhänge, Nachrichtentexte, cc/bcc oder frei eingebrachten Header. Ungeeignete Referenz unterdrückt nur den Betreff. tel akzeptiert globale Nummer mit + und 7–15 Ziffern, entfernt nur zulässige Leerzeichen/Bindestriche; keine lokalen Rufnummern ohne Länderkontext, Durchwahlen oder URI-Parameter. Konservative Formatsyntax, **keine Erreichbarkeits- oder Eigentumsbestätigung**. Internationalisierte/komplexe Mailadressen und weitere Telefonformate sind noch nicht unterstützt.

Links werden ausschließlich vom Benutzer aktiviert und an dessen konfigurierte E-Mail-/Telefonie-Anwendung übergeben. Kein Versand/Telefonat durch den Server und keine Erfolgsbehauptung. Empfänger und optionaler Rechnungsbetreff werden dabei an die konfigurierte Anwendung übergeben; vor echten Daten ist dieser lokale Übergabepfad samt Datenminimierung/Providerkontext zu bewerten. Nicht erreichbare synthetische Kontakte sind ausdrücklich gekennzeichnet: .invalid-Mailadressen, Telefonnummer +49 30 23125 001 aus dem Filmrufnummernbereich Berlin. Amtliche Quelle gelesen 18.10.2026: `https://www.bundesnetzagentur.de/DE/Fachthemen/Telekommunikation/Nummerierung/DramaNumbers/artikel.html`. Es wurden keine Nachrichten gesendet oder Anrufe ausgelöst.

### Primäre Darstellungsorte in Details

- **Rechnungsgrundlage**: Belegreferenz und Eingangs-/Quellenort der Rechnung einmal im Nachweisbereich. Aktuell nur Referenzen, keine Originaldatei.
- **Dokumente zum Vorgang**: genau ein Hinweis auf fehlende Originaldateien und noch nicht verfügbare Dokumentenaktionen. Der frühere zusätzliche Satz zum selben fehlenden Dateibestand ist entfernt.
- **Zahlungsnachweis anzeigen**: eigene aufklappbare Referenz/Quelle je einzelner Zahlung. Nicht mit Rechnungsgrundlage zusammengeführt. Die Darstellung bleibt derzeit die Quelle der Zahlung; separate Bankbestätigungs-/Zuordnungsnachweise besitzt der Kern, der heutige Anzeigevertrag liefert diese noch nicht vollständig aus.
- **Kontaktquelle**: Herkunft der Kontaktangaben am Kontakt. Kein Dokumenten- oder Zahlungsbeleg.

Rechnungsnummer, Orientierung und Beträge können im passenden Tabellen-/Detailkontext wieder erscheinen; keine zentrale Quelle wird dafür doppelt gespeichert. Dokumentenstatus, Zahlung, Bestätigung und Zuordnung bleiben fachlich getrennt.

### Fachliche Matrix: zwei Zahlungsrichtungen

**Nur synthetische Beispiele; Perspektive für diese Analyse ausdrücklich Beispielbetrieb Nord.** Der heutige API-Vertrag legt noch kein „eigenes Unternehmen“ fest. Seine Gesamtsumme ist deshalb keine Forderungs-/Verbindlichkeitssumme und kein saldierter Unternehmenszahlungsstand. UI weist auf die fehlende Unterscheidung hin; keine Umdeutung der bisherigen 910,00 EUR.

| Vorgang / Perspektive Nord | Betrag und bestätigte Zuordnungen | Rest | Heutiger Nachweis / fehlende Funktion |
| --- | --- | --- | --- |
| Ausgangsrechnung 001: Nord stellt ISD Rechnung; erwarteter Zahlungseingang | 1.280,00 EUR; 1.280,00 EUR | 0,00 EUR Forderung | Bestehender synthetischer Demofall, bezahlt. Explizite Richtung im Modell fehlt. |
| Eingangsrechnung 002: ISD stellt Nord Rechnung; eigener Zahlungsausgang | 960,00 EUR; 300,00 + 200,00 EUR | 460,00 EUR Verpflichtung | Bestehender Demofall: Nord Zahler, ISD Zahlungsempfänger. Zwei bestätigte Teilzahlungen sind abbildbar. Keine ausführbare Überweisung. |
| Ausgangsrechnung 003: Nord an West; Zahlung noch unbestätigt | 450,00 EUR; 0,00 EUR bestätigt (450,00 EUR zur Prüfung) | 450,00 EUR Forderung | Bestehender Demofall: Prüfung reduziert nichts. |
| Ausgangs-/Eingangsrechnung, spiegelbildlicher neuer Test | Je 240,00 EUR; 60,00 + 40,00 EUR, später weitere bestätigte 140,00 EUR | Erst 140,00 EUR, nach letzter Zuordnung 0,00 EUR | Zwei neue Kern-Testfälle prüfen beide Rollen des eigenen Unternehmens, zusätzlich abgewiesene unbestätigte Teilzahlung. Keine neuen UI-Fachdaten. |
| Ratenvereinbarung in beide Richtungen: je 600,00 EUR in drei vereinbarten Raten | Beispiel nur geplant: drei Raten à 200,00 EUR | Abhängig von bestätigter Zuordnung | Einzelne Teilzahlungen möglich, **Vereinbarung/Ratenplan/Fälligkeit/Abweichung nicht modelliert**. Teilzahlung ist kein Nachweis einer Ratenvereinbarung. |
| Versicherungsbeitrag, Behördenforderung, Sozialbeitrag, wiederkehrende Verpflichtung | Beispiel geplant: Beitrag 120,00 EUR, bestätigt 40,00 EUR | 80,00 EUR Verpflichtung | Keine eigenen Vorgangstypen, Perioden oder Pläne. Nicht als Rechnung vortäuschen. Künftige Forderungen in Gegenrichtung ebenfalls eigenständig einordnen. |

**Aus tatsächlichem Kern:** Rechnung besitzt Aussteller/Empfänger; Zahlung Zahler/Empfänger, exakte positive Dezimalbeträge, Datum und Nachweis. Zuordnung fordert bestätigte Zahlung und Zahlungsempfänger = Rechnungsaussteller; sie verhindert doppelte Rechnung–Zahlung-Paare und Überzuordnung. Deshalb funktionieren beide Richtungen, wenn Parteien passend gesetzt werden. Zahler muss derzeit nicht dem Rechnungsempfänger entsprechen (Drittzahlungen möglich); daraus keine automatische Schuldnerregel ableiten. Mehrere Zahlungen/Sammelzuordnung und unzugeordnete Überzahlung sind begrenzt vorhanden, keine produktive Finanzbuchhaltung.

**Noch nicht vorhanden:** eigene Unternehmensperspektive und Mandantenautorisierung, generische Vorgangsart, Forderung/Verbindlichkeit als explizite Dimension, Fälligkeits-/Raten-/Wiederholungsmodell, Konten-/Bankbewegungsabgleich, produktive Buchung, dauerhafte Identitäten im Anzeigevertrag oder Persistenz. Kern kennt keine mehrwährungsfähigen Geldobjekte; bisherige Anzeige/Importstrecke ist EUR-begrenzt. Unternehmensnamen im DTO ersetzen keine unternehmenssicheren IDs. Keine umfangreiche Finanzarchitektur in diesem Sprint geändert; Matrix muss vor einem solchen Folgepaket fachlich abgenommen werden.

### Gemeinsame Vorgangs-, Dokumenten- und Kommunikationsakte (Ziel, nicht implementiert)

Auf dem bestehenden Dokumentenentwurf aufbauen: Ein stabiler unternehmensgebundener Vorgangsbezug verknüpft Rollen/Zuständigkeiten, zentrale Dokumente und Versionen, Kommunikation, Notizen, Aufgaben und die **Referenzen** auf Finanzereignisse. Dateien einschließlich PDF/Scan, Word, Excel, strukturierter E-Rechnung, E-Mail und Anhängen zentral einmal verwalten; selbst erzeugte Schreiben als eigene abgeleitete/versionierte Dokumente mit Vorlage/Version und Quellenbezug. Kein DMS oder Vorlagenbestand pro Fachmodul. Rollen sind Beziehungen je Vorgang, kein unveränderliches Attribut „Kunde“ an jeder Organisation. Finanzkern bleibt Eigentümer von Bestätigung/Zuordnung/Saldo; Kontakt- oder Bearbeitungsereignisse dürfen ihn nicht verändern.

Priorität im aktuellen Detailkontext: E-Mail und Anrufen unmittelbar am passenden Beteiligten. Weitere Kontaktdetails erst mit echter Kontaktstrecke; später Telefonnotiz und Aufgabe/Wiedervorlage in ein kleines kontextbezogenes Menü, Dokument hinzufügen/Original öffnen in den Dokumentenbereich. Schreiben aus Vorlage erst mit zentraler Vorlagen-/Dokumentenfunktion. Rechte, Quelle, tatsächliche Daten und Berechtigung entscheiden, ob eine Aktion angeboten wird. Heute keine solchen Menüeinträge/Scheinbuttons. Ein Klick auf mailto/tel erzeugt auch keinen bestätigten Kommunikationshistorieneintrag; Telefonnotiz benötigt später bewusste Erfassung durch den Menschen.

**Ein gemeinsamer Eingangsbearbeitungsmechanismus**, kein gesonderter Automat pro Vorgangsart: E-Mail/Scanner/Upload → kontrollierter Eingang und unverändertes Original → sichere Formatprüfung → Extraktionskandidat mit Herkunft/Unsicherheit → Organisations-/Dokumentartvorschlag → Duplikatprüfung → vorhandener Aktenbezug oder Entwurf → menschliche Prüfung/Freigabe → zentrale Version/Ablage/geeignete Benennung → gegebenenfalls Aufgaben-/Fristenvorschlag → weitere Bearbeitung in derselben Akte. Import-/Workflowlauf mit Status, Quellhash, Idempotenz, Fehler/Wiederholung und Audit vorsehen. OCR/KI liefern überprüfbare Vorschläge, niemals feststehende Tatsachen; Originalbezeichnungen erhalten. Mandanten-/Rechteprüfung und kein Datenabfluss ohne Freigabe gelten über jeden Schritt.

Keine automatische Zahlung, ungeprüfte Buchung/Mahnung/Kontodatenänderung, rechtsverbindliche Erklärung oder E-Mail-Versendung. Jede spätere Integration benötigt eigenen Datenfluss-/Berechtigungs-/Sicherheitsnachweis. Dateiablage und Versionierung aus dem vorhandenen Dokumentenkonzept wiederverwenden; „Bezahlt“-Stempel bestätigt weiterhin weder Eingang noch Ausgang.

**Nächster kleiner Vorschlag nach Funktionsabnahme:** DOK-01 aus dem bestehenden Entwurf: ein stabil referenzierter synthetischer Rechnungsvorgang, zwei echte synthetische PDF-Testbelege, lesende Metadaten-/Versionszuordnung und kontrolliertes Dateiöffnen. Vorgangs-/Unternehmenskennung zuerst festlegen; keine Namen als Zugriffsschlüssel. Kontaktprojektion weiterverwenden. Kein Upload, keine Telefonnotiz-, Aufgaben-, Vorlagen-, OCR- oder Finanzschreibstrecke automatisch beginnen.


## DOK-01 – implementierter synthetischer PDF-Lesepfad (18.10.2026)

**Entscheidung und Status:** DOK-01 wurde als begrenzter lokaler Desktop-Demoauftrag freigegeben und umgesetzt; menschliche Funktionsabnahme steht aus. Die früheren „nächster Vorschlag“-Abschnitte dokumentieren die Planung davor. Kein produktives Archiv oder vollständiges Vorgangs-/Berechtigungsmodell.

### Kontext, Alternative und Konsequenz

Rechnung und einzelne Zahlung benötigen tatsächlich lesbare, getrennte Belege. DemoDokumente.cs erzeugt beim Initialisieren zwei feste einseitige PDF-1.4-Testdokumente mit Standardschrift, korrekter Objekttabelle und Byteoffsets. Nur konstante ASCII-Testtexte, keine fremden Dateien oder allgemeine PDF-Engine. Keine neuen Pakete, gespeicherten Binärfixtures oder .gitignore-Ausnahmen. Privater Katalog hält unveränderliche Metadaten und Bytes; Dateiabruf gibt eine Kopie zurück. SHA-256, Größe, Version, Datum und synthetische Quelle beschreiben die Dateiversion, bestätigen aber keine Echtheit oder Zahlung.

Alternative Einbettung benötigt eine Lockerung der vorhandenen object-src-/Frame-Regeln oder eine zusätzliche Renderingbibliothek. Deshalb **nativer PDF-Betrachter im neuen Browser-Tab**, rel=noopener/noreferrer, bestehende CSP unverändert. Edge hat beide echten Dateien lesbar angezeigt; Tab schließen erhält die Rechnung. Kein eigener Editor, OCR, Upload oder automatisches Speichern. Zoom/Suche/Bearbeitungs-/KI-Angebote des Browserbetrachters sind Browserfunktionen; keine entsprechende Produktintegration oder Freigabe von Uploads an externe Dienste. Keine übergreifende Kontrolle über Browsererweiterungen behaupten.

### Begrenzte Identitäten und Verträge

Additive optionale Demo-Anzeigeattribute: Rechnung.VorgangId und Perspektive (Unternehmensname plus bekannte Rolle Aussteller/Empfänger), Zahlung.Id. Explizite feste Zuordnung in DemoDaten statt Namensheuristik; vorhandene Finanzobjekte und Geldbeträge bleiben unverändert. Nord ist im Demo ausdrücklich die Unternehmensperspektive: 001/003 eigene Rechnung, 002 erhaltene Rechnung. Frontend zeigt Richtungen nur bei konsistenter Perspektive und tatsächlich passendem Zahler/Empfänger. Ohne belastbaren Kontext bleiben Namen sichtbar, keine erfundene Richtung. Unbestätigter Eingang heißt „Zahlungseingang zur Prüfung“, nicht erhaltenes Geld.

Stabile Kennung demo-nord-rechnung-002 verbindet zwei Dokumentversionen:

| Dokument-ID | Datei / Art | Bezug |
| --- | --- | --- |
| original-002-v1 | SYNTH-2026-002-Originalrechnung.pdf / Originalrechnung | Rechnung 002, 960,00 EUR, von ISD an Nord |
| zahlung-002-v1 | SYNTH-ZA-002-Zahlungsbeleg.pdf / Zahlungsbeleg | Nur demo-zahlung-002, bestätigte synthetische eigene Zahlung 300,00 EUR |

200,00-EUR-Zahlung besitzt weiterhin nur ihre ursprüngliche Belegreferenz/Quelle, keine vorgetäuschte PDF. Vorgänge 001/003 liefern eine leere Dokumentenliste. Ein Dokument wird einmal im zentralen Katalog gehalten; Original rechts, Zahlungsbeleg nur an der Zahlung. Beide verwenden dieselbe Dokumentkarte und denselben Abrufhandler. Rechnungs-/Zahlungsquellen und synthetische PDF-Erzeugungsquelle sind unterschiedliche Herkunftsnachweise, aufklappbar am jeweiligen Kontext.

- GET /api/vorgaenge/{vorgangId}/dokumente: nur bekannte feste Demo-Vorgänge, synthetische Metadatenliste.
- GET /api/vorgaenge/{vorgangId}/dokumente/{id}: nur exakt passende bekannte Kombination; PDF inline, application/pdf, bekannte feste Dateinamen. Kein Dateipfad wird aus Eingaben konstruiert und nichts vom Benutzer-Dateisystem gelesen.
- Unbekannte/falsch zugeordnete Kennung oder Pfadversuch: 404. Fehlende PDF liefert eine statische deutschsprachige HTML-Fehlerseite mit Hinweis zum Tab-Schließen, keine Ausnahme-/Dateipfadangabe. Andere Methoden erzeugen keine Schreibwirkung.
- Bestehende Loopback-/Host-/Origin-Grenzen sowie no-store, nosniff, no-referrer und CSP gelten auch für Dokumente. Feste PDF-Inhalte enthalten keine Skripte, Formulare, externen Ressourcen, Anhänge oder Makros. Das ist kein Sicherheitsversprechen für fremde PDFs.

Dokumente.tsx lädt Metadaten einmal je Detailansicht, prüft synthetische Kennzeichnung, IDs/Unternehmen/Vorgangsbezug, Typ, Version, Größe, Hashformat und doppelte IDs. Fehler bleiben wiederholbar, leere Fälle ehrlich. Keine URL aus Dateinamen oder vom API gelieferte fremde Vieweradresse. Für die fest vorhandenen Bytefolgen ist der native Link zugleich die größere Ansicht; keine zweite Vorschauarchitektur.

### Sicherheit, Desktop und spätere Erweiterung

**Alle Demo-Dokumente sind ausdrücklich synthetisch und für jeden lokalen Benutzer des Prototyps abrufbar.** Es gibt keine produktive Anmeldung, Mandantenautorisierung, revisionssichere Ablage oder Zugriffshistorie. Unternehmens-ID DEMO-NORD ist ein fester Demokontext, keine nachgewiesene Rechteprüfung. IDs, Loopback und versteckte Menüpunkte ersetzen keine Autorisierung. Nicht mit echten Daten oder außerhalb des Loopbacks betreiben.

Bei echten Dokumenten oder mehreren Unternehmen muss ein serverseitig autorisierter Vorgangs-/Dokumentdienst den Katalog ersetzen. Dokumentidentität/Version und Aktenzuordnung dann getrennt halten: mehrere berechtigte Akten verweisen auf dieselben unveränderten Bytes, Rechteprüfung je Verknüpfung. Rollen für Geschäftsführung, Büro und Mitarbeiter serverseitig am Zugriff durchsetzen. Notizen/Kommentare mit Autor/Zeit und Bearbeitungsereignisse können später an Vorgang oder Version anknüpfen; niemals Originalbytes überschreiben oder „Bezahlt“ als Finanzaktion behandeln. Aufbewahrung, Restore, Audit und Fremd-PDF-/Upload-Sicherheit bleiben offene Produktionsvoraussetzungen.

Neuprüfungs-Trigger: echte Daten, beliebige Dateien, Upload, Einbettung/Viewerbibliothek, weitere Dokumentformate, Persistenz oder produktive Rechte. Dann neue Sicherheits-/Abhängigkeitsentscheidung statt Erweiterung des Test-PDF-Generators. Desktop ist gemäß Nutzerentscheidung Priorität; keine eigenständige mobile Anwendung. Der gemischte Gesamtbestand von 910,00 EUR bleibt ausdrücklich kein Forderungssaldo. Keine Finanzberechnung, Importlogik, Bankbuchungsdaten oder fachliche Vorgangsarten erweitert.

## Aktuelle Ergänzung: allgemeine Vorgangsakte (18.10.2026)

Die frühere DOK-01-Beschreibung dokumentiert den damaligen festen PDF-Katalog. Der aktuelle gemeinsame Aktenbestand erweitert ihn: zentrale Originale/Integrität und getrennte Aktenverweise, versionierte Notizen/Soft Delete, Posteingang mit manueller Kategorie/Zuordnung, Korrespondenzverweise, Kontaktprüfentscheidungen und zentrale berechtigungsvorbereitete Suche. Upload, gezielter Drop und optionaler Scan-Ordner verwenden denselben Validator/Ablagedienst. Die alten Dokumentendpunkte bleiben kompatible Lesewege zu diesem Bestand, keine zweite Dokumentenverwaltung.

`Web/Akten` trennt Anzeigemodelle, Notizablage, Kontaktpflege, Datei-/Textextraktion, Aktenbestand, Suchdienst, Eingangsadapter und HTTP-Verträge. Finanz-/Importkern bleiben eigenständig. Modulaktivierung, Ressourcenrechte und mögliche spätere Lizenzen sind getrennte Ebenen; der Server prüft aktive Arbeitsbereiche, die Demo-Lesepolitik erlaubt bewusst alle synthetischen Ressourcen. Keine produktive Authentifizierung oder abschließend festgelegte Produktmodulliste.

Die aktuelle Notizablage ersetzt den sessionStorage-Prototyp; alte Tab-Notizen werden unverändert angezeigt. Fremde PDFs werden nicht als sicherer allgemeiner Inline-Viewer freigegeben. Office-Anhänge erzeugen keine Buchung und keinen Excel-Datenimport. Direkte Windows-Scannersteuerung ist nur vorbereitet; der aktivierbare lokale Testordner ist tatsächlich implementiert. Entscheidungen, Quellen, Grenzen und Revisionsanlässe stehen in [ADR 0003](adr/0003-zentrale-vorgangsakte-und-eingangsadapter.md).


## Aktueller Produktreife-Stand (18.10.2026)

Die zentrale Kontaktpflege verwaltet Firmen/Personen/Ansprechpartner, mehrere und primäre Kontaktwerte, Version/Dublettenprüfung/Verlauf/Archiv. Anzeigebereiche lösen dieselben stabilen Kontakt-IDs auf. Der gemeinsame Kategorienservice verwaltet Geschäftsrubriken mit einer Unterebene und Archiv; Systemstatus bleiben eigenständig. Arbeitsumgebung trennt globale Beispieldatenkennzeichnung und explizite Diagnose von Fachfunktionen. Server-Aktionspolitik, Ressourcenlesepolitik und Modulaktivierung sind getrennt und kein Ersatz für produktive Authentifizierung.

Korrespondenz referenziert mehrere zentrale Dokumente. Suchauswahl und Upload verwenden denselben Akten-/Dokumentenbestand. Hashnavigation modelliert Bereich/Datensatz/Dokument; Listenfilter bleiben beim unmittelbaren Detailrückweg erhalten. Posteingang/Vorgänge sind paginierte Arbeitslisten. Normale Mitarbeiteransichten enthalten keine rohen technischen Daten. Die ältere Beschreibung aufklappbarer Prüfdaten und des Tab-Notizprototyps ist historisch; aktuell gilt [ADR 0004](adr/0004-kontakte-kategorien-und-arbeitsumgebung.md). Kein produktives Schema, keine neue Abhängigkeit und keine Finanzregel wurde dafür eingeführt.


### Konsolidierung vor Persistenz (MVP-01.5)

Der Posteingang ist eine Eingangsschleuse für neue oder ungeklärte Inhalte, kein Pflichtweg und kein Archiv. Bekannte Zielakten unterstützen die bestehende direkte Dokumentablage. Eine falsche Dokumentbeziehung ist mit erwarteter Version und Prüfung von Dokument, Quell-/Zielakte und zugehörigen Firmenrechten korrigierbar; Original und ID bleiben erhalten. Der Verlauf hält vorherige/neue Akte, UTC-Zeitpunkt und serverseitigen Actor fest. Produktive Transaktion und dauerhaftes Audit bleiben vor PostgreSQL gesondert umzusetzen.

Notizen sind interne Informationen. Korrespondenz dokumentiert externe Kommunikation mit Richtung, zentralem Kontakt und tatsächlichem Zeitpunkt; Entwürfe bleiben unversandt. Kein Mailabruf oder Versand. Aktenstatus: Offen, In Bearbeitung, Wartet auf Rückmeldung, Erledigt, Archiviert; bisheriges Neu wird für Akten kontrolliert als Offen dargestellt, nicht für den Posteingang.

Suchvergleich ist deterministisch, tokenbasiert, umlaut-/schreibweisentolerant und begrenzt fehlertolerant. Er ist bewusst von der vorsichtigeren Dublettenentscheidung getrennt. Die globale Suche liefert begrenzte typisierte Ziele unter Ressourcen-/Modulprüfung; die Akten-Suche berücksichtigt aktuelle Dokumentbeziehungen, Kontakte und Rechnungsbeteiligte. Metadaten werden ohne Dokumentinhalt durchsucht. Firmenarbeit liefert zentrale Verweise je Bereich und maximal20 Ergebnisse pro Seite. Aktenabfragen unterstützen serverseitig text/status/kategorie/sort/ab/seite; bestehende lokale Listen sind weiterhin durch die In-Memory-Grenzen begrenzt. Diese Implementierung ist kein Lastnachweis: vor größeren Produktivbeständen Filter/Sortierung/Pagination und Beziehungsabfragen in den PostgreSQL-Readmodellen ausführen, indexieren und messen; keine In-Memory-Schleifen unverändert auf EF-Abfragen übertragen.

Zeitpunkte werden als DateTimeOffset/UTC gespeichert und als ISO-Zeit übertragen; Darstellung erfolgt in der Browserzeitzone. Erfassung, letzte Bearbeitung und tatsächliche Kommunikation bleiben getrennte Angaben. Interne Adressvorschläge verwenden nur vorhandene Kontaktwerte, keine externe Adress-/Geocoding-API.

Bestehende Security-by-Design-/Negativtestregeln bleiben verbindlich: mutierende Aktenaktionen benötigen Verwaltungs- und Ressourcenrechte, lokale Requests passende Origin und Aktionsheader. JSON ist getrennt auf128KiB und16 Ebenen begrenzt, unbekannte Felder werden abgewiesen; Datei-/Importgrenzen bleiben bestehen. Kein produktives Authentifizierungs-/Mandantenmodell behauptet. Dependency-Prüfung und ausstehende Patchfreigabe siehe Nachweis.


## MVP-02A — PostgreSQL persistence foundation

Status: PostgreSQL live gate passed on 19 October 2026 against native PostgreSQL 18.6 on loopback, database `firmenzentrale_dev`, using synthetic data only. The original environment/authentication blockers have been resolved. Real schema, CRUD/negative constraints, concurrency, reassignment/rollback, host/process restart and API/Edge browser checks are recorded in the existing development evidence. SQLite/model checks alone were not treated as live proof. This does not approve a production pilot.

The existing modular monolith remains. `Web/Persistenz` contains a request-scoped EF Core unit of work, storage records separated from API DTOs and the financial domain, database queries and the optional PostgreSQL host adapter. EF Core 10.0.11 and Npgsql EF provider 10.0.3 are used; SQLite 10.0.11 is test-only. The default `Demo` branch preserves the existing synthetic scenario, importer and optional scan folder. PostgreSQL has no seed, no implicit migration and no fallback. Existing in-memory processes are never migrated.

### Explicit local configuration and migrations

Use environment variables or .NET User Secrets (`FirmenZentrale.LocalDevelopment`), never committed credentials. Do not paste credentials into shared logs, source or command history. User Secrets are a local development convenience, not a production secret vault. Example without credentials, from the repository root:

```powershell
$env:FirmenZentrale__Persistence__Provider = 'PostgreSql'
$env:ConnectionStrings__FirmenZentrale = 'Host=127.0.0.1;Database=firmenzentrale_dev'
$env:FirmenZentrale__Persistence__DocumentRoot = '<absolute-local-data-directory-outside-the-repository>'
# Configure authentication privately for your separate synthetic development database.
# Apply only after reviewing the initial migration; do not target an existing business database.
dotnet run --project src/backend/Betriebsplattform.Web --no-launch-profile -- --apply-migrations
dotnet run --project src/backend/Betriebsplattform.Web --no-launch-profile
```

`PostgreSql` is the exact configured provider value. The schema command is explicit and exits; normal startup only checks connection and pending migrations. There is no `EnsureDeleted`, startup schema recreation or automatic production migration. Failure stops PostgreSQL startup with a sanitized error. Changing to Demo requires an explicit configuration change. The UI identifies the PostgreSQL development store instead of claiming it is reset on restart. The host still binds only to `127.0.0.1:5180`; neither network permissions nor database ports are opened.

### Relational model and concurrency

The initial migration covers contacts/structured addresses/company-person relationships/contact values, categories, cases, invoice direction and amounts, payments/confirmations/allocations, document metadata/current assignment/intake history, notes/revisions, correspondence/central attachment references and contact proposals/history. All actual relationships use foreign keys with restrictive delete rules. There is no hard-delete HTTP action for historical business records. Required fields, bounded strings, positive financial amounts, stable statuses, category self-reference checks, unique normalized categories/file hashes/storage keys, one current assignment per document and one active preferred value per contact/type are enforced where appropriate. Similar names and invoice numbers are not made globally unique.

Indexes target contact/name/address search, category hierarchy, case status/time, invoice issuer/reference/amount and status/date, payment dates, assignments and history timelines. Foreign-key indexes complement these paths. Case/category cycle prevention also remains in the service. Configuration/module selection is not authorization or licensing.

Application-managed integer `Version` is an EF concurrency token. Both synchronous and asynchronous saves advance it. Services compare the submitted version and EF checks the original database version during UPDATE; a stale update returns HTTP 409. Contact-value-only edits also enlist the contact version. UI status undo uses the returned version. Multi-save contact updates, document intake/assignment and A→B→A / A→B→C reassignment use explicit transactions, including history and any newly created case. Financial writes replay the relevant bounded aggregate through the unchanged financial domain and save dependent state atomically. This milestone adds no public financial-write API.

Document metadata contains identifiers, original name, MIME, size, hash, storage key, source/channel, status, timestamps and relationships; no binary content is stored in the relational model. `IDokumentdateien` separates file operations from metadata services. MVP-02B1 completes the local durable adapter; configuration and failure boundaries follow below. No production pilot or backup approval is implied.

### Bounded queries and remaining gates

Contact, invoice, inbox and case overviews use server filtering/sorting/pagination (20 records); company work uses SQL counts and lazy pages. Deep links fetch the selected record separately. Shared contact/case selectors request bounded pages rather than loading the full stock. Invoice sums remain whole-stock aggregates. Search first narrows SQL candidates and then applies the existing tolerant matcher to bounded results (100–200 candidates/type; linked document candidates capped at 1,000). This preserves bounded resource use but is not exhaustive fuzzy retrieval or a large-data performance claim. Measure and improve candidates/index plans before a large pilot.

Case detail reads are bounded to 200 documents, 250 notes, 200 correspondence entries, 1,000 note revisions and 100 contact proposals; contact histories are capped at 500 entries per request. These are development read limits, not production pagination for large archives. Categories currently have a bounded metadata list. Dedicated history/detail pagination and load measurement remain necessary before large real-data use. No binary previews are eagerly loaded.

SQL uses EF parameterization, fixed sort choices and validated IDs; no user-built SQL. Origin/action-header guards, strict JSON/size limits, resource/module hooks and output escaping remain in place. Provider errors, connection strings and inner exception details are not returned to users; sensitive EF/Npgsql logging is disabled. Aggregate list APIs fail closed for a custom permission policy until database-level permission predicates exist. Local demo permissions are not production authentication, tenant isolation or authorization.

Pilot gates: realistic-load query plans; production storage operations, crash recovery/reconciliation and malware scanning; backup with restore test; authenticated actors and database-level resource rights; schema deployment procedure, monitoring and controlled deployment. The restricted synthetic XLSX importer and scan-folder adapter remain operational in Demo mode and are explicitly unavailable in PostgreSQL mode; wiring real imports/scan intake needs a separate persistence-aware input package. No OCR, mail adapter or public database is introduced.


## MVP-02B1 — Durable local document storage

The PostgreSQL adapter uses the existing `IDokumentdateien` boundary: save, read/open, and internal compensation removal. Original filenames remain metadata; the server generates a random 32-character hexadecimal storage key and stores bytes as `<key>.bin`. Clients supply document/resource IDs, never storage keys or host paths. Reassignment changes only the relational link and history, retaining the same file and key. There is no public file-delete endpoint.

`FirmenZentrale:Persistence:DocumentRoot` must explicitly name an absolute local data directory outside the repository, application content/output and frontend/webroot. An administrator will later provision its OS access permissions. UNC paths, filesystem roots and reparse-point ancestors/files are rejected. Startup creates the configured directory if needed and checks write/read/removal using an isolated probe. Invalid or inaccessible storage stops startup without exposing the path or falling back to temporary/in-memory storage. The example placeholder above must be replaced locally, never committed with private user paths. Existing MVP-02A synthetic files are not automatically moved; retain them until any deliberate relocation has verified the stored size/hash. Demo catalog, upload and scan-folder storage remain separate and unchanged; no automatic Demo-to-persistent transfer occurs.

The shared upload validator retains its 20 MB limit and conservative PDF, image, UTF-8 and Office package checks; extension and declared MIME must agree with supported content. Windows device names, control characters, path separators, invalid filename characters and trailing spaces/dots are rejected. DOCM/XLSM remain unsupported. Files are never executed or opened by Office. This is not antivirus: local malware scanning and realistic document-format validation remain pilot gates. Unknown uploaded PDFs continue the existing download-only policy; the trusted synthetic Demo PDF preview and validated image/text previews remain available. No new PDF safety or OCR claim is made.

Writes use an exclusively created random `.pending` file in the same directory, flush the bytes, close the handle, then rename without overwrite. Only after that does the existing database transaction write metadata/assignment/history. A normal failed or cancelled DB operation rolls back and removes only its newly created storage object using a non-cancelled compensation step. File-write failure leaves no successful metadata record. Identical hashes follow existing duplicate rules and do not create a second file. Lost commit acknowledgements are checked against a separate context before compensation: a possibly committed original is never blindly deleted. Failure to establish commit outcome or remove a file returns a sanitized maintenance error rather than claiming success.

Filesystem and PostgreSQL have no shared atomic transaction. A hard process termination/power loss between file finalization and DB commit can still leave an orphan; a total storage failure can also prevent compensation. Do not automatically sweep files or delete originals. Crash reconciliation, coordinated backup/restore, recovery procedures and restricted service-account/OS directory permissions must be proven before pilot use. Root/reparse checks do not defend against a privileged local process racing filesystem operations.

Originals are delivered through existing resource-filtered API endpoints only; no static directory mapping is added. The adapter checks stored size and SHA-256 through a bounded-buffer read, rewinds the same read-only handle and streams the response without loading another whole-file byte array. Unknown/missing documents return controlled 404; integrity mismatch returns 409. Safe preview types retain controlled MIME, CSP and `nosniff`; other formats use attachment download. Lists remain metadata-only and previews remain lazy. Upload validation still buffers at most the existing 20 MB input; this block introduces no unbounded upload or background processing infrastructure.

Local synthetic PostgreSQL 18.6 verification passed: HTTP upload, metadata/hash/file checks, service/host restart, original retrieval, A→B→A and A→B→C, rollback compensation after file write, PDF download and PNG inline response. Tests also cover traversal/device names, unsupported/mismatched types, empty/oversized content, blocked storage, cancellation, overwrite prevention, corruption, missing originals and lost commit acknowledgements. The existing evidence document records commands and counts. Production authentication/resource rights, malware scanning, backup/restore, deployment and LAN security remain open.


## MVP-02B2–02B4 — local operations and authentication foundation

This section supersedes the remaining-gate status recorded in earlier milestones; it is not a production authorization. The uncommitted MVP-02B1 storage implementation is retained. No real business data, installer, Windows service, firewall rule, external identity, cloud storage or package dependency was introduced.

### Offline checkpoint, backup and restore

`Betrieb/Sicherung` creates a PostgreSQL 18 custom-format dump, copies immutable document originals and encrypted authentication key files, then writes `manifest.json` last. The versioned manifest records UTC time, PostgreSQL major version, assembly/schema migration versions, filename/storage key, byte length and SHA-256; it contains no connection strings or plaintext credentials. Source/target locations must be distinct and external. No success is reported for an incomplete backup. Originals remain unchanged.

All PostgreSQL web hosts hold a shared session advisory lease; backup requires the corresponding exclusive lease and refuses to proceed while a host is active. This is an explicitly offline maintenance checkpoint, not a distributed filesystem/DB transaction. External SQL writers and manual storage changes must also be stopped. Inventory verifies metadata/file agreement before the dump and copy; archive validation verifies all listed files. Hashes detect corruption, not a maliciously replaced manifest and archive: only trusted, access-controlled backup media may be restored. Backup retention, an encrypted independent/offline destination and operator access controls still require pilot provisioning.

Restore accepts only a new `firmenzentrale_restore_...` database and a new storage directory, rejects existing targets and checks format/schema/hash compatibility first. PostgreSQL restore runs in a single transaction using a newly generated, restricted database-owner login, never a superuser restore session; that login is disabled afterwards. Administrative credentials create the isolated target but are not supplied to the restore subprocess. Failed targets are retained for controlled inspection without a completion marker; nothing is automatically dropped. Runtime table grants are applied separately after restore. Restored session stamps are rotated, and old encrypted keys are archived under `recovered-keyring`; fresh active keys and a new login are required. Business data and password hashes remain usable without transferring old Windows DPAPI session keys to another account.

Operations are deliberately local administrator CLI actions, not web endpoints. Configure `FirmenZentrale:Persistence:Provider`, the private connection, `DocumentRoot` and `FirmenZentrale:Operations:PostgresBin` via environment/User Secrets in development, or restricted external runtime configuration. Set `BackupRoot`, or `RestoreSource`, `RestoreDatabase`, `RestoreRoot` for the chosen action. No credential examples belong in source, scripts or logs. Stop all hosts first. Use the built host with `--ops inventory`, `--ops backup` or `--ops restore`. The inventory command returns nonzero and a bounded report when inconsistent. It detects missing/mismatched originals, orphan `.bin` files, `.pending` remnants and unknown files/directories; it never deletes or repairs automatically. Pilot startup requires a consistent inventory. Use an inspected backup/isolated restore for recovery rather than deleting unknown files.

### Individual accounts and resource rights

`FirmenZentrale:Security:Enabled=true` enables the Windows pilot authentication path only with PostgreSQL, explicit external storage and an HTTPS certificate (`Security:CertificatePath`; password through private configuration). Production uses environment/OS-protected configuration; .NET User Secrets remain development-only. No credentials were displayed or changed during verification. With the owner/migration connection and stopped host, `--ops bootstrap` reads the initial login/display name and a hidden password interactively; it refuses an already initialized user store. There is no default account, public registration or fixed password. Migration remains a separate explicit administrative action.

The additive `PilotSecurity` migration creates `Benutzer`, `Ressourcenfreigaben` and `Sicherheitsereignisse` with required/unique/FK/check/version protections; it does not replace the initial migration or seed users. Passwords use the framework versioned PBKDF2 hasher (210,000 iterations), including rehash when needed. Two roles: Administrator manages accounts, grants and categories; Mitarbeiter performs authorized office work. Accounts can be deactivated and roles changed through authenticated administrator APIs/UI with version checks. Serializable administrator updates preserve at least one active administrator. Individual IDs remain stable; no user hard-delete endpoint exists.

Pilot API groups require framework authorization; admin endpoint groups additionally require Administrator. SQL-composable EF query filters enforce company/case/document grants on lists, search, detail queries and linked notes/correspondence/history. New employee-owned contacts/cases/documents get explicit creator grants. A company grant includes its contacts and linked work; a case/document grant is narrower and does not reveal unrelated company master data. Invoice visibility requires the relevant case and issuer/recipient contact access. Authorized invoice reads include allocated payment facts, so hiding a third-party contact cannot change the paid/open amounts; inaccessible contact names are replaced by a neutral label. This rule has a red/green regression test. Module selection is not authorization. No unrestricted browser-side stock is loaded for access filtering. Contexts remain non-pooled, request-aware units of work.

Cookies are HttpOnly, Secure, SameSite Strict and expire after 30 minutes without sliding extension. Framework antiforgery cookies/tokens protect writes, including login/logout; the token stays in memory and same-origin headers, never LocalStorage. The shared transport rejects non-local API URLs. Every request revalidates active/lockout/security-stamp state, so deactivation and role changes invalidate existing sessions. Lockout after five failures and a bounded login-rate policy are implemented; unknown/incorrect accounts share a generic failure response. CORS is not opened; host/origin checks remain restrictive. New security audit rows record actor ID/time/action/resource without passwords/tokens; business histories use the authenticated display name plus stable ID. Logout clears the cookie even if its best-effort audit write fails. Legacy histories are not falsely assigned to new accounts.

### Database, files and local deployment

`--ops provision` creates a validated `fz_app_...` login with a hidden, strong generated hex secret supplied interactively. `--ops runtime-grants` reapplies reviewed runtime grants after schema deployment or restore. The web host uses that runtime identity; migrations/backup administration use a separate owner connection. Runtime gets required read/insert/update rights and grant-removal permission, not schema creation, user hard deletion, migration-history writes or audit modification/deletion. Pilot startup rejects superuser, database/schema owner/creator and privileged server-file/program role memberships. Initial provisioning and private runtime configuration remain explicit operator steps, not a silent switch of existing secrets.

The B1 storage boundary remains outside code/webroot, keyed internally, hash/size verified and streamed through authenticated resource endpoints. No static file mapping or client-controlled storage key was added. `IMalwarepruefung` is a fail-closed integration boundary: pilot uploads are rejected until a vetted adapter is configured; Demo test uploads remain separate. The Windows Defender service and tool were found locally, but no elevated scanner process or system-policy change was forced. Microsoft documents elevated execution for [MpCmdRun](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus); scanner privilege separation, sample-submission/privacy policy and actual clean/error/detection verification are external gates.

`npm.cmd run build`, followed by `dotnet publish ... --configuration Release`, packages the existing frontend into the host's `wwwroot`; publishing refuses a missing frontend build. This is a local artifact, not an installer or release. The packaged host was started against isolated restored PostgreSQL data with a pinned, temporary test certificate. HTTPS is mandatory in pilot mode; no certificate is installed or trusted system-wide by this work. `FirmenZentrale:Network:Port` permits an explicit local port; non-loopback `Network:Address` is rejected pending a separate LAN gate. Secure cookies, headers, origin checks and generic error responses remain active. Browsers never connect directly to PostgreSQL or storage.

### Security/pilot gate — NOT PASSED

Automated foundations passed: authentication, role/resource denial and filtered search, CSRF/overposting/session tests, least-privilege runtime, storage integrity/traversal protections, real backup/restore/restart, detected crash remnants and dependency checks. This is a single-agent implementation/verification, not an independent penetration test or human pilot approval.

Concrete external/manual gates remain: trusted production HTTPS certificate and renewal; actual service-account/NTFS and backup ACLs; independent encrypted/offline backup retention and restore rehearsal under the intended operator; vetted malware scanning without elevating the web host; human approval of resource grants and operating procedures; a final independent security review and browser usability check. Runtime/OS patch policy and load measurement are not replaced by package audit.

The existing local PostgreSQL listener was observed on `0.0.0.0:5432` and `[::]:5432`. This proves all-interface binding, not LAN/internet reachability; effective firewall/HBA exposure was not proven. No PostgreSQL, firewall or router setting was changed. Review/restrict this binding and server access before any pilot. No real-data pilot is recommended while these gates remain open. Current dumps/storages contain only isolated fictional test data.


### Startup and multi-user refresh ? 19 October 2026
Vite remains the sole frontend build. The host explicitly serves those files through Webschutz; SDK static-web-assets discovery is disabled to avoid development manifests referencing missing source-machine wwwroot directories. A normal build copies an existing Vite build to the executable's wwwroot; publish still requires a frontend build. The build removes obsolete SDK runtime/endpoint manifests from its output. Without a built frontend the API can start and the UI returns a controlled 503; no placeholder frontend or document storage is created in the webroot.

Visible workspaces use bounded 5-second polling of existing authorized endpoints, plus focus/online/visibility resynchronization. There is no SignalR/WebSocket/SSE or push event feed. No background document-content loading is added. Request cancellation protects list responses, active contact editing pauses detail refresh, and note editors keep their own draft/version while metadata refreshes. Existing optimistic concurrency and server-side resource checks remain authoritative. This prepares one host office PC and a second browser PC, not a third dedicated server. Actual cross-PC use remains blocked by the separately approved LAN/TLS configuration gate.

The read-only network inspection found all-interface PostgreSQL binding, a configuration file allowing only loopback SCRAM clients and active default-inbound-block Windows Firewall profiles. No explicit PostgreSQL/5432 allow rule was found; general Windows-app rules and the loaded HBA state were not conclusively resolved. No LAN/internet reachability or network change is claimed. MFA, account recovery/temporary-password handover and production certificate/OS/backup/malware controls require explicit pilot acceptance; no new authentication architecture was added.


Authenticated users can change their own password from the profile using their current password and a new 14-to-128-character nonblank password. The existing framework hasher, cookie authentication, CSRF and login rate limiter are reused. Only the authenticated principal determines the account; unknown DTO members are rejected. Hash, security stamp, version and audit event are saved together. The current cookie is renewed with its existing lifetime; older sessions fail validation on their next request. No periodic expiry policy or recovery/MFA implementation was added. The five-second interval is centralized; background list reads share an outstanding promise instead of canceling a slow request every tick, and case headers reuse the case workspace response.

## Lokale Assistenz und Arbeitsübersichten (22.10.2026)

Die erste Assistenz ist eine begrenzte, synchrone Backendstrecke im authentifizierten Host. `Assistenzkontext` verwendet die vorhandenen Aufgabenrechte, Datenbankfilter und aktivierten Fachmodule. Die Quellenabfrage ist deterministisch und unabhängig vom Modell: persönliche offene Aufgaben, begrenzte Suche oder explizit berechtigter Akten-/Kontakt-/Dokumentkontext. Dokumentauszüge enthalten keine erfundenen Seitenpositionen. Dieselben Originale bleiben in der vorhandenen Ablage. Die neue Dokumentliste ist paginiert und verwendet die vollständigen vorhandenen Dokumentrechte.

`ILokaleInferenz` trennt Kontext und Ollama-HTTP. Ausschließlich `http://127.0.0.1:11434`, ohne Proxy, Weiterleitung, Tools, Cloud-Fallback oder automatisches Pull. Standard ist deaktiviert. Vor einer Freigabe müssen Runtime, Hardware und Cloud-Abschaltung geprüft werden. Erst dann `FirmenZentrale:KI:LokalerBetriebGeprueft=true` und `FirmenZentrale:KI:Modell=<tatsächlich installierte Kennung>` konfigurieren. Ollama selbst muss mit abgeschalteten Cloud-Funktionen betrieben werden (siehe [Ollama FAQ](https://docs.ollama.com/faq)). Die Anwendung ändert keine Systemkonfiguration. Modellvorprüfung verlangt einen installierten GGUF-Eintrag von 100 MB bis 4 GB, Q4/Q5 und ohne Remote-Metadaten; Cloud-Kennungen werden abgewiesen. Diese Größenprüfung ersetzt keinen Hardwaretest.

Ein Modelljob ohne Warteschlange; insgesamt 30 Sekunden Zeitbudget, Abbruch, 64-KB-Antwortgrenze, 1000 Zeichen Frage, höchstens 20 begrenzte Quellen, 512 Ausgabetokens und 4096 Kontexttokens. Keine Chatpersistenz oder sensiblen Promptlogs. Antwortquellen müssen zur serverseitigen Auswahl gehören; nach Inferenz werden Quellen/Rechte erneut gelesen. Text wird ausschließlich als React-Text bzw. editierbarer Entwurf angezeigt. Eine interne Notiz wird nur am ausgewählten Aktenkontext nach ausdrücklicher Prüfung über den bisherigen, erneut autorisierenden und auditierten Notizendpunkt gespeichert. Keine automatische Aufgabe, E-Mail oder Finanzaktion.

Kalender ist zunächst eine paginierte Agenda bestehender Aufgaben mit vorhandenen Fälligkeitsregeln, keine externe Synchronisation. Prozesse zeigen vorhandene Dokumentenabläufe. Berichte übernehmen bestehende serverseitige Finanz-/Aufgabenwerte ohne neue Geldberechnung. Zeiterfassung und Profilbildspeicher bleiben eigene offene Strecken. Modellvertrags-/SQLite-HTTPS-Tests sind kein Nachweis tatsächlicher Inferenz, PostgreSQL-End-to-End oder einer Modell-Sicherheitsfreigabe.

## KMU-Modulregister und Kalenderübersicht (22.10.2026)
`Module.tsx` ist das gemeinsame UI-Register für Kernnavigation, Weitere Module, Verzeichnis und Hub. Aktivierte Servermodule, persönliche Anmeldung, Kategorie und Priorität bestimmen die Anzeige. Dieses Register ersetzt keine serverseitige Autorisierung. Organisationsname wird lesend aus der lokalen Hostkonfiguration geliefert (`FirmenZentrale:Organisation:Name`,80Zeichen); Produktmarke bleibt MBOP. Kein Logo-Dateipfad/Upload und keine neue Mehrmandantenverwaltung.

Der Kalender liest über `/api/kalender` maximal42Tage eigener berechtigter Aufgabenfristen (maximal200Einträge mit explizitem Gesamtzähler). AufgabenDienst übernimmt Konto-, Objekt- und Zuständigkeitsprüfung. Kein neuer Datenspeicher, keine Terminmigration, keine externen Synchronisationen. Monat/Woche/Tag sind echte alternative Ansichten dieser Fristen. Datum/Uhrzeit können in den bestehenden Aufgabeneditor übernommen werden; Aufgaben bleiben Aufgaben. Eigenständige Termine mit Dauer, Ort/Teilnehmern, Bearbeitung und Löschung sind noch nicht implementiert.

Die lokale Inferenz bleibt standardmäßig deaktiviert. Das bisher feste4GB-Paketlimit ist jetzt `FirmenZentrale:KI:MaxModellBytes` (Default4.000.000.000, obere Grenze6.000.000.000), damit bewusst geprüfte kleinere8B-Pakete nicht durch Modellnamen ausgeschlossen sind. Größe ersetzt keine RAM-/Leistungsprüfung. Local-only-Freigabe und alle bisherigen Quellen-, Timeout-, Abbruch- und Aktionsgrenzen bleiben erhalten. `/api/assistenz/status` meldet die Konfiguration, keinen unbewiesenen Runtimezustand.


## Persönliche Termine im bestehenden Kalender (22.10.2026)
Entscheidung: eigener versionierter Termin im vorhandenen FirmenDbContext statt Aufgaben mit künstlichem Terminstatus oder einem zweiten Kalendersystem. Jeder bestehende Host/Datenbankkontext ist eine Organisation; ausschließlich der aktive eingerichtete Eigentümer (auch gegenüber Administratoren) darf lesen, ändern oder löschen. Keine organisationsübergreifende Auswahl und keine ungeprüften Objektverknüpfungen.

GET /api/termine/?von=YYYY-MM-DD&bis=YYYY-MM-DD liefert überlappende persönliche Termine (Ende exklusiv, höchstens42Tage/500Treffer mit Gesamtzahl); GET /api/termine/{id} den Termin. POST /api/termine/ legt mit stabiler AnlageId (UUID-N) an; POST /api/termine/{id} ändert mit Version; POST /api/termine/{id}/loeschen entfernt mit Version aus der Anzeige. Bestehende Cookie-/MFA-/CSRF-Grenzen bleiben verbindlich. Anlagekennung und unveränderlicher Eingabehash verhindern doppelte Anlage auch bei parallelen Anfragen; Wiederverwendung nach Änderung/Löschung führt zu409. Änderung/Löschung verwenden vorhandene EF-Versionierung. Softdelete erhält Nachvollziehbarkeit, ohne fremde Termine offenzulegen. Terminverlauf mit Vorher/Nachher und Sicherheitsereignis werden in derselben EF-Speicherung atomar geschrieben; Laufzeitrolle darf den Verlauf nicht überschreiben.

Lokale ISO-Minuten yyyy-MM-ddTHH:mm werden strikt als Europe/Berlin interpretiert; UTC-Anfang/Ende zusätzlich gespeichert. Ganztag nutzt lokale Mitternacht und exklusives Enddatum, daher23/25Stunden am Zeitwechsel möglich. Doppeldeutige Herbststunden werden bewusst abgewiesen, statt eine der beiden still auszuwählen. Erweiterung um explizite Offsetwahl bei späterem Bedarf prüfen. Dauer höchstens366Tage, Titel160/Beschreibung4000/Ort200Zeichen. Kein Schema-/Datenbankwechsel im laufenden Produktbetrieb: additive Migration EigenstaendigeTermine und aktualisierte Laufzeitrechte müssen separat freigegeben werden; automatische Migration bleibt aus.

Die Vorschau simuliert CRUD ausschließlich im Tab-Arbeitsspeicher und setzt beim Reload zurück. Nur der authentifizierte PostgreSQL-Weg ist dauerhafte Speicherung. Revisionsanlässe: geteilte Kalender, zusätzliche Mandanten in derselben Datenbank, Teilnehmer/Kontakt-/Aktenrechte oder Serientermine benötigen einen gesonderten Entwurf.


## Firmenlogo und Profilbild: gesperrte Annahmegrenze (22.10.2026)
Entscheidung: Vorbereitete Status-/Sperr-API im bestehenden authentifizierten Host, kein zweiter Dateispeicher. Die vorhandene generische Dokumentprüfung/Malwaregrenze ist kein Rasterbilddecoder. Im Projekt fehlen freigegebene vollständige PNG-/JPEG-Dekodierung mit Begrenzung/Neuencodierung/Metadatenentfernung, ein produktiver Malwareadapter und objektgebundene Bildablage. Deshalb bleibt Bildannahme unveränderlich gesperrt; es gibt keinen Konfigurationsschalter zum Umgehen dieser Voraussetzungen. Alternative ungeprüfte Originale im Dokumentenspeicher oder wwwroot wird verworfen.

GET /api/bildverwaltung/status liefert nur Sperrgründe und rollenbezogene Verwaltungsrechte. POST /api/bildverwaltung/profil und /firmenlogo sowie deren /entfernen-Pfade prüfen aktives eingerichtetes Konto, Firmenlogo zusätzlich Administratorrolle, dann503. GET /profil/datei und /firmenlogo/datei liefern404,keine Bildbytes. Nur eigene Identität und aktueller Host/Datenbankkontext; keine Benutzer-,Firmen-,Dateipfad- oder URL-Parameter. Es wird kein Requestbody eingelesen, dekodiert, gehasht, protokolliert oder gespeichert. Auch eine isolierte zustimmende Scanner-Testdouble kann die fehlende Bildpipeline nicht öffnen. Keine neue Tabelle/Migration oder Bildpersistenz. Allgemeine Bodygrenze2.000.000Bytes für exakt diese beiden Annahmepfade; andere API-JSON-Grenzen bleiben bestehen. Neue Mutationspfade verwenden bestehendes Cookie/MFA/CSRF/Origin-Gate und eine IP-begrenzte20Anfragen/Minute-Policy ohne Warteschlange. Vorhandene Dateiantwortsicherheit/no-store/nosniff bleibt erhalten; derzeit ausschließlich JSON-Fehler,keine öffentlichen Bildressourcen.

UI zeigt nachvollziehbar gesperrte Aktionen und Text-/Initialenfallbacks. PNG/JPEG,2MB,2048Pixel je Richtung und4Millionen Gesamtpixel sind vorgesehene Grenzen,kein Behauptungsnachweis eines implementierten Decoders. Kontaktfotos bleiben offen. Keine Weiterverwendung synthetischer Katalog-Ausnahmen oder Base64-/Browserstorage-Umgehung.

Neuprüfungs-Gate vor tatsächlicher Freischaltung: getrennte Freigabe einer geprüften Decoder-/Neuencoder-Abhängigkeit und ihrer Lizenz/Betriebsgrenzen, tatsächlicher Malware-/Quarantäneprozess, sichere private Bildstorage-Metadaten mit Eigentümer-/Organisationsrechten, atomarem Ersetzen/Version/Idempotenz/Audit, Metadatenentfernung, Quotas und echte isolierte Persistenz-/Negativ-/Browser-CRUD-Kette. Erst danach Bildannahme ausdrücklich neu implementieren/prüfen. Dieser Status stellt keine Sicherheitsgarantie oder Produktivfreigabe dar.


### Quellengebundene Folgeaufgabe aus Versicherungsschreiben
Die bestehende Eingangskette ergänzt die explizite Vorlage `versicherung-antwort-v1`; die vorhandenen Dokumentmuster bleiben Extraktions-/Klassifizierungsmuster. `GET /api/posteingang/{id}/aufgabenvorschlag` liefert ohne Speicherung einen Vorschlag oder eine Sperrbegründung. Voraussetzung: bestätigte Zuordnung, aktive Akte/Absender/Zuständigkeit, eindeutige Analyse und belegtes bestätigtes Datum. Keine rechtliche Fristberechnung.
Der vorhandene `POST /api/posteingang/{id}/aufgabe` nimmt optional den serverseitig erneut berechneten Regelbezug entgegen. Dieser bindet Eingangsversion, Dokumenthash/-version, Akten-/Absenderversion und Fundstelle; er ist kein Autorisierungstoken. Aktuelle Rechte und Originalbytes werden separat geprüft. Die serialisierbare bestehende Transaktion umfasst Aufgabe, Workflowbezug und Audit; Konflikte melden 409, keine automatische Wiederholung. Replays desselben Eingangs liefern nur die bereits berechtigte bestehende Aufgabe. Regel/Quelle werden serverseitig in Aufgabenbeschreibung und Eingangsaudit festgehalten, editierte Angaben zusätzlich im Aufgabenverlauf. Abbruch/Ablehnung erzeugen keinen Datensatz. Keine Schemaänderung, keine automatische Aktion und keine Öffnung des fail-closed Malwaregates. Organisationsisolation weiterhin über den bestehenden eigenen Host-/Datenbankkontext, keine neue gemeinsame Mehrmandantendatenbank.

## Optionale interne Leistungsnachweise (23.10.2026)
Ein ausdrücklich aktivierter authentifizierter Host kann bestehende Firmenkontakte mit Dienstleistungsobjekten, unveränderlichen Planversionen und eingefrorenen Einsatzchecklisten verbinden. Aktivierung: `FirmenZentrale:Leistungsnachweise:Enabled=true`, nur bei passendem separat freigegebenem Datenbankschema. Standard aus; keine Verfügbarkeit im flüchtigen Vorschaumodus. Admin legt Objekte an; explizite Büroperson und zugewiesene Mitarbeiter benötigen weiterhin aktuelle Kontakt-/optionale Aktenrechte. Es gibt keine neue globale Bürorolle. Checkliste→Meldung→Büroprüfung→interne Freigabe→manuell markierte externe Rückmeldung, optional genau eine bestehende Nacharbeitsaufgabe. Plan-/Nachweis-/Aktionshistorien sind unveränderlich; Korrektur erzeugt neuen Einsatz mit Vorgänger/Grund. Interne Freigabe und manuell erfasste Rückmeldung sind keine verifizierte Kundenunterschrift oder automatische vertragliche Abnahme.
Nachweise sind zunächst gespeicherte, SHA256-geprüfte und HTML-encodierte A4-Druckansichten mit autorisiertem Zugriff; keine neue PDF-Writerbibliothek, kein öffentlicher Link/Versand oder Fremdupload. Fotos/Signaturen/Scanner und reguläre Dateiannahme bleiben gesperrt. Migration ausschließlich bewusst gegen isolierte Testdatenbank beziehungsweise nach separater Schemafreigabe anwenden; keine automatische Hauptdatenbankmigration. Globale Katalogverwaltung, Objektarchivierung und öffentliche Bestätigung sind nicht Bestandteil dieses ersten Schnitts.


### Ergänzung: Objektpflege und Leistungskatalog
Objektbüro beziehungsweise Administration können Bezeichnung, Anschrift, Aktivstatus und interne Bemerkung mit Versionsprüfung bearbeiten. Kunden-, Akten- und Bürozuordnung werden dabei nicht geändert; alte Einsatz-Snapshots bleiben erhalten. Inaktive Objekte bleiben lesbar, neue Pläne/Einsätze sind gesperrt. Suche und Aktivfilter werden serverseitig nach Autorisierung und vor Pagination angewendet.

Der Leistungskatalog ist eine kleine Stammdatentabelle im bestehenden FirmenDbContext. Administration pflegt Name, Beschreibung, Kategorie, Reihenfolge und Aktivstatus. Objektbüro mit aktueller Kontextfreigabe darf paginiert lesen. Die Planoberfläche kopiert gewählte Werte in eine neue unveränderliche Planversion; eine spätere Katalogänderung ändert keine historischen Inhalte. Deaktivierung statt endgültiger Löschung. Bestehender aktorgebundener Aktionsledger, Transaktion, Audit und Versionsschutz werden wiederverwendet; Katalogaktionen besitzen keinen Objektbezug. Die optionalisierte Fremdschlüsselspalte erhält bisherige Objektaktionen. Alternative ausschließlich freie Planpositionen bleibt möglich; keine zweite Aufgaben-/Kontaktarchitektur. Neu bewerten bei differenzierten Katalogrollen oder zentralem Löschkonzept.

Additive Entwicklungsmigrationen Objektpflege/Leistungskatalog nur nach isolierter Schemafreigabe anwenden. Kein automatischer Hauptdatenbankwechsel, kein neuer Datei-/Foto-/Signaturweg. Automatische serverseitige PDF-Erzeugung und persistierte Kalendererinnerungen bleiben offen.


### Normaler isolierter Start und persönliche Erinnerungen
Der reguläre Programmeinstieg `--isoliert=private/arbeitsumgebung/ready.json` verwendet bewusst einen separat vorbereiteten, dauerhaften lokalen Entwicklungsbestand, dieselbe Anmeldung, UI und Navigation einschließlich Leistungsnachweisen. Kein Testprozess und keine45-Minuten-Grenze; keine automatische Migration oder Datenbefüllung. Profil und Datenbankpräfix, lokale Datenbank, feste private Ablage und bestehendes vertrauenswürdiges Zertifikat werden geprüft; eingeschränkte Laufzeitrolle. Die Konfiguration enthält keine Verbindungspasswörter. Vorbereitung/Provisionierung des isolierten Bestands ist ein ausdrücklicher lokaler Administrationsschritt; ein fehlendes Profil führt nicht in eine flüchtige Vorschau. Keine Produktionsfreigabe aus diesem Modus ableiten.

Passwortänderung: acht Unicodezeichen nur nach tatsächlich bestätigtem MFA; kurze Passwörter bleiben persistent an MFA gebunden. Erst ein neues Passwort mit mindestens15Zeichen erlaubt wieder eine freiwillige MFA-Abschaltung. Erstzugang/Wiederherstellung vor bestätigtem MFA verlangen15Zeichen, maximal128UTF16Einheiten; keine künstlichen Zusammensetzungsregeln. Bestandskennwörter werden nicht heimlich ersetzt. Dies ist keine vollständige NIST-Konformitätsaussage.

Termine speichern eine optionale strukturierte Adresse und persönliche Vorwarnzeit (0/5/15/30/1440Minuten). Fällige unquittierte Erinnerungen sind serverseitig eigentümergebunden paginiert, nach Wiederanmeldung weiterhin erreichbar und mit neuer Kennung nach Verschiebung/Erinnerungsänderung geschützt. Quittierung ist auditierbar und wiederholbar; Löschung verhindert Hinweise. In-App-Kopfzeile/Liste nutzt bei verfügbarem Web Locks einen automatischen15-Sekunden-Abgleich pro Origin; ohne diese Browserfunktion manuell öffnbar. Keine Mail/SMS/OS-Push- oder Offline-Verfügbarkeitszusage.24Stunden Vorwarnung ist eine feste UTC-Dauer; Berliner Ereigniszeiten und DST-Validierung bleiben erhalten.


## Strukturprüfung 25.10.2026 – Testassembly und Modulgrenzen

Die Solution `src/backend/Betriebsplattform.slnx` enthält vier Projekte. `Rechnungen` ist der unabhängige Finanzfachkern; `Import` referenziert ihn für XLSX-Prüfung und Herkunft. `Web` referenziert beide und komponiert HTTP, Authentifizierung, Fachmodule, EF/PostgreSQL und lokale Dokumentadapter. Das Frontend wird getrennt mit TypeScript/Vite gebaut und vom Webhost ausgeliefert. Der Projektgraph ist azyklisch. Fachmodule innerhalb von Web sind Ordner/Namespaces, keine technisch erzwungenen Assemblygrenzen.

`Betriebsplattform.Rechnungen.Tests` ist inzwischen eine gemeinsame Backend-Testassembly. Der historische Name beschreibt ihren heutigen Umfang unvollständig: Sie enthält Finanz-/Importtests, SQLite-Fachtests, echte Kestrel-/HTTPS-Vertragstests, opt-in PostgreSQL-/Restoretests und ein betreutes Browser-Gate. `MfaTests`, `PosteingangTests` und `SynthetischeArbeitsmappe` stellen auch von anderen Testklassen verwendete Fixtures bereit. SQLite-Nachweise sind kein Ersatz für PostgreSQL-Constraints und Parallelität; das betreute Browser-Gate ist kein autonomer CI-Test.

Vorläufig bleibt die gemeinsame Assembly erhalten (Alternative C). Ein sofortiger Split würde die bestehenden Fixtureabhängigkeiten, `InternalsVisibleTo`, Testfilter und öffentliche Dateiliste gleichzeitig ändern. Alternative A, fachliche Ordner innerhalb derselben Assembly, verbessert Navigation, trennt aber gemischte HTTP-/DB-Testklassen nicht automatisch. Alternative B ist später sinnvoll als kleiner vorbereiteter Split: zunächst gemeinsame synthetische Fixtures entkoppeln, dann den unabhängigen Rechnungs-/Importkern von Web- und opt-in Betriebsgates trennen. Dafür sind ein eigener Plan und Testdiscovery-/VS-Abnahme erforderlich. Revisionsauslöser: messbare Testbeeinflussung, getrennte CI-Anforderungen, deutlich längere Laufzeiten oder unabhängige Modulverantwortung.

Bis dahin nach konkreten Klassen filtern, beispielsweise `--filter "FullyQualifiedName~RechnungskernTests|FullyQualifiedName~ExcelImportvorschauTests|FullyQualifiedName~ExcelSicherheitTests"`. Der vollständige Offline-Lauf bleibt `dotnet test ... --no-restore`; externe Gates bleiben standardmäßig aus. Neue Tests dürfen eine übersprungene PostgreSQL- oder Hardwareprüfung nicht als bestandenen Integrationsnachweis ausweisen.

## Benutzerverwaltung und künftiges Berechtigungsmodell – 29.10.2026

### Phase 1: begrenzte Mitarbeitersuche

`Benutzerverwaltung.tsx` zeigt beim Öffnen keine Kontenliste. Eine lokale Suche nach Anzeigename oder Login zeigt höchstens zehn passende Konten; bei weiteren Treffern muss die Suche eingegrenzt werden. Die Auswahl öffnet ausschließlich das vorhandene Benutzerformular; „Zurück zur Benutzersuche“ leert Auswahl und Suche. Keine Browserpersistenz oder zusätzliche Konto-/Rechteänderung durch Suche und Auswahl. Benutzeranlage und der bestehende MFA-Notfallbereich starten eingeklappt. Kompakte Seitenabstände gelten nur für `data-bereich="benutzer"`; geöffnete Sonderfunktionen dürfen die Seite verlängern. Die persönliche Browserabnahme bleibt erforderlich.

**Offene Skalierungsgrenze:** `GET /api/admin/benutzer` in `Sicherheit/Anmeldung.cs` liefert nach Login sortiert höchstens 100 Konten ohne Gesamtanzahl/Pagination. Die zehn Treffer werden aus diesem geladenen Ausschnitt ermittelt, nicht aus allen Konten der Datenbank. Ein Konto außerhalb des Ausschnitts ist über diese Suche nicht erreichbar. Später separat beauftragen: serverseitige, administratorgeschützte Suche nach Login/Anzeigename mit stabiler Pagination, Gesamt-/Trefferanzahl, begrenzter Eingabe und Tests mit mehr als 100 Konten. Die Kontoauswahl im MFA-Notfallbereich verwendet ebenfalls diese bisherige API; keine Behauptung, dieses Limit sei dort behoben. Kein Backendumbau in Phase 1.

### Ergänzung zum lokalen Sitzungsverhalten und zur Anordnung – 29.10.2026

Im authentifizierten `Development`-Host stellt `Anmeldung.Dienste` pro Hostinstanz eine neue Kennung bereit. `OnSigningIn` bindet jedes Sitzungscookie über geschützte `AuthenticationProperties` daran; `OnValidatePrincipal` verwirft fehlende oder fremde Kennungen vor der bisherigen Konto-/MFA-Prüfung. Deshalb erfordert ein neuer lokaler Host eine erneute Passwortanmeldung, während Navigation/Reload innerhalb desselben Hosts angemeldet bleiben. Auch die gemeinsame Passwort-/MFA-Sitzungserneuerung erhält die Bindung. Produktions- und Staginghosts verwenden diese Bindung nicht; Cookielebensdauer, gespeicherter Schlüsselbund und vorhandene Sicherheitsprüfungen bleiben unverändert. Explizite Demo-/Vorschaumodi bleiben getrennte Modi ohne echte Anmeldung, kein Ersatz für den authentifizierten Visual-Studio-Start.

Die Benutzerseite ordnet Suche/Auswahl, Einzelbearbeitung, eingeklappte Benutzeranlage und eingeklappte MFA-Wiederherstellung in dieser Reihenfolge an. Das Desktop-Anlageformular nutzt zwei Spalten, schmal eine; Kopfverdichtung gilt nur für die Benutzerseite. Bestehende Einstellungsübersicht und Bereichsrechte bleiben unverändert. Ausgangsfehler durch HTTPS-Test reproduziert: Cookie wurde nach Hostneustart in Development akzeptiert. Nach Änderung 20 gezielte Backendtests und 40 Frontendtests bestanden; Testdatenhaltung ausschließlich synthetisches SQLite, getrennte temporäre Testschlüssel, HTTP/HTTPS und Frontend-fixtures. Keine Hauptdatenbankverbindung oder Migration. Der Neustarttest erzeugt zwei Hosts mit denselben Cookies, demselben Schlüsselbund und demselben SQLite-Bestand; er ist kein tatsächlicher Visual-Studio-Play-/OS-Prozess- oder Browsernachweis. Nächste Abnahme: persönlicher Neustart über das authentifizierte Development-Profil und Layout bei 1680 × 900. Die zuvor dokumentierte 100-Konten-Grenze bleibt offen.

### Phase 2: Analyse und Umsetzungsvorschlag, noch nicht implementiert

**Fortschreibung 29.10.2026:** Der separat beauftragte erste durchgängige Pilot **Kontakte** ist inzwischen implementiert: versionierte Mitarbeitervorlagen, individuelle Abweichungen, Admin-Matrix, Aktionsprüfungen, Bestandsübernahme und Audit. Umfang, Migration und erforderliche manuelle Übernahme: [ADR 0007](adr/0007-kontaktberechtigungen.md). Die nachfolgende frühere Gesamtplanung gilt weiterhin nur als Vorschlag für die übrigen Bereiche und eine zukünftige Datenbereichsverwaltung. Es wurde keine bestehende Datenbank migriert.

Dieser Abschnitt ist ein Vorschlag, keine neue Rechtefreigabe, beschlossene ADR oder Produktionsfreigabe. Bestehende Konten, Rollen, Ressourcenfreigaben, Daten und Migrationen werden durch Phase 1 nicht verändert. Keine automatische Library-Synchronisierung.

**Bestehende Kontrollen:**

- `Sicherheit/Anmeldung.cs`: Cookieanmeldung, MFA-Einrichtungssperre und Administratorgruppe `/api/admin`; Rollen `Administrator` und `Mitarbeiter`. Rollen-/Aktivstatusänderung mit Version, Sicherheitskennung und Schutz des letzten aktiven eingerichteten Administrators. Keine Rolle „Teamleitung“ oder „Büromitarbeiter“.
- `Sicherheit/Pilotzugriff.cs`, `Persistenz/Pilotmodell.cs`: `Ressourcenfreigabe` enthält genau einen Kontakt-/Firmen-, Akten- oder Dokumentbezug. Firmenfreigaben vererben Zugriff auf zugehörige Kontakte/Akten; Dokument-/Rechnungs-/Zahlungszugriffe werden zusätzlich aus Bezügen berechnet. Das ist keine Trennung von Lesen/Bearbeiten/Löschen. Administratoren sind von den allgemeinen EF-Ressourcenfiltern ausgenommen; persönliche Eigentumsregeln bleiben gesondert.
- `Akten/Arbeitsumgebung.cs`, `Persistenz/PostgreSqlHost.cs`: feste `IArbeitsrechte` für Kontakte/Akten/Kategorien/Diagnose, ergänzt um Administratorprüfungen für Kategorien/Diagnose. Kein persistiertes Benutzerrecht je Aktion. `Arbeitsmodule` und `src/frontend/src/Module.tsx` steuern Verfügbarkeit/Navigation, nicht individuelle Datenrechte.
- `Aufgaben/AufgabenDienst.cs`: Ersteller/Zuständiger oder Administrator, immer mit aktueller Prüfung referenzierter Daten. `Aufgaben/Termine.cs`: persönliche Eigentümerbindung einschließlich Entfernen. Kalender verbindet persönliche Termine und berechtigte Aufgabenfristen.
- `Leistungen/Leistungsdienst.cs`: optionales Modul; Kunden-/Aktenkontext plus Objektbüro/Einsatzzuordnung. Objekt-/Kataloganlage teilweise Administratoraufgabe. Keine allgemeine Leitungsrolle. `Sicherheit/Bildverwaltung.cs`: Dateischreibwege bleiben gesperrt; ein Bereichsrecht darf diese Sperre nicht öffnen.
- Ressourcenfreigabe/-entzug über `POST /api/admin/benutzer/{id}/freigaben` schreibt bestehende Sicherheitsereignisse mit Akteur/Aktion/Ressource. Der Zielbenutzer und vollständige Vorher-/Nachher-Rechte stehen dort derzeit nicht als eigener vollständiger Änderungsnachweis. Es gibt keinen Admin-Lesevertrag für alle wirksamen Bereichsrechte/Vorlagen.

**Bereiche und Aktionskatalog:** Die UI darf fachlich zusammenfassen, während das Backend unterschiedliche Aktionen getrennt prüft. Noch keine dieser neuen Berechtigungskennungen ist aktiv.

| UI-Bereich | Vorhandene Grundlage | Vorgesehene getrennte Aktionen und Grenzen |
| --- | --- | --- |
| Dashboard | bestehende Übersichten | Nur Lesen; jede Teilkarte prüft zusätzlich ihr Quellrecht. Keine Bearbeitungs-/Löschrechte erfinden. |
| Firmen und Objekte | Firmenkontakte und optionale Leistungsobjekte | Lesen/Bearbeiten getrennt je Firmenteil bzw. Objekt. Aktivierung/Archivierung ausdrücklich benennen; kein allgemeines Löschen. Firmenkontakte teilen das Kontaktmodell, daher keine zweite widersprüchliche Kontaktprüfung. |
| Kontakte | Firmen, Personen, Ansprechpartner und Vorschläge | Lesen/Bearbeiten; Archivieren als eigene Aktion, nicht als Löschen ausgeben. |
| Akten und Dokumente | Akten, Notizen, Korrespondenz, Dokumente, Posteingang und Arbeitsabläufe | Lesen/Bearbeiten; Zuordnen/Umordnen, Notizarchiv und Muster-/Scanverwaltung gesondert inventarisieren. Kein pauschales Dokumentlöschen, keine Öffnung von Malware-/Uploadgates. |
| Termine und Aufgaben | persönliche Termine, Aufgaben und Kalender | Lesen/Bearbeiten getrennt nach Termin/Aufgabe; nur Termine haben den vorhandenen Löschweg. Aufgabenarchivierung bleibt Archivierung; Eigentum, Zuständigkeit und Referenzrechte bleiben zusätzlich nötig. |
| Rechnungen | bestehende Rechnungs-/Zahlungsansichten | Lesen. Keine neuen Buchungs-, Zahlungs-, Storno- oder Löschfunktionen durch eine Checkbox. Strukturierter Import bleibt im Kontenbetrieb gesperrt. |
| Leistungen und Nachweise | optionales Leistungsmodul mit Objekten, Plänen, Einsätzen und Katalog | Lesen/Bearbeiten; Prüfen/Freigeben und Katalogpflege getrennt. Unveränderliche Nachweise/Planversionen nicht nachträglich bearbeitbar machen; kein Löschen vorsehen. |
| Auswertungen | vorhandene finanzielle Übersichten | Nur Lesen, zusätzlich Quell-/Finanzrecht und Datenbereich prüfen. |
| Benutzerverwaltung | bestehende Admin-Endpunkte | Konten lesen/verwalten und Berechtigungen verwalten getrennt. Sicherheitsrolle Administrator und starke Bestätigung bleiben Voraussetzung; keine Benutzerlöschung hinzufügen. |

Modulübergreifende Suche, Originaldateien, Vorschauen, Historien, Referenzen, Exporte und Assistenzkontexte müssen die Rechte der jeweiligen Quelldaten beachten. Bei zusammengesetzten Antworten fehlende Teilbereiche gezielt auslassen oder abweisen, niemals aufgrund eines Dashboard-/Suchrechts freischalten. Selbstbedienungsfunktionen für eigenes Passwort/MFA bleiben gesonderte Sicherheitsabläufe.

**Geplante Auswertungsregel:** Zugriff erfordert aktives eingerichtetes Konto, erfüllte Authentifizierungs-/MFA-Anforderungen, aktiviertes Modul, ausdrücklich erlaubte Aktion, erlaubten Datenbereich und die bestehenden fachlichen Zusatzbedingungen. Fehlendes oder unbekanntes Recht bedeutet im neuen Modell Ablehnung. Bereichsrechte werden mit dem Datenbereich UND-verknüpft; sie erweitern ihn nicht. Lesen folgt nicht automatisch aus Bearbeiten; ein Speichervorgang darf eine inkonsistente Auswahl verständlich ablehnen. Löschen und Rechteverwaltung werden niemals aus Bearbeiten abgeleitet.

**Geplante persistente Strukturen, Namen vorläufig:**

| Struktur | Zweck und Mindestinhalt |
| --- | --- |
| `BenutzerRechteprofil` | Benutzer-Fremdschlüssel, Versions-/Concurrencywert, bewusst zugeordnete feste Vorlagenversion, Übergangsstatus; keine neue Sicherheitsrolle. |
| `Rechtevorlage`, `Rechtevorlagenversion`, `Vorlagenrecht` | Name, Beschreibung, unveränderliche freigegebene Version und bekannte Bereich/Aktion-Paare. Änderungen einer Vorlage ändern bestehende Zuweisungen nicht automatisch. |
| `BenutzerRechteabweichung` | Eindeutiges Benutzer/Bereich/Aktion-Paar mit explizitem Erlauben oder Verweigern; individuelle Verweigerung hat Vorrang vor Vorlagenerlaubnis. Rückkehr zu „Vorlage“ entfernt die Abweichung bewusst. |
| `Datenbereich`, `DatenbereichFirma`, `BenutzerDatenbereich` | Explizite Gruppen freigegebener Firmen/Kunden im bestehenden eigenen Host-/Datenbankkontext. Firma/Akte/Dokument-Beziehungen für Nachfolger wiederverwenden; bestehende Einzel-Freigaben zunächst erhalten. Kein implizites „alle Daten“ und keine Annahme einer vorhandenen Mehrmandantenarchitektur. Vererbung bei neuen/verschobenen Daten ausdrücklich festlegen und testen. |
| `Berechtigungsaenderung` | Unveränderlicher Auditdatensatz mit Akteur, Zielbenutzer, Zeitpunkt, Vorher-/Nachher-Profil einschließlich wirksamem Unterschied, Vorlagenversion, Datenbereich und Korrelationskennung; keine Zugangscodes/Passwörter. Transaktion gemeinsam mit der Rechteänderung. |

Vorlagen „Büromitarbeiter“ und „Teamleitung“ sind fachliche Ausgangspakete, keine Sicherheitsrollen. Büro: freigegebene alltägliche Lese-/Bearbeitungsaktionen, kein Löschen oder Sicherheitsadministrieren. Teamleitung: gezielt zusätzlich freigegebene fachliche Leitungsaktionen; konkrete Bereiche und Freigabe-/Zuweisungsaktionen vor Umsetzung einzeln bestätigen. Keine pauschalen Leitungsrechte aus dem Namen ableiten. „Administrator“ bleibt eine privilegierte Rolle mit getrennt bestätigtem Vergabeweg; eine Vorlage/Abweichung darf diese Rolle weder setzen noch umgehen. Eigene Rechteerweiterung auch über Vorlagen-/Datenbereichsänderungen verhindern. Vorlagenwechsel zeigt einen Vergleich und erfordert Speichern; die UI kennzeichnet Herkunft, individuelle Abweichung und effektiv erlaubte/verweigerte Rechte.

**Serverseitige Prüfstellen:**

1. Bestehende Authentifizierungs-/MFA-Pipeline in `Anmeldung.cs` erhalten; separater zentraler Aktionsprüfer mit bekannter Bereich/Aktion-Kennung und Endpoint-Metadaten. Nicht allein aus HTTP-Methode oder URL ableiten: Archivieren, Zurücksetzen, Zuordnen und Löschen sind oft alle POST.
2. `PostgreSqlHost.cs` und seine Ressourcen-/Schreibfilter sowie Kontakt-/Akten-/Dokument-/Finanzdienste anbinden. Datenbereich bereits vor Zählen, Suche und Pagination anwenden; bestehende EF-Filter und historische Kontextprüfung erhalten.
3. Eigenständige Endpointgruppen für Aufgaben, Termine, Kalender, Leistungen, Posteingang, Dokumentanalyse/-muster, Scanverwaltung, Assistenz und Sicherheitsverwaltung vollständig inventarisieren. Endpunktfilter einer API-Gruppe allein deckt diese nicht ab. Direkte Downloads/Previews, Sammelwege, Neustarts und aktuelle Sitzungen mitprüfen.
4. Künftige Admin-GET/POST-Verträge für Rechteprofil, Vorlagen und Datenbereiche: Administratorrolle plus separates Verwaltungsrecht und frische starke Bestätigung für Änderungen, CSRF, Versionsprüfung, zulässige delegierbare Rechte und Transaktion/Audit. Mitarbeiter dürfen diese Endpunkte nicht verwenden. Rollenwechsel, Schutz des letzten Administrators und bestehende Notfallverfahren bleiben getrennt. Keine implizite Selbstprivilegierung oder Vorlagenausweitung für den handelnden Admin.
5. Keine langfristig veralteten Rechteclaims: wirksame Rechte je Anfrage aus aktueller Version auswerten oder einen nachweislich invalidierten Cache verwenden. Entzug wirkt auf die nächste Anfrage, nicht erst nach Logout. Gleichzeitige Rechteänderungen müssen einen Konflikt statt überschreibender Bestätigung liefern.

**Sichere Reihenfolge und Abnahmepunkte:**

1. **Kleinster separat beauftragbarer Schritt:** reinen C#-Aktionskatalog und Berechtigungsauswerter mit synthetischen, speicherinternen Tests erstellen. Eingaben: explizite Aktionsrechte, individuelle Abweichungen, expliziter Datenbereich und bestehende Kontextentscheidung; Ausgabe: Erlaubt/Verweigert mit internem Grund. Kein Anschluss an laufende Endpunkte, keine Persistenz/Migration oder UI. Tests: unbekannt/fehlend verweigert, Verweigerung schlägt Vorlage, Aktion UND Datenbereich, Bearbeiten erteilt weder Löschen noch Rechteverwaltung. Dieser Schritt ist hier nur geplant.
2. Endpointinventar und vorgeschlagene Aktionen fachlich abnehmen; Querverweise und zusammengesetzte Antworten ergänzen. Dann additive Tabellen/Constraints und neue Admin-Leseverträge implementieren; Migration ausschließlich gegen separat freigegebene isolierte PostgreSQL-Testdatenbank samt Rollback-/Restoreplan prüfen.
3. Snapshot der tatsächlich bestehenden Rollen/Ressourcen und effektiven Zugriffe erstellen, ohne reale Daten in Berichte zu kopieren. Bestehende Konten bleiben bis zum bewusst bestätigten Wechsel im ausdrücklich gekennzeichneten Bestandsmodus. Neue leere Profile bedeuten niemals Vollzugriff. Umschaltung erst nach Gegenvergleich; keine automatische Massenumstellung und keine pauschale Datenbereichsfreigabe. Bestehende Rechte weder unbeabsichtigt verlieren noch erweitern.
4. Rechteprofile/Vorlagen/Datenbereiche mit Versionsschutz, Selbstschutz und vollständigem Audit schreibbar machen. Noch keine allgemeine Freischaltung. Vorlagenanpassung darf zugeordnete Benutzer nicht still ändern.
5. Je Fachbereich Backend-Endpunkte und Dienste anbinden, zunächst einen kleinen lesenden Bereich isoliert abnehmen. Positive/negative HTTP-Tests mit zwei Benutzern, fehlendem Aktionsrecht, fehlendem Datenbereich, direkten IDs, Suche/Zählung/Historie, laufender Sitzung nach Entzug und konkurrierenden Änderungen; danach tatsächliche PostgreSQL-Constraints und Neustart prüfen. Zusammengesetzte Module erst nach ihren Quellen aktivieren.
6. Erst auf belegter Serverdurchsetzung die kompakte Matrix mit unterstützten Aktionen, Vorlagenvergleich, Datenbereich und klaren Save-/Fehlerzuständen bauen. Vorschau ohne Speichern verändert nichts. Anschließend pro Konto kontrollierte Übernahme, vollständiger Regressions-/Browsernachweis und persönliche Abnahme. Produktionsfreigabe und produktive Migration bleiben gesonderte Aufträge.

Nicht in diesem Durchlauf enthalten: Umsetzung des Auswerters, neue Rollen/Aktionsrechte, serverseitige Mitarbeitersuche, neue Tabellen/Endpunkte, Migrationen, produktive Datenbereichszuordnung oder funktionsfähige Berechtigungsvorlagen.
