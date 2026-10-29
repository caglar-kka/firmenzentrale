# Mitarbeit und Git-Regeln

## Aktueller Prüfstand – 20.10.2026

Der anschließende gezielte authentifizierte Browser-Systemtest hat den Dokument-/Aufgabenablauf einschließlich PostgreSQL-Speicherung und Kestrel-Neustart zusätzlich belegt. Dafür existiert ein ausdrücklich aktivierbares, begleitetes Testgate `AuthentifizierterBrowserTests`; normale Testläufe überspringen es. Es benötigt vorhandene lokale PostgreSQL-Verwaltungskonfiguration, `FZ_AUTH_BROWSER=1`, `FZ_BROWSER_WORKSPACE` und `FZ_BROWSER_CERT_THUMBPRINT` eines bereits vertrauenswürdigen Zertifikats für 127.0.0.1. Es richtet keinen Trust ein, verwendet neue isolierte Datenbanken, läuft höchstens 45 Minuten und erwartet Browserbedienung sowie lokale Kontrollkommandos. Es ist kein selbständig grüner CI-E2E-Test. Aktueller Nachweis und Grenzen: [Entwicklungsnachweis](docs/public/MVP-01-NACHWEIS.md).

Das lokale Profil `Betriebsplattform.Web` startet ausdrücklich die synthetische Vorschau. Das Profil `Persoenliche Anmeldung (HTTPS-Konfiguration erforderlich)` startet den getrennten Kontenbetrieb; ohne dessen Voraussetzungen bleibt der Zugriff gesperrt. Ältere Erläuterungen zum damaligen Standardprofil weiter unten beschreiben den vorherigen Stand.

Die Gesamtregression bestand mit 315 Backendtests, drei bewusst übersprungenen Live-/Hilfstests, 113 Frontendtests und sieben Repositorytests. Vier geprüfte PostgreSQL-Gates verwenden neue isolierte Datenbanken. Die älteren Gates für die bestehende Entwicklungsdatenbank dürfen nicht pauschal aktiviert werden. Die normale Suite benötigt deren Aktivierung nicht. Der zusätzliche Frontend-HTTP-Test wird mit `FZ_VORSCHAU_URL=http://127.0.0.1:5180` gegen den separat gestarteten synthetischen Vorschauhost ausgeführt; er ist kein Browser-/PostgreSQL-E2E-Test.

Debug/Release einschließlich TypeScript/Vite sowie aktuelle npm-/NuGet-Advisoryabfragen bestanden. Keine bekannten Advisories gemäß diesen Quellen; keine vollständige Lizenz- oder Sicherheitsfreigabe. Aktuelle fachliche Grenzen und verbleibende Abnahmen stehen im [Entwicklungsnachweis](docs/public/MVP-01-NACHWEIS.md).

## Kleine, prüfbare Änderungen

Die allgemeinen Entwicklungsregeln stehen in der öffentlichen AGENTS.md. Sie und diese Anleitung benötigen keine persönlichen Skills. Eine optionale lokale AGENTS.override.md wird von Git ausgeschlossen; bei ihrer Verwendung müssen auch die öffentlichen Projektanweisungen ausdrücklich gelesen werden.

Vor einer Änderung Ziel, fachliche Regeln, betroffene Dateien und Akzeptanzfälle klären. Kleine Änderungen mit verständlichen Commit-Nachrichten bevorzugen. Bei klarer Fachlogik zuerst einen tatsächlich fehlschlagenden Test ausführen und nach der Umsetzung erneut prüfen. Fehler zuerst reproduzieren und ihre Ursache untersuchen.

Für die spätere Zusammenarbeit sind kurze Aufgabenbranches und Pull Requests vorgesehen. Ein Pull Request beschreibt Problem, Änderung, tatsächlich ausgeführte Prüfungen und verbleibende Grenzen. Branchschutz und CI sind noch nicht eingerichtet.

## Daten und Konfiguration

Nur synthetische Beispieldaten verwenden. Keine Kundendaten, Originaldateien, Zugangsdaten, lokalen Datenbanken oder persönlichen Projektunterlagen veröffentlichen. Auch Dateinamen, Screenshots, Logs und Fehlermeldungen können vertrauliche Informationen enthalten.

Die `.gitignore` lässt nur ausdrücklich vorgesehene Root-Dateien und die Bereiche `src/`, `tests/` und `docs/public/` zu. Innerhalb dieser Bereiche gelten weitere Ausschlüsse für sensible Formate und Konfigurationen. Neue Ausnahmen benötigen eine Inhaltsprüfung. Die Regeln erkennen keine Geheimnisse in beliebigem Quelltext und ersetzen keine Prüfung.

Private lokale Entwicklungsunterlagen gehören nicht zum öffentlichen Projekt. Ein öffentlicher Klon setzt keine persönlichen Skills oder Playbooks voraus. Produktprüfungen müssen später ohne solche Unterlagen ausführbar sein.

## Vor einem Commit und vor Veröffentlichung

1. Nur konkret geprüfte Dateien mit `git add -- <Dateipfad>` auswählen; kein pauschales `git add .` und kein `git add -f` für ignorierte Dateien.
2. Mit `git status --short`, `git diff --cached --stat`, `git diff --cached` und `git diff --cached --check` den tatsächlichen Commit-Inhalt prüfen.
3. Mit `git ls-files` die vollständige Dateiliste prüfen. `.gitignore` entfernt keine bereits erfassten Dateien und bereinigt keine Historie.
4. Relevante Tests frisch ausführen; nicht ausgeführte Tests und fehlende Abdeckung benennen.
5. Vor einer Veröffentlichung die gesamte vorgesehene Historie, Metadaten und Inhalte auf private Informationen und Secrets prüfen. Git-Autorname und E-Mail bewusst wählen; keine globalen Einstellungen durch Projektskripte ändern.
6. Push, Veröffentlichung und externe Datenübertragung benötigen eine gesonderte Freigabe. Die Lizenzentscheidung bleibt offen.

Wenn ein Geheimnis versehentlich veröffentlicht wird, reicht das Löschen der Datei nicht aus: Veröffentlichung stoppen, betroffene Zugänge sperren beziehungsweise erneuern und die Bereinigung der Historie gesondert planen. Keine echten Geheimnisse in öffentlichen Issues melden.

## Öffentliche Repository-Prüfung

Voraussetzungen: Python 3 und eine vorhandene Git-Installation. Aus dem Projektordner:

```powershell
python -B -m unittest discover -s tests/repository -v
```

Falls Git nicht im Suchpfad liegt, kann für diesen Testprozess die Variable `BETRIEBSPLATTFORM_GIT` auf die vorhandene `git.exe` gesetzt werden. Die Tests verwenden ein isoliertes temporäres Git-Repository und synthetische Pfade. Sie erstellen keinen Commit und initialisieren nicht den Projektordner.

## Produktprüfungen ab MVP-01

Voraussetzung: .NET-10-SDK. Aus der Repository-Wurzel:

```powershell
dotnet restore src/backend/Betriebsplattform.slnx
dotnet test src/backend/Betriebsplattform.slnx --no-restore
dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release
```

Paketdownloads benötigen die entsprechende Erlaubnis. Bei vollständig vorhandenem lokalen Paketcache funktioniert Restore offline:

```powershell
dotnet restore src/backend/Betriebsplattform.slnx --source "$env:USERPROFILE/.nuget/packages" -p:NuGetAudit=false
```

Dieser Offline-Befehl führt keine aktuelle Schwachstellenabfrage aus. Er ist kein Nachweis unbedenklicher Abhängigkeiten. Die Testabhängigkeiten sind Microsoft.NET.Test.Sdk 17.14.1 (MIT), xunit 2.9.3 und xunit.runner.visualstudio 3.1.4 (Apache-2.0), gemäß lokalen Paketmanifesten. Sie sind ausschließlich Testwerkzeuge. Vor neuen Paketen oder produktivem Einsatz sind Herkunft, aktuelle Advisories und Lizenzpflichten erneut zu prüfen.

Neue öffentliche Dateien nach Inhaltsprüfung einzeln in der Inventarliste `PUBLIC_FILES` in `tests/repository/test_public_layout.py` ergänzen. Private Dateien und generierte `bin/`-/`obj/`-Verzeichnisse bleiben ausgeschlossen.

## Importvorschau prüfen

MVP-02 verwendet keine zusätzlichen NuGet-Pakete. Der bestehende Testbefehl führt auch die XLSX-, Validierungs- und Sicherheitstests aus. Gezielt:

```powershell
dotnet test src/backend/Betriebsplattform.slnx --no-restore --filter "FullyQualifiedName~Excel"
```

Synthetische Arbeitsmappen entstehen im Speicher aus öffentlichem C#-Testcode. Nur der Beispielszenario-Test schreibt eine künstliche XLSX-Datei und einen Textbericht in sein ignoriertes `bin/`-Verzeichnis. Keine Excel-Originaldateien oder binären Fixtures versionieren und keine globale Ausnahme für Tabellenformate ergänzen. Die Vorlagengrenzen und eine Beispielverwendung stehen in der Architekturübersicht.

## Oberfläche und HTTP-Vertrag ab MVP-03

Visual Studio: Startprojekt `src/backend/Betriebsplattform.Web`. Die geheimnisfreie Profilvorlage startet mit `Betriebsplattform.Web` ausdrücklich die synthetische Vorschau. Vorhandene lokale Profile werden nicht überschrieben; sie können zusätzlich einen vorbereiteten dauerhaften isolierten HTTPS-Bestand anbieten. Den gewählten Profilnamen und seine Argumente prüfen. Der Programmeinstieg ohne expliziten Startmodus verlangt persönliche Anmeldung. Ohne ausdrücklich konfigurierte PostgreSQL-/HTTPS-Sicherheitsvoraussetzungen erscheint unter `http://127.0.0.1:5180` ausschließlich der Einrichtungshinweis; Geschäfts-APIs bleiben gesperrt. Eine vorhandene Verbindungszeichenfolge allein aktiviert keinen Kontenbetrieb. Keine automatische Migration, Kontoanlage oder Zertifikatinstallation. Nach freigegebener HTTPS-Einrichtung muss die lokale Browser-Startadresse zum tatsächlichen HTTPS-Endpunkt passen. Die Einrichtungsschritte und externen Gates stehen in `docs/public/ARCHITEKTUR.md`.

Bei Quelländerungen baut der Webprojekt-Build TypeScript/Vite automatisch und übernimmt die neu erzeugten Assets nach `wwwroot`. Visual Studios Aktualitätsprüfung berücksichtigt die Frontendquellen. Kein zusätzlicher manueller Frontendbuild vor Play; installierte lokale Node-Abhängigkeiten sind Voraussetzung. Bei einem Buildfehler nicht „letzten erfolgreichen Build starten“ wählen. Reiner Beispieldatenbetrieb ist ausdrücklich getrennt: Profil `Demo (nur Beispieldaten)` oder `--FirmenZentrale:Startmodus=Demo --FirmenZentrale:Persistence:Provider=Demo --FirmenZentrale:Security:Enabled=false`. Er verwendet keine Konten oder Hauptdatenbank. Die geheimnisfreie Profilvorlage liegt unter `Properties/launchSettings.template.json`; bestehende lokale Profile werden beim Build nicht überschrieben.

Das Frontend liegt unter `src/frontend/`. Dort laufen `npm.cmd test` (TypeScript-Compile und Node-Test-Runner mit React/jsdom) und `npm.cmd run build` (TypeScript strict und Vite). Der gemeinsame Backend-Testlauf startet auch einen echten lokalen Kestrel auf einem temporären Loopback-Port und prüft HTTP-Vertrag, Header, Host-/Origin-Grenzen, unbekannte Funktionen und abgewiesene Schreibversuche. Dafür sind keine zusätzlichen Testhost-Pakete nötig.

Direkte Frontendpakete: React und React DOM 19.2.8, Vite 7.3.7, TypeScript 5.9.3, React-Typen 19.2.18, jsdom 26.1.0. Lokale Paketmanifeste: MIT, TypeScript Apache-2.0. Das Lockfile fixiert die Auflösung einschließlich optionaler Plattformpakete. esbuild wird auf die vorhandene Version 0.27.0 begrenzt; sie liegt innerhalb der vom installierten Vite deklarierten Abhängigkeitsspanne. Installation ausschließlich offline mit `--ignore-scripts --no-audit --no-fund`. Eine Deprecation-Warnung zu transitivem `whatwg-encoding` wurde beobachtet, kein aktueller Online-Advisoryscan durchgeführt. Vor Veröffentlichung müssen die vollständigen Lizenz-/Advisorypflichten geprüft werden.

Für React-DOM sind nur die verwendeten `createRoot`-/Root-Methoden lokal typisiert; kein Ersatz für vollständige externe React-DOM-Typdefinitionen. Der Produktcode bleibt TypeScript-strict. jsdom ist nur ein Testwerkzeug. Keine lokalen Konfigurationen, `node_modules`, `dist`, Buildartefakte oder privaten operativen Unterlagen versionieren.

Optionaler Entwicklungsbetrieb: `npm.cmd run dev` startet Vite ausschließlich auf `127.0.0.1:5173` mit API-Proxy zum Backend. Der nachgewiesene Gesamtlauf verwendet den gebauten Frontendstand über denselben Backend-Origin auf Port 5180. Keine CORS-Wildcards, `--host 0.0.0.0` oder globale Sicherheitsänderungen ergänzen. `--no-launch-profile` verhindert unbeabsichtigte lokale IDE-Startprofile; die Anwendung erzwingt ihre Loopback-Bindung zusätzlich im Code.

## UI-Änderungen und Sichtabnahme

Vor UI-Arbeiten das [Designsystem](src/frontend/DESIGN.md) lesen. Neue Oberflächen gemeinsam mit Fachlogik, Datenschutz, Eingabe-/Fehlerfällen und Tests umsetzen. Vorhandene Systemschrift, CSS-Tokens und native Controls verwenden; keine neue UI-Bibliothek ohne belegten Bedarf.

Die Frontendtests prüfen zusätzlich Filter/Sortierung, aufklappbare Details, Serverbeträge ohne Clientberechnung, Statussemantik, Datumdarstellung, Tastaturscrollen und Kontrastgrenzen der CSS-Tokens. Kontrasttests sind kein vollständiges Accessibility-Audit. Reale Browserprüfungen bei 1440/768/375 px, Fokus, 200 % Vergrößerung und Screenreaderabdeckung getrennt dokumentieren; nicht automatisierbare Prüfungen ausdrücklich offenlassen. Lokale Screenshots mit ausschließlich synthetischen Daten bleiben außerhalb der öffentlichen Produktdateiliste.

Build und Tests benötigen bei vorhandenem Cache/Restore keine Downloads: `npm.cmd test` und `npm.cmd run build` im Frontendordner, .NET mit `--no-restore`. Vor Backend-Neubuild den eigenen laufenden Prototyp mit Strg+C beenden, um unter Windows gesperrte Builddateien zu vermeiden. Keine pauschalen Shell-/Full-Access-Freigaben empfehlen; vorhandene konkrete Berechtigungen nutzen, notwendige Erweiterungen separat genehmigen lassen.


## Optionale lokale Dokumentanalyse

Nur vorhandene und bewusst freigegebene ausführbare Dateien konfigurieren: `FirmenZentrale:Dokumentanalyse:Ghostscript`, optional `Tesseract` und `Sprachdaten` (Verzeichnis mit `deu.traineddata`). Ohne PDF-/OCR-Werkzeug bleibt die entsprechende Analyse offen; kein automatischer Ersatzbetrieb. Malwarefreigabe ist davon unabhängig und bleibt erforderlich. Lizenz und Betrieb gemäß [ADR 0005](docs/public/adr/0005-lokale-dokumentanalyse-und-kontrollierte-muster.md) vor Kundeneinsatz prüfen.

Die optionalen Tests verwenden `FZ_PDF_TEST_EXE`, `FZ_OCR_TEST_EXE`, `FZ_OCR_TEST_DATA`. `FZ_DOCUMENT_PACKAGE` bezeichnet den vorhandenen fiktiven PDF-Paketordner mit README und Manifest, `FZ_DOCUMENT_EVIDENCE` einen getrennten lokalen Ergebnisordner. `FZ_ANALYSE_POSTGRES=1` erzeugt neue isolierte Testdatenbanken für Dokumentkorrektur, Muster und Scanordner; bestehende Geschäfts-Datenbanken sind kein Testziel. `FZ_WIA_TEST=1` liest ausschließlich das Geräteinventar. Ohne diese Voraussetzungen werden die betreffenden Tests ausdrücklich übersprungen.

Scanordner ist standardmäßig aus. Die lokale Betriebskonfiguration benötigt ausdrücklich `FirmenZentrale:Scanordner:Aktiv`, `QuelleId`, `Pfad` und `BenutzerId`. Nur ein bestehender berechtigter Benutzer und ein freigegebener lokaler Ordner sind zulässig, keine Laufwerkswurzel/UNC/Reparsepoints. Ohne produktiven Malwareadapter keine beliebige externe Aufnahme. Administrative Wiederholung im Posteingang ersetzt keine Sicherheitsprüfung. Quellen werden nicht gelöscht. Direkte WIA-Aufnahme/Companion und Hardwareabnahme sind offen.
