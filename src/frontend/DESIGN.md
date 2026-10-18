# Designsystem – integrierter Arbeitsraum

**Stand 18.10.2026:** Der Nutzer hat die Arbeitsraumrichtung Graphit/Off-White/Indigo ausdrücklich als vorläufige Grundlage akzeptiert. Sie ist jetzt die reguläre Oberfläche. Die Rechnungsdetailansicht ist ausdrücklich nicht abgenommen; die vereinfachte Fassung benötigt erneut menschliche Sichtprüfung. „Betriebsplattform“, B-Zeichen und Akzentfarbe bleiben vorläufig; endgültiger Produktname/Logo folgen separat. Keine weitere Designsuche aus diesem Auftrag ableiten.

Verbindlich gemeinsam mit [AGENTS.md](../../AGENTS.md), [Architektur](../../docs/public/ARCHITEKTUR.md) und [Entwicklungsnachweis](../../docs/public/MVP-01-NACHWEIS.md). Frühere A/B/C-Entwürfe und der isolierte Prototyp sind historisch dokumentiert und privat bytegenau gesichert, keine parallelen aktiven Designs.

## Identität, Sprache und fachliche Grenzen

Firmenneutrale KMU-Software für DACH, Pilotbetrieb ohne kundenspezifische Produktmarke. Ruhige helle Arbeitsfläche, eindeutige dunkle Orientierung, eine Indigo-Aktionsfarbe. Lebendigkeit aus Typografie, Proportionen und bedienbaren Komponenten; keine bunten Kennzahlen, Verläufe, großen Schatten oder unnötigen Animationen. Keine medizinisch belegte Ergonomie oder geprüfte achtstündige Nutzung behaupten.

Deutsche UI: Rechnungen, Rechnungsbestand, Zahlungen, bestätigte Zuordnung, offener Betrag, Importvorschau, Datensätze prüfen, Beleg und Herkunft. Nur vorhandene Rollen benennen: Aussteller/Empfänger beziehungsweise Zahler/Zahlungsempfänger. Keine pauschale Gleichsetzung mit Kunden oder Lieferanten. ISD bleibt vorläufig Geschäftspartner ohne bestätigten vollständigen Firmennamen; Rolle je Vorgang.

Rechnungsstand, Bestätigung einer Zahlung, Fälligkeit, Importprüfung und spätere Dokumentenbearbeitung sind getrennte Dimensionen. Geldwerte und finanzielle Zustände kommen ausschließlich aus dem Backend. Keine Geldtextkonvertierung, Prozentberechnung, Währungsumrechnung oder zusätzlichen Fälligkeitsdaten im Frontend. Region DE/AT/CH vorbereitet, aktiv de-DE/EUR; kein nationaler Steuer-/Rechtsregelcode.

## Tokens und Typografie

Einzige aktive Palette in `src/design-tokens.css` unter `.studio`; keine freien Einzelfarben in Komponenten ergänzen. `src/style.css` enthält Grundregeln und lädt Tokens sowie `Arbeitsraum.css`. Letztere definiert die Arbeitsoberfläche/Breakpoints, nicht neue konkurrierende Themes.

| Rolle | Tokens / Werte |
| --- | --- |
| Haupt-/Sekundärtext | `--text: #242631`, `--leise: #5d626f` |
| Seite / Register / neutral | `--grund: #f6f6f3`, `--flaeche: #ffffff`, `--weich: #f0f0f5` |
| Aktion / Hover / aktive Fläche | `--akzent: #5745c5`, `--akzent-dunkel: #423292`, `--akzent-hell: #efedfb` |
| Rahmen / Eingabe / Fokus | `--rand: #dedee5`, `--eingaberand: #858594`, `--fokus: #5745c5` |
| Navigation | `--rail: #252730`, `--rail-text: #f8f8fb`, `--rail-leise: #bfc1cd`, aktive/hovernde/fokussierte Zustände über eigene Rail-Tokens |
| Tabelle / Detail / Nachweise | `--tabellenkopf: #f7f7fa`, `--tabellentext: #555a69`, `--detail: #f4f3fa`, `--nachweis-bg: #eeedf3` |
| Bezahlt / Bestätigt | `--erfolg-text: #17633f`, `--erfolg-bg: #e8f4ed` |
| Teilzahlung | `--arbeit-text: #8c4a12`, `--arbeit-bg: #fff1de` |
| Offen / neutral | `--neutral-text: #465368`, `--neutral-bg: #eaf0f5` |
| Zur Prüfung | `--warnung-text: #78590b`, `--warnung-bg: #fff6d9` |
| Fehler | `--fehler-text: #b42338`, `--fehler-bg: #fcecef` |

Systemschrift Segoe UI Variable Text / Segoe UI / System, keine externen Fonts. Basis 16 px; Übersichtstabellen und Zeilentitel 17 px (`--schrift-liste`), Tabellenköpfe 16 px (`--schrift-listenkopf`), ergänzende Tabellenangaben/Status 15 px (`--schrift-listenneben`); Felder 15 px; ergänzende Rollen-, Status- und Kontexttexte mindestens 13 px; Detailtexte 15 px. Zeilenhöhe 1,55 (Übersichtstabellen 1,6), Titel 30–42 px/Gewicht 650, Hauptkennzahl 44 px und Nebenwerte 28 px. Geldspalten rechtsbündig mit tabellarischen Ziffern. Keine festen Texthöhen/abgeschnittenen Fachwerte. Abstände überwiegend 8/12/16/20/24/28/36 px, kleine Radien 5–12 px.

## Shell, Navigation und Arbeitsansichten

- Durchgehende Graphit-Sidebar, 232 px Desktop/80 px kompakt, zugängliche Namen auch ohne sichtbaren Linktext. Rechnungen, Importe, Posteingang, Vorgänge und Kontakte sind verfügbare Arbeitsansichten. Weitere Bereiche erst mit echter Funktion ergänzen. Kontextkopf und Arbeitstitel sind keine vorgetäuschte Unternehmens-/Benutzerauswahl.
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


MVP-01.5: fachlich nützliche Tabellenköpfe mit gemeinsamem Sortierkopf, nativen Buttons und aria-sort auf-/absteigend bedienen. Sichtbarer Pfeil ergänzt die Beschriftung. Offene Rechnungen/Akten nutzen kontrastreiches Amber mit Wort und Rahmen, Überfällig bleibt rot vorbereitet ohne erfundene Fälligkeit. Globale Suche sichtbar als global kennzeichnen, lokale Filter bleiben auf ihre Liste begrenzt. Kategorien initial nur als kompakte Hauptkategorien zeigen, Kinder einzeln oder gemeinsam aufklappen; Beschreibung im Editor, Aktionen nah an der Zeile. Notizen ausdrücklich intern, Korrespondenz ausdrücklich externe Kommunikation/Entwurf; sekundäre Archivaktionen zurückhaltend. Firmenarbeit zeigt begrenzte zentrale Verweise statt duplizierter Inhalte. Die bestehende Screenshot-/Nachweisregel aus AGENTS.md gilt weiter.
