# MVP-01 bis MVP-03 – Entwicklungsnachweise

Lokaler Entwicklungsnachweis vom 17.10.2026. Kein synchronisierter Projektstand und keine Produktivfreigabe.

## Ausgangszustand und Umfang

Ausgangscommit: `885397b` auf `main`, Arbeitsverzeichnis vor Änderungen sauber. Vorhanden waren öffentliche Projektdokumentation und sieben Repository-Prüfungen, noch kein Produktprojekt. .NET-SDK 10.0.402 und Testpakete waren lokal vorhanden. Git wurde aus der bestehenden IDE-Installation verwendet; es wurde nichts committed oder gepusht.

Einzelagent mit gezielten Architektur-, Entwicklungs-, Testing-, Security- und Dokumentationsprüfungen; keine weiteren Modelle oder Agenten. Auftrag: begrenzte lokale Implementierung mit erfundenen Daten. Risiko Q1 im synthetischen, reversiblen Entwicklungsumfang; kein Zugriff auf echte Geschäfts- oder Zahlungsdaten. Es gab keine UI, API, Migration, Bankanbindung oder produktive finanzielle Aktion.

## Änderungen

- Neue Solution: `src/backend/Betriebsplattform.slnx`.
- Neues Fachprojekt unter `src/backend/Betriebsplattform.Rechnungen/`: Projektdatei, `Rechnung.cs`, `Zahlung.cs`, `Nachweis.cs`, `Zahlungsbestaetigung.cs`, `Zahlungszuordnung.cs`, `Rechnungskern.cs`, `Pruefung.cs`.
- Neues xUnit-Projekt unter `tests/backend/Betriebsplattform.Rechnungen.Tests/`: Projektdatei und `RechnungskernTests.cs`.
- Neues ADR mit Fachvertrag unter `docs/public/adr/0001-rechnungs-und-zahlungskern.md` und dieser Nachweis.
- README, CONTRIBUTING, Architekturübersicht und geprüfte öffentliche Dateiliste im Repository-Test angepasst. `.gitignore` unverändert: Produktdateien sind einzeln versionierbar, private und generierte Dateien bleiben ausgeschlossen. Keine Dateien zur Git-Staging-Area hinzugefügt.

## Tatsächlich ausgeführte Prüfungen

Umgebung: Windows, .NET-SDK 10.0.402, Laufzeit .NET 10.0.13; xUnit 2.9.3, VS-Testadapter 3.1.4, Microsoft.NET.Test.Sdk 17.14.1. Alle Daten und Nachweise sind synthetisch.

1. Vor Änderungen: `python -B -m unittest discover -s tests/repository -v` zunächst ohne erreichbares Git fehlgeschlagen (Exit 1, kein Test ausgeführt). Mit prozesslokalem `BETRIEBSPLATTFORM_GIT` anschließend sieben Tests bestanden (Exit 0).
2. Restore ausschließlich aus dem vorhandenen lokalen NuGet-Paketcache mit `dotnet restore src/backend/Betriebsplattform.slnx --source "$env:USERPROFILE/.nuget/packages" -p:NuGetAudit=false`: Exit 0. Vorher war die .NET-Erstinitialisierung an Schreibrechten gescheitert; nach gesonderter Freigabe erfolgreich. Dabei erzeugte das SDK auch ein lokales HTTPS-Entwicklungszertifikat. Keine Pakete heruntergeladen.
3. Test-first: `dotnet test src/backend/Betriebsplattform.slnx --no-restore --verbosity quiet`: 25 ausgeführte Tests fehlgeschlagen, Exit 1. Der kompilierbare Kern enthielt ausschließlich nicht implementierte Operationen. Ein anschließender Einzelfall mit normaler Ausgabe belegte `NotImplementedException` als Ursache.
4. Nach Implementierung: `dotnet test src/backend/Betriebsplattform.slnx --no-restore --verbosity minimal`: 25 bestanden, Exit 0.
5. Zusätzliche Reviewfälle: 28 Tests, davon zunächst 27 bestanden und einer fehlgeschlagen (Exit 1). Der neue Negativtest zeigte die fehlende Betragsobergrenze. Ergänzte Validierung reserviert zwei Dezimalstellen, sodass auch an der erlaubten Obergrenze Cent-Teilzahlungen exakt bleiben.

6. Abschließend: `dotnet test src/backend/Betriebsplattform.slnx --no-restore --verbosity minimal`: 28 bestanden, keiner fehlgeschlagen oder übersprungen, Exit 0.
7. `dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release`: erfolgreich, null Warnungen und Fehler, Exit 0.
8. `python -B -m unittest discover -s tests/repository -v` mit prozesslokalem Git-Pfad: sieben bestanden, Exit 0. Dabei wurden die vollständige sichtbare Dateiliste, private Ausschlüsse, sensible Formate, Dokumentationslinks und offensichtliche Secret-/Privatpfadindikatoren geprüft.
9. `git diff --check`: Exit 0. Inhaltlicher Diff-Review der vier geänderten Bestandsdateien sowie Review der 13 neuen Produkt-/Test-/Dokumentationsdateien. Neue Dateien zusätzlich ohne Staging mit `git diff --no-index --check -- NUL <Datei>` auf Whitespacefehler geprüft. Git-Index unverändert; `.gitignore` und private Dateien unverändert.
10. `dotnet list src/backend/Betriebsplattform.slnx package --include-transitive --no-restore`: nach gesonderter Lesefreigabe für die lokale NuGet-Konfiguration Exit 0 (vorher Exit 1 wegen Zugriffsverweigerung). Direkte Testpakete wie angegeben; transitive Testpakete: Microsoft.CodeCoverage, Microsoft.TestPlatform.ObjectModel und Microsoft.TestPlatform.TestHost jeweils 17.14.1, Newtonsoft.Json 13.0.3, xunit.abstractions 2.0.3, xunit.analyzers 1.18.0 sowie xunit.assert/core/extensibility.core/extensibility.execution jeweils 2.9.3. Keine Paketverweise im Fachprojekt. Diese Bestandsaufnahme ersetzt keinen aktuellen Schwachstellenscan.

## Prüfgrenzen und nächste Aufgabe

Manuelles fokussiertes Review des eigenen Codes: unveränderliche Eingaben, Nachweisreferenzen, geschützte Salden, eindeutige IDs und Zuordnungspaare, Validierung vor Mutation, Ausschluss privater Dateien. Keine unabhängige Zweitperson und kein umfassendes Security-Audit. Tests prüfen den In-Memory-Fachvertrag; sie beweisen keine Echtheit von Belegen. Kein aktueller Online-Schwachstellenscan; Lizenzen der direkten Testpakete anhand lokaler Manifeste geprüft. Keine externen Laufzeitabhängigkeiten im Fachprojekt.

Noch nicht geprüft oder implementiert: Persistenz, parallele Zugriffe, Rechte/Mandanten, dauerhaftes manipulationsgeschütztes Audit, fachliche Dubletten mit neuen IDs, Storno, Korrekturen und reale Bank-/Importdaten. Diese Grenzen sind im ADR konkretisiert.

Als Nächstes: gemeinsame fachliche Durchsicht des kleinen Modells und der Tests, anschließend die kleinste Importvorschau mit synthetischen Daten und Prüfbericht planen. Vor weiteren Funktionen stabile Importidentitäten und Korrekturregeln klären; keine echte Excel-Migration beginnen.

## Session MVP-02 – Excel-Importvorschau, 17.10.2026

Dieser Abschnitt ergänzt den historischen MVP-01-Nachweis. Er ist eine lokale Momentaufnahme, keine Library-Synchronisierung oder Produktivfreigabe. Ausgangszustand: MVP-01 wie oben implementiert, 28 Fachtests erneut grün; dessen Änderungen weiterhin uncommitted und neue Produktdateien unversioniert. Keine bestehenden Nutzeränderungen verworfen. Auftrag: begrenzte lokale Entwicklung, ausschließlich künstliche Daten, kein schreibender Import. Einordnung: Datenimport, Architektur, Entwicklung, Testing, Security und Dokumentation; Q1 im reversiblen synthetischen Umfang mit fokussiertem Review unvertrauenswürdiger Datei-/XML-Eingaben. Ein Agent, keine zusätzlichen Modelle, keine Kundendaten.

Umgesetzt wurde die in der bestehenden ADR ergänzte Schrittfolge: rote Tests, sicher begrenztes Paketlesen, Quellen/Originalwerte, explizites Mapping, Validierung, Dubletten, Bericht, Negativtests und Review. Die konkrete Vorlagen- und API-Spezifikation steht in der bestehenden Architekturübersicht; keine neue Dokumentationsstruktur angelegt.

### Dateien und Verhalten

- Neues Modul `src/backend/Betriebsplattform.Import/`: `Betriebsplattform.Import.csproj`, `ExcelImportvorschau.cs`, `Importmodell.cs`, `XlsxLeser.cs`, `Zeilenpruefung.cs`. Kein zusätzliches NuGet-Paket. Das Modul referenziert die vorhandenen Fachobjekte und übernimmt deren Konstruktorvalidierung. Kein schreibender Aufruf eines Rechnungskerns.
- Neue Dateien im bestehenden Testprojekt: `SynthetischeArbeitsmappe.cs`, `ExcelImportvorschauTests.cs`, `ExcelSicherheitTests.cs`. XLSX-Testpakete werden aus C# im Speicher erzeugt; es werden keine Originaldateien eingelesen.
- Solution und Test-Projektreferenz ergänzt; README, CONTRIBUTING, Architekturübersicht, bestehende ADR, dieser Nachweis und öffentliche Inventarliste gezielt aktualisiert. Fachkern und `.gitignore` unverändert. Keine neue Freigabe für binäre Fixtures.
- Begrenzt auf die ausdrücklich synthetische Vorlage; ISD bleibt ungeklärt. Datei-SHA-256, Blatt, Zeile, Zelle, Rohwert, Text, Typ, Formel und Formatindex bleiben nachvollziehbar. Exakte EUR-Beträge, ISO-Daten, Unternehmensmapping und Pflichtwerte werden geprüft. Dublettenvergleich anhand Dateiinhalt, Quellschlüssel und Aussteller/Rechnungsnummer, einschließlich übergebener Vergleichshistorie. Kein stilles Überschreiben.
- Nur ungespeicherte Vorschaukandidaten. Zahlungen bleiben unbestätigt; keine automatische Zuordnung, Buchung, Speicherung oder Bankprüfung. Fehlerhafte Zeilen haben keine Kandidaten. Strukturell fehlerhafte Dateien geben keine Teilvorschau zurück. Ein Textbericht zeigt verständliche Ergebnisse und Quellen ohne ungeprüfte Zelltexte in Logs.

### Ausgeführte Prüfungen und behobene Fehler

Umgebung wie MVP-01: .NET-SDK 10.0.402, Windows, vorhandene lokale Testpakete. Der Offline-Restore mit `--source "$env:USERPROFILE/.nuget/packages" -p:NuGetAudit=false` benötigte erneut die ausdrückliche Lesefreigabe für die lokale NuGet-Konfiguration; danach Exit 0. Kein Download und keine neue Paketabhängigkeit. Vor der Freigabe scheiterte Restore an Zugriffsrechten; ein Testversuch ohne vollständige Assets konnte deshalb nicht laufen.

- Test-first: 13 neue Importfälle scheiterten am absichtlich nicht implementierten Leser, während alle 28 Fachtests bestanden (41 Fälle, Exit 1). Danach echte XLSX-Paket-/XML- und Fachvalidierung implementiert.
- Ursache des ersten Implementierungsfehlers eingegrenzt: Testpakete bestanden eine separate XML-Wohlgeformtheitsprüfung; im Leser wurde eine Beziehungs-URI irrtümlich durch den `XNamespace`-Operator als XML-Name gebaut. Korrektur auf URI-Text; danach 42 Tests grün.
- Zusätzliche Negativtests reproduzierten widersprüchlichen numerischen/Inline-Zellinhalt, doppelte Inhaltstypdeklarationen, einen geschlossenen Eingabestream sowie mehrdeutige Inline-Texte. Prüfungen vor Verarbeitung ergänzt; die ursprünglichen Gegenfälle und die gesamte Suite anschließend grün. Ein Syntaxfehler im Testaufruf und ein ungültiges XML-Steuerzeichen in einem Testgenerator wurden korrigiert; die Kontrolle solcher Steuerzeichen wird separat am mutierten Paket geprüft.
- Abschließender Produktlauf: `dotnet test src/backend/Betriebsplattform.slnx --no-restore --verbosity minimal` – **106 bestanden**, null fehlgeschlagen/übersprungen, Exit 0. Davon 28 bisherige Fachfälle und 78 neue Import-/Sicherheits-/Paketfälle.
- `dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release` – erfolgreich, **0 Warnungen / 0 Fehler**, Exit 0.
- Öffentliche Repository-Prüfungen: `python -B -m unittest discover -s tests/repository -v` mit prozesslokalem Git-Pfad – **7 bestanden**, Exit 0. Inventar, private Ausschlüsse, sensible Dateiformate, Links und offensichtliche Secret-/Privatpfadindikatoren geprüft.
- Statischer Review der neuen Quellen und Projektverweise: keine Netzwerk-/Prozess-/Dateischreiboperationen im Importmodul; DTDs deaktiviert, Resolver null, Paket-/Beziehungspositivlisten, begrenzte Dekompression/XML-Tiefe, valide Referenzen und Fehlermeldungen ohne interne Ausnahmedetails. Nur der synthetische Beispielszenario-Test schreibt ausdrücklich erzeugte Artefakte in `bin/`.
- Git-Diff und neue Dateien einschließlich Whitespaceprüfung kontrolliert; Staging-Area leer. Private Dateien und generierte XLSX-/Berichtsdateien bleiben ignoriert. Es gab weder Commit noch Push oder Deployment.
- Zusätzliche Gegenprüfung des erzeugten Beispiels mit Python-Standardbibliothek: ZIP-CRC über `zipfile.testzip`, XML-Wohlgeformtheit aller Paketbestandteile, drei Datenzeilen und SHA-256-Abgleich mit dem Bericht erfolgreich (Exit 0). Assets des Importprojekts bestätigen null NuGet-Paketabhängigkeiten. Whitespaceprüfung über alle 21 neuen Dateien des noch uncommitted MVP-01/02-Stands erfolgreich; davon acht in dieser MVP-02-Session neu.

### Überprüfbares Beispiel und Grenzen

Der Szenario-Test erzeugt unter `tests/backend/Betriebsplattform.Rechnungen.Tests/bin/Debug/net10.0/` die Dateien `MVP-02-synthetisch.xlsx` und `MVP-02-Pruefbericht.txt`. Ergebnis: drei Zeilen, davon zwei ohne Zeilenfehler (Rechnung 100,25 EUR, unbestätigte Zahlung 50 EUR) und eine mit unzulässigem negativem Betrag. Keine Importfreigabe. Datei und Bericht bleiben lokal und sind vollständig aus dem versionierbaren Testcode reproduzierbar.

Fokussiertes eigenes Review mit Tests, keine unabhängige Zweitperson, kein umfassendes Audit oder aktueller Online-Schwachstellenscan. Die Testpakete bleiben unverändert; NuGet-Audit war beim Offline-Restore ausgeschaltet. Es gibt keinen Nachweis allgemeiner Excel-Kompatibilität oder eines Excel-Anwendungs-/Windows-E2E-Laufs. Der Leser unterstützt einen engen XLSX-Ausschnitt, keine vollständige OpenXML-Schemavalidierung. Kein CSV/XLS/XLSM/XLSB, keine Excel-Seriendaten, keine Formelauswertung, keine automatische Interpretation von Anzeigeformaten, keine persistente Historie und keine Erkennung identischer Bankumsätze mit verschiedenen externen IDs. Kein dauerhafter Import oder Schutz produktiver paralleler Buchungen.

Nächstes sinnvolles Arbeitspaket: eine kleine lokale Anzeige dieser vorhandenen Vorschau mit Original-/Prüfwerten nebeneinander und expliziter Mappingprüfung. Dabei weiterhin nur synthetische Daten verwenden; keine automatische Buchung. Vor einem späteren schreibenden Import sind stabile externe Identitäten, Konflikt-/Korrekturregeln, Benutzerfreigabe, persistenter Wiederholungsschutz und Transaktions-/Auditkonzept gesondert umzusetzen.

## Session MVP-03 – Review, lokaler Prototyp und Abnahmepunkt, 17.10.2026

Lokaler Implementierungsnachweis, keine automatische Synchronisierung und keine Produktionsfreigabe. Ausgangsstand weiterhin uncommitted auf der bestehenden öffentlichen Grundlage. Ein Agent mit gezielten Architektur-, Entwicklungs-, Testing-, Security-, API- und Dokumentationsprüfungen. Keine zusätzlichen Modelle oder Unternehmensdaten.

### Phase A/B: MVP-02 und ISD-Klarstellung

Tatsächliche C#-Quellen und Tests geprüft, 106 bestehende Fälle und Release-Build erneut grün. Herkunft, Originalwerte, exakte Beträge, EUR, explizites Firmenmapping, Quell-/Rechnungs-/Dateidubletten und fehlerhafte Pakete bleiben durch vorhandene Gegenfälle abgesichert. Kein kritischer technischer Fehler im dokumentierten seriellen synthetischen Umfang bestätigt. Die bekannten Grenzen des engen XLSX-Ausschnitts, fehlender persistenter Importhistorie, Rollen und Datenbanktransaktionen bleiben ausdrücklich bestehen.

Die neue Nutzerklarstellung wurde lokal festgehalten: ISD vorläufig Firma/Geschäftspartner, vollständiger Firmenname unbestätigt, Rolle je Vorgang. Das ist eine neue fachliche Präzisierung, kein nachträglich behaupteter Fehler der früheren Arbeitsannahme. Zwei neue Testfälle belegten zunächst die fehlende aktuelle Berichtsaussage (rot), danach unterschiedliche Rollen als Aussteller und Empfänger (grün). `ISD-Testdaten`, Originalzellwert `ISD` und Zellquellen bleiben unverändert. Hinweis-/Berichtstexte aktualisiert, keine neue Rechnungsart oder finanzielle Regel erfunden.

### Phase C: tatsächlich implementierter Ausschnitt

- Neuer ASP.NET-Core-Host unter `src/backend/Betriebsplattform.Web/`: Projekt, Einstieg, API-Aufbau, reine Ansichtsverträge, synthetische Rechnungs-/Zahlungsszenarien und XLSX-Beispielgenerator. Referenzen auf vorhandenen Kern und Importleser; keine neuen NuGet-Pakete. Kein zweiter Saldenalgorithmus.
- Neues Frontend unter `src/frontend/`: eigene Designregeln, React/TypeScript-strict/Vite, CSS, HTML-Einstieg, typisierter und zur Laufzeit geprüfter API-Vertrag, Rechnungs-/Importansichten und DOM-Tests. Lockfile vorhanden. Keine weiteren UI-Frameworks, Webfonts oder Analytics.
- Drei Rechnungen: voll bezahlt, mit zwei Teilzahlungen, offen trotz unbestätigter Zahlung. Offener Gesamtbetrag 910 EUR wird vom Kern geliefert. Beleg, Herkunft, Beteiligte und Datum sind sichtbar. Die Daten bleiben vollständig künstlich.
- Drei XLSX-Fälle: ohne Zeilenfehler, mit negativem Betrag, mit widersprüchlicher Zahlungsdublette. Alle verwenden den echten vorhandenen Leser. Hash, Zellquellen, Rohwerte, Typen, normalisierte Zuordnungen und verständliche Meldungen werden dargestellt. Keine Dateiuploads oder Buchungsendpunkte.
- Navigation, Lade-/Leer-/Fehlerzustände, erneuter Versuch, Tastatur-Sprunglink und responsive Karten/Quelltabellen implementiert. Die Oberfläche berechnet oder konvertiert keine Geldbeträge; Originalwerte sind escaped React-Text.
- Bestehende Architektur-/ADR-/Entwicklungsdokumentation, README und CONTRIBUTING sowie öffentliche Dateiliste aktualisiert. Private Arbeitsunterlagen bleiben ausgeschlossen; keine Cloud-Synchronisierung oder zusätzliche Paketfreigabe aus lokalen Anweisungen abgeleitet.

### Ausgeführte Tests und Builds

1. Anfangslauf: 106 .NET-Fälle bestanden, Exit 0; bestehender Release-Build 0 Warnungen/Fehler, Exit 0.
2. Zwei ISD-Regressionen zunächst fehlgeschlagen, dann Gesamtlauf 108 bestanden, Exit 0.
3. Frontend-Test-first: zwei API-Vertrags-/Fehlerfälle bestanden, zwei noch nicht implementierte Oberflächenfälle fehlgeschlagen. Nach Implementierung vier Tests bestanden.
4. `dotnet test src/backend/Betriebsplattform.slnx --no-restore --verbosity minimal`: **113 bestanden**, Exit 0. Zusätzlich `dotnet test src/backend/Betriebsplattform.slnx --configuration Release --no-build --no-restore --verbosity minimal`: **113 bestanden**, Exit 0. Darunter fünf neue Webfälle (drei parameterisierte Importfälle, eine Ansichtsprüfung, ein echter HTTP-Integrationstest).
5. `dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release`: **0 Warnungen/Fehler**, Exit 0.
6. `npm.cmd test` unter `src/frontend`: TypeScript-Compile und Node-Test-Runner mit React/jsdom, **vier Tests bestanden**, Exit 0. Geprüft: synthetischer JSON-Vertrag und Negativfälle, verständliche Netzwerk-/HTTP-/Formatfehler, serverseitige Saldenanzeige, Navigation, Quellen und HTML-Textescaping, Backend-Ausfall/erneuter Versuch sowie die Sprunglink-Regression.
7. `npm.cmd run build`: TypeScript strict und Vite, Exit 0. 30 transformierte Module, ca. 205 kB JavaScript und 6,3 kB CSS vor Gzip. Keine Source Maps oder externen Laufzeitressourcen erzeugt.
8. `python -B -m unittest discover -s tests/repository -v` mit vorhandenem Git-Pfad: **sieben bestanden**, Exit 0; öffentliche Dateiliste einschließlich neuer Produktdateien sowie private Plan-/Instruktionsgrenzen geprüft. Git-Diff-/Whitespaceprüfung über alle 41 neuen öffentlichen Dateien des noch uncommitted MVP-01/02/03-Stands erfolgreich; Index leer. `.gitattributes` um LF-Regeln für HTML, CSS und MJS ergänzt, damit die neuen Quellen beim späteren Versionieren keine unerwarteten Zeilenendkonvertierungen erhalten. `.gitignore` unverändert; private Instruktionen, lokaler Kontext, operativer Plan, `node_modules` und `dist` zusätzlich mit `git check-ignore` bestätigt. Produktmodule enthalten laut Assets keine externen NuGet-Paketabhängigkeiten.

### Reproduzierte und behobene technische Probleme

- Offline-Installation versuchte zunächst ein nicht gecachtes esbuild 0.28.1 aufzulösen und brach mit `ENOTCACHED` ab. Kein Download. Die vorhandenen Vite-Metadaten erlauben `^0.27.0 || ^0.28.0`; die gecachte 0.27.0 wurde kompatibel fixiert und im Lockfile festgehalten. Anschließend Offline-Installation erfolgreich.
- Der erste Vite-Build scheiterte an Dateisandbox-Leserechten von esbuild in übergeordneten Verzeichnissen. Nach konkreter lokaler Toolfreigabe Build erfolgreich; keine System-Sicherheitsänderung oder Paketinstallation aus dem Netz.
- Der erste selbstständige Hoststart lud ein ignoriertes lokales IDE-Profil. Eine Bindungswarnung führte beim Windows-EventLog-Logger zu fehlenden Schreibrechten. Logger auf Konsole begrenzt und Start ohne Launch-Profil dokumentiert; keine globalen EventLog-Rechte vergeben. Ein noch laufender Fehlstart blockierte vorübergehend die ausführbare Datei beim nächsten Testbuild; gezielt beendet, danach Tests/Build frisch erfolgreich.
- Den Frontendpfad unabhängig vom Prozess-Arbeitsverzeichnis an das Repository-Buildlayout gebunden, damit der dokumentierte Hoststart den gebauten Stand ausliefert. Ein anderer Publish-Aufbau ist nicht geprüft.
- Der Tastatur-Sprunglink änderte anfangs in der Importansicht versehentlich auch die Ansicht. Mit einem roten Regressionstest reproduziert, auf reine Fokus-/Ankernavigation korrigiert, anschließend im DOM-Test und echten Browser bestätigt.

### Tatsächlicher Browser-/HTTP-Smoke und Sicherheitsreview

Der Release-Host wurde unter `http://127.0.0.1:5180` gestartet. Echter lokaler HTTP-Test mit temporärem Port: Loopback auch bei einem abweichenden URL-Konfigurationswert, Rechnungs-/Import-JSON, Enumtexte, Header, ungültiger Beispielname, unbekannte Funktion, fehlender Frontendbuild, abgewiesener Schreibversuch sowie fremder Origin und Host geprüft. Keine öffentliche Bindung und keine allgemeinen CORS-Freigaben.

Edge-Browser mit Produktionsbuild: Rechnungsübersicht, beide Teilzahlungen samt Belegen und Quellen sowie Importansicht mit Fehler in `ISD-Testdaten!G4` tatsächlich bedient. Backend gestoppt: verständliche Verbindungsfehlermeldung beobachtet; nach Neustart über „Erneut versuchen“ erfolgreich wiederhergestellt. Dublettenauswahl zeigt zwei gesperrte Zahlungszeilen, Originaldetails zeigen weiterhin den Quellwert `ISD`.

Visuelle Screenshots/DOM-Messungen bei 1440 × 1000 und 375 × 812: Desktop-Seitenbreite 1425 bei 1440 Viewport, mobil 360 bei 375 Viewport, keine horizontale Seitenüberbreite. Aufgeklappte mobile Quelltabelle: Inhalt 640 px innerhalb eines 284 px breiten eigenen Scrollbereichs. Tastatur-Sprunglink per Enter: Fokus auf `inhalt`, Importansicht und aktive Navigation bleiben erhalten. Das ist ein begrenzter echter Browser-Smoke, kein vollständiger automatisierter E2E-, Screenreader- oder Accessibility-Nachweis.

Security-/Datenschutzreview: ausschließlich künstliche Daten, read-only API, kein Upload, kein gemeinsam mutierter Rechnungskern, kontrollierte Beispiele, fester Loopback-Listener, Host-/Originprüfung, CSP ohne Inline-Skripte, no-store/nosniff, keine frei ausgegebenen internen Fehlerdetails, keine HTML-Interpretation von Originalwerten. Kein produktives Rechte-/Mandanten- oder Backupkonzept behauptet.

### Abhängigkeiten, Grenzen und Abnahme

Frontend ausschließlich offline mit `npm.cmd install --offline --ignore-scripts --no-audit --no-fund` aus bereits vorhandenem Cache aufgebaut. React/React DOM 19.2.8, Vite 7.3.6, TypeScript 5.9.3, React-Typen 19.2.18, jsdom 26.1.0; esbuild 0.27.0. Direkte Lizenzen anhand lokaler Manifeste geprüft: MIT, TypeScript Apache-2.0. Lockfile umfasst 110 Paketeinträge einschließlich optionaler Plattformpakete. Eine Deprecation-Warnung des transitiven `whatwg-encoding` ist dokumentiert. Kein aktueller Online-Advisoryscan und keine pauschale Supply-Chain-/Lizenzfreigabe. .NET-Testpakete unverändert, keine neuen NuGet-Pakete.

Zur Abnahme als **lokaler synthetischer Entwicklungsprototyp** bereit. Noch kein allgemeiner Dateiimport, keine Datenbank, Anmeldung, Mandantentrennung oder echten Geschäftsbelege. Kein öffentliches Hosting, sicherer Fernzugriff, echter Smartphone-Zugriff vom Netzwerk oder geprüfte Publish-/Installationsstrecke. Der responsive Browsercheck ersetzt keinen Test auf einem physischen Smartphone.

Nächste zusammenhängende Vorschläge nach menschlicher Abnahme: explizite Mappingprüfung und kontrollierte synthetische Dateiauswahl; anschließend gesondert freizugebende persistente Importidentitäten, Transaktionen und Audit. Keines dieser Folgepakete wurde durch diesen Abschluss automatisch begonnen. Keine Commits, Pushes oder produktiven Änderungen.

## Design-/UX-Sprint – 17.10.2026

### Ergebnis und geprüfter Umfang

Bestehende Rechnungskarten durch kompakte Vergleichstabelle mit echter Suche, Zahlungsstatusfilter, Sortierauswahl und zugänglichen Zahlungs-/Belegdetails ersetzt. Bestätigte Zuordnungssumme zusätzlich serverseitig ausgegeben: 1280 / 500 / 0 EUR, offene Beträge weiterhin 0 / 460 / 450 EUR, gesamt 910 EUR. Unbestätigte Zahlung weiterhin ohne Saldenwirkung. Importvorschau als Arbeitsablauf mit kompakter Tabelle, Fehler-/Warnfilter, getrennten Status, adressierten Meldungen, Originalwerten und einklappbarem Hash/Vollbericht gestaltet. Kein neuer Import-/Buchungsweg.

Bestehende DESIGN.md erweitert (Alternativen, Tokens, Typografie, Komponenten, Sprache, Regionalisierung, Status, responsive Verhalten, Zustände und messbare Gates) und in beiden geltenden Instruktionsdateien verankert. Projektlokale Design-/Engineering-Playbooks lagen vor. Ein Agent, gezielte Architektur-/Coding-/Testing-/Security-/Evidence-/Dokumentationsmethoden; keine erneute Wettbewerbsrecherche. Offizielle WCAG-2.2- und OpenAI-Berechtigungsdokumentation gezielt eingesehen. Keine fremden Marken, Assets oder Webfonts übernommen.

### Tatsächliche Ausführung

- Ausgangsbasis neu ausgeführt: 113 Backend-Fälle und vier Frontendtests bestanden.
- Test-first: neue Suche-/Statusfälle scheiterten zunächst am fehlenden Darstellungsmodul; Backend-Prüfung der neuen Zuordnungssumme scheiterte zunächst am fehlenden Anzeigefeld. Danach implementiert und grün. Der Backend-Testfall wurde erweitert, seine Anzahl blieb gleich.
- `dotnet test src/backend/Betriebsplattform.slnx --configuration Release --no-build --no-restore --verbosity minimal`: **113 bestanden, 0 fehlgeschlagen/übersprungen, Exit 0**. Zuvor auch mit Build ausgeführt.
- `dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release --verbosity minimal`: **Exit 0, 0 Warnungen/Fehler**.
- `npm.cmd test` im Frontendordner: **11 bestanden, 0 fehlgeschlagen/übersprungen, Exit 0**. Vertrags-/Netzwerk-/Negativfälle, Serverwerte, Detailbedienung, Filter/Sortierung, Zurücksetzen, Navigation/Sprunglink, HTML-Textescaping, Fehler/Wiederholung, Leer-/Ladezustand, getrennte Status, ISO-Datum/Regionen und CSS-Kontrastpaare. Seitliches Tastaturscrollen zusätzlich als DOM-Regression geprüft.
- `npm.cmd run build` im Frontendordner: **Exit 0**, TypeScript strict und Vite; 31 Module, ca. 213,6 kB JS / 10,4 kB CSS vor Gzip.
- Repositoryprüfungen: **7 bestanden, Exit 0**; öffentliche Inventarliste um `darstellung.ts` ergänzt. Git- und zusätzliche Inhalts-/Whitespaceprüfung der unversionierten Produktquellen durchgeführt. Keine Pakete installiert oder heruntergeladen.

### Browser und behobene Darstellungsprobleme

Produktionsbuild in echtem Edge auf Loopback bedient, beide Ansichten bei 1440/768/375 × 1000 CSS-px geprüft und lokale JPEGs gesichert. Seitenbreite überschreitet den Viewport nicht. Bei 768 px breite Datentabellen im eigenen 1020-px-Bereich; bei 375 px alle fachlichen Werte als beschriftete Zeilen. Die Screenshots sind lokale synthetische Prüfartefakte, keine Veröffentlichung und keine physische Smartphone-Prüfung.

Suche ohne Treffer samt Zurücksetzen, Teilzahlungsfilter, Partnersortierung, aufklappbare Zahlung mit 300/200 EUR sowie unbestätigte Zahlung mit 0 EUR Zuordnung tatsächlich geprüft. Importfälle fehlerhafter Betrag, Dublette (zwei gesperrte Zeilen) und Vorschau ohne Zeilenfehler bedient; Originalwert -5 an ISD-Testdaten!G4 und unverändertes ISD erreichbar, Hash/Vollbericht per Tastatur geöffnet. Backend gezielt beendet: verständlicher Fehler; nach Neustart „Erneut versuchen“ erfolgreich, offener Gesamtbetrag wieder 910 EUR.

Tastatur: Sprunglink setzt Fokus auf Inhalt und bewahrt Importansicht; Enter/Leertaste bedienen Rechnungsdetails, Tab bewegt Suche → Zahlungsstatus, Fokusumriss 3 px sichtbar. Seitliches Tablet-Tastaturscrollen war zunächst nicht wirksam beobachtbar; expliziter Links-/Rechts-Handler bei Fokus des Scrollbereichs ergänzt und mit DOM-Regression sowie im Browser auf `scrollLeft=300` gegen 711 px Bereich/1020 px Tabelle bestätigt. Keine Tastenumleitung aus inneren Controls.

Tabletüberschriften wurden zunächst störend zerlegt; eigener Tabellen-Scrollbereich korrigiert dies. Mobile CSS-Regeln erfassten anfangs versehentlich verschachtelte Quelltabellen: im Browser `display:block` reproduziert, Selektoren auf direkte Kinder eingegrenzt, nach Neubuild `display:table-row` und unverbreiterte Seite bestätigt. Ein eigener noch laufender Backendprozess sperrte vorübergehend eine Builddatei; gezielt beendet, frischer Build ohne Warnungen. Zwei npm-Aufrufe im falschen Arbeitsordner lieferten ENOENT; anschließend ausschließlich dokumentierte Frontendbefehle verwendet. Das waren keine fehlgeschlagenen Fachtests.

Kontrasttest der 14 Tokenpaare: zwölf Text-/Flächenpaare ≥4,5:1, Eingabegrenze und Fokus gegen Weiß ≥3:1. Das prüft konkrete Tokens, nicht alle denkbaren Kombinationen oder eine vollständige WCAG-Konformität. Referenz: `https://www.w3.org/TR/WCAG22/`.

### Grenzen, Security und Abnahme

Begrenzter eigener Securityreview der geänderten Anzeigeverträge, JSX-Textausgabe, Suche/Filter, Herkunftsanzeige und bestehenden lokalen API-Grenzen; keine neue bestätigte Sicherheitslücke im geänderten Ausschnitt. Kein unabhängiger Audit oder vollständiger Schwachstellenscan. Bestehende Datei-/HTTP-Negativtests erneut bestanden. Keine neuen Netzaufrufe, externe Fonts, HTML-Injektion, schreibenden Endpunkte oder Kundendaten. Abhängigkeiten/Lockfile nicht geändert; kein neuer Online-Advisoryscan. Das ist keine Sicherheitsgarantie.

Offen: belastbarer 200%-Text-/Browserzoomtest (der Tastaturversuch ergab keine messbare Vergrößerung), Screenreader-/Mehrbrowseraudit, echte Geräte, größere Datenmengen und längere Bürobenutzung. CHF/regionale Geldformate nur vorbereitet, nicht fachlich aktiviert. Keine Fälligkeitsdaten, echte Belegdateien, Datenbank oder produktiver Import.

**Bereit zur persönlichen Sichtabnahme als lokaler synthetischer Prototyp**, mit den genannten offenen Prüfungen. Nächster Schritt ist diese Sichtabnahme, danach ein gesondert freizugebendes zusammenhängendes Paket zur expliziten Spalten-/Unternehmenszuordnung mit synthetischer Importvorschau und Quellbericht. Keine Commits, Pushes, produktiven Änderungen oder Library-Synchronisierung.

### Geänderte Dateien in diesem Sprint

Produkt: `src/frontend/DESIGN.md`, `src/frontend/src/App.tsx`, `src/frontend/src/style.css`, `src/frontend/src/api.ts`, neu `src/frontend/src/darstellung.ts`; Backend-Anzeigevertrag `src/backend/Betriebsplattform.Web/ApiModelle.cs` und `DemoDaten.cs`.

Tests: `src/frontend/tests/app.test.mjs`, `tests/backend/Betriebsplattform.Rechnungen.Tests/WebPrototypTests.cs`, `tests/repository/test_public_layout.py`. Öffentliche Dokumentation: `AGENTS.md`, `README.md`, `CONTRIBUTING.md`, `docs/public/ARCHITEKTUR.md` und dieser bestehende Nachweis. Lokale Instruktionen und operativer Plan separat aktualisiert, weiterhin ignoriert.

Abschlussprüfung des uncommitteten Bestands auf Basis `885397b`: alle 49 öffentlichen Inventardateien zusätzlich per `git diff --no-index --check` gegen eine leere Datei geprüft, keine Whitespacebefunde; reguläres `git diff --check` Exit 0. Index leer. Bestehende unversionierte MVP-Quellen erhalten, keine Freigabe zur Veröffentlichung abgeleitet. Die Ignore-Grenzen wurden durch die sieben Repositorytests geprüft; Screenshots bleiben im privaten Bereich.

## Git-Sicherung und visueller Feinschliff – 17.10.2026

Begrenzte Weiterarbeit nach Screenshot-Feedback: Markenkopf dunkelpetrol, aktive Navigation als klarer Petrolbereich, stärkere neutrale Flächenabstufung, Kennzahlen mit Akzentkante, kontrastreichere fette Tabellenköpfe, wechselnde Zeilenflächen/Trennlinien, farbig konturierte Status mit Text und kräftigerem Symbol. Tabellen, Zahlungsdetails, Geldberechnung und Importherkunft unverändert. Schrittfolge bewusst als nicht interaktive Textorientierung dargestellt, damit keine nicht vorhandene Importfreigabe suggeriert wird. Einheitliche Begriffe: Navigation „Importvorschau“, Seite „Excel-Importvorschau“, Tätigkeit „Datensätze prüfen“.

Geändert: `src/frontend/src/style.css`, `src/frontend/src/App.tsx` (nur UI-Texte), `src/frontend/tests/app.test.mjs`, `src/frontend/DESIGN.md` und dieser bestehende Nachweis. Operativer Plan und Prüfartefakte bleiben lokal. Keine zusätzlichen Pakete, Endpunkte oder Fachfunktionen.

### Ausgeführte Prüfungen

- Ausgangsstand und Endstand: **113 Backend-Fälle**, **elf Frontendtests**, **sieben Repositorytests** bestanden, keine übersprungenen Fälle. Backend mit `dotnet test src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`; Frontend `npm.cmd test` im Frontendordner; Repository `python -B -m unittest discover -s tests/repository -v` mit vorhandenem Git-Pfad. Alle Exit 0. Wording-Prüfung zuerst rot (Navigation noch „Daten prüfen“), nach UI-Korrektur grün.
- Releasebuild `dotnet build src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`: Exit 0, null Warnungen/Fehler. Frontend `npm.cmd run build`: Exit 0, TypeScript strict/Vite; ca. 213,7 kB JS / 11,7 kB CSS vor Gzip.
- Kontrasttest um Header, Badge, Tabellenkopf, Details und Wechselzeilen erweitert: 21 Paare geprüft, davon 19 Textpaare ≥4,5:1, zwei Eingabe-/Fokuspaare ≥3:1. Kein vollständiges Accessibility-Audit.
- Echter Edge-Browser, beide Ansichten bei 1440/768/375 CSS-px und 100 % Zoom; sechs neue lokale Screenshots. Keine horizontale Seitenüberbreite. Zahlungsdetails mit 300/200 EUR, unveränderter offener Gesamtbetrag 910 EUR, ISD-Testdaten!G4 mit Originalbetrag -5 und fokussierter innerer Quellbereich mit scrollLeft=300 tatsächlich geprüft. Sprunglink setzt Fokus auf Inhalt und bewahrt Importansicht, sichtbarer 3-px-Fokus. Vorher-/Nachher-Screenshots verglichen: stärkerer Kopf, eindeutiger aktive Navigation und erkennbare Tabellen-/Statusabgrenzung; persönliche Sichtabnahme steht aus.
- Browserzoom anfänglich messbar bei 80 %, für definierte Größen auf 100 % gesetzt. Versuche für 200 % über mehrere Plus-Tastenkombinationen ohne messbare Wirkung; 200%-Prüfung weiterhin ausdrücklich offen. Screenreader, weitere Browser, physische Geräte und Langzeitnutzung nicht geprüft. Viewportoverride zum Abschluss zurückgesetzt. Browserzoom blieb abschließend messbar bei 100 %; die automatische Rückkehr auf anfängliche 80 % gelang ebenfalls nicht.

### Git-Prüfung und Freigabegrenze

Gesamtes Git-Inventar erfasst: sieben verfolgte und 42 unverfolgte vorgesehene öffentliche Dateien. 49-Dateien-Positivliste einschließlich Backend, Frontend, Tests und öffentlicher Dokumentation bestätigt. Ignorierte lokale/erzeugte Bereiche inventarisiert, kein ignorierter Pfad bereits verfolgt. Whitelist unverändert. Begrenzter Inhaltscheck auf Schlüssel-/Tokenmuster, hart codierte Zugangsdaten, Zugangsdaten in URLs und persönliche absolute Pfade ohne Treffer; synthetische Daten in Produkt und Tests geprüft. Das ist kein vollständiger Secret- oder Lizenz-Audit. Keine neue Lizenz ausgewählt; Veröffentlichung bleibt offen. Git-Autoridentität lokal geprüft, nicht in öffentliche Produktdokumentation übernommen. Kein Remote konfiguriert, angelegt oder kontaktiert.

Gezieltes `git add --` mit den 49 einzeln benannten geprüften Dateien versucht: Exit 128, `index.lock: Permission denied`. Die Sitzung darf den geschützten Git-Bereich nicht schreiben. Keine Eskalation, keine Änderung von Sandbox oder Berechtigungen. Keine Änderungen sind gestaged, HEAD `885397b`, kein neuer Commit. Exakte lokale Staging-/Diff-Befehle mit fester Dateiliste, endgültiges SHA-256-Inventar und Commitvorschlag privat vorbereitet. Kein blindes `git add -A`, kein Force-Add, kein Push. Vor tatsächlichem Baseline-Commit ist die ausdrückliche einmalige Nutzerfreigabe noch erforderlich.

Abschluss: technisch geprüfter lokaler Stand zur Sichtabnahme, Git-Sicherung wegen der beschriebenen Schreibgrenze noch nicht als Commit vollzogen. Nächste Aktion ist lokales gezieltes Staging und Prüfung des vollständigen staged Diff, anschließend ausdrückliche Commitfreigabe. Keine neuen Module oder Datenbankarbeiten begonnen.
