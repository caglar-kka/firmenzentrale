# Designsystem – integrierter Arbeitsraum

**Verbindliche Nutzerentscheidung 22.10.2026:** MBOP verwendet Weiß/Blau/Navy/Cyan als aktives Standarddesign. Die frühere Graphit/Off-White/Indigo-Richtung ist abgelöst. Die Altpalette bleibt ausschließlich als `data-theme="legacy"` an `.studio` rückkehrbar; kein Benutzerschalter und keine duplizierte Seite. Frühere Abschnitte unten dokumentieren die Entwicklung; ihre Fach-, Kontext-, Sicherheits- und Interaktionsregeln bleiben erhalten. Bei Farb-/Markenwidersprüchen gilt dieser aktuelle Abschnitt.

Visuell ausgewertet: `01_Kommandobruecke_Hero.png` (Hero), `02_Login_Branding_Referenz.png` (Login), `03_White_Blue_Dashboard_HAUPTREFERENZ.png` (verbindliche Struktur/Palette), `04_White_Blue_Gesamtvorschau.png` (Ergänzung). Aktualisierung durch den ausdrücklichen Handoff-Auftrag: Der Originalausschnitt `02_Kommandobruecke_Hero_Originalausschnitt.png` ist unverändert als `public/mbop-kommandobruecke.png` eingebunden, mit Textalternative und ohne doppelte Schriftüberlagerung. Alle übrigen UI-Elemente bleiben HTML/TS/SVG und Systemschrift, ohne Downloads. Produktkennzeichnung MBOP; Mandanten- und Benutzeridentität weiterhin aus bestehenden Daten.

Verbindlich gemeinsam mit [AGENTS.md](../../AGENTS.md), [Architektur](../../docs/public/ARCHITEKTUR.md) und [Entwicklungsnachweis](../../docs/public/MVP-01-NACHWEIS.md). Frühere A/B/C-Entwürfe und der isolierte Prototyp sind historisch dokumentiert und privat bytegenau gesichert, keine parallelen aktiven Designs.

## Identität, Sprache und fachliche Grenzen

Firmenneutrale KMU-Software für DACH, Pilotbetrieb ohne kundenspezifische Produktmarke. Kühle helle Arbeitsfläche, Navy-Navigation, weiße Karten, klare blaue Aktionen und Cyan/Teal-Akzente. Violett ausschließlich für geplante KI-Hinweise. Subtile Schatten und sparsame illustrative Verläufe; keine Daueranimationen. Keine medizinisch belegte Ergonomie oder geprüfte achtstündige Nutzung behaupten.

Deutsche UI: Rechnungen, Rechnungsbestand, Zahlungen, bestätigte Zuordnung, offener Betrag, Importvorschau, Datensätze prüfen, Beleg und Herkunft. Nur vorhandene Rollen benennen: Aussteller/Empfänger beziehungsweise Zahler/Zahlungsempfänger. Keine pauschale Gleichsetzung mit Kunden oder Lieferanten. ISD bleibt vorläufig Geschäftspartner ohne bestätigten vollständigen Firmennamen; Rolle je Vorgang.

Rechnungsstand, Bestätigung einer Zahlung, Fälligkeit, Importprüfung und spätere Dokumentenbearbeitung sind getrennte Dimensionen. Geldwerte und finanzielle Zustände kommen ausschließlich aus dem Backend. Keine Geldtextkonvertierung, Prozentberechnung, Währungsumrechnung oder zusätzlichen Fälligkeitsdaten im Frontend. Region DE/AT/CH vorbereitet, aktiv de-DE/EUR; kein nationaler Steuer-/Rechtsregelcode.

## Tokens und Typografie

Zentrale Palette in `src/design-tokens.css` unter `:root, .studio`, Altwerte nur unter `.studio[data-theme="legacy"]`. Auch Login und MFA verwenden dieselben Tokens. Keine neue Bibliothek oder zweite CSS-Welt. `Arbeitsraum.css` enthält die gemeinsamen Komponenten und vorhandenen Fachlayouts.

| Token | Aktiver Wert |
| --- | --- |
| `--text` | `#12243d` |
| `--leise` | `#526780` |
| `--grund` | `#f2f6fc` |
| `--flaeche` | `#ffffff` |
| `--weich` | `#eaf2fc` |
| `--akzent` | `#1463df` |
| `--akzent-dunkel` | `#104cac` |
| `--akzent-hell` | `#e9f1ff` |
| `--fokus` | `#1463df` |
| `--rand` | `#d9e4f1` |
| `--eingaberand` | `#778ba5` |
| `--rail` | `#0f1b2d` |
| `--rail-text` | `#ffffff` |
| `--rail-leise` | `#bed0e7` |
| `--rail-active` | `#12518a` |
| `--rail-hover` | `#1b3655` |
| `--rail-fokus` | `#76dcff` |
| `--rail-kante` | `#29d2ed` |
| `--rail-rand` | `#49627e` |
| `--nachweis-bg` | `#edf4fc` |
| `--neutral-text` | `#405b78` |
| `--neutral-bg` | `#edf3f9` |
| `--erfolg-text` | `#12644f` |
| `--erfolg-bg` | `#e5f5ee` |
| `--arbeit-text` | `#805017` |
| `--arbeit-bg` | `#fff3e2` |
| `--warnung-text` | `#745810` |
| `--warnung-bg` | `#fff7e3` |
| `--fehler-text` | `#b42338` |
| `--fehler-bg` | `#fcecef` |
| `--kopf-bg` | `#ffffff` |
| `--kopf-text` | `#12243d` |
| `--kopf-leise` | `#526780` |
| `--kopf-badge` | `#eaf2fc` |
| `--tabellenkopf` | `#f4f8fd` |
| `--tabellentext` | `#405b78` |
| `--detail` | `#edf4fc` |
| `--zeile-alternativ` | `#f7faff` |
| `--zweite-text` | `#075f70` |
| `--zweite-farbe` | `#087f93` |
| `--zweite-hell` | `#e2f7fa` |
| `--cyan` | `#29d2ed` |
| `--teal` | `#076f80` |
| `--teal-hell` | `#e2f7fa` |
| `--ki` | `#7042c2` |
| `--ki-hell` | `#f1eafd` |
| `--hero` | `#0c2246` |
| `--hero-linie` | `#398af2` |
| `--hero-text` | `#d9eaff` |
| `--chart-blau` | `#1463df` |
| `--chart-teal` | `#087f93` |
| `--disabled-bg` | `#edf2f8` |
| `--disabled-text` | `#526780` |

Historische Legacy-Palette, ausschließlich unter `.studio[data-theme="legacy"]`; keine aktiven Standardwerte:

| Token | Legacy-Wert |
| --- | --- |
| `--text` | `#242631` |
| `--leise` | `#5d626f` |
| `--grund` | `#f6f6f3` |
| `--flaeche` | `#ffffff` |
| `--weich` | `#f0f0f5` |
| `--akzent` | `#5745c5` |
| `--akzent-dunkel` | `#423292` |
| `--akzent-hell` | `#efedfb` |
| `--fokus` | `#5745c5` |
| `--rand` | `#dedee5` |
| `--eingaberand` | `#858594` |
| `--rail` | `#252730` |
| `--rail-text` | `#f8f8fb` |
| `--rail-leise` | `#bfc1cd` |
| `--rail-active` | `#41404e` |
| `--rail-hover` | `#383a45` |
| `--rail-fokus` | `#c5bcff` |
| `--rail-kante` | `#a79af5` |
| `--rail-rand` | `#60616d` |
| `--nachweis-bg` | `#eeedf3` |
| `--neutral-text` | `#465368` |
| `--neutral-bg` | `#eaf0f5` |
| `--erfolg-text` | `#17633f` |
| `--erfolg-bg` | `#e8f4ed` |
| `--arbeit-text` | `#8c4a12` |
| `--arbeit-bg` | `#fff1de` |
| `--warnung-text` | `#78590b` |
| `--warnung-bg` | `#fff6d9` |
| `--fehler-text` | `#b42338` |
| `--fehler-bg` | `#fcecef` |
| `--kopf-bg` | `#ffffff` |
| `--kopf-text` | `#242631` |
| `--kopf-leise` | `#5d626f` |
| `--kopf-badge` | `#f0f0f5` |
| `--tabellenkopf` | `#f7f7fa` |
| `--tabellentext` | `#555a69` |
| `--detail` | `#f4f3fa` |
| `--zeile-alternativ` | `#fbfbfd` |
| `--zweite-text` | `#423292` |
| `--zweite-farbe` | `#5745c5` |
| `--zweite-hell` | `#efedfb` |

`--schatten: 0 4px 20px rgb(17 54 100 / 5%)`, `--schatten-dialog: 0 20px 80px rgb(15 27 45 / 22%)`. Die Blau-/Cyanrichtung ist aus Referenz 03 abgeleitet, Text-/Aktionsfarben zugunsten lesbarer Kontraste dunkler gewählt. Blau für Hauptaktionen/Links/Fokus, Navy für Orientierung, Teal für sekundäre Hinweise. Disabled erhält explizite Text-/Flächentokens statt pauschaler Transparenz. Text und Symbole ergänzen alle Statusfarben.

Systemschrift Segoe UI Variable Text / Segoe UI / System, keine externen Fonts. Basis 16 px; Übersichtstabellen und Zeilentitel 17 px (`--schrift-liste`), Tabellenköpfe 16 px (`--schrift-listenkopf`), ergänzende Tabellenangaben/Status 15 px (`--schrift-listenneben`); Felder 15 px; ergänzende Rollen-, Status- und Kontexttexte mindestens 13 px; Detailtexte 15 px. Zeilenhöhe 1,55 (Übersichtstabellen 1,6), Titel 30–42 px/Gewicht 650, Hauptkennzahl 44 px und Nebenwerte 28 px. Geldspalten rechtsbündig mit tabellarischen Ziffern. Keine festen Texthöhen/abgeschnittenen Fachwerte. Abstände überwiegend 8/12/16/20/24/28/36 px, kleine Radien 5–12 px.

## Shell, Navigation und Arbeitsansichten

- Durchgehende Navy-Sidebar, 232 px Desktop/80 px kompakt, zugängliche Namen auch ohne sichtbaren Linktext. Rechnungen, Importe, Posteingang, Vorgänge und Kontakte sind verfügbare Arbeitsansichten. Weitere Bereiche erst mit echter Funktion ergänzen. Kontextkopf und Arbeitstitel sind keine vorgetäuschte Unternehmens-/Benutzerauswahl.
- Hauptinhalt ohne schmale Maximalkappung. Bei ≤1050 px kompakte Navigation, bei ≤680 px horizontale Textnavigation; native Felder bleiben mindestens 44 px hoch. Breite Tabellen haben eigene benannte, fokussierbare Scrollbereiche, Links-/Rechts-Tasten bewegen ausschließlich den fokussierten Bereich.
- Rechnungsbestand mit zusammenhängender Kennzahlenzeile, integrierter Suche, echten Zahlungsstatusfiltern und Nummern-/Datums-/Rollensortierung. Gesamtkennzahlen beziehen sich ausdrücklich auf alle Rechnungen. „Rechnungsbestand“ statt „Alle Vorgänge“, Spalte „Aussteller / Empfänger“ statt unspezifischem „Geschäftspartner“.
- Kompakte Detailarbeitsfläche: Identität, Gegenpartei und Zahlungsstatus oben; Rechnungsbetrag, bereits bestätigt bezahlt und noch offen in einer Zeile. Rechnung von/an und kompakte Zahlungshistorie links, eine rechtsbündige Aktionsgruppe und Dokumentenvorschau rechts. Rückweg und Breadcrumb erhalten Suche/Filter/Sortierung und Fokus. Detailtitel erhält beim Öffnen Fokus; weiterhin kein Detail-Deep-Link.
- Importe: tatsächliche XLSX-Dateiauswahl nach Bestätigung synthetischer Daten, erkanntes Vorlagenblatt und bestätigte Firmenzuordnung, prüfbare Zeilen und ausdrückliche Auswahl gültiger Rechnungen, separate Übernahmebestätigung, verständlicher Bericht. Dateiauswahl und bestätigtes Mapping klappen beim laufenden Vorgang ein. Originalwerte je Zeile; Hash, Rohdaten, Mapping und Prüfentscheidung in einem technischen Prüfbereich. Alte synthetische Prüffälle bleiben als eingeklappte Übungen verfügbar und verwenden denselben Inspektor.
- Dokumente: Originalrechnung und Zahlungsbeleg als getrennte Auswahl im selben rechten Bereich. Ein Zahlungslink wählt dort den zugehörigen Beleg aus. Native PDF-Vorschau, Vergrößern und Schließen; keine redundanten Quellenkarten. Leere und fehlerhafte Zustände bleiben verständlich. Originalbytes werden serverseitig nicht verändert.

## Komponenten und Status

`App.tsx`: eine reguläre Shell und Hashnavigation. `Rechnungen.tsx`, `Rechnungsdetails.tsx`, `Importvorschau.tsx`: fachliche Anzeigeansichten. `ui.tsx`: gemeinsamer Datenhook, Lade-/Fehlerzustand mit Wiederholung, Status, Meldungen, Titel, Kennzahlen und Tastaturscrollen. `Icon.tsx`: eigene SVG-Geometrien, 24-px-Raster/Strichstärke 1,8/abgerundete Enden. Keine zweite Fachberechnung oder zusätzliche Bibliothek.

| Zustand | Darstellung | Bedeutung |
| --- | --- | --- |
| Bezahlt | Grün, Check, Text | Backend meldet vollständig bestätigte Zuordnung |
| Bestätigt | Grün, Check, eigener Text | Einzelne erfundene Zahlungsbestätigung, kein realer Banknachweis |
| Teilweise bezahlt | Amber/Orange, Teilkreis, Text | Bestätigte Teilzuordnung; tatsächliche Beträge in Tabelle und Detailübersicht |
| Offen | Slate, offener Kreis, Text | Regulär offen, weder Fehler noch Überfälligkeit |
| Zur Prüfung | Gold, Dreieck, Text | Unbestätigte Zahlung oder Prüfbedarf, keine Saldenwirkung suggerieren |
| Fehler | Rot, Kreuz, konkrete Meldung | Nachgewiesener Fehler |

„Keine Zeilenfehler“ bleibt neutral und bedeutet keine Importfreigabe. Ohne Fälligkeitsdaten kein Überfälligkeitsstatus. Farben nie alleinige Bedeutungsträger; SVGs dekorativ und nicht separat fokussierbar. Native Buttons, Selects und Disclosure, keine funktionslosen Demoaktionen. Originalwerte ausschließlich React-Text, nie HTML.

## Zustände, Accessibility und Abnahme

Laden als Statusmeldung, erneute Prüfung bis Antwort gesperrt. Verständliche Fehler mit erneutem Versuch, kein technischer Fehlertext nach außen. Leeren Bestand von leeren Filterergebnissen unterscheiden. 3-px-Fokus, Sprunglink zur aktuellen Ansicht, sichtbare aktive Navigation und Filter. Mobile Datensätze beschriftet untereinander; verschachtelte Originaltabellen behalten ihr eigenes zweidimensionales Layout.

Nach Änderungen relevante Fach-/API-/Frontend-/Repositorytests und Builds tatsächlich ausführen. Desktop zuerst: drei Szenarien bei 1440/1760 px. Kleinere Bildschirme nur auf offensichtliche Regressionen prüfen; keine eigenständige mobile Oberfläche in DOK-01. Kein horizontaler Seitenüberlauf, Rechnungsnummer nicht in schmale Spalten gedrängt. Rückkehr über Button und Breadcrumb mit Suche/Filter/Sortierung/Fokus, vollständige Zahlungs- und Importnachweise prüfen. Kontrastprüfung 30 Tokenpaare: 27 Textpaare ≥4,5:1, drei Fokus-/Eingabepaare ≥3:1. Bezug WCAG 2.2: `https://www.w3.org/TR/WCAG22/`; kein vollständiger Konformitätsnachweis daraus.

200%-Zoom, Screenreader, weitere Browser/echte Geräte, größere Bestände und längere Nutzung getrennt nachweisen oder offen kennzeichnen. Ergebnisse nur im bestehenden Entwicklungsnachweis und privaten operativen Plan; keine automatische Library-Synchronisierung. Vorläufige Designannahme ersetzt weder Funktionsabnahme noch spätere Marken-/Produktionsfreigabe.

## Kompakte Büroaktionen, Dokumente und Demoimport – 18.10.2026

Eine horizontale Aktionsgruppe rechts über den Dokumenten: E-Mail, Telefon, Notiz, Kontaktinformationen. Gemeinsame Kontaktwahl benennt Firma und Rechnungsrolle; standardmäßig die Gegenpartei bei bekannter eigener Perspektive. Native Links nur bei durch die bestehenden Helfer validierten Daten, fehlende Werte sichtbar deaktiviert. Einheitliche eigene SVGs, 38-px-Ziele, 6 px Abstand, zugängliche Namen, Titel und Texttooltips bei Tastaturfokus/Hover. Native Details und Dialoge; Fokus nach Schließen zurück auf den Auslöser. Keine weitere Aktionsleiste an den Parteien.

Firma und vorhandener Ansprechpartner stehen als knappe Rechnung-von/an-Angaben, keine dauerhaft offenen Kontaktkarten. Die bestehende Unternehmensperspektive begründet „Eigene Zahlung“ oder „Zahlung erhalten“; ohne konsistente Perspektive Zahler und Empfänger neutral zeigen. Beträge bleiben Serverwerte. Die globale Beispieldatenkennzeichnung macht den Entwicklungskontext deutlich; eine bestätigte Zahlung heißt „Zahlung bestätigt“.

Notizen erscheinen nach tatsächlichem Speichern mit Betreff, Autor, Inhalt und Erstellungs-/Änderungszeit. Die zentrale In-Memory-Akte hält Version und Verlauf bis zum Serverneustart. Archivierung verlangt Bestätigung und bleibt wiederherstellbar; archivierte Notizen sind standardmäßig ausgeblendet. Frühere Tab-Notizen sind nur ein eingeklappter Altbestand. Keine Änderung von Originalen oder Zahlungsständen.

Originalrechnung und Zahlungsbeleg verwenden denselben Vorschauweg. Inlineansicht rechts mit beschriftetem Frame; vergrößerte Ansicht als nativer modaler Dialog, Zurück/Escape erhält die Rechnung. Ladezustand, verständlicher Fehler mit Wiederholung, ehrlicher Dokumenten-Leerzustand. Technische Kennungen, Hash/Größe, Pfade und rohe Quellen bleiben vollständig außerhalb der normalen Mitarbeiteransicht. Diagnose ist ein expliziter separater Servermodus. Browser-PDF-Werkzeuge können lokale Kopien bearbeiten; die Anwendung bietet keinen Rückschreibweg für Originale.

Importe sind eine begrenzte vertikale Demo für ein Vorlagenblatt und neun fest benannte Spalten, kein allgemeiner Excel-Import. Ein globaler Beispieldaten-/Neustartverlust-Hinweis bleibt sichtbar. Firma aus Datei bewusst zuordnen; historische Originalbezeichnungen wie ISD bleiben unverändert und ergeben keine Rolle ohne den jeweiligen Vorgang. Nur gültige Rechnungszeilen sind auswählbar; Zahlungen und Fehler bleiben zur Prüfung. Doppelte Übernahmen werden verhindert. Vier verständliche Berichtswerte: übernommen, übersprungen, fehlerhaft, zur Prüfung. Die reguläre Rechnungsübersicht verwendet die übernommenen Daten. Kein technisches Prüfprotokoll im normalen Arbeitsbereich.

Desktop zuerst; bestehende Reflow-/Tabellenscrollregeln erhalten. Tatsächliche Browser-, Fokus-, Kontrast- und Fehlerprüfungen stehen im bestehenden Entwicklungsnachweis. 200-%-Zoom und Screenreader nur bei wirklichem Nachweis als geprüft melden. Diese Designquelle enthält die verbindlichen Darstellungsregeln; Funktions- und visuelle Abnahme bleiben beim Auftraggeber.

## Ergänzung: Aktenarbeit und Eingang (18.10.2026)

Rechnungen in der Sidebar öffnet auch aus derselben Hashroute die Übersicht; deren Such-/Filter-/Sortierzustand bleibt beim direkten Detailrückweg erhalten. Eine einzige rechtsbündige horizontale Icongruppe mit gleichen 38-px-Rahmen/18-px-Icons, Tooltips/Fokus; bei mehreren E-Mail-/Telefonwerten ausdrücklich auswählbar. Neue Kontaktentscheidungen aktualisieren die Rechnungsprojektion.

Ein Aktenbereich mit Dokumente/Notizen/Korrespondenz, Zählern, Pfeiltasten/Home/End und einem aktiven Panel. Listen maximal 260 px hoch und separat fokussierbar/scrollbar; vollständige Inhalte erst nach Auswahl. Notizen haben Betreff, Autor/Zeit, Auszug, Änderungsinformation und aufklappbaren Verlauf. „Notiz archivieren“ mit Bestätigungsdialog und Wiederherstellung. Alte Tab-Notizen sind eingeklappter Bestandshinweis, keine zweite primäre Ablage.

Dateiauswahl bleibt barrierefreie Alternative. Drop-Hervorhebung nur während tatsächlicher Datei-Drags über dem jeweiligen Eingangsbereich, keine globale Verarbeitung. Ablegen stellt Dateien zur ausdrücklichen Bestätigung bereit; Makros und Statuswörter erzeugen keine Finanzwirkung. Büroansicht zeigt Titel/Art/Zeit; technische Integritätsdaten bleiben nur in der separaten Diagnose abrufbar. PDF/Bild/Text und begrenzter DOCX-Textextrakt; Office und fremde PDFs kontrolliert herunterladen. Kein Schein-Scanner-/OCR-Button.

Zentrale Suchvorschläge ab zwei Zeichen mit 250-ms-Debounce, tatsächlichen Datenarten, Pfeiltasten/Enter/Escape, Löschen und klarem Leer-/Fehlerzustand. Posteingang bleibt allgemeiner Eingang mit Kategorie „Ungeklärt“ und getrenntem Prüfstatus, bestehender oder neuer Akte und ausdrücklicher Zuordnung. Kontaktvorschläge nennen Ziel-Firma, bisherigen Wert, Quellenwert und Quelle; keine stille Übernahme. Palette/Schrift und Desktoprichtung bleiben bestehen. Menschliche Designabnahme bleibt offen.


## Produktreife: zentrale Stammdaten und Arbeitslisten (18.10.2026)

Die normale Oberfläche verwendet erfundene Geschäftsnamen, Rechnungsnummern und Personen. Ein zentral abschaltbarer Hinweis im Footer kennzeichnet Beispieldaten und Neustartverlust. Keine wiederholten Demo-/SYNTH-Labels, rohe JSON-Ausgabe, technischen Testaktionen oder Prüfdatenbuttons. Interne IDs/Testflags und Originalwerte des Imports bleiben erhalten.

Sidebar öffnet immer die jeweilige Übersicht, auch aus demselben Bereich. Bereich/Datensatz/Dokument stehen in der Hashroute; Breadcrumbs und Browser Back/Forward verwenden diese Navigation. Suche/Filter/Sortierung bleiben beim direkten Detailrückweg erhalten. Arbeitslisten zeigen Kategorie, Kontakt, Bearbeitungsstatus und nur bekannte Ereignisse; keine erfundenen Fristen. Listen verwenden jeweils 20 Einträge pro Seite.

Kontakte sind zentral gepflegte Firmen und Personen, mit Ansprechpartnerbezug, Anschrift/Rolle/Abteilung, mehreren Telefon-/E-Mail-Werten und genau einem Hauptwert je Art. Native Formulare, Pflichtfelder, explizite Dublettenbestätigung, Versionskonflikte und nachvollziehbarer Verlauf. Archivieren erhält Verknüpfungen. Kontaktvorschläge verwenden „Als Hauptnummer übernehmen“, „Zusätzlich speichern“, „Ignorieren“; vorherige Werte bleiben erhalten.

Kategorienverwaltung nur für die serverseitig berechtigte lokale Arbeitsidentität. Gemeinsame Rubriken, eine Unterkategorieebene, Beschreibung/Reihenfolge/Archiv. Systemstatus werden hier nicht bearbeitet. Kategorieauswahl anderer Bereiche liest denselben Dienst. Sonstiges und Ungeklärt bleiben Fallbacks.

Korrespondenz bietet Brief, E-Mail, Antwortschreiben, Anfrage, Erinnerung, Gesprächsvermerk und Sonstige Korrespondenz. „In Akte speichern“ führt keinen Versand aus. Mehrere Anhänge aus einer durchsuchbaren begrenzten Dokumentliste; neuer Upload über denselben Dateieingang direkt im Dialog. Dokumente bleiben zentrale Originale mit Verweisen. Native Checkboxen/Radiofelder bleiben kompakt und tastaturbedienbar. Druckblendregeln entfernen Sidebar, Navigation, Diagnosen, Entwicklungshinweise und unnötige Controls.


## Sichtprüfungs-Deltas (18.10.2026)

Rechnungs-Zahlungsstatus, Vorgangs-Bearbeitungsstatus und Eingangs-Verarbeitungsstatus sind getrennte Fachbegriffe. Vorgänge: Neu, In Bearbeitung, Wartet auf Rückmeldung, Erledigt, Archiviert. Eingang: Neu, Zur Prüfung, Zuordnung vorgeschlagen, Zugeordnet, Erledigt. Keine gemeinsame konfigurierbare Statusliste. Keine Fälligkeit ohne Daten. Die Rechnungsübersicht zeigt getrennte offene Forderungen/Verbindlichkeiten aus vorhandener Backend-Perspektive; bei unvollständiger Richtung stattdessen ausdrücklich ungetrennte offene Rechnungsbeträge. Anzahl natürlich als „3 Rechnungen“.

Posteingang: Suche, Verarbeitungsstatus und Kategorie direkt; Firma/Kanal/Zeitraum unter „Weitere Filter“. Sichtbare Kanäle Upload/Scan-Ordner, technische Herkunft separat erhalten. Lesbare Dokumenttitel, keine Dateinamen als primäre Bezeichnung bei bekanntem Titel. Kontaktliste mit Kontaktart sowie Firma/Rolle, ohne wiederholte Aktiv-Spalte. Keine ungesicherten Telefon-Untertypen ableiten.

Kategorien: eingerückte Unterkategorien, keine technische Sortierzahl im Arbeitsplatz oder Editor. Hoch/Runter innerhalb derselben Ebene; Bearbeiten/Archivieren unter kompakter Zeilenaktion. Archivstatus nur bei archivierten Einträgen. Kontextbezogene Primäraktion, sekundäre umrandete Aktion und zurückhaltende Textaktion: insbesondere Hauptnummer übernehmen / Zusätzlich speichern / Nicht übernehmen. Rückkehr und Drucken unterscheiden sich entsprechend.

Lokale „Hinweise / Zu prüfen“ bleiben unabhängig vom gewählten Akten-Tab sichtbar; kein globales Aufgabenmodell. Korrespondenzdialog heißt „Korrespondenz erfassen“, speichert „In Akte speichern“. PDF-Browseransicht erhält initial view=FitH; Original und Overlay bleiben unverändert. Statusauswahl begrenzt breit, Kategorienzeilen kompakter; bestehende Palette und Tastaturfokus bleiben verbindlich.


### Pflichtfelder und Datenqualität
In berührten und neuen Formularen Pflichtfelder mit sichtbarem Stern, einmaligem „* Pflichtfeld“, required und aria-required="true" kennzeichnen. Fehler konkret in Text anzeigen, nie nur durch Farbe. Bevorzugte Telefonnummer/E-Mail statt „Hauptwert“. Ähnliche Einträge mit relevanten Vergleichsangaben zeigen; keine automatische Zusammenführung. Sichtbare Bereiche heißen Akten/Digitale Akten, technische Routen bleiben bestehen. Kategorien liegen unter Einstellungen.

Strukturierte Kontakte: Personen mit Vorname/Nachname, Organisationen mit zusammenhängendem Firmennamen; Anschrift mit Straße, Hausnummer, PLZ, Ort und Land. Vorhandenen Freitext ohne belegbare Zerlegung erhalten. Firma benötigt Name, Telefon und E-Mail; Person Vorname, Nachname und Telefon. Ansprechpartner sind zentrale Personenkontakte, keine zweite Kontaktablage. Optionale zusätzliche Kontaktzeilen werden erst mit gültigem Wert gespeichert.

Dokumentvorschauen bleiben beim Seitenaufruf geschlossen: zunächst Metadaten, Inhalt erst nach bewusster Auswahl. Uploadhinweise nennen nur tatsächlich unterstützte Kurzformate und das Limit; technische Grenzen erst im Fehlerfall erklären. Geeignete Autosave-Aktionen zeigen Speichern/Erfolg/Fehler sofort; kurzes Rückgängig nur mit Versionsschutz. Aktenstatus: zehn Sekunden Rückgängig, kein stilles Überschreiben. Herkunft aus dem Posteingang mit eindeutigem Rückweg erhalten. Seltene Kategoriearchivierung erklärt den Erhalt bestehender Zuordnungen.


MVP-01.5: fachlich nützliche Tabellenköpfe mit gemeinsamem Sortierkopf, nativen Buttons und aria-sort auf-/absteigend bedienen. Sichtbarer Pfeil ergänzt die Beschriftung. Offene Rechnungen/Akten nutzen neutrale Chips mit Wort und Rahmen, Überfällig bleibt rot vorbereitet ohne erfundene Fälligkeit. Globale Suche sichtbar als global kennzeichnen, lokale Filter bleiben auf ihre Liste begrenzt. Kategorien initial nur als kompakte Hauptkategorien zeigen, Kinder einzeln oder gemeinsam aufklappen; Beschreibung im Editor, Aktionen nah an der Zeile. Notizen ausdrücklich intern, Korrespondenz ausdrücklich externe Kommunikation/Entwurf; sekundäre Archivaktionen zurückhaltend. Firmenarbeit zeigt begrenzte zentrale Verweise statt duplizierter Inhalte. Die bestehende Screenshot-/Nachweisregel aus AGENTS.md gilt weiter.


## Dokumentenprüfung und Eingang – gezielte UX, 21.10.2026

Dokumentkontext, Original/Felddaten, geschäftliche Zuordnung und Prüfentscheidung bilden eine lesbare Folge. Feldgruppen: Beteiligte, Referenzen/Nummern, Datum/Zeitraum, Beträge und passende Zusatzangaben. Kurze Werte erhalten begrenzte Breiten in Zeichenmaßen; Felder bleiben responsiv. Bestätigen/Fundstelle stehen kompakt beim Feld, Status und Unsicherheit bleiben textlich erkennbar. Das Original bleibt auf breiten Ansichten während der Feldarbeit sichtbar; schmale Ansichten ordnen Original und Felder untereinander. Die Prüfaktionsleiste gehört zur Entscheidung, ohne wichtige Inhalte zu verdecken.

Ein sichtbarer Dokumenteingang bietet Dateiauswahl und einen ehrlich nicht verfügbaren Scannerweg. Direkte Aufnahme darf nicht simuliert werden; bestehende administrative Scanquellen sind getrennt abrufbar. Ausgewählte oder korrigierte Daten werden erst nach serverseitiger Bestätigung übernommen. Nicht gespeicherte Prüfangaben verlangen beim Schließen eine Entscheidung im bestehenden Dialog; Fortsetzen erhält Werte und Fokus. Keine neue Farbwelt, Navigation oder UI-Bibliothek.


## Kontextnavigation und Druck – 22.10.2026

Globale Aufgaben-/Posteingangszähler stehen im Arbeitsplatz, nicht im Rechnungsbestand. Rechnungsrichtung wird aus der bestehenden expliziten Unternehmensperspektive angezeigt; Salden bleiben Backendwerte. Aufgabenaktionen aus Rechnung, Akte und Kontakt öffnen den gemeinsamen Editor über dem Ursprung. Abbrechen/Escape erhält Datensatz, aktiven Tab und Fokus; nach Speichern bleibt der Ursprung mit einem ausdrücklichen Link zur Aufgabe erhalten. Rückwege und Textaktionen sind schon ohne Hover erkennbar.

Kontaktanschriften verwenden Straße/Hausnummer und PLZ/Ort mit begrenzten kurzen Feldern; Firmenauswahl ist suchbar und auf 20 Treffer pro Seite begrenzt. Die Dokumentenprüfung zeigt ohne Seitenanalyse die vorhandene sichere Text-/PDF-Vorschau, ohne erfundene Positionen. Eine gespeicherte Bestätigung, die nicht mehr zur aktuellen Analyse passt, verlangt eine sichtbare erneute Feldprüfung. Analyse-Verbindungsfehler werden angezeigt, manuelle Eingaben bleiben erhalten.

„Scannen“ öffnet die ehrliche Einrichtung und gegebenenfalls administrative Host-Inventur. Keine Aufnahme simulieren oder Treiberinstallation auslösen. Druckaktionen benennen Ansicht beziehungsweise Original, verwenden den normalen Browserdialog und erhalten Originalbytes. A4-Regeln entfernen Bedienelemente, erhalten Tabellenüberschriften und begrenzen Zeilenumbrüche. Layoutprüfung, geöffneter Druckdialog und Papierausdruck sind getrennte Nachweise.


## MBOP Dashboard und Login – 22.10.2026

Leere/ungültige Hashroute öffnet Dashboard, bestehendes `#arbeitsplatz` bleibt kompatibel. Begrüßung und Datum dynamisch; eigene Initialen und tatsächlicher Benutzerstatus unten in der Sidebar, vorhandenes Profilmenü im Kopf bleibt erhalten. Echte globale Suche weiterverwendet. Kein erfundenes Mandantenlogo oder Kundenname.

Vier Karten: Rechnungsbestand (Server-Gesamtanzahl, sonst vollständiger Demobestand), Kontakte (Serveranzahl oder vollständige Demoliste), meine offenen Aufgaben und neue berechtigte Eingänge. Geldtexte unverändert vom Server; keine eigene Saldenberechnung. Hero, KPI, Modulhub, KI-Status rechts, Fristenhinweise und untere Karten sind echte HTML/React-Komponenten. Hub enthält ausschließlich vorhandene Module. KI ist geplant, ohne Chatfeld oder Scheinaktion. Fehlende Zeitreihen/Aktivitätsaggregation haben ausdrücklich leere Zustände.

Prioritäten zeigen höchstens fünf offene Einträge der ersten nach Fälligkeit sortierten persönlichen Aufgabenseite; Einschränkung und erreichbare vollständige Liste sind sichtbar. Keine still begrenzte Historie. Rechte bleiben in vorhandenen Diensten, 403 entfernt vorherige Daten. Dashboard führt keine Schreiboperation aus.

Gemeinsames Mapping: Shell/Karten/Tabellen/Formulare/Dialoge/Tabs/Status über bestehende Komponenten und Tokens. 1280px: zweispaltige KPIs und Hinweise unter Hub; 680px: Navigation oben, Hub ohne Linien als Kachelliste, einspaltige untere Karten. 1050px: bestehende kompakte Rail. Fokus 3px; keine neuen Animationen. Login: eigene Navy-Illustrationsfläche links, kompaktes weißes Formular rechts; schmal untereinander. Login/MFA/CSRF/Recovery-Verträge unverändert.

Nachweise und tatsächliche Grenzen im bestehenden privaten Abschlussbericht, Abschnitt Weiß-Blau 22.10.2026. Browser-/Zoom-/Screenreader- und menschliche Abnahme nur gemäß tatsächlich dokumentiertem Umfang.


## Referenztreue und gemeinsame Seitenmuster – Fortsetzung 22.10.2026

Die Fortsetzung korrigiert ausdrücklich die zuvor zu ähnliche alte Seitenstruktur. Sidebar: Dashboard, Akten, Kontakte, Rechnungen, Posteingang, Aufgaben; anschließend Kalender, Zeiterfassung, Prozesse, Dokumente und Berichte ehrlich geplant und ohne href/Handler. Einstellungen bleibt rechteabhängig; vorhandene Importe stehen separat unter Werkzeugen. Dokumente sind in Akten/Posteingang vorhanden, lediglich eine eigene Gesamtübersicht ist geplant. Interner vertikaler Navigationsscroll; Profilmenü ausschließlich links unten mit echtem Namen, bekanntem Status und Initialenavatar. Mobil öffnet ein benannter Menübutton die Navigation; Profil bleibt erreichbar.

Kommandobrücke: eigene räumliche SVG mit Fensterfront/Trägern, Stadthorizont, vernetztem Globus, Monitorpulten, Sitzen und perspektivischem Boden. Referenz 01 bleibt Vorbild; keine kopierte Referenzgrafik und kein fotorealistisches Ergebnis behauptet. Geplante Strategie oben am Hub, sechs vorhandene Module rings um den Kern. Kompakter gemeinsamer Begrüßungs-/Suchkopf, vier KPIs, KI-Hinweise rechts, Prioritäten unten rechts mit gemeinsamer SVG-Flagge. Keine Zeitreihen oder zusätzliche KI-Funktionen erfunden.

Seitenmuster: `Arbeitsliste` in ui.tsx für Suche/Filter/Tabelle/Pagination; `p-seitenkopf` für Titel und kontextuelle Aktionen. Rechnung: kompakte drei Finanzkarten, einheitliche Liste; Objektkopf, Parteien/Beträge als Faktenband, Dokument-/Aktenbereich links, Zahlungshistorie rechts. CRM: Stammdaten/Ansprechpartner links, bestehende Verknüpfungsregister und Verlauf rechts. Akte: Informationsspalte und vorhandene Aktenregister. Posteingang: kompakter aufklappbarer Eingang, Arbeitsliste, Original/Zuordnung als zwei Bereiche; vorhandene Quellenmarkierung und Feldkorrekturen im Prüfdialog bleiben erhalten. Aufgaben verwenden dieselbe Arbeitsliste und beschriftete Prioritätsflaggen. Verwaltung nutzt kompakte Funktionsgruppen. Auth-, Speichern-, Abbruch-, Rechte- und Dokumentenhandler bleiben bestehen.

Profil-/Kontaktfoto: NICHT IMPLEMENTIERT. Im vorhandenen Modell wurde kein dedizierter Bildspeicher gefunden. Keine unsichere Browserpersistenz, frei adressierbaren Bilder oder Umgehung des geschlossenen Malwaregates ergänzen. Eine vollständige Bildstrecke mit Decodierung/Neuencodierung, Format-/Größenlimits, Metadatenentfernung, objektgebundener Autorisierung und Lösch-/Änderungspfad benötigt einen eigenen überprüfbaren Block. Der jetzige Avatar ist ausdrücklich neutral.

Zusätzliche zentrale Illustrationstokens: `--szene-nacht: #061328`, `--szene-himmel: #142d56`, `--szene-horizont: #697aa5`, `--szene-stahl: #233d60`, `--szene-ville: #0d2241`, `--szene-fenster: #9bbddb`, `--szene-globus: #55a8ff`, `--szene-licht: #e1fbff`. Ausschließlich Illustration; keine alternativen Status-/Textpaletten.

## Handoff-Fortsetzung 22.10.2026
Originalbanner mit intrinsischem Seitenverhältnis; auf schmalen Ansichten links ausgerichteter Zuschnitt mit vollständiger Textalternative. Hubkacheln auf 235 px begrenzt, untere Paare zum Kern versetzt. Kalender als paginierte Aufgabenagenda; Prozesse als bestehender Dokumentenworkflow; Dokumente als autorisierte Gesamtliste im authentifizierten Host; Berichte aus bestehenden Serverübersichten. Zeiterfassung bleibt offen. Ein zentraler Assistenzdialog als rechte Seitenleiste nutzt strukturierte Quellen und optionale lokale Inferenz. Keine automatisch gespeicherten Vorschläge; interne Notiz nur am bestehenden Aktenkontext mit ausdrücklicher Prüfung.

## KMU Dashboard 2.0 – 22.10.2026
Aktuelle Nutzerentscheidung: kompakter Überblick mit lesbaren 15–16px Inhalten,14px Nebenangaben. Originalmotiv als proportional beschnittener Panorama-Ausschnitt ohne eingebrannte Werbeschrift; sachlicher HTML-Text. Keine neue Bilddatei. Gemeinsames Modulregister speist Kernnavigation, einklappbare Weitere Module, Verzeichnis und höchstens sechs Hubkacheln. Serveraktivierung und persönliche Anmeldung filtern die Anzeige; Serverrechte bleiben maßgeblich. Einstellungen und Profil unten. Hubhover/-fokus mit begrenztem Glow und passender Linie, keine Daueranimation; Reduced Motion ohne Anhebung/Transition. Ein einzelner benannter, fokussierbarer Orb rechts unten öffnet denselben vorhandenen Assistenten.
KPIs: Zu prüfen=posteingang/uebersicht.pruefen; Aufgaben & Fristen=aufgaben/meine-uebersicht.offen mit heute/ueberfaellig; offene Forderungen/Verbindlichkeiten=unveränderte Serverbeträge aus uebersicht.offeneForderungen/offeneVerbindlichkeiten. Fehlende Werte ausdrücklich nicht verfügbar. Keine neue Finanzberechnung. Heute wichtig zeigt höchstens drei offene Aufgaben aus erster fälligkeitssortierter Seite sowie den echten Prüfbestand, keine Aktivitäts-/Trendattrappen.
Organisationsname stammt aus Hostkonfiguration FirmenZentrale:Organisation:Name, getrennt von MBOP. Fallback Name/Initialen/MBOP, kein ungesicherter Logoupload. Jeder bestehende Host hat einen eigenen Organisations-/Datenbankkontext, keine neue Mehrmandantenfunktion. Kalender: Monats-/Wochen-/Tagesraster über eigene berechtigte Aufgabenfristen, Europe/Berlin, begrenzte Zeitraumsabfrage. Eigenständige Terminpersistenz, Drag-and-drop und Serien noch nicht implementiert; keine Aufgabenfrist als Termin mit Dauer ausgeben.


## Eigenständige persönliche Termine – 22.10.2026
Im bestehenden Kalender werden persönliche Termine und autorisierte Aufgabenfristen getrennt dargestellt: Termin-Button mit Beginn/Ende, Frist-Link zur Aufgabe. Neuer Termin und freier Tag/Zeitblock öffnen denselben kompakten Dialog. Pflichtfelder, Europe/Berlin, exklusives Ganztag-Enddatum und explizite Löschbestätigung. Nicht existente/doppelte lokale Uhrzeiten werden erklärt und abgelehnt. Abbruch/Escape erhält Route und Ursprungsknoten/Fokus; laufende Speicherung verhindert Mehrfachklick. Konflikte lassen Entwürfe stehen. Reine Loopback-Vorschau bleibt deutlich flüchtig; authentifizierter Host speichert mit Eigentümerrecht, Versionsschutz und Audit im bestehenden EF-Kontext. Keine Kontakte-/Aktenzwangsverknüpfung, Einladungen, Serien oder Drag-and-drop. Dashboard unverändert.


## Bildverwaltung – sichere Vorbereitung, 22.10.2026
Firmenlogo unter administrativen Einstellungen, eigenes Profilbild unter Mein Profil. Solange Decoder/Neuencodierung, Malware-Freigabe und autorisierte private Bildablage fehlen: ausdrücklich Bildannahme gesperrt, kein Dateiinput, keine lokale Bildvorschau, keine Upload-/Ersetzen-/Entfernen-Handler. Deaktivierte Aktionen mit erklärendem Text, technische Freigabehürde bei persönlicher Anmeldung unter aufklappbarer Begründung. Vorschau benennt Beispielkonto und fehlenden dauerhaften Bildspeicher. Vorhandene Initialen werden über denselben passiven Initialenavatar dargestellt; Firmenname/Hub und MBOP-Produktmarke bleiben getrennt. Kein Dashboard-/Theme-/Kalenderumbau. Kein Bild in Browserstorage,Base64 oder fremdem URL-Speicher.


## Belegter Aufgaben-Regelvorschlag – 22.10.2026
Im bestehenden Folgeaufgaben-Dialog: genau eine versionierte Vorlage für bestätigte Versicherungsschreiben. Zielakte, Vorher/Nachher, Quellenhinweis sowie aufklappbarer Regel-/Originalbezug. Betreff, Hinweise, Zuständigkeit, Priorität und Datum bleiben editierbar; Aufgabe und belegte Fälligkeit benötigen getrennte bewusste Bestätigung. Abbrechen, Escape und Vorschlag ablehnen schließen ohne Anlage und mit Fokus am Ursprung. Technische Details bleiben aufklappbar. Keine neue allgemeine Workflowoberfläche und kein Uploadfreigabepfad; reine Vorschau bleibt manuell/flüchtig.


## Büroformulare und Rückmeldungen – 23.10.2026
Bestehende Formulare verwenden die gemeinsame Formularprüfung: deutsche Fehlerübersicht, erste ungültige Eingabe fokussieren, aria-invalid/aria-describedby und vorhandene Hilfebeziehungen erhalten. Pflichtsterne und Legende vor Eingabe; kein Verlust von Entwürfen. Firmenneuanlage benötigt strukturierte Straße/Hausnummer/PLZ/Ort. Historisch unvollständige Anschriften dürfen bei anderer Pflege unvollständig bleiben; eine bereits vollständige Firmenanschrift darf nicht still entleert werden. Telefonnummern mit internationalen Ziffern-/Trennzeichen, keine Buchstaben; Länder-PLZ für DE/AT/CH, konservative alphanumerische Prüfung anderer Länder. Kontakt-/Kalenderspeicherung zeigt Erfolg; flüchtige Vorschau benennt Neustart-/Reloadverlust. Terminbeschreibung bleibt die vorhandene Notizmöglichkeit. Kein Erinnerungsversprechen ohne persistente, quittierbare Implementierung.
Rechnungstyp vor Rechnungsnummer, Eingangs-/Ausgangsfilter getrennt von offenen Forderungen/Verbindlichkeiten. Status Offen nutzt bestehende Blautokens plus Text/Symbol. Nächste persönliche Termine stammen ausschließlich aus berechtigtem Kalender, höchstens zwei im Siebentagefenster, ohne Fake-Aktivitäten. Keine globale Cursor-/Textauswahlunterdrückung. Bild-/Dateigate bleibt geschlossen.

## Restpunkte Büro-UX – 23.10.2026
Unter Hub/Heute wichtig zwei kompakte echte Arbeitskarten: nächste berechtigte Termine und maximal zwei Eingänge zur Prüfung, jeweils mit Leer-/Fehlerzustand und Fachlink. Keine zweite Aufgaben-/KI-Karte und kein erfundener Aktivitätenfeed. Verstrichene Termine ausblenden, laufende zulassen; Ganztag-Enddatum bleibt exklusiv, Berlin unabhängig von Hostzeitzone. Keine feste Dashboardhöhe oder Schriftschrumpfung zur erzwungenen Kompaktheit; neue 100%/200%-Sichtabnahme bleibt erforderlich.
Auswertungen verwenden kompakte tabellarische Serverbestände mit getrennten Forderungen/Verbindlichkeiten und persönlichen Aufgaben-/Fälligkeitszahlen statt drei großer Wiederholungskarten. Keine Summierung überlappender Aufgabenmengen oder neue Finanzformeln. Termindialog benennt fehlende Erinnerungen ausdrücklich; keine wählbare Scheinfunktion ohne Persistenz/Quittierung/Wiederaufnahme.

## Interne Leistungsnachweise –23.10.2026
Optional aktivierter authentifizierter Fachbereich unter Weitere Module. Kunde→Objekt→versionierter Plan→Einsatz→Büroprüfung→unveränderlicher HTML-Nachweis→manuell dokumentierte Kundenrückmeldung. Bestehende Dialog-/Formular-/Quellenlistenmuster, keine neue UI-Welt. Teil-/Nicht-/Nicht-anwendbar immer beschriftet und begründet. Erfassende Person/Büroherkunft sichtbar; interne Freigabe keine Kundenunterschrift. Druckansicht ausdrücklich HTML-Fallback, SHA256 getrennt. Bilder/Portal/Versand bleiben gesperrt. In flüchtiger Vorschau nicht als verfügbar anzeigen.


### Objektpflege und Leistungskatalog
Bestehende Dialoge, Pflichtfeldhinweise, Quellenlisten und Seitensteuerung wiederverwenden. Objektliste mit Suche und Aktivstatus; Objektbearbeitung mit erhaltener Berechtigungszuordnung, sichtbarer Speicherung und begründet gesperrten Neuanlagen bei inaktivem Objekt. Katalog über den Fachbereich erreichbar, keine zusätzliche permanente Sidebarposition. Auswahl übernimmt eine bearbeitbare Kopie in den Plan; Katalog- und Planversion bleiben fachlich getrennt. Normale Geschäftssprache in Nachweisen und Oberfläche, Vorschau weiterhin ehrlich als flüchtig kennzeichnen. Keine Änderung am Dashboard/Banner/Hub/KI-Einstieg.


### Persönliche Terminerinnerungen
Eine kompakte Kopfzeilenaktion zeigt echte fällige Hinweise; Liste mit Terminöffnung und Quittierung im bestehenden Dialog. Kein zweiter KI-Einstieg, keine ungefragten Popups oder externer Push. Ortsbezeichnung und strukturierte Adresse fachlich getrennt, Vorwarnzeit gespeichert, Browser-/Offlinegrenzen verständlich. Vorhandene Navigationsschrift darf lange Modulnamen umbrechen; keine horizontale Abschneidung erzwingen. Echter200%-Browserzoom bleibt getrennt von schmaler CSS-Reflowprüfung nachzuweisen.


### Praxisabnahme – 25.10.2026
Die persönliche Erinnerung ergänzt die Kopfzeile rechts oben als kompakte, im Dokumentfluss liegende Hinweiskarte: Titel, lokale Berlin-Zeit mit Datum, strukturierte Adresse, Termin öffnen und Als gelesen markieren. Keine Überdeckung des Inhalts. Eine automatische Hinweiskarte pro WebLock-fähiger Browsersitzung; jeder Tab kann die vollständige Liste öffnen und gleicht spätestens beim nächsten erfolgreichen 15-Sekunden-Poll ab. Verpasste Hinweise bleiben bis zur Quittierung erhalten. Jede fachliche Terminänderung entwertet die alte Quittierung. Kein Push bei geschlossener Anwendung.
Planpositionen behalten editierbare Reihenfolge, Aktivstatus und Häufigkeit einschließlich Einmalig. Bestehende Einsatz-Snapshots ändern sich dadurch nicht. Freie Sonderpositionen gehören ausschließlich zum konkreten Entwurf; spätere Katalog-/Planübernahme bleibt eine gesonderte Büroentscheidung. Positions- und Gesamtfreitext bleiben frei editierbar. Nicht erfasste Positionen bleiben offen.
Vorhandene originale MBOP-Bildmarke und Wortmarke bleiben Produktbranding; NB im Organisationshub ist der ehrliche Firmeninitialenfallback. Statischer dezenter Cyan-Schatten an der Produktmarke, bei Reduced Motion ohne Effekt. Keine neue Bildmarke. Browsernachweise im privaten Prüfbericht; echte 200%-Zoom-/Screenreaderabnahme weiterhin offen. HTML-Druckarchiv bleibt ausdrücklich HTML, keine erzeugte PDF-Datei.
