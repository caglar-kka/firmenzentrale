# Designsystem der Betriebsplattform

Status: verbindliche UI-Grundlage für den lokalen Prototyp, vorläufige visuelle Richtung bis zur persönlichen Sichtabnahme. Gilt zusammen mit [AGENTS.md](../../AGENTS.md) und [Architektur](../../docs/public/ARCHITEKTUR.md). Kein zweites Engineering-Regelwerk und keine Zusage vollständiger Barrierefreiheitskonformität.

## Identität und Entscheidung

Leistungsfähige Geschäftssoftware mit ruhiger, verständlicher Bedienung für DACH-KMU. Firmenneutral; weder Pilotmarke noch fremde Logos, Designs oder Assets. Die bereitgestellten zwölf Software-Referenzen dienen als Prinzipien: kompakte Vorgangslisten, belegbare Details, konsistente Navigation und getrennte Finanz-/Prüfstatus. Keine neue Wettbewerbsbewertung durchgeführt.

Zwei konzeptionelle Richtungen wurden verglichen:

| Richtung | Wirkung und Vorteil | Nachteil |
| --- | --- | --- |
| Petrol und kühles Papier | Klar, sachlich, dezente Orientierung, ausreichend Abstand zu Warnfarben | Weniger wohnlich; großflächiges Petrol vermeiden |
| Graphit und warmer Sand | Zurückhaltend, papiernah, vertraute Dokumentanmutung | Bernsteinwarnungen heben sich weniger klar vom Umfeld ab |

Gewählt: Petrol und kühles Papier. Neutrale Arbeitsflächen dominieren, Akzentflächen bleiben klein. Keine Behauptung medizinisch belegter Augenfreundlichkeit oder achtstündiger Nutzungsvalidierung. Neu prüfen nach persönlicher Sichtabnahme, längeren Bürotests oder einer realen größeren Datenmenge. Dunkelmodus ist noch kein Teil dieses Vertrags.

## Tokens und Typografie

Die CSS-Variablen in `src/style.css` setzen diese Werte um. Neue Komponenten verwenden dieselben semantischen Tokens.

| Zweck | Text / Farbe | Fläche |
| --- | --- | --- |
| Haupttext | `--text: #19343b` | `--flaeche: #ffffff` |
| Nebentext | `--leise: #435d65` | `--grund: #e7edef`, `--weich: #dce7ea` |
| Primäre Aktion | `--akzent: #125c64`, weißer Text | Hover `--akzent-dunkel: #0a434b` |
| Hervorhebung | `--akzent` | `--akzent-hell: #d5e8e8` |
| Offen / neutral | `--neutral-text: #344e59` | `--neutral-bg: #e3eaed` |
| Erledigt / bestätigt | `--erfolg-text: #175838` | `--erfolg-bg: #d5eadf` |
| Teilzahlung / Bearbeitung | `--arbeit-text: #174f73` | `--arbeit-bg: #d8e9f5` |
| Prüfbedarf / Warnung | `--warnung-text: #6a4700` | `--warnung-bg: #f8e9c8` |
| Nachgewiesener Fehler | `--fehler-text: #94282e` | `--fehler-bg: #f7dedf` |
| Fokus | `--fokus: #176bb0` | 3 px Umriss, 3 px Abstand |
| Struktur / Eingabegrenze | `--rand: #bdcdd2` / `--eingaberand: #627b85` | Helle Fläche |

Systemschrift Segoe UI/system-ui ohne externe Requests. Basis 16 px mit Zeilenhöhe 1,5; Felder und Buttons erben die Basis. Tabellen 14 px, Metadaten 14 px, Tabellenköpfe 14 px fett / Status 13 px, kleine Rollenlabels 12 px. Haupttitel 30–38 px, Abschnitt 17,6 px, Details 16 px. Geld rechtsbündig und mit tabellarischen Ziffern. Browsertextvergrößerung zulassen, keine festen Texthöhen oder abgeschnittenen Werte. 12 px nur für ergänzende Rollenlabels, nie alleinige Fachinformation.

## Layout und Navigation

Inhalt maximal 1360 px einschließlich 32 px Seitenabstand; bei ≤1000 px 20 px, bei ≤680 px 16 px. Abstandsleiter 4/8/12/16/24/32 px, Radius 10 px für Bereiche, 6 px für Bedienelemente. Keine dekorativen Schatten, Verläufe oder Animationen. Weiß ist für eigentliche Arbeitsbereiche reserviert; Seitenhintergrund neutral.

Ein Markenkopf, eine Hauptnavigation, ein Hauptinhalt und ein Footer. Aktive Seite mit weißem Text auf Petrol, dunkler Unterkante und `aria-current`. Markenkopf dunkelpetrol, Hauptnavigation weiß; Kennzahlen mit dezenter oberer Akzentkante, offener Gesamtbetrag zusätzlich hervorgehoben. Sprunglink „Zum Inhalt“, sichtbar bei Fokus. Navigation enthält ausschließlich implementierte Ziele: Rechnungen und Importvorschau. Seitentitel „Excel-Importvorschau“ präzisiert das Dateiformat; „Datensätze prüfen“ bezeichnet die Tätigkeit innerhalb dieser Ansicht. Keine leeren Menüs oder Demo-Aktionsbuttons. Kein fixer Kopf, der Fokus oder Inhalte verdeckt.

## Listen, Formulare und Details

Desktop: semantische Tabellen mit Kopfzellen, Zeilenköpfen und Beschriftung; keine großen Rechnungskarten. Zeilen zeigen beide Partnerrollen, Datum, Rechnungsbetrag, bestätigte Zuordnung, offenen Betrag und Zahlungsstatus. Keine geratene allgemeine Kunden-/Lieferantenrolle. Suche durchsucht Nummer, beide Partner, Beleg und Quelle; Statusfilter und deklarierte Text-/Datumsortierung funktionieren lokal. Keine Geldsortierung durch Parsen formatierter Geldtexte. Gesamtkennzahlen bleiben über alle Rechnungen, unabhängig von Filtern, ausdrücklich beschriftet.

Details direkt nach der zugehörigen Zeile über einen mindestens 44 px hohen Textbutton mit `aria-expanded`/`aria-controls`. Button bleibt fokussiert; Enter/Leertaste öffnen und schließen. Zugeordnete Zahlungen, Zahler, Empfänger, Datum, Beleg und Quelle bleiben sichtbar erreichbar. Keine angeblichen Dokumentlinks ohne Datei. Fälligkeit fehlt im aktuellen Datenvertrag; kein überfälliger Status wird erfunden.

Formulare: sichtbare Labels, native Suche und Selects, 44 px Mindesthöhe. Primärbutton Petrol, sekundäre Aktion weiß mit klarer Beschriftung. Fokus nie nur durch Farbe. Kein Iconbutton ohne zugänglichen Namen. Eingabegrenzen ausreichend kontrastreich. Keine automatisch ausgelösten finanziellen Aktionen.

Import: Beispiel auswählen → Datensätze prüfen → Prüfbericht ansehen. Die nummerierte Liste ist eine Orientierung, kein interaktiver Buchungsassistent oder Hinweis auf verfügbare Importfreigabe. Zusammenfassung ohne Buchungs-/Freigabeversprechen; kompakte Tabelle mit Fehler-/Warnfilter. Meldungen zur Datei sind eigenständig erreichbar, bei leerer Fehlerantwort aufgeklappt. Zeilendetails zeigen Meldungen, Blatt/Zeile/Zelle, Datentyp, Originalwert, aufgelösten Text, Formatindex und nicht ausgeführte Formel. Datei-Hash und Vollbericht in benanntem Disclosure. Fehler haben Vorrang vor Warnungen, Warnungen vor neutraler Anzeige. Keine grüne „Import erfolgreich“-Anzeige für bloße Vorschau.

## Status und Sprache

Farbe immer mit Text und zusätzlichem Symbol: ✓ grün erledigt/bezahlt, ◐ blau teilweise bezahlt, ○ grau offen, △ bernstein zur Prüfung, × rot Fehler. Symbole dekorativ `aria-hidden`, Text bleibt zugänglich. „Bestätigt (synthetisch)“ ist ausdrücklich keine reale Bankbestätigung. Rechnungszahlungsstatus, Zahlungsbestätigung, Fälligkeit und Importprüfung bleiben fachlich getrennt. „Keine Zeilenfehler“ heißt weder importiert noch freigegeben.

Einfache deutsche Begriffe: Rechnungen, Zahlungen, Teilweise bezahlt, Offener Betrag, Daten prüfen, Originalwerte, Quelle, Zur Prüfung. ISD ist vorläufig Geschäftspartner ohne bestätigten vollständigen Namen; Rolle jeweils aus dem Vorgang. Originalbezeichnungen bleiben unverändert.

`src/darstellung.ts` bündelt regionale Metadaten `DE/de-DE/EUR`, `AT/de-AT/EUR`, `CH/de-CH/CHF`, Datumdarstellung und wiederverwendbare UI-Texte/Status. Das ist ein Erweiterungspunkt, keine vollständige Übersetzung. Aktuell aktiv: Deutsch/Deutschland, serverseitig formatierte EUR-Texte. Regionale Geldformate müssen später vom Backend mit exakten Werten geliefert werden; kein Geldparsing oder Umrechnung im Browser. CHF ist als regionale Metadaten vorbereitet, im Rechnungskern und Import weiterhin nicht freigegeben. Keine nationalen Steuer-/Rechtsregeln implementiert.

## Zustände und wiederverwendbare Bausteine

Wiederverwendbar: Status, Fehlerkasten mit erneutem Versuch, Datenlade-Hook, Metadatenraster, Listenbereich mit Werkzeugleiste, Disclosure, Quelltabellenbereich und Darstellungshilfen. Bestehendes React/CSS genügt; keine neue Komponentenbibliothek.

Laden: ruhige Textmeldung mit `role=status`, keine blinkenden Platzhalter. Leer: Ursache unterscheiden (kein Bestand / kein Filtertreffer / keine Vorschau), konkrete Hilfestellung und wo sinnvoll Filter zurücksetzen. Fehler: verständlicher Text, `role=alert`, funktionierender erneuter Versuch, keine internen Ausnahmen. Erfolg nur bei nachgewiesenem Zustand; keine Buchungswirkung einer Vorschau suggerieren. Requests werden abgebrochen und zeitlich begrenzt.

## Responsive und Accessibility-Abnahme

Bei 1440 px kompakte Vergleichstabelle; bei 768 px Tabelle in einem benannten, fokussierbaren seitlichen Scrollbereich mit mindestens 1020 px Tabellenbreite statt zerstückelter Überschriften; ≤680 px dieselben Datensätze als beschriftete gestapelte Zeilen, ohne Fachfelder auszublenden. Quelltabellen behalten ihre zwei Dimensionen und scrollen in einem benannten, fokussierbaren Bereich. Kein horizontales Scrollen der gesamten Seite. Details und lange Rohwerte dürfen nicht überlaufen.

Messbare Gates, ausgerichtet an WCAG 2.2: normaler Text ≥4,5:1, großer Text ≥3:1, relevante Bedienelementgrenzen/Fokus ≥3:1. Ziele projektseitig möglichst ≥44 px hoch (strenger als die 24-px-Mindestgröße mit Ausnahmen in WCAG 2.2). Tastatur ohne Falle, sichtbarer Fokus, logische Reihenfolge, eindeutig benannte Controls, Information nicht allein durch Farbe. Bei 200 % Vergrößerung keine verlorenen Funktionen oder abgeschnittenen Texte; Reflow bei schmalen Ansichten, für echte Datentabellen begrenzte zweidimensionale Ausnahme. Kein vollständiges Konformitätsversprechen ohne Screenreader-, Browser- und umfassendes Audit.

Vor UI-Abnahme tatsächlich ausführen und im bestehenden Entwicklungsnachweis festhalten:

- Backend-/Frontend-/Repositorytests und Release-/Frontend-Build.
- Suche, Statusfilter, Sortierung, auf-/zuklappbare Rechnungs-/Quelldetails und Navigation.
- Vollzahlung, Teilzahlungen, unbestätigte Zahlung ohne Saldenwirkung und Dublettenprüfung.
- Screenshots beider Ansichten bei 1440/768/375 px; keine horizontale Seitenüberbreite, alle Controls bedienbar.
- Tastatur-Sprunglink, Navigation, Formulare, Details, sichtbarer Fokus; 200 % Text-/Browservergrößerung soweit Werkzeuge es unterstützen.
- Kontrastmessung der tatsächlichen Tokenpaare sowie Lade-/Leer-/Fehler-/Wiederholungsfälle.
- Sicherheits-/Datenschutzgrenzen, neue Abhängigkeiten und öffentliche Dateiliste prüfen.

Normative Referenz: W3C WCAG 2.2, `https://www.w3.org/TR/WCAG22/`. Quellenprüfung ersetzt keine Prüfung der implementierten Oberfläche.

## Visueller Feinschliff nach Screenshot-Feedback

Bestehende Tabellen-/Detailstruktur beibehalten. Stärker abgestufte neutrale Flächen statt zusätzlicher Dekoration: Seitenhintergrund #e7edef, weiße Arbeitsbereiche, Tabellenkopf `--tabellenkopf: #dae6e8` mit `--tabellentext: #294852`, Details `--detail: #eaf2f3` und wechselnde Zeilen `--zeile-alternativ: #f3f7f8`. Erkennbare Trennlinien, fett gesetzte Tabellenüberschriften und Status mit Kontur sowie kräftigerem dekorativem Symbol. Keine Animation, kein fremdes Asset, keine zusätzliche Bibliothek.

Kopftokens: `--kopf-bg: #123c43`, `--kopf-text: #ffffff`, `--kopf-leise: #c9dcdf`, `--kopf-badge: #254e54`. Der dunkle Bereich bleibt auf den kompakten Markenkopf beschränkt. Bei Fokus auf der weißen Navigation bleibt der blaue Umriss mit Abstand sichtbar. Header-/Tabellen-/Detail-Kontrastpaare zusätzlich automatisiert prüfen. Die Verbesserung ist eine gestalterische Entscheidung; langfristige Büroverträglichkeit muss mit tatsächlicher Nutzung bewertet werden.
