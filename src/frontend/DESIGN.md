# Designsystem – integrierter Arbeitsraum

**Stand 18.10.2026:** Der Nutzer hat die Arbeitsraumrichtung Graphit/Off-White/Indigo ausdrücklich als vorläufige Grundlage akzeptiert. Sie ist jetzt die reguläre Oberfläche. Die aktuelle Integration wartet auf menschliche Funktionsabnahme. „Betriebsplattform“, B-Zeichen und Akzentfarbe bleiben vorläufig; endgültiger Produktname/Logo folgen separat. Keine weitere Designsuche aus diesem Auftrag ableiten.

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

Systemschrift Segoe UI Variable Text / Segoe UI / System, keine externen Fonts. Basis 16 px; Tabellen/Felder 15 px; ergänzende Rollen-, Status- und Kontexttexte mindestens 13 px; Detailtexte 15 px. Zeilenhöhe 1,55, Titel 30–42 px/Gewicht 650, Hauptkennzahl 44 px und Nebenwerte 28 px. Geldspalten rechtsbündig mit tabellarischen Ziffern. Keine festen Texthöhen/abgeschnittenen Fachwerte. Abstände überwiegend 8/12/16/20/24/28/36 px, kleine Radien 5–12 px.

## Shell, Navigation und Arbeitsansichten

- Durchgehende Graphit-Sidebar, 232 px Desktop/80 px kompakt, zugängliche Namen auch ohne sichtbaren Linktext. Nur Rechnungen und Importvorschau sind verfügbar. Spätere Module erst mit echter Funktion ergänzen. Kontextkopf und Arbeitstitel sind keine vorgetäuschte Unternehmens-/Benutzerauswahl.
- Hauptinhalt ohne schmale Maximalkappung. Bei ≤1050 px kompakte Navigation, bei ≤680 px horizontale Textnavigation; native Felder bleiben mindestens 44 px hoch. Breite Tabellen haben eigene benannte, fokussierbare Scrollbereiche, Links-/Rechts-Tasten bewegen ausschließlich den fokussierten Bereich.
- Rechnungsbestand mit zusammenhängender Kennzahlenzeile, integrierter Suche, echten Zahlungsstatusfiltern und Nummern-/Datums-/Rollensortierung. Gesamtkennzahlen beziehen sich ausdrücklich auf alle Rechnungen. „Rechnungsbestand“ statt „Alle Vorgänge“, Spalte „Aussteller / Empfänger“ statt unspezifischem „Geschäftspartner“.
- Eigene Detailarbeitsfläche: Beträge und bestätigte Zuordnung zusammen, Beteiligte und Zahlungsfolge links, Nachweise rechts; mobil untereinander. Deutlich sichtbarer umrandeter Button **„Zurück zur Rechnungsübersicht“** und Breadcrumb **Rechnungen / Rechnungsnummer** verwenden denselben Rückweg. Suche, Statusfilter und Sortierung bleiben erhalten; Fokus zurück auf den ursprünglichen Rechnungsknopf. Detailtitel erhält beim Öffnen Fokus. Detailauswahl ist derzeit flüchtig, kein Detail-Deep-Link.
- Import: synthetische Beispielauswahl, Prüfresümee und Datensatzregister, Originalzellinspektor an der geöffneten Zeile. Originalwert, aufgelöster Text, Datentyp, Formatindex, nicht ausgeführte Formel, Blatt/Zeile/Zelle bleiben erhalten. Datei-Hash und Vollbericht separat einklappbar. Hash belegt Dateibytes, keine Echtheit. Schritte sind Textorientierung, keine Importfreigabe.
- Dokumentenbereich zeigt derzeit ausschließlich den ehrlichen Leer-/Vorbereitungszustand. Keine klickbaren Scheinanhänge, kein Upload-, Notiz-, Stempel- oder Bezahlknopf. Fachkonzeption und späterer Lesepfad stehen in der Architektur.

## Komponenten und Status

`App.tsx`: eine reguläre Shell und Hashnavigation. `Rechnungen.tsx`, `Rechnungsdetails.tsx`, `Importvorschau.tsx`: fachliche Anzeigeansichten. `ui.tsx`: gemeinsamer Datenhook, Lade-/Fehlerzustand mit Wiederholung, Status, Meldungen, Titel, Kennzahlen und Tastaturscrollen. `Icon.tsx`: eigene SVG-Geometrien, 24-px-Raster/Strichstärke 1,8/abgerundete Enden. Keine zweite Fachberechnung oder zusätzliche Bibliothek.

| Zustand | Darstellung | Bedeutung |
| --- | --- | --- |
| Bezahlt | Grün, Check, Text | Backend meldet vollständig bestätigte Zuordnung |
| Bestätigt (synthetisch) | Grün, Check, eigener Text | Einzelne erfundene Zahlungsbestätigung, kein realer Banknachweis |
| Teilweise bezahlt | Amber/Orange, Teilkreis, Text | Bestätigte Teilzuordnung; tatsächliche Beträge in Tabelle und Detailübersicht |
| Offen | Slate, offener Kreis, Text | Regulär offen, weder Fehler noch Überfälligkeit |
| Zur Prüfung | Gold, Dreieck, Text | Unbestätigte Zahlung oder Prüfbedarf, keine Saldenwirkung suggerieren |
| Fehler | Rot, Kreuz, konkrete Meldung | Nachgewiesener Fehler |

„Keine Zeilenfehler“ bleibt neutral und bedeutet keine Importfreigabe. Ohne Fälligkeitsdaten kein Überfälligkeitsstatus. Farben nie alleinige Bedeutungsträger; SVGs dekorativ und nicht separat fokussierbar. Native Buttons, Selects und Disclosure, keine funktionslosen Demoaktionen. Originalwerte ausschließlich React-Text, nie HTML.

## Zustände, Accessibility und Abnahme

Laden als Statusmeldung, erneute Prüfung bis Antwort gesperrt. Verständliche Fehler mit erneutem Versuch, kein technischer Fehlertext nach außen. Leeren Bestand von leeren Filterergebnissen unterscheiden. 3-px-Fokus, Sprunglink zur aktuellen Ansicht, sichtbare aktive Navigation und Filter. Mobile Datensätze beschriftet untereinander; verschachtelte Originaltabellen behalten ihr eigenes zweidimensionales Layout.

Nach Änderungen relevante Fach-/API-/Frontend-/Repositorytests und Builds tatsächlich ausführen. Desktop zuerst: drei Szenarien bei 1440/1760 px. Kleinere Bildschirme nur auf offensichtliche Regressionen prüfen; keine eigenständige mobile Oberfläche in DOK-01. Kein horizontaler Seitenüberlauf, Rechnungsnummer nicht in schmale Spalten gedrängt. Rückkehr über Button und Breadcrumb mit Suche/Filter/Sortierung/Fokus, vollständige Zahlungs- und Importnachweise prüfen. Kontrastprüfung 30 Tokenpaare: 27 Textpaare ≥4,5:1, drei Fokus-/Eingabepaare ≥3:1. Bezug WCAG 2.2: `https://www.w3.org/TR/WCAG22/`; kein vollständiger Konformitätsnachweis daraus.

200%-Zoom, Screenreader, weitere Browser/echte Geräte, größere Bestände und längere Nutzung getrennt nachweisen oder offen kennzeichnen. Ergebnisse nur im bestehenden Entwicklungsnachweis und privaten operativen Plan; keine automatische Library-Synchronisierung. Vorläufige Designannahme ersetzt weder Funktionsabnahme noch spätere Marken-/Produktionsfreigabe.

## Kompakte Büroaktionen und Dokumente – DOK-01, 18.10.2026

Kontaktkarte.tsx bleibt die einzige Darstellung beider Parteien. Im UI „Firma & Kontakt“, „Rechnung von“ und „Rechnung an“. Firma und vorhandener Ansprechpartner sichtbar; Mail-/Telefon-/Detailsicons in einer kompakten Leiste statt großer Karten. Native Links/Details, 36-px-Ziele mit 6 px Abstand für Desktop, gleicher SVG-Stil, zugänglicher Name einschließlich Firma. Texttooltips bei Hover und Tastaturfokus; ausführliche Kontaktdaten/Herkunft nur in Kontaktdetails. Bestehende sichere URI-Helfer unverändert. Fehlende/ungültige Werte ohne Mail-/Telefonaktion; Kontaktdetails zeigen den tatsächlichen Fehlbestand. Kein Notiz-/Schreiben-Scheinbutton.

Rechnungsnummer/Datum/Status, Betragszeile „Rechnungsbetrag“, „Bestätigt bezahlt“, kontextabhängig „Noch zu bezahlen/erhalten“ oder neutral „Noch offen“. Richtung nur aus expliziter konsistenter Unternehmensperspektive. Keine neue Geldberechnung. Danach kompakte Kontakte und Hauptbereich mit Zahlungen links, Dokumenten rechts. „Zahlungsdatum“ ist kein Bankbuchungstag. Bearbeitungsverlauf bleibt als nicht erfasst gekennzeichnet; keine erfundenen Ereignisse.

Dokumente.tsx stellt die Originalrechnung zentral rechts dar, einen Zahlungsbeleg ausschließlich bei der zugehörigen Zahlung. Eine Dokumentkarte und ein Abrufweg, keine doppelten Dateien/Linklisten. Dateiname/Art sichtbar, Quelle/Version/Datum/Größe/Hash aufklappbar. Native PDF-Anzeige im neuen Tab ist die bewusst gewählte erste Lesefunktion; keine Einbettung/CSP-Lockerung. Tab schließen kehrt zur unveränderten Rechnung zurück. „Öffnen“ verspricht keine PDF-Bearbeitung oder Uploadfunktion. Metadaten mit Lade-/Fehler-/Leerzustand und erneutem Versuch; fehlende PDF mit verständlicher 404-Seite. Alle Dateien ausdrücklich synthetisch.

Dokumenten-Demo und aktive Schnittstellen/Sicherheitsgrenzen im Architekturort, ausgeführte Prüfungen im Entwicklungsnachweis. Unternehmens-/Vorgangs-/Dokumentkennungen bleiben technische Angaben außerhalb der primären UI. Desktop 1440/1760 px hat Vorrang; Tablet/Smartphone nur Regression. Vollständige mobile Fachoberfläche und produktive Rollenrechte bleiben spätere Pakete.
