# MVP-01 bis MVP-03 – Entwicklungsnachweise

## Authentifizierter Browser-Systemtest – 20.10.2026

**BESTANDEN:** Echter Edge-Browser → vorhandenes vertrauenswürdiges HTTPS-Zertifikat → echte Anmeldung/CSRF/Ressourcenrechte → Produkt-API → neu isoliertes PostgreSQL 18 → Speichern → Browserneuladen → kontrollierter Kestrel-Hostneustart → erneute Anzeige. Fiktiver Vorgang: Elbequartier Objektservice, Nordlicht Haustechnik/Mara Falk, Kontorhaus Südhafen, Wartungsbeleg RE-2026-1042 über 1.249,50 EUR, Aufgabe für Anna Berg mit Prüfung bis 30.10.2026. Dokument und Folgeaufgabe bleiben in derselben Akte. Keine Rechnung gebucht oder Zahlung ausgelöst.

Drei echte Testidentitäten, 26 dokumentierte HTTP-Prüffälle einschließlich parallelem 200/409-Konflikt, Objekt-/Download-/Historienrechte, Entzug/Widerruf, CSRF/Origin und unveränderter Malware-Sperre bestanden. Zwei echte Browsertabs bestätigen den Versionskonflikt mit erhaltenem Entwurf. Ein Zähler-Vertragsfehler der Arbeitsplatzkarte wurde mit rot/grün korrigiert. Gezielter begleiteter Systemtest (1), Backendregression (6), Frontendregression (114), Repositorytests (7) sowie TypeScript/Vite und Web-Debug/Release bestanden. Keine unnötige vollständige Backend-Gesamtregression.

Der Browserablauf verwendet die echte Anwendung, wird jedoch durch den Agenten bedient und ist keine unbeaufsichtigte CI-Browserautomation oder menschliche Abnahme. Hostneustart bedeutet Stop/Dispose/neue WebApplication innerhalb desselben Testprozesses. **NICHT GETESTET:** separater Prozess-/Windowsneustart, natürlicher Sitzungsablauf, MFA im echten Browser (keine Pflicht für neue Testkonten), physische Geräte/LAN und menschliche Fach-/UX-Abnahme. **BLOCKIERT:** beliebige produktive Uploads ohne Malware-Adapter. Der separate HTTPS-Testhost wurde beendet; die bisherige flüchtige Vorschau auf 5180 bleibt startfähig. Keine Hauptdatenbank, Trust-Konfiguration, Echtdaten oder bestehenden Sicherungen verändert.

## Gesamtregression und Posteingang – 20.10.2026

**BESTANDEN:** Debug-/Release-Build mit TypeScript strict und Vite, 315 Backendtests, 113 Frontendtests einschließlich echtem HTTP-Vorschauablauf sowie sieben Repositorytests. Drei Backendfälle wurden bewusst übersprungen: zwei ältere Entwicklungsdatenbank-Gates und ein nur indirekt verwendeter Crash-Helfer. Vier neu isolierte PostgreSQL-Gates prüfen Migrationen, Erstzugang/Wiederherstellung, HTTPS/Autorisierung, MFA-/Aufgabenrennen, Posteingang/Folgeaufgabe und Backup/Restore einschließlich Dokumentintegrität und Rechte nach Wiederherstellung. Keine Hauptdatenbankmigration oder Produktivdatenverarbeitung.

Behoben: falscher Posteingangsvertrag im älteren Demobetrieb, fehlender Kontakt-Detailendpunkt bei Absenderwahl im Vorschauadapter, Suchabbruch nach 200 Kandidaten bei Kontakten/Akten/Rechnungen und HTTP 500 bei fehlenden Folgeaufgabendaten. Negative und positive Vertragsfälle, echte HTTP-Validierung und 225er-Suchregression auf SQLite und PostgreSQL bestanden. Veraltete Migrations-/Rate-Limit- und UI-Fixtures korrigiert; keine Sicherheitslimits reduziert. Testnachweise sind an lokale Quellhashes einschließlich unversionierter Dateien gebunden.

**BESTANDEN:** Edge-Bedienung des korrigierten synthetischen Posteingangs mit Absender/Akte, bestätigter Folgeaufgabe und Rückweg. Escape/Fokusrückgabe geprüft; keine erfassten Browserwarnungen/-fehler in dieser Strecke. Dies ist keine menschliche UX-Abnahme oder PostgreSQL-Browserkette. Start mit tatsächlichem VS-Profil und bytegleiche HTTP-/dist-/wwwroot-Assets nachgewiesen; Visual-Studio-Play selbst nicht bedient.

**NICHT GETESTET:** Durchgehender Browser–HTTPS–PostgreSQL-Ablauf, echte Geräte/LAN/Linux, Screenreader/200-%-Zoom, umfangreicher Last-/Lecktest und menschliche Pilotabnahme. **BLOCKIERT:** Beliebige Dokumentannahme im authentifizierten Pilot ohne produktiven Malware-Adapter. **NICHT IMPLEMENTIERT:** Dauerhafter strukturierter Excelimport, vollständige UI-/API-Finanzerfassung und externe Mail-/Kalender-/OCR-Adapter. Aktuelle npm-/NuGet-Abfragen melden keine bekannten Advisories; keine vollständige Lizenz-/Sicherheitsfreigabe. **Der Betrieb mit echten Kundendaten bleibt gesperrt.**

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

## Zwei offene Designrichtungen – A/B-Vorschau

Ausgangsstand: Nutzer hat die Baseline lokal als `f3e9e29` gesichert; Arbeitskopie vor diesem Auftrag sauber. Die bisherige Petrol-/Graugestaltung ist ausdrücklich **nicht abgenommen**. Keine finale Designwahl vorgenommen. A als erste Vorschau ist nur eine Reihenfolge.

Implementiert: A „Modern Blue“ mit hellem Markenband, sachlichem Blau und horizontaler Navigation; B „Fresh Blue & Teal“ mit Blau/Teal, weicheren Komponenten und linker Desktop-Navigation. Gemeinsame neue Typografiehierarchie (16 px Basis, 15 px Tabelleninhalte, 14 px Labels/Status), neue eigene SVG-Geometrien für Marke, Navigation, Kennzahlen, Suche, Details, Quellen und Status. Weiße Arbeitsflächen, klarere Felder und ruhig abgestufte Details. Kein Icon-/Fontpaket, externer Font oder fremdes Asset. Bestehende Tabellen, Filter, Sortierung und Herkunftsnachweise bleiben vorhanden.

Der obere Umschalter verändert nur Darstellung und URL-Parameter `design=a|b`. Kein Remount der Fachansichten, keine neue Datenanfrage beim Umschalten, kein Cookie/Local Storage und keine zweite Fachberechnung. Filter, geöffnete Details und geladenes Importszenario bleiben erhalten. Direktlinks mit `?design=a#rechnungen` beziehungsweise `?design=b#rechnungen`, für Import `#import`; gleicher Zustand und gleiche Fenstergröße für fairen Vergleich. Beide Vorschläge sind direkt in der Oberfläche als nicht abgenommen markiert.

### Tatsächliche Prüfungen

- Test-first: zusätzlicher A/B-Zustandstest zunächst rot wegen fehlender Vorschau; nach Implementierung grün.
- `npm.cmd test` im Frontendordner: **13 bestanden, 0 fehlgeschlagen/übersprungen, Exit 0**. Bisherige API-, Fehler-/Lade-/Leer-, Filter-/Sortier-, Quellen-/Textescaping- und Sprunglinkfälle weiterhin grün. Neue Fälle prüfen erhaltene Filter/Details/Serversalden, null zusätzliche Fetch-Aufrufe beim Designwechsel, SVG-Status, URL-Auswahl und Direktlink zu B samt Importansicht sowie kontrollierten Rückfall unbekannter Parameter auf A.
- Kontrasttest getrennt für beide Paletten: je 23 Paare, davon 21 Textpaare ≥4,5:1 und zwei Eingabe-/Fokuspaare ≥3:1. Auch B-Navigation/Teal-Kennzahl über semantische Tokens erfasst. Kein vollständiges Accessibility-Audit.
- `npm.cmd run build`: **Exit 0**, TypeScript strict und Vite; 32 Module, ca. 216,9 kB JS / 14,9 kB CSS vor Gzip.
- Repositorysuite: **7 bestanden, Exit 0**; öffentliche Dateiliste um den geprüften Icon-Baustein ergänzt. Keine Erweiterung der Ignore-Whitelist nötig, private Screenshots und Plan bleiben ignoriert. Regulärer Diff-Check erfolgreich.
- Echter Edge mit Produktionsbuild: A und B, Rechnungsübersicht und Importvorschau jeweils bei 1440/768/375 × 1000 CSS-px geprüft und zwölf lokale JPEGs gesichert. Seitenbreiten jeweils 1425/753/360 px, keine horizontale Seitenüberbreite. Die B-Seitenleiste wird unter 1001 px zur horizontalen Navigation. Breite Tabellen haben einen eigenen Scrollbereich; auf Mobilansichten beschriftete Datensätze mit denselben Feldern.
- Im Browser per Enter Importzeile geöffnet und zwischen Designs gewechselt: Originalquelle `ISD-Testdaten!G4` blieb geöffnet/lesbar, Fokusumriss 3 px sichtbar. SVGs bleiben dekorativ und nicht separat fokussierbar. Echte Geräte, Screenreader, weitere Browser und belastbare 200%-Vergrößerung wurden in diesem Paket nicht geprüft. Langzeitverträglichkeit und endgültige Schriftwirkung bleiben menschliche Sichtprüfung.

### Änderungen und Grenzen

Produkt: `src/frontend/src/App.tsx`, `src/frontend/src/style.css`, neu `src/frontend/src/Icon.tsx`. Tests: `src/frontend/tests/app.test.mjs` und `tests/repository/test_public_layout.py`. Dokumentation: `src/frontend/DESIGN.md`, `README.md`, `docs/public/ARCHITEKTUR.md`, dieser bestehende Nachweis und der lokale operative Plan. DESIGN.md trennt nun verbindliche Funktions-/Usabilitygrenzen von den ausdrücklich offenen Designvorschlägen; frühere Petrolwahl ist keine aktuelle Vorgabe.

Diff gegen Baseline bestätigt: Backend, API-Verträge, `api.ts`, `darstellung.ts`, Paketmanifeste/Lockfile und synthetische Fachdaten unverändert. Backendtests/-build wurden deshalb in diesem reinen Frontendpaket nicht erneut ausgeführt. Keine Downloads, Installationen, schreibenden Finanzaktionen, Commits, Pushes oder Veröffentlichungen. Begrenzte eigene Prüfung der neuen URL-Auswahl, JSX-/SVG-Ausgabe und fehlender externer Assets; keine Sicherheitsgarantie oder aktuelle Advisoryprüfung.

Nächster Schritt ausschließlich menschliche Sichtabnahme: gleiche Rechnungs-/Importansicht in beiden Richtungen vergleichen, Typografie/Statussymbole/Abstände beurteilen und Feedback festhalten. Keine Variante allein aufgrund bestandener Tests als angenommen erklären. Keine neuen Fachmodule automatisch beginnen.


## Design-Sprint 3 – C · Premium Hybrid Blue (Vorschlag)

Nach menschlicher Sichtung der A/B-Screenshots wurde gezielt ein Hybrid beauftragt: B-Seitennavigation und helle Wirkung, A-Blau und stärkere Hierarchie. **Keine Gestaltung ist endgültig abgenommen.** Bestehende A/B-Arbeit auf Basis `f3e9e29` erhalten; keine Änderungen zurückgesetzt.

Implementiert: C im gemeinsamen Designumschalter und als `?design=c#rechnungen` / `?design=c#import`. Helle neutrale Arbeitsflächen, kräftiges Primärblau und begrenzte Teal-Akzente; eigenständiges SVG-Bogen-/B-Monogramm, ergänzte Filter-/Zahlungs-/Beleg-/Navigationssymbole und konsistente Text-/Iconstatus. Desktop-Sidebar 208 px, per Enter auf 76 px einklappbar, benannte Links auch kompakt; unter 1001 px horizontale Textnavigation. Hauptinhalt ohne Breitenkappung. Tabellen/Labels 15 px, Partnerrollen 14 px, Basis/Felder 16 px, Kennzahlen 32 px (mobil 22 px, Geldwert ohne Zeilenumbruch). Keine zusätzlichen Module oder dekorativen Effekte.

C zeigt „Offen“ korallorange, unabhängig von Fehler/Überfälligkeit. „Keine Zeilenfehler“ bleibt neutral; „Zur Prüfung“ bleibt bernstein. Keine erfundenen Fälligkeiten. Der Hinweis bei Teilzahlung zeigt ausschließlich vorhandene Servertexte für bestätigte Zuordnung und Rechnungsbetrag, keine Prozentberechnung im Browser. Zwei mobile Darstellungsfehler bei der Sichtprüfung behoben: getrennter Euro-Umbruch in Kennzahlen und Teilzahlungshinweis in falscher Rasterspalte. Gemeinsame SVG-Verbesserungen gelten auch für A/B, deren Layouts und Paletten bleiben vergleichbar.

### Tatsächlich ausgeführte Prüfungen

- Test-first: zwei zusätzliche C-Regressionsfälle zunächst rot (fehlender C-Direktlink bzw. offene Statusklasse), nach Umsetzung grün. `npm.cmd test`: **15 Tests bestanden**, keine übersprungenen Fälle. Neue Fälle prüfen Direktlink C, Einklappen, zugängliche Linknamen, erhaltene Details/Filter/Serverwerte beim A/C-Wechsel sowie die Trennung offener Zahlungsdarstellung von Importfehlern. Bestehende Lade-/Leer-/Fehler-/Erneut-versuchen-/Escaping-/Filter-/Sortier-/Quellenfälle weiterhin grün.
- `npm.cmd run build`: **Exit 0**, TypeScript strict und Vite, 32 Module; rund 218,92 kB JS / 21,32 kB CSS vor Gzip. Keine Pakete oder Lockfiles geändert.
- `python -B -m unittest discover -s tests/repository -v` mit vorhandenem Git-Pfad: **7 bestanden**, einschließlich öffentlichem Dateiinventar, privaten Grenzen, Markdownlinks und begrenzter Suche nach offensichtlichen Geheimnissen. Keine neue öffentliche Datei zusätzlich zum bereits vorhandenen A/B-Icon-Baustein; Inventar weiterhin 50 Dateien.
- Kontrastprüfung aller drei Paletten bestanden: A/B je 23 Paare, C 24 einschließlich Korallorange; Textpaare ≥4,5:1, Eingabe-/Fokuspaare ≥3:1. Nur konkrete semantische Kombinationen geprüft, keine umfassende WCAG-Konformitätsbehauptung.
- Echter Edge mit gebautem Frontend: Rechnungen und Import bei **1440/1760/768/375 × 1000 CSS-px**. Seitenbreiten 1425/1745/753/360 px, keine horizontale Seitenüberbreite. Arbeitsbereich bei 1760 px 1537 px breit, eingeklappt 1669 px. Eigene Scrollbereiche bleiben für Tablet-/Quelltabellen erhalten. Tabellen-/Label-/Rollenwerte im DOM mit 15/15/14 px bestätigt. Mobile Kennzahlen nach Korrektur 22 px, nicht umbrechende Geldwerte.
- Im Browser tatsächlich bedient: Suche nach Rechnung 002, offener Zahlungsfilter, Ausstellersortierung, Teilzahlungsdetails mit 300/200 EUR und 460 EUR offen, Designwechsel mit erhaltener Suche/offenem Detail, Sidebar per Enter mit 3-px-Fokus und erhaltenen Linknamen. Importfehlerfilter zeigt eine von drei Zeilen; alle drei Beispielszenarien geprüft (gültig weiterhin „Zur Prüfung“, Dublette mit zwei Fehlerzeilen), Herkunft `ISD-Testdaten!G4` mit Originalwert -5 und Datentyp n erreichbar, SHA-256 mit 64 Zeichen und Vollbericht per Enter geöffnet.
- 13 echte lokale Screenshots ausschließlich unter `private/pruefungen/design-c/`: C beider Ansichten bei vier Breiten; zusätzlich kompakte Navigation, Zahlungsdetails und mobile Quellen; A/B-Rechnungen als Vergleich bei derselben 1440-px-Fenstergröße. Browsererweiterungs-Overlays können auf den Aufnahmen sichtbar sein; sie sind kein Produktbestandteil.

### Review, Dateien und Grenzen

In diesem C-Sprint geändert: `src/frontend/src/App.tsx`, `src/frontend/src/style.css`, `src/frontend/src/Icon.tsx`, `src/frontend/tests/app.test.mjs`, `src/frontend/DESIGN.md`, `README.md`, `docs/public/ARCHITEKTUR.md` und dieser Nachweis; zusätzlich vorhandener privater Entwicklungsplan. Die Repository-Inventaränderung stammt bereits aus dem erhaltenen A/B-Auftrag.

Diff gegen Baseline bestätigt unveränderte Backend-/Testquellen, Anzeige-/Importverträge, `api.ts`, `darstellung.ts`, Paketdateien und synthetische Fachdaten. Deshalb Backendtests/-build in diesem reinen UI-Sprint nicht erneut ausgeführt; frühere Zahlen sind historische Nachweise. Eigener begrenzter Review: nur URL-Darstellungsauswahl und flüchtiger Sidebarzustand, Textausgabe durch React, lokale Inline-SVGs ohne HTML-Injektion oder externe Assets; keine neuen Netzaufrufe, Schreibendpunkte oder Kundendaten. Kein unabhängiger Securityaudit/Online-Advisoryscan.

`git diff --check` ohne Befund; Index unverändert, kein Commit/Push/Remote-/Veröffentlichungsvorgang. Private Regeln, Screenshots, Plan und erzeugte Artefakte bleiben ausgeschlossen. Keine Sandbox-/Berechtigungsänderung oder Installation. Keine Library-Synchronisierung.

Offen: belastbarer **200%-Zoomtest**, Screenreader, weitere Browser/echte Geräte, alle Zustandskombinationen und längere tägliche Büronutzung. Kein vollständiges Accessibility-Audit, keine endgültige Markenprüfung und keine Designabnahme. Aktueller nächster Schritt ist ausschließlich die persönliche Sichtabnahme von C im direkten A/B/C-Vergleich; keine Fach-/Datenbankarbeiten automatisch beginnen.


Abschließende Gegenprüfung: 15 Frontendtests erneut grün, sieben Repositorytests nach den Dokumentationsänderungen grün; regulärer Whitespacecheck und zusätzlicher Check der noch unverfolgten Icon.tsx ohne Befund. Ein wiederholter Build im Sandboxkontext scheiterte am Zugriff des Konfigurationsladers auf ein übergeordnetes Verzeichnis. Derselbe unveränderte `npm.cmd run build` lief anschließend mit der bereits bestehenden eng begrenzten Build-Präfixfreigabe erfolgreich (Exit 0, identische Artefaktgrößen); keine Paketinstallation oder Änderung von Sicherheitsregeln. Sprunglink in C setzt per Enter den Fokus auf `inhalt`, erhält die Importansicht und zeigt 3-px-Fokus. Temporäre Viewportvorgabe zurückgesetzt, C-Rechnungsansicht zur Sichtabnahme offengelassen. HEAD weiterhin `f3e9e29`, keine gestagten Änderungen.


## Design-Sprint 4 – Premium Business Blue (18.10.2026)

**Auftrag und Entscheidung:** Bestehenden C-Kandidaten gezielt verfeinert, keine vierte Variante und keine endgültige Designfreigabe. A/B und sämtliche vorhandene uncommittete Arbeit auf Basis `f3e9e29` erhalten. Verwendet: lokale AGENTS-Regeln, Architektur-/Coding-Harness-/Testing-/Security-/Dokumentationspflege-Skills, UI-Playbook und Checklist §§26/27. Single-Agent, deterministische Tests und eigener begrenzter Review; menschliche Sichtabnahme bleibt das Abschlussgate. Die vom Nutzer genannten vier Referenzseiten wurden gezielt lesend aufgerufen; methodische Übertragung und Nicht-Übernahmen stehen in DESIGN.md. Keine neuen Frameworks, Assets, Pakete oder Fonts.

### Tatsächlich umgesetzt

C heißt nun „Premium Business Blue“. Navy für Haupttext, ein kräftiges Blau für Marke/Aktionen/Orientierung, neutrale helle Seite und weiße Karten. Teal-Kanten an Kennzahlen, Zahlungs- und Prüfbereichen entfernt; kompatible Zweitfarbrollen in C sind Aliasse auf die einzige blaue Palette. Statusfarben ausschließlich fachlich: Bezahlt grün mit Check, Teilzahlung warmes Amber mit Teilkreis, Offen Slate mit offenem Kreis, Zur Prüfung Gold mit eigenem Dreieck, Fehler rot mit Kreuz. Keine Fälligkeit oder Überfälligkeit erfunden. Teilkreis ersetzt das Münz-/Checkmotiv; vorhandenes Markenmonogramm nach Sichtprüfung beibehalten, alle Linien in gemeinsamer SVG-Sprache. Gemeinsamer Teilkreis gilt auch in A/B, deren Paletten und Layouts bleiben erhalten.

Kennzahlen klar konturiert, nur offener Gesamtbetrag als blaue Hauptkennzahl mit weißer Schrift. Metadatenlabels und Tabellenköpfe konsequenter gewichtet, Teilzahlungsnachweis von 13 auf 14 px vergrößert. Basis 16 px, Tabellen/Labels 15 px, Partnerrollen/Status 14 px. Ruhige blau-graue Tabellen-/Detailflächen und minimale 1-px-Tiefenwirkung, keine Verläufe oder Animationen. Bewährte 208-/76-px-Sidebar, breite Arbeitsfläche, Such-/Filter-/Sortierfunktionen, Beleg-/Zahlungsdetails und Importquellen erhalten.

Bestätigte kleine CSS-Korrektur: Ein doppelter Grundregelblock im B-Breakpoint überschrieb dessen horizontale Tablet-Navigation. Vor Korrektur am gebauten Stand bei 768 px `flex-direction: column`, nach Entfernung `row` im Browser nachgewiesen. Keine neue Navigationsfunktion oder Fachlogik.

### Ausgeführte Prüfungen und Ergebnisse

- `npm.cmd test` im Frontendordner: **15 bestanden, null fehlgeschlagen/übersprungen, Exit 0**. C-Test zunächst rot wegen des erwarteten neuen Vorschautitels, nach Umsetzung grün. Bestehenden Zustandserhalt um getrennte Teilzahlungs-/Prüf-SVGs mit dekorativem ARIA ergänzt. API-/Fehler-/Lade-/Leer-/Filter-/Sortier-/Escaping-/Quellenfälle weiterhin grün.
- Kontrasttest mit Auflösung semantischer Token-Aliasse einschließlich fehlender/zyklischer Referenzen: A/B je 23 Paare, C **26 Paare**. C: 24 Textpaare ≥4,5:1 und zwei Eingabe-/Fokuspaare ≥3:1; neue Informationsrolle und Weiß auf blauem Kennzahlhintergrund eingeschlossen. Nicht alle möglichen Zustandskombinationen oder vollständige WCAG-Konformität geprüft.
- `npm.cmd run build`: **Exit 0**, TypeScript strict und Vite, 32 Module; 218,97 kB JS / 21,86 kB CSS vor Gzip. Build ohne neuen Eskalationsantrag oder Installation ausgeführt.
- `python -B -m unittest discover -s tests/repository -v` mit vorhandenem Git-Pfad: **7 bestanden, Exit 0**. Neue externe Markdownlinks im Designtext wurden zunächst vom vorhandenen lokalen Link-Gate zurückgewiesen; Quellenadressen anschließend als dokumentierte URL-Literale geführt. Die Repositoryregel wurde nicht gelockert. Öffentliche Dateiliste weiterhin 50 Dateien; keine neue Produktdatei in diesem Sprint.
- Echter Edge mit gebautem Frontend: Rechnungen und Import bei **1440/1760/768/375 × 1000 CSS-px**, Seitenbreiten jeweils 1425/1745/753/360 px, kein horizontaler Seitenüberlauf. Hauptinhalt bei 1440 px 1217 px, bei 1760 px 1537 px breit. Tabelle/Labels tatsächlich 15 px, Partnerrollen und Teilzahlungsnachweis 14 px bei allen vier Breiten. Mobile Geldwerte bleiben ungeteilt, Quellen im inneren Scrollbereich.
- Tatsächlich im Browser bedient: Suche nach Rechnung 002, offener Zahlungsfilter (eine von drei Rechnungen), Ausstellersortierung (001/003/002), Details mit bestätigten 300/200 EUR, Designwechsel A/C mit erhaltener Suche und geöffneten Details, Sidebar per Enter samt benannten Iconlinks und sichtbarem 3-px-Fokus. Unbestätigte 450-EUR-Zahlung bei Rechnung 003 bleibt „Zur Prüfung“, Zuordnung 0,00 EUR; kein bezahlter oder überfälliger Zustand erfunden.
- Importfehlerfilter zeigt eine von drei Zeilen; alle drei Beispielszenarien bedient: gültiger Datensatz weiterhin ohne Importfreigabe „Zur Prüfung“, Dublette mit zwei Fehlerzeilen, fehlerhafter Betrag adressiert. `ISD-Testdaten!G4` mit Originalwert -5 erreichbar, SHA-256 mit 64 Zeichen und Vollbericht per Enter geöffnet. Tablet-Tabellenbereich per Rechtspfeil auf scrollLeft=300 bewegt, Fokus 3 px. Sprunglink setzt Fokus auf Inhalt und erhält die aktuelle Importansicht.
- 12 echte lokale Screenshots unter `private/pruefungen/design-sprint-4/`: vorherige C-Rechnungsansicht bei 1440 px; neue C-Rechnungs-/Importansichten bei vier Breiten; Zahlungsdetails 1440 px, kompakte Sidebar 1440 px und mobile Originalquellen. Drei zentrale Aufnahmen nach finalem Build erneuert. Sichtbare Browsererweiterungs-Overlays gehören nicht zum Produkt.

### Grenzen, Dateien und Abnahmepunkt

Aktuelle Änderungen: `src/frontend/src/style.css`, `src/frontend/src/App.tsx` (Vorschautitel), `src/frontend/src/Icon.tsx` (Teilkreis), `src/frontend/tests/app.test.mjs`, `src/frontend/DESIGN.md`, README, `docs/public/ARCHITEKTUR.md`, dieser bestehende Nachweis und vorhandener privater Entwicklungsplan. Keine zusätzlichen Dokumentationsstrukturen. Die bereits unversionierte Icon-Datei und Repository-Inventaränderung stammen aus dem erhaltenen A/B-Bestand.

Eigener begrenzter Diff-/Securityreview: keine neuen Datenflüsse/Requests, Fremdassets, HTML-Injektion, Schreibendpunkte oder Berechtigungen; SVGs bleiben dekorativ, Originalwerte React-Text. Diff gegen Baseline für Backend, Backendtests, `api.ts`, `darstellung.ts`, Paket-/Lockdateien und .gitignore leer. Finanzlogik, API-/Importverträge und synthetische Fachdaten unverändert. Backendtests/-build in diesem reinen UI-Sprint deshalb nicht erneut ausgeführt. Keine Aussage über aktuellen Online-Advisoryscan, vollständigen Securityaudit oder Produktionstauglichkeit.

**Offen:** 200%-Browserzoom wurde versucht, zeigte nach Rücksetzen der Viewportvorgabe aber keine messbare Änderung (DPR 1, innerWidth 1912 vor/nach Tastenkombination); kein 200%-Nachweis. Screenreader, andere Browser/physische Geräte, Langzeitnutzung und endgültige Marken-/Designabnahme offen. Kontrast-/DOM-/Browser-Spotchecks sind kein vollständiges Accessibility-Audit.

Bereit zur persönlichen Sichtabnahme als vorläufiges Designfundament. Einstieg `http://127.0.0.1:5180/?design=c#rechnungen`; Import über Navigation, A/B nur Vergleich. **Kein weiteres Designexperiment oder neues Fachpaket automatisch beginnen.** Keine Commits, Pushes, Veröffentlichung, Sicherheitsänderungen oder Library-Synchronisierung.


## Isolierte UI/UX-Neukonzeption – 18.10.2026

**Status:** Ein neuer isolierter Designprototyp, nicht in die bestehende Anwendung übernommen und nicht menschlich abgenommen. Vorhandene A/B/C-Arbeit auf Basis f3e9e29 erhalten. Nutzer hat die frühere Blaupflicht aufgehoben; Produktname, Platzhalterzeichen und Akzentfarbe bleiben vorläufig. Keine vierte parallele Themenvariante innerhalb des A/B/C-Umschalters.

### Konzept und tatsächliche Umsetzung

Graphit-Seitennavigation über volle Höhe, kleiner Kontextkopf und heller Off-White-Arbeitsraum mit einer Indigo-Aktionsfarbe. Eine zusammenhängende Kennzahlenzeile ersetzt die Kartenreihe. Suche und echte Statusreiter sind in das Register integriert. Separate Rechnungs-/Zahlungsarbeitsfläche mit Betragsübersicht, Beteiligten, Zahlungsfolge und Nachweisspalte ersetzt die eingebettete Detailzeile. Importprüfung trennt Prüfresümee, Datensätze und Originalzellinspektor. Damit ändern sich Informationsarchitektur, Proportionen und Arbeitswege, nicht nur Farben. Kein Startdashboard oder leeres Modul ergänzt.

Einstieg `http://127.0.0.1:5180/?vorschau=arbeitsraum#rechnungen`, Import mit `#import`. Nur der genaue Vorschauparameter lädt den neuen React-Baustein. Ohne ihn bleibt die bisherige Anwendung erreichbar, einschließlich C unter `?design=c#rechnungen`. Gemeinsamer API-Client, dieselben synthetischen Serverdaten, Suche/Sortierung und vier kleine vorhandene Helfer (Datenhook, Status, Meldungsliste, Tastaturscrollen) wiederverwendet; App.tsx dafür nur um benannte Exporte ergänzt. Keine zusätzliche Bibliothek oder Fachberechnung.

Neue Produktdateien: `src/frontend/src/Designprototyp.tsx` und `src/frontend/src/prototyp.css`. Geändert: Einstieg `main.tsx`, vier Exportdeklarationen in `App.tsx`, bestehende Frontendtests und öffentliches Repository-Inventar; DESIGN.md, README, Architektur, dieser Nachweis und vorhandener privater Plan. A/B/C-Styles und Icon-Geometrien im Auftrag nicht verändert. Kein neuer Dokumentationsbaum. Öffentliche Dateiliste jetzt 52 Dateien; privater Plan/Screenshots bleiben ausgeschlossen.

Gezielte Referenzsichtung: shadcn-Dashboardblöcke, Tabler, Linear und Origin UI (Weiterleitung zu coss ui); Tremor aus vorhandener Sichtung ergänzend berücksichtigt. Übertragene Prinzipien und Nicht-Übernahmen im vorhandenen Designsystem dokumentiert. Kein fremdes Dashboard, Logo, Code oder Font übernommen; keine Installation/Downloads von Paketen. Arbeitsname bleibt Betriebsplattform; das schlichte B-Zeichen ist ausdrücklich ein Platzhalter.

### Ausgeführte Prüfungen

- `npm.cmd test`: **18 Frontendtests bestanden, null fehlgeschlagen/übersprungen**. Bisherige 15 Fälle weiterhin grün, drei zusätzliche Fälle für den isolierten Prototyp: Servertexte/Filter/Detailnavigation mit erhaltener Auswahl und Fokusrückkehr; Importquellen/Originalwerte/Textescaping/Hash/Bericht und echter Prüffilter; Verbindungsfehler ohne interne Details samt erneutem Versuch und kompakter Navigation. Vorhandene synthetische Testfixtures unverändert wiederverwendet. DOM-Tests ersetzen keine Browserlayoutprüfung.
- Kontrastprüfung ergänzt: Prototyp **30 Tokenpaare**, davon 27 Textpaare ≥4,5:1 und drei Eingabe-/Fokuspaare ≥3:1, einschließlich dunkler Navigation, aktiven/hovernden Einträgen und Nachweisfläche. A/B/C-Kontrastfälle weiterhin grün. Zunächst erkannte der CSS-Test zusätzlich einen Media-Selektor als Palette; auf den tatsächlichen Root-Tokenblock begrenzt, ohne Kontrastgrenzen abzusenken.
- `npm.cmd run build`: **Exit 0**, TypeScript strict/Vite, 35 Module. Neuer separat geladener Prototypchunk 18,28 kB (4,81 kB Gzip), Haupt-JS 220,59 kB, gemeinsames CSS 39,34 kB (7,89 kB Gzip). Ein Wiederholungsbuild im Sandboxkontext wurde am Verzeichniszugriff des Konfigurationsladers blockiert; identischer Build anschließend mit vorhandener enger Build-Präfixfreigabe erfolgreich. Keine Konfigurations-/Sicherheitsänderung.
- `python -B -m unittest discover -s tests/repository -v` mit vorhandenem Git-Pfad: **sieben bestanden**, inklusive tatsächlichem öffentlichem Dateiinventar, privater Abgrenzung, lokalen Dokumentationslinks und begrenzter Secret-Mustersuche. Die Whitelist wurde nicht erweitert; neue Produktquellen sind bereits im erlaubten Frontendbereich.
- Echter Edge mit Produktionsbuild: alle **drei Szenarien bei 1440/1760/768/375 × 1000 CSS-px** geprüft und fotografiert. Seitenbreiten 1425/1745/753/360 px bei der Messreihe, keine horizontale Seitenüberbreite. Arbeitsfläche bei 1440 px 1193 px, bei 1760 px 1513 px, bei Tablet 673 px neben der kompakten Navigation. Tabelle tatsächlich 15 px. Details starten am Seitenanfang, Smartphone-Inhalte und Nachweise bleiben zugänglich.
- Sichtprüfung fand eine mobile CSS-Priorität: Die Desktopbreite der ersten Tabellenspalte zerlegte Rechnungsnummern trotz Blocklayout in mehrere kurze Zeilen. Direkte mobile Zeilenköpfe auf volle Breite gesetzt und am frischen Build überprüft: 294 px Zeilenkopf, ca. 140 px Rechnungsbutton mit ungeteilter SYNTH-2026-001, Seite 360 px bei 375 px Viewport. Mobile Rechnungs-/Detail-/Import-/Quellscreenshots danach erneuert. Quellen bleiben zweidimensional im eigenen Bereich (650 px Tabelle in 283 px Bereich), Tastatur bewegt diesen auf scrollLeft=300, Fokusumriss 3 px.
- Im Browser tatsächlich bedient: Rechnungssuche nach 002, offener Filter (eine von drei Rechnungen), Ausstellersortierung 001/003/002, Detailseite mit bestätigten 300/200 EUR und 460 EUR offen; Rückkehr erhält Suchbegriff und fokussiert den auslösenden Rechnungsknopf. Sidebar per Enter eingeklappt, Linknamen bleiben vorhanden. Rechnung 003 bleibt mit 450 EUR offen, 0 EUR bestätigt zugeordnet und 450-EUR-Zahlung „Zur Prüfung“. Keine erfundene Fälligkeit/Überfälligkeit.
- Import: Fehlerfilter eine von drei Zeilen; alle Beispielszenarien tatsächlich gewechselt (gültig weiterhin „Zur Prüfung“, widersprüchliche Dublette zwei Fehlerzeilen). Originalquelle ISD-Testdaten!G4 mit Datentyp n und -5 erhalten, SHA-256 64 Zeichen, Vollbericht erreichbar. Sprunglink fokussiert p-inhalt und bewahrt die Importansicht. Originalwerte erscheinen als React-Text, keine HTML-Ausführung.
- Echte private Screenshots unter `private/pruefungen/neukonzeption/`: neuer Rechnungsbestand, Zahlungsdetails und Import jeweils bei vier Breiten, mobile Originalquellen und kompakte Navigation; C-Rechnungsübersicht bei identischer 1440-px-Größe als Vorhervergleich. Vergleichbare Daten: drei Rechnungen, 2690 EUR Gesamtbetrag und 910 EUR offen. Browsererweiterungs-Overlays sind kein Teil des Produkts.

### Review, Einschränkungen und Integrationsaufwand

Eigener begrenzter Review von Einstieg, JSX, CSS-Isolation, Datenfluss und Diff: nur literal ausgewählter lokaler Prototypimport, keine frei zusammengesetzten Importpfade, externe Gestaltungslasten oder neuen Schreibendpunkte. Keine Änderungen an Backend, Backendtests, API-/Importverträgen, api.ts, darstellung.ts, Paket-/Lockdateien, Fachdaten oder .gitignore. Backendtests/-build für diesen reinen Frontendprototyp nicht erneut ausgeführt. Keine neuen Datenübertragungen oder Kundendaten; kein vollständiger Security-/Advisoryaudit.

Offen: belastbarer **200%-Zoom** (erneuter Tastaturversuch nach Rücksetzen der Viewportvorgabe unverändert DPR 1/innerWidth 1912), Screenreader, weitere Browser/echte Geräte, große Datenbestände und längerfristige Bürobenutzung. Keine vollständige Barrierefreiheits- oder Ergonomiebehauptung. Detailauswahl flüchtig, kein Detail-Deep-Link. Alle sichtbaren Hauptabläufe sind bedienbar; produktiver Import, Belegdownloads und Buchungen sind ausdrücklich nicht vorhanden.

Integration nach menschlicher Freigabe als begrenztes mittleres Frontendpaket: geteilte Helfer aus App.tsx herauslösen, Shell/Register/Detail-/Quellkomponenten kontrolliert übernehmen, Navigation und Fokus-/Zustandsregeln vervollständigen, alte Vorschauen erst nach gesonderter Entscheidung ablösen; anschließend Backendanzeige-/Frontend-/Repository- und Accessibility-/Browserregression prüfen. Kein Backend-/Datenbankumbau aus dem Entwurf notwendig. Zusätzliche vorläufige CSS-Last im gemeinsamen Bundle und zwei Darstellungswege sind Wartungsrisiken bis zur Entscheidung. Keine ungeprüfte Zeit-/Kostenzusage.

**Abnahmepunkt:** genau diesen Entwurf persönlich prüfen, nicht automatisch integrieren oder weitere Varianten erzeugen. Spätere professionelle Namens-/Logo-/Markenrechts-/Verwechslungs-/Domainprüfung als separates noch nicht freigegebenes Paket im vorhandenen Plan vorgemerkt. Keine Commits, Pushes, Veröffentlichungen oder automatische Library-Synchronisierung.


Finale Gegenprüfung: 18 Frontendtests nach letzter CSS-Korrektur erneut grün (Exit 0), sieben Repositorytests nach Dokumentationsänderungen grün, regulärer Diff-Check und Whitespacecheck der unverfolgten Produktdateien ohne Befund. Gespeicherte Design-/Nachweis-/Planabschnitte zurückgelesen. Vergleichslink im Browser tatsächlich bedient: erhaltener C-Stand zeigt drei Rechnungen und 910,00 EUR offen, Rückkehr lädt die isolierte Vorschau. Die drei zentralen 1440-px-Bilder am Endstand erneuert, Viewportvorgabe zurückgesetzt und neue Vorschau offengelassen. HEAD weiterhin f3e9e29, Index ohne Änderungen; keine Commits oder Pushes.

## Kontrollierte Arbeitsraum-Integration und Dokumentenakte – 18.10.2026

**Aktueller Status:** Nutzer hat die Gestaltungsrichtung vorläufig akzeptiert. Sie ist jetzt die reguläre React-Anwendung unter `http://127.0.0.1:5180/#rechnungen`. Menschliche Funktionsabnahme steht aus. Frühere Berichte über isolierten Prototyp und A/B/C beschreiben historische Stände.

Eine Anwendung statt paralleler Oberflächen: App.tsx enthält die Shell, Rechnungen.tsx das Register, Rechnungsdetails.tsx die separate Detailansicht und Importvorschau.tsx die bestehende Prüfung. ui.tsx bündelt Datenladen/Abbruch/Wiederholung, Status, Meldungen, Titel, Kennzahlen und Tastaturscrollen. design-tokens.css und Arbeitsraum.css trennen Tokens und Layout. Designprototyp.tsx bleibt nur Exportalias derselben App, prototyp.css historischer Hinweis; regulär werden beide nicht geladen. Keine neue Bibliothek oder Finanzberechnung.

Vorherquellen einschließlich A/B/C-Styles, Tests und Dokumentation privat bytegenau mit SHA-256-Inventar gesichert. Bestehende Arbeit nicht gelöscht oder zurückgesetzt. Sechs neue modulare Produktdateien innerhalb bestehender Whitelist; öffentliches Sollinventar 58 Dateien. Private Pläne/Sicherungen/Skills/Screenshots bleiben ausgeschlossen.

Graphit-Navigation, Off-White-Arbeitsraum, Indigo-Akzent, Kennzahlenzeile, Tabelle, separate Details und Herkunftsinspektor übernommen. Suche, Statusfilter, Sortierung, Bestätigungen/Zuordnungen, Rollen, Belegreferenzen und Importquellen erhalten. „Alle Vorgänge“ heißt „Rechnungsbestand“, die Rollenspalte „Aussteller / Empfänger“. Keine pauschalen Kundenrollen oder neuen Kategorien.

Sichtbarer Button **„Zurück zur Rechnungsübersicht“** und Breadcrumb erhalten Suche, Zahlungsfilter und Sortierung; Fokus kehrt zur geöffneten Rechnungsnummer zurück. Liste bleibt während Details verborgen gemountet. Hauptbereichswechsel oder Neuladen persistiert diesen lokalen Zustand weiterhin nicht. Dokumentenbereich ist ein ausdrücklicher Leer-/Vorbereitungszustand ohne Scheinanhänge.

### Neu ausgeführte Prüfungen

- Test-first: neue Integrationsregression zunächst tatsächlich rot (18 bestehende Fälle grün, ein neuer rot), danach regulären Einstieg integriert.
- `npm.cmd test`: **16 bestanden, null fehlgeschlagen/übersprungen**, zuletzt nach Navigations-CSS-Korrektur erneut ausgeführt. Verträge/Fehler, Serverwerte, Suche/Filter/Sortierung, Status, beide Rückwege/Fokus, Dokumenten-Leerzustand, Laden/Leer/Fehler/Wiederholung und Importquellen/Originalwerte/Textescaping/Hash/Bericht/Tastatur abgedeckt. Historische A/B/C-Umschalttests privat erhalten, nicht als aktuelle Funktion weitergetestet.
- Kontrast: **30 semantische Tokenpaare**, 27 Textpaare mindestens 4,5:1 und drei Eingabe-/Fokuspaare mindestens 3:1. Kein vollständiges WCAG-Audit.
- `npm.cmd run build`: **erfolgreich**, TypeScript strict/Vite, 36 Module, CSS 21,33 kB (4,70 kB Gzip), JS 219,79 kB (68,06 kB Gzip), kein separater Prototypchunk. Sandbox-Wiederholung am Verzeichniszugriff blockiert; identischer Build mit vorhandener enger Build-Präfixfreigabe erfolgreich. Keine Sicherheitsänderung.
- `dotnet test src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`: **113 bestanden, null fehlgeschlagen/übersprungen**. Eigener laufender Host sperrte zunächst DLLs; Host beendet, Tests erfolgreich abgeschlossen und danach mit --no-build/--no-restore lokal neu gestartet.
- `dotnet build src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`: **erfolgreich, null Warnungen/Fehler**.
- Edge mit gebauter Anwendung: Rechnungen, Details und Import bei **1440/1760/768/375 × 1000 CSS-px**, jeweils fotografiert und ohne horizontalen Seitenüberlauf. Mobile Originalzellen separat scrollbar: 650 px Inhalt in 283 px Bereich, Tastatur scrollLeft 300, Fokus 3 px.
- Beide Rückwege im Browser mit Suche SYNTH-2026-002, Filter „Teilweise bezahlt“, Sortierung Empfänger bedient: alle Werte, eine Ergebniszeile und Rückfokus erhalten. Rechnung 002: 960,00 EUR / 500,00 EUR bestätigt zugeordnet / 460,00 EUR offen. Unbestätigte 450,00 EUR bei Rechnung 003 bleiben 0,00 EUR bestätigt zugeordnet / 450,00 EUR offen. Zahlungsquelle, Importfehler G4=-5, Blatt/Zeile/Zelle, SHA-256 und vollständigen Bericht geöffnet.
- Browserreview fand eine durch Extraktion fehlende Flexregel der Sidebarlinks. Gezielt korrigiert, neu gebaut und alle Ansichtsgrößen mit frischem Build fotografiert.

Screenshots privat unter `private/pruefungen/integration-arbeitsraum/`: drei Szenarien für vier Breiten, zusätzlicher Zahlungsnachweis und mobile Originalzellen. Sichtbare Browsererweiterungen am Bildrand gehören nicht zum Produkt.

### Dokumentenakte und Grenzen

Die [kompakte Fachkonzeption](ARCHITEKTUR.md) behandelt tenantgebundenen Vorgangsbezug, mehrere Dokumente, unveränderliche Originalversionen, Notizen mit Autor/UTC, Bearbeitung/Annotationen/Exporte/Audit und strikt getrennte Finanzzustände. „Bezahlt“-Stempel besitzt keine Finanzwirkung. Rechte, PDF-Sicherheit, Scan/digitales Original/strukturierte E-Rechnung, optional überprüfte OCR und noch zu klärende Aufbewahrung berücksichtigt. Kein Upload, PDF-Viewer/-Editor, OCR, Stempel oder Notizspeicher implementiert.

Nächstes vorgeschlagenes Paket **DOK-01**: ein synthetischer Vorgang, zwei selbst erzeugte echte PDF-Testbelege, lesende Metadaten/Version/Hash und kontrolliertes Öffnen im Browser. Stabilen Vorgangsbezug und Zugriff zuerst definieren. Kein automatischer Beginn oder Upload. Aktueller Finanzvertrag dafür nicht erweitert.

Offen: belastbarer 200%-Zoomnachweis, Screenreader, weitere Browser/reale Geräte, Langzeitnutzung und menschliche Funktionsabnahme. Keine vollständige Accessibility-, Security- oder Produktionstauglichkeitsgarantie. Keine Kundendaten, Pakete, Rechte-, Backend-, Vertrags- oder Fachdatenänderungen; keine Commits, Pushes oder Library-Synchronisierung.

Finale Gegenprüfung der Integration: sieben Repositorytests nach Dokumentationsänderung bestanden; Diff-Check und neue Quelldateien ohne Whitespacebefund. Historische Sicherung mit zehn SHA-256-Einträgen erneut verifiziert. Zoomversuch mit fokussierbarem Button und Ctrl++ zeigte unverändert innerWidth 1912 / DPR 1, deshalb kein 200%-Nachweis. Viewportoverride zurückgesetzt, reguläre Übersicht zur Funktionsabnahme offen. Finanz-/API-/Fachdaten-/Paket-/Ignore-Diff leer; keine Git-Schreibaktion.

## Kontakt-Sofortaktionen, klare Nachweise und beide Zahlungsrichtungen – 18.10.2026

**Iststand:** f3e9e29 mit vorhandenen uncommittierten Design-/Integrationsänderungen; regulärer Arbeitsraum und eine React-App bestätigt. Keine erneute Designintegration. Vor Änderungen alle 58 vorgesehenen Produktdateien und privaten Plan bytegleich mit SHA-256 gesichert; historische Arbeit bleibt erhalten. Sicherung und Screenshots privat unter `private/pruefungen/kontaktaktionen/`.

**Implementiert:** wiederverwendbare Kontaktkarte für Rechnungsaussteller/Rechnungsempfänger. Organisation und Rolle aus Rechnung, Ansprechpartner/E-Mail/Telefon/Quelle optional aus additiver Demo-Projektion. Vorher nur Organisationsnamen und Zahlungsrollen vorhanden, keine Kontaktdaten. Zuordnung über bestehende Demo-Unternehmenskennungen, kein Namensmatching. Nord: erfundener Ansprechpartner, .invalid-E-Mail und Filmrufnummer; ISD: .invalid-E-Mail ohne Telefon/Person; West: keine Kontaktdaten. Fehlende Werte und ungeeignete Formate erzeugen Text statt Scheinaktionen. Kontaktherkunft aufklappbar.

E-Mail öffnet ausschließlich nach Benutzeraktion die konfigurierte Anwendung mittels mailto mit Einzeladresse und kodiertem Rechnungsbetreff. Telefon mittels tel mit normalisierter globaler Nummer. Keine externe SDK-/CRM-/Versand-/Telefonie-Integration und keine Historienbehauptung. ASCII-Mailprofil ist bewusst begrenzt; Steuerzeichen, Mehrfachempfänger, Header-/Parameterinjektion, Überlängen und ungeeignete Telefonformate werden verworfen. Beschädigte Unicode-Referenz unterdrückt den Betreff statt Renderabbruch. Formatsyntax bestätigt weder Erreichbarkeit noch Eigentum. Synthetische Kontakte ausdrücklich nicht erreichbar, Telefonnummer aus amtlichem Filmrufnummernbereich; Quelle im Architekturort. Kein E-Mail-/Telefonprogramm automatisch aktiviert und keine Nachricht/kein Anruf ausgeführt.

**Gezielte UI-Bereinigung:** Rechnungsgrundlage rechts benennt ihre einzelne Belegreferenz/Quelle. Zahlungsnachweis bleibt pro Zahlung in einem aufklappbaren Bereich. Doppelter Hinweis auf fehlende Originaldateien entfernt; ein ehrlicher Dokumenten-Leerzustand bleibt. Kontaktquelle ist fachlich eine dritte Herkunft und kein Zahlungsbeleg. Keine Beleg-/Herkunftsangaben gelöscht. Übersicht weist darauf hin, dass Gesamtbeträge noch nicht zwischen Forderungen und Verbindlichkeiten unterscheiden. Keine neuen Richtungs-/Fälligkeits-/Vorgangskategorien vorgetäuscht, keine Salden geändert.

**Fachlicher Entwurf:** [Architektur](ARCHITEKTUR.md) enthält kompakte synthetische Matrix aus Ausgangs-/Eingangsrechnung, Voll-/Teilzahlung, unbestätigter Zahlung, geplanten Raten und weiteren Verpflichtungen. Nord-Perspektive nur für Analyse ausdrücklich angenommen, im API noch nicht modelliert. Kern lässt beide Richtungen bei passenden Parteien zu; eigene Perspektive, Vorgangsarten, Ratenplan und wiederkehrende Verpflichtungen fehlen. 910,00 EUR bleibt gemischte Bestandsanzeige, kein Forderungs-/Verbindlichkeitssaldo. Gemeinsamer Vorgangs-/Dokumenten-/Kommunikationsplan und zentraler Eingangsbearbeitungsmechanismus ergänzt; keine Finanzarchitektur, Dokumentenverwaltung, OCR, Vorlagenbibliothek oder Workflowerweiterung implementiert.

### Tatsächlich ausgeführte Prüfungen

- Test-first: `npm.cmd test --prefix src/frontend` zunächst **16 bestehende Fälle grün, fünf neue Fälle rot** (fehlende URI-Helfer/Kontaktstruktur/UI und primäre Nachweisorte). Anschließend Implementierung. TypeScript-strict fand zusätzlich ungesicherten Arrayzugriff nach Adressaufteilung; expliziten Guard ergänzt.
- `npm.cmd test` aus Frontend: **21 bestanden, null fehlgeschlagen/übersprungen**, nach letzter relevanter Änderung erneut ausgeführt. Neue Fälle: sichere URI-Kodierung/ungültige Werte/Steuerzeichen/Unicode; optionale rückwärtskompatible Kontakte und ungültige Strukturen; korrekte Rollen/Links/Icons/Serverbeträge/Rückfokus; fehlende oder manipulierte Angaben ohne Links/HTML; Rechnungsgrundlage und individueller Zahlungsnachweis einmalig. Bestehende Filter-/Sortier-/Import-/Fehler-/Kontrasttests unverändert grün. 30 Tokenpaare weiterhin geprüft.
- `dotnet test src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`: **117 bestanden, null fehlgeschlagen/übersprungen**. Zwei neue Kontakt-/Kompatibilitätsfälle; zwei spiegelbildliche Kernfälle für eigene Gläubiger-/Schuldnerrolle mit 240,00 EUR, bestätigten 60,00 + 40,00 EUR, abgewiesener unbestätigter Zahlung und anschließendem Ausgleich. Bestehenden echten HTTP-Test um Kontakt-JSON/Rollenzuordnung/null ergänzt. Finanzkern unverändert.
- `dotnet build src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`: **erfolgreich, null Warnungen/Fehler**. Eigenen vorherigen Host vor Backendbuild beendet, danach lokal neu gestartet; keine fremden Prozesse beendet.
- `npm.cmd run build`: **erfolgreich**, TypeScript strict/Vite, 38 Module, CSS 22,94 kB / 4,93 kB Gzip, JS 222,35 kB / 68,89 kB Gzip. Erster Build erfolgreich; nach ergänzendem Summenhinweis Wiederholung am Sandbox-Verzeichniszugriff blockiert, identischer Befehl mit vorhandener enger Build-Präfixfreigabe erfolgreich. Keine Sandbox-/Sicherheitseinstellung geändert.
- Repositoryprüfung mit `BETRIEBSPLATTFORM_GIT` und `python -B -m unittest discover -s tests/repository -v`: **sieben bestanden**; Inventar um zwei Produktquellen auf 60 Dateien ergänzt. Keine Ignore-Ausnahme erforderlich.
- Edge mit frischem Build: Kontakt-/Detailansicht bei **1440/1760/768/375 × 1000 CSS-px**, keine horizontale Überbreite. Desktop zwei Kontaktspalten, Tablet/Mobil untereinander. Neue Aktionslinks im Desktop rund 64 px hoch, SVG dekorativ/gleiches Raster, Fokus 3 px. Per Tab von Detailüberschrift zur Mailaktion sowie von Telefon zur Kontaktquelle navigiert; keine externen Handler aktiviert.
- Beide Rückwege tatsächlich mit Suche SYNTH-2026-002, Filter Teilweise bezahlt und Empfängersortierung bedient: alle Werte, eine Ergebniszeile und Rechnungsfokus erhalten. Bestandswerte 2.690,00 EUR / 910,00 EUR unverändert; Rechnung 002: 960,00 / 500,00 bestätigt / 460,00 offen. Rechnung 003: 450,00 offen, 0,00 bestätigt zugeordnet und Zahlung Zur Prüfung; West ohne Aktionslinks.
- Alle drei Importbeispiele im Browser bedient: gültiger Kandidat bleibt Zur Prüfung, Fehlerfall G4=-5 mit Originalwert/Datentyp/Quelle, Dublette zwei Fehlerzeilen. Mobile Originaltabelle separat per Tastatur auf scrollLeft 300, kein Seitenüberlauf. Importcode/-vertrag unverändert.

Screenshots: Kontakte bei vier Breiten, fehlende Kontakte mobil, Rechnungsliste Desktop, individueller Zahlungsnachweis mobil und Import Desktop/Mobil im privaten Prüfverzeichnis. Browsererweiterungssymbole am Rand gehören nicht zum Produkt.

### Geänderte Dateien dieses Pakets und Grenzen

Neu: `src/frontend/src/Kontaktkarte.tsx`, `src/frontend/src/kontaktaktionen.ts`. Angepasst: Web/ApiModelle.cs und DemoDaten.cs; Frontend api.ts, Icon.tsx, Rechnungsdetails.tsx, Rechnungen.tsx, Arbeitsraum.css, app.test.mjs; Backend WebPrototypTests.cs und RechnungskernTests.cs; Repository-Inventartest; README, DESIGN.md, ARCHITEKTUR.md, dieser Nachweis sowie privater Entwicklungsplan. Vorhandene App/Shell, Finanzkern, Importmodul/-daten, Pakete/Lockfile, Sicherheitsheader und .gitignore unverändert.

Offen: tatsächliche Übergabe an installierte E-Mail-/Telefonie-Anwendung abhängig vom Benutzergerät (keinen Handler/Anruf automatisiert gestartet), 200%-Zoom, Screenreader, andere Browser/reale Mobilgeräte und menschliche Funktionsabnahme. Keine vollständige Erreichbarkeits-, Sicherheits- oder Accessibility-Garantie. Kein Online-Paketadvisoryscan, keine neuen Pakete/Downloads, echten Daten, produktiven Aktionen oder Git-Schreibaktionen. Keine Library-Synchronisierung.

**Nächster Vorschlag:** nach Funktionsabnahme und fachlicher Bestätigung der Richtungs-Matrix DOK-01 mit genau einem stabil referenzierten synthetischen Vorgang und zwei echten künstlichen PDFs, lesender Zuordnung/Version/Hash und kontrolliertem Öffnen. Gemeinsamen Vorgangs-/Unternehmensbezug zuerst klären, keine Finanzarchitektur ohne Abnahme erweitern. Folgepaket nicht automatisch begonnen.

## DOK-01 – Desktop-Vorgangsakte und echte PDF-Belege, 18.10.2026

**Status:** begrenzte synthetische Lesedemo implementiert, bereit zur menschlichen Funktionsabnahme. Regulärer Arbeitsraum, keine neue App/Designvariante. Ausgang f3e9e29 mit uncommittierten Vorarbeiten erhalten; 60 öffentliche Produktdateien und privater Plan vor Änderungen bytegleich mit 61 SHA-256-Einträgen gesichert. Nachweise/Screenshots privat unter `private/pruefungen/dok-01/`.

### Tatsächlich umgesetzt

- Große Kontaktkarten durch kompakte Zeilen „Firma & Kontakt“, „Rechnung von/an“ ersetzt. Firma/Ansprechpartner sichtbar, Mail/Telefon als kleine native Iconlinks, ausführliche Daten/Quelle über funktionierende Kontaktdetails. Dieselben URI-Helfer wiederverwendet. Zugängliche Namen mit Firma, Texttooltips per CSS-Hover und Tastaturfokus. Keine Notiz-/Schreiben-Scheinaktionen.
- Klarere Begriffe: Bestätigt bezahlt, Noch zu bezahlen/erhalten, Zahlungen, Zahlungsdatum. Additive explizite Demo-Perspektive und stabile Anzeige-IDs ermöglichen richtige Richtung ohne Finanzmodelländerung. Ohne konsistenten Kontext bleiben Zahler/Empfänger sichtbar. 002: 960,00 EUR, eigene bestätigte 300,00 + 200,00 EUR, noch 460,00 EUR zu bezahlen. 003: unbestätigte 450,00 EUR bleiben offen. 910,00 EUR heißt gemischter Bestand offener Positionen, kein Forderungssaldo. Kein Bearbeitungsverlauf erfunden.
- DemoDokumente.cs erzeugt zentral zwei unveränderliche echte PDFs im Speicher: SYNTH-2026-002-Originalrechnung.pdf und SYNTH-ZA-002-Zahlungsbeleg.pdf. Version/Datum/Bytegröße/Hash/Quelle/Vorgangs-/Zahlungsbezug vorhanden, kein Binärfixture oder Dateisystempfad nötig. Original zentral rechts, Zahlungsbeleg nur an der 300-EUR-Zahlung; 200-EUR-Zahlung behält ihren vorhandenen Referenz-/Quellennachweis ohne Schein-PDF. Ein Katalog, ein Dateiabrufhandler und eine Dokumentkarte.
- Kontrollierte GET-Metadaten-/PDF-Strecke mit exakten bekannten Kennungen, application/pdf und inline-Dateiname. Bestehende Loopback-/Host-/Origin-/CSP-Grenzen unverändert. Native PDF-Ansicht im neuen Tab statt Lockerung der Einbettungssperre oder neuer Renderingbibliothek. Schließen des Tabs erhält die Rechnung; fehlende Datei liefert eine lesbare 404-Seite. Lade-/Fehler-/Leerzustand und Wiederholung für Metadaten.

### Frische automatisierte Prüfungen

- Backendtest zuerst angelegt und tatsächlich ausgeführt: Compilerabbruch wegen noch fehlendem Katalog/Anzeigevertrag. Danach implementiert; kein bestandener Fachtest vor Implementierung behauptet.
- Frontend zunächst **21 bestehende Tests bestanden, drei neue DOK-01-Fälle fehlgeschlagen**. Anschließend implementiert; zusätzlicher Negativfall für Katalogmetadaten ergänzt.
- `dotnet test src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`: **118 bestanden, null fehlgeschlagen/übersprungen**, nach letzter Backendänderung erneut ausgeführt. Neuer Katalogfall prüft zwei PDFs, Zuordnung, Header/EOF/xref, Bytegröße/SHA-256 und unveränderliche Kopien. Bestehender echter HTTP-Test prüft beide GET-Pfade, PDF-Content-Type/Disposition/Integrität, fehlende/falsche Kombinationen und codierten Pfadversuch, verständliche 404-HTML-Seite, abgewiesene Schreibmethode sowie Host-/Origin-Grenzen auch für PDF. Bestehende Finanz-/Import-/Securityfälle weiterhin grün.
- `npm.cmd test` / zuletzt `npm.cmd test --prefix src/frontend`: **25 bestanden, null fehlgeschlagen/übersprungen**, nach letzter Änderung ausgeführt. DOK-01 testet kompakte Kontaktaktionen/Tooltips, Rolle/Richtung, unveränderte Serverbeträge, getrennte einmalige PDF-Verweise mit noopener und neuem Tab, Metadatenfehler/Wiederholung, fehlende/widersprüchliche Perspektive, doppelte/fremde/unvollständige IDs/Metadaten einschließlich Hash mit Zeilenende. Bestehende 21 Verhaltensfälle und 30 semantische Kontrastpaare bleiben grün; bisheriger Tabellenlabel-Test auf natürliche neue Beschriftung angepasst.
- `dotnet build src/backend/Betriebsplattform.slnx --configuration Release --no-restore --verbosity minimal`: **erfolgreich, null Warnungen/Fehler**.
- `npm.cmd run build`: **erfolgreich**, TypeScript strict/Vite, 39 Module, CSS 24,56 kB (5,27 kB Gzip), JS 227,06 kB (70,11 kB Gzip). Eine Zwischenwiederholung war am bekannten Sandbox-Verzeichniszugriff blockiert; identischer Build mit vorhandener enger Präfixfreigabe erfolgreich. Finaler Build nach Hashprüfungsergänzung regulär erfolgreich. Keine Sandbox-/Rechteänderung.
- Repositorytestbefehl aus CONTRIBUTING mit vorhandenem Git-Pfad: **sieben bestanden**, öffentliches Inventar um zwei Quellen auf 62 erweitert. Keine Ignore-Ausnahme für PDFs; generierte Inhalte nur zur Laufzeit.

### Praktische Browserprüfung

- Edge Desktop 1440/1760 × 1000 CSS-px: vollständige Akte geprüft und fotografiert, keine horizontale Seitenüberbreite. Kontaktbereich rund 200 px statt großer Karten; Mailaktion mit 3-px-Fokus und sichtbarem Tastaturtooltip, Kontaktdetails per Enter geöffnet. Hoverregel vorhanden, kein gesonderter automatisierter Maus-Hovernachweis. Metadaten/Quellen weiter erreichbar. Bei Sichtprüfung zu große geerbte Notiz-Innenabstände im Dokumentpanel erkannt, gezielt verdichtet und neu gebaut.
- Original-PDF **tatsächlich aus der App geöffnet und im nativen Browserbetrachter lesbar geprüft**, enthält Rechnung 002 / 960,00 EUR / synthetische Rollen. Zahlungs-PDF ebenfalls tatsächlich geöffnet und gelesen: nur 300,00 EUR, Zahler Nord, Empfänger ISD, Datum 17.10.2026, ausdrücklicher Test-/Nicht-Banknachweis-Hinweis. Screenshots beider nativen PDF-Ansichten gespeichert. Kein browserseitiger KI-/Cloud-/Bearbeitungsdienst verwendet.
- Beide PDF-Tabs geschlossen, Rechnung blieb geöffnet. Am finalen Build Original zusätzlich aus gefilterter Liste per Enter geöffnet; nach Schließen und Zurück erhalten: Suche 002, Teilzahlungsfilter, Fokus auf 002. Zuvor beide Rückwege mit Empfängersortierung geprüft; Werte erhalten. PDF-Tab übernimmt keinen Appzustand und verändert nichts daran.
- Fehlende PDF über absichtlich unbekannte ID im Browser geprüft: „PDF nicht verfügbar“ mit verständlicher Schließ-/Rückkehranleitung, keine internen Pfade.
- Rechnung 001 zeigt bestätigt erhaltene Zahlung und 0,00 EUR noch zu erhalten. 003 zeigt Zahlungseingang zur Prüfung, 0,00 EUR bestätigt und 450,00 EUR noch zu erhalten, West ohne Mail-/Telefonaktionen. Keine Umdeutung in Überfälligkeit.
- Kleine Bildschirme nur Regression: 768/375 px ohne Seitenüberlauf; keine eigene mobile UX neu entwickelt. Importprüfung am Desktop erneut mit Fehlerzeile G4=-5 geöffnet, Originalwerte/Datentyp/Blatt/Zelle erhalten. Alle Import-/Finanztests zusätzlich automatisch ausgeführt.

Screenshots im privaten DOK-01-Prüfverzeichnis: akte-1440.jpg, akte-1760.jpg, akte-desktop-ausschnitt.jpg, originalrechnung-browser.jpg, zahlungsbeleg-browser.jpg, pdf-fehlt.jpg, rechnungen-1760.jpg, kleiner-bildschirm-375.jpg und import-1440.jpg. Sichtbare Browsererweiterungs-/Viewerangebote gehören nicht zur Anwendung.

### Review, Dateien und Grenzen

Neue Quellen: Web/DemoDokumente.cs, Frontend/Dokumente.tsx. Geändert: ApiAnwendung.cs, ApiModelle.cs, DemoDaten.cs; api.ts, darstellung.ts, Kontaktkarte.tsx, Icon.tsx, Rechnungen.tsx, Rechnungsdetails.tsx, ui.tsx, Arbeitsraum.css; WebPrototypTests.cs, app.test.mjs, Repository-Inventartest; README, DESIGN.md, ARCHITEKTUR.md, dieser Nachweis und privater Plan. Bestehende Finanzobjekte/-berechnung, Importmodul/-testdaten, URI-Helfer, Pakete/Lockfile und .gitignore unverändert.

Fokussierter eigener Review: keine Pfadkonstruktion aus Dokumentkennungen, keine fremden Inhalte/HTML aus Metadaten, kein Upload oder Schreibendpunkt, keine externen Assets/Pakete, keine gelockerten Sicherheitsheader. Dokumente sind feste öffentlich gedachte synthetische Demo-Inhalte ohne produktive Auth/Mandantensicherheit. Kein allgemeiner PDF-Sicherheits-/Schadsoftwaretest oder vollständiger Accessibilityaudit. Laufzeitgenerierung ersetzt keine produktive Originalablage; Hash ist keine Echtheitsgarantie.

Offen: Screenreader/andere Browser/reale Geräte, 200%-Zoom, separate Viewer-Suche/-Zoom-Bedienprüfung und OS-Mail-/Telefonieübergabe. Browserviewer kann eigene Bearbeitungs-/Cloudangebote anzeigen; Nutzung nicht Teil dieser Implementierung. Produktionsrechte, echte Dateiverarbeitung, Persistenz/Audit/Aufbewahrung, Notizen/Verlauf und weitere Formate bleiben geplante Pakete gemäß [Architektur](ARCHITEKTUR.md). Keine Uploads, OCR, PDF-Änderungen, echten Daten, Finanzaktionen, Pakete, Commits/Pushes oder Library-Synchronisierung. Stop zur persönlichen DOK-01-Abnahme.


## Nach DOK-01 – kompakte Rechnungsakte und echter Demoimport, 18.10.2026

Status: umgesetzt und technisch geprüft, menschliche Sicht-/Funktionsabnahme offen. Die vorherige Rechnungsdetailansicht war ausdrücklich nicht abgenommen. Bestehende Anwendung und Arbeitsraumrichtung erhalten; keine weitere Anwendung oder Designvariante.

### Lokale Versionierung

Vor Beginn 28 vorhandene geänderte/neue öffentliche Dateien gegen f3e9e29 geprüft, 62 öffentliche Dateien bytegleich lokal gesichert und SHA-256 verifiziert. Nach ausdrücklicher Nutzerzustimmung lokaler Commit `5f735de890d75cff68579db756a5d4d5e5fb9182`, `chore: Arbeitsraum und synthetische Dokumentendemo sichern`. Danach sauberer Worktree. Die nachfolgende Umsetzung bleibt uncommitted; kein Push, Deployment oder Veröffentlichung. Private Dateien, echte Dokumente und Buildartefakte bleiben ausgeschlossen.

### Tatsächlich implementiert

Rechnungsidentität, Gegenpartei, drei Serverbeträge und Zahlungsstatus bilden den Kopf. Rechnung von/an und kompakte Zahlungsfolge mit Datum, Richtung, bestätigter Zuordnung und getrenntem PDF-Nachweis links. Eine rechtsbündige Aktionsgruppe mit Firma-/Rollenwahl, sicheren Mail-/Telefonlinks, echter lokaler Demonotiz und Kontaktinformationen rechts. Kontakte werden nicht mehr als große offene Karten dargestellt. Notizen: Pflichttext bis 2.000 Zeichen, höchstens 20 je Rechnung, synthetischer Benutzer/Zeit, sessionStorage je Browser-Tab, verständlicher Speicherfehler. Kein automatischer Versand oder Anruf.

Originalrechnung und Zahlungsbeleg als getrennte Auswahl in derselben nativen PDF-Vorschau, optional vergrößert, Schließen/Escape und Rückkehr zur gleichen Rechnung. Bekannte unveränderliche PDF-Testbytes und bestehende Endpunkte wiederverwendet. Nur ihre Antworten erlauben Einbettung im eigenen Ursprung; keine allgemeine Freigabe fremder Frames, Objekte oder PDF-Inhalte. Technische Quellen, Referenzen, IDs, Hash/Größe liegen außerhalb des normalen Arbeitsbereichs in aufklappbaren Prüfansichten. Keine Originalveränderung oder PDF-Schreibschnittstelle.

Importe: tatsächliches File-Input und Rohdateitransport zum lokalen Backend, Format-/Paket-/XML-/Größen-/Formelprüfung durch MVP-02, bestätigtes Vorlagenblatt/Firmenmapping, Originalwerte und Prüfhinweise, Auswahl gültiger Rechnungszeilen, eigene Übernahmebestätigung, verständlicher Bericht. Übernommene Domain-Rechnungen erscheinen in genau dem regulären Demo-Rechnungsbestand und verwenden dessen gemeinsame Fachprojektion. Zahlungskandidaten bleiben ausschließlich im Prüfbericht, ohne Bestätigung, Zuordnung oder Saldenwirkung. Bytegleiche Dateien liefern denselben Vorgang; frische Quell-/Rechnungsdublettensperren und ein gemeinsamer Lock verhindern mehrfache/parallele Übernahme. Originalbytes, Hash, Blatt/Zelle/Zeile, Mapping, Zeilenauswahl und UTC-Entscheidungszeit bleiben bis Neustart verfügbar.

### Frisch erfolgreich ausgeführte Prüfungen

Alle folgenden Kommandos mit Exitcode 0, ohne Paketdownload:

| Kommando / Arbeitsordner | Tatsächliches Ergebnis |
| --- | --- |
| `dotnet test src/backend/Betriebsplattform.slnx --no-restore` / Root | 125 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| `dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release` / Root | erfolgreich, 0 Warnungen, 0 Fehler |
| `npm.cmd test` / src/frontend | 29 bestanden, TypeScript strict kompiliert |
| `npm.cmd run build` / src/frontend | erfolgreich, 39 Module, CSS 28,95 kB / JavaScript 239,64 kB |
| `python -B -m unittest discover -s tests/repository -v` / Root | 7 bestanden; öffentliche Inventar-/Ignore-/Link-/Secretindikatorprüfungen |
| `git diff --check` | kein Whitespacefehler |

Neue Backendtests prüfen Mapping-/Auswahl-/Bestätigungspflicht, tatsächlichen Bestand und Neustartverlust, beide Rechnungsrichtungen, keine Zahlungsübernahme, negative/ungeklärte Zeilen, parallele und veränderte Wiederholungen, Originalbytekopie, atomare Ablehnung, Sitzungslimit, echte HTTP-Übertragung, 413 bei Übergröße, beschädigten Inhalt, unzulässigen Inhaltstyp und Host-/Origin-/Headergrenzen. Bestehende XLSX-/Finanz-/Dokumentenregressionen bleiben enthalten. Frontendtests prüfen Kontaktwechsel, URI-Grenzen, lokalen Notizablauf einschließlich Rückkehr, Dateieingabe/Request-Body, Mapping, Auswahl und ausdrückliche Bestätigung, Bericht, defekte Importantworten, PDF-Fehler/Wiederholung, Öffnen/Schließen/Vergrößern und Fokus-Rückkehr.

Neue Aktionsgruppen-/Notizprüfung zunächst tatsächlich rot bei 25 bestehenden grünen Fällen. Neue Importtests zunächst Compilerrot vor dem Service. Eine fehlerhafte synthetische Testfixture teilte zunächst versehentlich die Rechnungsnummer mit einer gültigen Zeile; der vorhandene Dublettenschutz sperrte korrekt beide. Fixture berichtigt, keine Fachregel gelockert. Sandboxartefakt-/Dateisperren durch enge Werkzeugfreigabe beziehungsweise Beenden des eigenen Hosts gelöst; letzte Builds sauber. Keine neuen Pakete, Projekt-/Lockfile-/Ignoreänderungen. Kein aktueller Online-Advisoryscan und kein vollständiges unabhängiges Securityaudit.

### Tatsächliche Browserprüfung und ihre Grenze

Microsoft Edge auf dem lokalen Releasehost `http://127.0.0.1:5180`. Beide PDFs im eingebetteten Browserviewer wirklich gelesen, vergrößert und geschlossen. Original: SYNTH-2026-002 / 960 EUR, Zahlungsbeleg: 300 EUR von Nord an ISD. Native PDF-Werkzeuge können lokale Kopien bearbeiten; das Serveroriginal bleibt ohne Rückschreibweg unverändert. Enter öffnet Notiz-/PDF-Dialog, Escape beendet die Vergrößerung. Fokusverlust nach Schließen eines noch modalen Dialogs im Browser reproduziert und durch Fokus-Rückkehr nach dem React-Unmount behoben; anschließend Fokus auf Notiz/Vergrößern mit 3-px-Outline nachgewiesen. Notiz tatsächlich gespeichert und nach Rechnungswechsel/Reload im selben Tab wieder vorhanden. Kontaktwahl ändert tatsächlich Mail-/Telefonziel; ungültige/fehlende Werte ohne aktiven Link. Keine externen OS-Handler aufgerufen.

Synthetische XLSX-Datei über den lokalen Vorlagenendpunkt tatsächlich heruntergeladen. Dateidialog über den sichtbaren Button öffnet sich. Das automatische Befüllen wurde von der Edge-Erweiterung wegen fehlender Dateizugriffsberechtigung blockiert. Keine Erweiterungs-/Sicherheitseinstellung geändert. Deshalb kein vollständiger Browser-Dateipicker-End-to-End-Nachweis: die konkrete heruntergeladene künstliche Datei wurde stattdessen mit eng freigegebenem lokalen HTTP-Aufruf an denselben Uploadendpunkt übertragen. Anschließend echter Browserablauf über Bisherige Demoimporte: Nord/ISD zugeordnet, Mapping bestätigt, Originalwert -5 in G4 und Fehlerfilter geprüft, nur gültige Zeile 2 ausgewählt, separat bestätigt. Bericht 1 übernommen / 0 übersprungen / 1 fehlerhaft / 1 zur Prüfung. SYNTH-RE-01 danach im regulären Bestand geöffnet: 100,25 EUR offen, 0,00 bestätigt, keine Zahlung. Nach absichtlichem Neustart für den finalen Release-Build denselben Ablauf im neuen flüchtigen Bestand wiederholt. Bericht bleibt über die Sitzungsliste aufrufbar.

Detailansicht bei 1760/1440/768/720/375 px und Importbericht bei 1440/768/375 px ohne horizontalen Seitenüberlauf; am Detail immer genau eine Aktionsgruppe. 720 px ist nur Reflowprüfung. Browser-Zoomkürzel änderten innerWidth/DPR nicht; echter 200-%-Zoom bleibt unbestätigt. Vorhandene automatisierte Token-Kontrastgrenzen bestanden, keine vollständige Accessibility-Konformitätsaussage. Leere Rechnungssuche mit Reset, ehrlicher Dokumenten-/Zahlungsleerzustand, falsche PDF-Kennung mit verständlicher Fehlerseite im Browser geprüft; Netzwerk-/Speicher-/Antwortfehler überwiegend automatisiert. Screenreader, weitere Browser/Geräte, OS-Mail-/Telefonieübergabe und vollständige manuelle Dateiauswahl bleiben offen.

### Geänderte öffentliche Dateien seit Baselinecommit

- `README.md`
- `docs/public/ARCHITEKTUR.md`
- `docs/public/MVP-01-NACHWEIS.md`
- `docs/public/adr/0002-lokaler-demoimport-und-pdf-vorschau.md`
- `src/backend/Betriebsplattform.Web/ApiAnwendung.cs`
- `src/backend/Betriebsplattform.Web/DemoDaten.cs`
- `src/backend/Betriebsplattform.Web/DemoImportbestand.cs`
- `src/frontend/DESIGN.md`
- `src/frontend/src/App.tsx`
- `src/frontend/src/Arbeitsraum.css`
- `src/frontend/src/Dokumente.tsx`
- `src/frontend/src/Icon.tsx`
- `src/frontend/src/Importvorschau.tsx`
- `src/frontend/src/Kontaktkarte.tsx`
- `src/frontend/src/Rechnungen.tsx`
- `src/frontend/src/Rechnungsdetails.tsx`
- `src/frontend/src/api.ts`
- `src/frontend/tests/app.test.mjs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/DemoImportTests.cs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/WebPrototypTests.cs`
- `tests/repository/test_public_layout.py`

Zusätzlich den bestehenden privaten operativen Plan fortgeschrieben und lokale Screenshotnachweise mit SHA-256 gesichert; keine Library-Synchronisierung. Bilder zeigen Rechnung, beide PDFs, Dateiauswahl, Mapping, Vorschau, Bericht und übernommene Rechnung. Sie bleiben außerhalb des öffentlichen Produktinventars.

### Verbleibende Produktgrenzen und Abnahme

Nur synthetische lokale Demo: feste XLSX-Vorlage mit einem Blatt und neun Spalten, keine beliebige Arbeitsmappe oder 111-Blatt-Migration, maximal 2 MiB je Datei, 20 Dateien und 5.000 übernommene Rechnungen. Keine Zahlungsübernahme oder produktive Buchung. Keine produktive Authentifizierung, Datenbank, dauerhaftes Audit/Idempotenz oder Firmenmigration. Backend-Neustart verliert Importdaten; Tab-Schließen kann Notizen verlieren. Keine automatische Designabnahme. Prüfen: Rechnungen 002 und 003, Kontakt-/Notizaktionen, Originalrechnung/Zahlungsbeleg und Vergrößern; Importe mit synthetischer Übungsdatei beziehungsweise bereits vorhandener Sitzung und SYNTH-RE-01. Menschliche Sicht-/Funktionsentscheidung bleibt ausstehend. [Architekturentscheidung](adr/0002-lokaler-demoimport-und-pdf-vorschau.md).

## Autonomer Aktenblock 18.10.2026 – tatsächlich geprüfter Abschluss

Der vorhandene Stand main/5f735de mit 21 bereits lokalen öffentlichen Änderungen wurde erhalten und vor der Arbeit bytegenau mit SHA-256 gesichert. Der freigegebene zusammenhängende Block wurde mit einem Agenten im bestehenden Arbeitsraum umgesetzt. Keine weiteren Pakete installiert, keine Datenbank/Schemaänderung, echte Firmendaten, externen Produktaktionen, Commits, Pushes oder Veröffentlichung. Die Library wurde nicht synchronisiert. Designabnahme bleibt ausdrücklich beim Auftraggeber.

### Umgesetztes Verhalten

- Sidebar Rechnungen öffnet auch aus derselben Detailroute die Übersicht; Suche/Filter/Sortierung bleiben beim direkten Rückweg erhalten. Eine Aktionsgruppe mit 38×38-px-Rahmen/18-px-Icons, zugänglichen Namen, Tooltips, Fokus und konsistentem Disabled-Zustand. E-Mail/Telefon verwenden validierte Werte; mehrere Werte sind auswählbar. Keine Nachricht versendet und kein Telefonhandler ausgelöst.
- Zentrale lokale Vorgangsakte für Rechnungen und allgemeine Vorgänge. Dokumente/Notizen/Korrespondenz mit Zählern, einem aktiven Panel, Tastaturtabs und begrenzten Scrollflächen; Auswahl öffnet Inhalt. Backend- und DOMtests verwenden 50 Notizen/80 Dokumente/100 Schreiben. Im Browser wurden mehrere echte synthetische Originale, zwei Notizen und Korrespondenzeinträge verwendet.
- Notizen mit Betreff, Inhalt, serverseitigem Demo-Autor, Erstellt-/Geändertzeit, Version und vollständigem lokalen Verlauf. Keine stille Überschreibung bei veralteter Version. Entfernen mit Bestätigung, Soft Delete und Wiederherstellen. Frühere Tab-Notizen bleiben lesbar. Korrespondenz kann Brief/E-Mail/Entwurf manuell dokumentieren und vorhandene Dokumente referenzieren; kein Versand/Mailabruf und kein zweiter Anhangsspeicher.
- Gemeinsamer Dokumentbestand für manuelle Auswahl, gezielten Drop und Scan-Ordner. Unveränderte Bytes, Hash, Dateiname, Eingangsweg, Zeit und Aktenverweise. Gleicher Inhalt erzeugt keine zweite Originalkopie. Technische Werte nur separat abrufbar. PDF, PNG/JPG, TXT, begrenztes DOCX/XLSX bis 2 MiB. Datei-/MIME-/Struktur-/ZIP-/XML-/Größenprüfung, keine Makro-/Formelausführung. Maximal 512 Dateien/64 MiB im Prozess, begrenzte Notiz-/Korrespondenzspeicher.
- Bekannte Originalrechnung und Zahlungsbeleg weiterhin getrennt und lesbar im nativen PDF-Viewer, vergrößern/schließen/Escape mit Fokus-Rückkehr. Bilder/TXT und DOCX-Textextrakt; XLSX als Anhang ausschließlich kontrollierter Download. Neue fremde PDFs konservativ geprüft und nicht inline ausgeführt. Browser-/Systemdruck verwenden den Originalbestand nur lesend; tatsächliche Hardwareausgabe nicht ausgeführt.
- Dateiauswahl und Drag & Drop nutzen dieselbe begrenzte Mehrfachauswahl mit Typ-/MIME-/Größenfehlern, Zielrückmeldung und ausdrücklicher Bestätigung. Nur tatsächliche Datei-Drags markieren die jeweilige Fläche. Ein ungezielter Datei-Drop wird abgefangen und nicht verarbeitet. Excel-Drop bleibt vom Dokumentanhang getrennt.
- Zentraler Posteingang: zunächst Zur Prüfung, manuelle Kategorie und ausdrückliche Zuordnung zu bestehender oder neuer Akte mit Firma. Im Browser wurde automatisch eingelesene synthetische Papierpost der allgemeinen Versicherungsakte zugeordnet und dort wiedergefunden.
- Kontaktvorschläge aus tatsächlich gelesenem, eindeutig beschriftetem Text. Ziel-Firma, bisheriger/neuer Wert und Quelldokument sind sichtbar. Übernehmen/Zusätzlich speichern/Ignorieren sind versioniert mit alter/neuer Information, Quelle, Benutzer und Zeit. Alte Werte bleiben erhalten; mehrere aktive Kontakte sind möglich. Im Browser wurden zusätzliche E-Mail und Telefonübernahme inklusive sofortiger Aktualisierung der Rechnungsaktionen geprüft; alle drei Entscheidungen laufen zusätzlich als Backendtests.
- Wiederverwendbarer Suchdienst über tatsächliche Firmen/Personen/Rechnungen/Vorgänge/Dokumente/Eingang. Mindestens zwei Zeichen, 250-ms-Debounce, Abbruch veralteter Anfragen, Pfeil hoch/runter/Enter/Escape, sichtbare Auswahl, Löschen, keine Treffer/Fehler. Der echte synthetische Treffer „Alli“ wurde im Browser per Tastatur geöffnet. Ressourcenlesepolitik und Arbeitsmodulaktivierung liegen serverseitig; keine produktive Authentifizierung/Autorisierung oder Lizenzverwaltung behauptet.

### Scanner, Textauswertung und Architekturgrenzen

Direkte Windows-Scannerintegration: Architektur und `IScannerAdapter` vorbereitet, keine Hardware/Companion-/Treiberinstallation und kein Fake-Scannen. Die Microsoft-Primärquellen und notwendige spätere Entscheidung WIA/TWAIN, Hardware, Signierung, Sitzungsrechte und IPC stehen in ADR 0003.

Scan-to-Folder: tatsächlich implementierter optionaler Polling-Adapter, in dieser Prüfung mit `--scan-demo` am eigenen `src/backend/private/scan-eingang` aktiv. Zwei stabile Beobachtungen, keine Reparse-Points/rekursiven Pfade, gemeinsamer Validator/Hash-Dublettenschutz, kein Löschen. Zwei synthetische Dateien wurden automatisch eingelesen, 0 abgewiesen, 0 wartend; eine im Browser zugeordnet. Der Prozess liest nach Neustart erneut ein, weil auch die Zielablage flüchtig ist. Kein physischer Scanner getestet.

TXT/DOCX-Text und begrenzte unkomprimierte PDF-Textoperationen sind deterministisch; keine KI oder OCR für Scanbilder. EML/MSG, allgemeine PDF-Auswertung, beliebige Office-Inhalte, automatische Kontakt-/Klassifikationsentscheidung, Outlook/Graph und produktive Modul-/Rechte-/Lizenzverwaltung bleiben bewusst offen. Der Formatfilter ist kein Malware-Scanner. Vor realen Daten zusätzlich Autorisierung, persistente Audit-/Schreibrechte, Aufbewahrung/Löschung, Restore, Betrieb und sichere Scanner-Vertrauensgrenze entscheiden. Alle neuen Akten-/Kontaktänderungen können beim Serverneustart verloren gehen; Dateien im Scan-Quellordner werden nicht gelöscht.

### Tatsächlich ausgeführte Prüfungen

| Prüfung | Ergebnis |
| --- | --- |
| `dotnet test src/backend/Betriebsplattform.slnx --no-restore` | 144 erfolgreich, 0 fehlgeschlagen, 0 übersprungen |
| `npm.cmd test` in `src/frontend` | 38 erfolgreich, 0 fehlgeschlagen |
| `python -m unittest discover -s tests/repository -p test_*.py` | 7 erfolgreich |
| `dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release` | erfolgreich, 0 Warnungen/0 Fehler |
| `npm.cmd run build` | TypeScript strict/Vite erfolgreich, 44 Module |
| Öffentliche Grenze/Dateiinventar/erkennbare Secrets/MD-Verweise | Repositorytests erfolgreich; drei geprüfte Microsoft-Zitate ausdrücklich im Linktest erfasst |

Der neue Navigationstest war zunächst rot; die Sidebar-Korrektur machte ihn grün. Neue Backendtests erforderten zuerst die korrekte optionale API-Signatur in der Fixture. Alte Frontendtests wurden auf die primäre Akte umgestellt, ohne Finanz-/Importprüfungen zu entfernen. Der neue Debounce-Test war wegen React/jsdom-Eingabesimulation zunächst rot; native Value-Setter plus Legacy-keyup korrigieren ausschließlich die Testeingabe. Im echten Browser bestätigter Farbkontrastfehler der Eintragsbuttons wurde mit bestehenden Tokens behoben. Ein zunächst vor dem PDF-Rendern aufgenommenes graues Vorschaufenster wurde durch eine sichtbar lesbare Aufnahme ersetzt. Kein unabhängiger Agentenreview oder vollständiges Securityaudit behauptet.

Realer Edge-Browser: Rechnung → Sidebar-Übersicht; Notiz erstellen/bearbeiten/Verlauf/Soft Delete/Wiederherstellen; native PDF-Vergrößerung und Escape/Fokus (3-px-Outline); PNG vollständig geladen (640×400), DOCX-Textextrakt/TXT und XLSX-Downloadhinweis; Kontaktentscheidung mit E-Mail-Auswahl/Telefonlink; Papierposteingang/Zuordnung/Versicherungsakte; Teilbegriffsuche/Pfeil/Enter; Korrespondenz erfassen und zentralen Dokumentverweis öffnen. Keine neuen Browserfehler im geprüften Tab.

XLSX-Regression: ausschließlich lokale synthetische Vorlage über denselben Backend-Endpunkt übertragen, anschließend in der echten UI über Bisherige Demoimporte geöffnet, Nord/ISD gemappt, Zeile 2 und Übernahme ausdrücklich bestätigt. Ergebnis 1 übernommen/0 übersprungen/1 fehlerhaft/1 zur Prüfung. SYNTH-RE-01 danach im regulären Bestand mit 100,25 EUR offen/0 bestätigt/keiner Zahlung geöffnet. Die als Dokument abgelegte XLSX löste davor keinen Import aus.

Reflow ohne horizontalen Seitenüberlauf: Rechnungsakte bei 1440/768/720/375 px; Posteingang und Import-Einstieg bei 1440/720/375 px. Dokumentliste tatsächlich maximal 160 px, weitere Listen maximal 260 px. Das ist kein Nachweis echten 200-%-Browserzooms und kein vollständiger Screenreader-/Accessibility-Audit.

**Offener Browsernachweis:** Der native Dateidialog wurde über die echte Schaltfläche angefordert. Die ChatGPT-Edge-Erweiterung blockiert das automatische Setzen lokaler Dateien wegen fehlendem „Allow access to file URLs“. Keine Erweiterungs-/Sicherheitseinstellung geändert. Daher kein vollständiger Browser-Dateiauswahl-/Datei-Drop-End-to-End-Nachweis und kein vorgetäuschter Drag-&-Drop-Browserscreenshot. Mehrfachauswahl/Drop einschließlich sichtbarem Dragzustand, falschem Typ, MIME, Größe und Bestätigung sind als DOMtests geprüft; echte Byteübertragung und Zuordnung zusätzlich als Backend-/HTTPtests. Manueller Datei-Drop/Dateiauswahl, echter 200-%-Zoom, Screenreader sowie Scanner-/Druckerhardware bleiben Abnahmepunkte.

### Screenshots und konkrete Sichtprüfung

Neun echte synthetische Edge-Aufnahmen unter `src/backend/private/pruefungen/aktenblock` mit SHA-256 in `screenshots.json`: `01-rechnungsdetail.jpg`, `02-notizen.jpg`, `03-pdf-vorschau.jpg`, `04-dokumente.jpg`, `05-posteingang.jpg`, `06-kontaktvorschlag.jpg`, `07-suche.jpg`, `08-excel-import.jpg`, `09-importbericht.jpg`. Ein Drag-&-Drop-Browserscreenshot fehlt aus dem oben dokumentierten Grund. Reflow-Messungen liegen daneben als JSON; keine Library-Synchronisierung.

Zur menschlichen Prüfung: Rechnungen → SYNTH-2026-002 (Dokumente/Notizen/Korrespondenz, Original/Zahlungsbeleg, offene Ansprechpartnerinformation); Posteingang → verbleibende synthetische Dateien/Kategorie/Zuordnung; Vorgänge → Betriebsversicherung 2026 mit zugeordneter Papierpost; Kontakte → ISD mit zusätzlichen Werten und Verlauf; globale Suche z. B. Alli/Nord/SYNTH; Importe → Bisherige Demoimporte → übernommener Importbericht, danach SYNTH-RE-01 im Bestand. Keine automatische UX-Abnahme.

### Lokale Dateien und Git

HEAD bleibt `5f735de`. 20 bereits getrackte Dateien geändert und 19 neue öffentliche Dateien untracked (einschließlich der zuvor vorhandenen Produktänderungen). Nichts gestaged; kein weiterer Commit oder Push. Private Planung, Vorher-Sicherung und Prüfarbeitsdateien bleiben ausgeschlossen. Die vollständige Dateiliste dieses uncommitteten Gesamtstands:

- `README.md`
- `docs/public/ARCHITEKTUR.md`
- `docs/public/MVP-01-NACHWEIS.md`
- `src/backend/Betriebsplattform.Web/ApiAnwendung.cs`
- `src/backend/Betriebsplattform.Web/ApiModelle.cs`
- `src/backend/Betriebsplattform.Web/DemoDaten.cs`
- `src/backend/Betriebsplattform.Web/DemoDokumente.cs`
- `src/frontend/DESIGN.md`
- `src/frontend/src/App.tsx`
- `src/frontend/src/Arbeitsraum.css`
- `src/frontend/src/Dokumente.tsx`
- `src/frontend/src/Icon.tsx`
- `src/frontend/src/Importvorschau.tsx`
- `src/frontend/src/Kontaktkarte.tsx`
- `src/frontend/src/Rechnungen.tsx`
- `src/frontend/src/Rechnungsdetails.tsx`
- `src/frontend/src/api.ts`
- `src/frontend/tests/app.test.mjs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/WebPrototypTests.cs`
- `tests/repository/test_public_layout.py`
- `docs/public/adr/0002-lokaler-demoimport-und-pdf-vorschau.md`
- `docs/public/adr/0003-zentrale-vorgangsakte-und-eingangsadapter.md`
- `src/backend/Betriebsplattform.Web/Akten/AktenEndpoints.cs`
- `src/backend/Betriebsplattform.Web/Akten/AktenSuche.cs`
- `src/backend/Betriebsplattform.Web/Akten/Aktenbestand.cs`
- `src/backend/Betriebsplattform.Web/Akten/Aktenmodell.cs`
- `src/backend/Betriebsplattform.Web/Akten/Dokumentpruefung.cs`
- `src/backend/Betriebsplattform.Web/Akten/Kontaktpflege.cs`
- `src/backend/Betriebsplattform.Web/Akten/Notizablage.cs`
- `src/backend/Betriebsplattform.Web/Akten/ScanOrdnerAdapter.cs`
- `src/backend/Betriebsplattform.Web/DemoImportbestand.cs`
- `src/frontend/src/Aktenansichten.tsx`
- `src/frontend/src/Aktenbereich.tsx`
- `src/frontend/src/Dateieingang.tsx`
- `src/frontend/src/Suche.tsx`
- `src/frontend/src/akte-api.ts`
- `src/frontend/tests/akte.test.mjs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/AktenTests.cs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/DemoImportTests.cs`


## Produktreife-Block: tatsächlicher Abschlussstand 18.10.2026

### Rekonstruktion und Erhalt des Arbeitsstands

HEAD bleibt 5f735de. Der vorhandene größere uncommittierte Arbeitsstand wurde erhalten und vor Erweiterung privat mit Dateiinventar/Hash gesichert. Bei der Fortsetzung belegten Quellcode und Tests bereits zentrale Akten/Notizen/Dokumente, Upload-/Dropkomponente, optionalen Scan-Ordner, Typeahead und begrenzten XLSX-Import. Diese Strecken wurden erweitert, nicht parallel neu aufgebaut. Die zunächst noch offene Browserprüfung und Dokumentation wurden fortgesetzt.

Zusätzlich implementiert: erfundenes zusammenhängendes Beispielszenario, eine zentral abschaltbare globale Kennzeichnung; stabile Unternehmens-/Personenreferenzen; zentrale Kontaktanlage/-bearbeitung mit Anschrift, Ansprechpartnern, Abteilung/Rolle, mehreren Telefon-/E-Mail-Werten und Hauptwerten, Dublettenbestätigung, Version/Verlauf/Archiv. Dokumententscheidungen erhalten frühere Werte; offene Vorschläge werden nach einer Kontaktbearbeitung gegen die aktuelle Version geprüft. Keine harte Kontaktlöschung.

Gemeinsame Kategorienverwaltung mit stabilen IDs, Beschreibung/Reihenfolge, einer Unterebene, Version und Archiv. Systemstatus können dort nicht umdefiniert werden. Sonstiges/Ungeklärt bleiben verfügbar. Backend-Verwaltungspolitik wird zusätzlich zur UI geprüft. Die feste lokale Beispielidentität ist keine produktive Anmeldung. Normale Mitarbeiteransichten enthalten keine Prüfdatenbuttons, JSON-Rohdaten, Hash-/Byte-/Pfadkarten. Diagnose ist expliziter Servermodus plus Diagnose.Lesen.

Sidebar führt in allen fünf Arbeitsbereichen zur Übersicht. Hashroute modelliert Bereich/Datensatz/Dokument; Browser Back/Forward funktioniert. Direkter Rückweg erhält vorhandene Listenfilter; ein dauerhafter bereichsübergreifender Workspace-Zustand ist nicht implementiert. Kontakte, Vorgänge und Posteingang haben skalierbare Listen/Pagination; Vorgänge Suche/Kategorie/Status/Sortierung, Eingang Suche/Status/Kategorie/Firma/Zeitraum/Kanal. Die Vorgangsspalte heißt Bearbeitungsstatus; nicht erfasste Aufgaben/Fristen/Ereignisse werden nicht erfunden.

Notizen verwenden klare Archivierungsbeschriftung, Bestätigung, ausgeblendete archivierte Einträge, Wiederherstellung und Verlauf. Korrespondenz unterstützt sieben Büroarten und mehrere durchsuchbar ausgewählte Dokumentverweise aus der vollständigen aktuellen Akte. Upload im Dialog verwendet denselben Eingang; erfolgreich hinzugefügte Dokumente sind unmittelbar ausgewählt. Kein Versand und keine Dateikopie. Ein begrenzter Aktenbereich bleibt aktiv, die Listen besitzen eigene Scrollflächen.

### Tatsächlich gefundene und behobene Fehler

- Die reale Browseranlage zeigte: erfolgreiche POST-Einzelantworten von Kontakt/Kategorie wurden irrtümlich als GET-Liste validiert. Zwei zusätzliche rote Regressionen reproduzierten den offenen Dialog; gemeinsame Antwortprüfung nach Operation korrigiert, beide anschließend grün und Anlage im Browser erfolgreich.
- Kontaktbearbeitung blendete zusätzliche Anschriften aus; weitere Kontaktarten bleiben erhalten. Offene Dokumentvorschläge erhalten aktuelle Kontaktversion und Vergleichswerte. Primärwahl ersetzt keinen alten Telefon-/Mailwert ohne Verlauf.
- Unterkategorieanlage und Kategorienreihenfolge korrigiert: keine irrtümliche Wurzelknotenprüfung bei neuer Kategorie; Kinder nach dem Elternknoten, maximal eine Unterebene.
- Kollidierende React-Keys von Vorschau/Zuordnungsformular getrennt. Letzter Frontendtestlauf ohne diese Warnung; sichtbarer Browser ohne Warnungen/Fehler.
- Native Formularbuttons im eingebetteten Dateieingang lösen keinen versehentlichen Korrespondenz-Submit aus. Radio-/Checkboxgrößen, Fokus auf erstes Eingabefeld, Kontaktwertbeschriftungen und Ladezustände verbessert.
- Übergröße wird früh anhand Content-Length abgewiesen. Der HTTP-Grenztest verwendet Expect: 100-continue, um die 413-Antwort statt eines betriebssystemabhängigen Abbruchs beim parallelen Senden zu prüfen; zeilenweise/gestreamte Prüfung und 2-MiB-Limit bleiben bestehen.

### Frisch ausgeführte Quality-Gates

| Prüfung | Tatsächlich ausgeführter Befehl | Ergebnis |
|---|---|---|
| Backend vollständig | dotnet test src/backend/Betriebsplattform.slnx --no-restore | 160 erfolgreich, 0 Fehler/übersprungen |
| Frontend vollständig | npm.cmd test (src/frontend) | 46 erfolgreich, 0 Fehler/übersprungen |
| Repository | python -B -m unittest discover -s tests/repository -v | 7 erfolgreich |
| Backend Release | dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release | 0 Fehler, 0 Warnungen |
| Frontend Build | npm.cmd run build (src/frontend) | TypeScript strict + Vite erfolgreich, 49 Module |
| Diff | git diff --check | erfolgreich |
| Fachkern/Importkern/Abhängigkeiten | git diff --quiet HEAD für beide Kernverzeichnisse sowie package.json/package-lock.json | Exitcode 0, unverändert |

Tests umfassen bestehende Rechnungs-/Teilzahlungs-/Importregeln, Datei-/Office-/PDF-Negativfälle, Scanner-Idempotenz, Notizversionen/Archiv, 50 Notizen/80 Dokumente/100 Schreiben, Kontaktentscheidung/-version/-historie/-archiv/-dubletten/-hauptwerte, Kategorien/Fallbacks/Systemstatus, serverseitige Verwaltungsverweigerung, explizite Diagnose, Navigation, Typeahead mit Debounce/Tastatur, Mehrfachanhänge und Upload direkt im Dialog. Das ist ein fokussierter Single-Agent-Review mit tatsächlichen Tests, kein unabhängiges Vollaudit oder vollständiger Produktions-/Barrierefreiheitsnachweis.

### Sichtbare Browserprüfung und laufender Beispieldatenbestand

Reale lokale Edge-Anwendung geprüft. PDF lesbar im eigenen Arbeitsraum und vergrößert; Escape schließt den Dialog und erhält dieselbe Rechnung. Vier Aktionsrahmen jeweils rund 38 × 38 CSS-px und Icons rund 18 px; Fokuswechsel zur Kontaktinformation beobachtet. Firma angelegt, Ansprechpartner zugeordnet, Kontaktanschrift und zusätzliche Hauptnummer gespeichert; vorheriger Wert im Verlauf erhalten. Kategorie tatsächlich angelegt und in der Eingangszuordnung verfügbar. Eingangsdokument ohne Kopie zu neuem Vorgang mit gewählter Firma/Kategorie zugeordnet. Korrespondenz mit zwei aktuellen Dokumentverweisen tatsächlich gespeichert. Übersicht/Detail, Sidebar sowie Browser Back/Forward geprüft. Autocomplete mit Vers, Pfeiltaste und Enter öffnet den gefundenen Datensatz/das Dokument.

Die vorlagenkonforme XLSX wurde als echte lokale Datei über den vorhandenen HTTP-Eingang übertragen. Danach im Browser Firmen zugeordnet, gültige Zeile ausgewählt, Übernahme bestätigt und Bericht geöffnet: 1 übernommen, 1 Fehler, 1 Zahlung zur Prüfung. RE-2026-1063 ist mit 100,25 EUR offen/0 bestätigt im regulären Bestand auffindbar. Kein Zahlungseingang aus Excel bestätigt. Nach dem abschließenden Build/Neustart wurden dieselben ausschließlich erfundenen Abnahmebeispiele über die tatsächlichen Endpunkte wieder angelegt: vier Rechnungen, insgesamt 1.010,25 EUR gemischter offener Gesamtbetrag; eigener zusätzlicher Kontakt mit Ansprechpartner, Kategorie Objektunterlagen und zugehöriger allgemeiner Vorgang. Die Oberfläche verwendet genau diesen In-Memory-Bestand.

Desktop und schmalere Reflow-Prüfung ohne horizontalen Seitenüberlauf: beobachtet innerWidth 1600/scrollWidth 1583 und innerWidth 800/scrollWidth 783. Temporäre Viewportvorgabe anschließend zurückgesetzt; dies ist ausdrücklich kein Nachweis für echten 200-%-Browserzoom. Letzte Browserkonsole ohne Warnungen/Fehler.

Screenshots lokal und von Git ausgeschlossen: Rechnungsübersicht/-detail, geöffnete PDF, Kontaktübersicht/-editor/-verlauf, Vorgangsübersicht, Versicherungsakte/Dokumentbestand, Kontaktänderungsvorschlag, Notizen, Korrespondenzdialog mit zwei Anhängen, Kategorienverwaltung, Posteingang/-zuordnung, geöffnetes Autocomplete, Importübersicht/-mapping/-vorschau/-bericht und Reflow. Die private Screenshotübersicht enthält die Einzelbilder und ihren jeweiligen Prüfkontext. Keine automatische Designabnahme.

### Bewusst offene Grenzen und Abnahme

- Native Browser-Dateiauswahl und echtes Datei-Drag-&-Drop bleiben manuell durch den Auftraggeber nachzuweisen. Bestehende Edge-Erweiterungs-/Dateizugriffsrechte wurden nicht verändert. Komponenten-/HTTP-Tests belegen Auswahl, mehrere Dateien, Drop, Validierung und echten Upload; sie ersetzen diesen nativen End-to-End-Nachweis nicht.
- Echter 200-%-Zoom, Screenreader, weitere Browser/Geräte, OS-Mail-/Telefonieübergabe und Papier-/Systemdruck bleiben manuell offen. Druckfunktion/CSS und unveränderte Fachwerte sind geprüft; kein tatsächlicher Papierdruck behauptet.
- Scan-to-Folder ist implementiert und regressionsgetestet, wird optional mit --scan-demo aktiviert; der abschließend laufende Host ist ohne diesen Schalter gestartet. Direkter Scanner ist vorbereitet, keine Hardware angebunden/keine Treiber installiert.
- PDF-Text nur deterministisch begrenzt, DOCX nur Text, TXT direkt; keine OCR für Bilder/Scans. Eigene bekannte PDFs/Bilder/Text als Vorschau, andere validierte Office-Dateien und fremde PDFs kontrollierter Download. Keine EML-/MSG-Auswertung, kein Outlook-/Graph-Client.
- XLSX-Import bleibt auf ein Vorlagenblatt ISD-Testdaten/neun Spalten begrenzt. Historische Quellwerte bleiben unverändert. Keine beliebige Excel-Migration, produktive Buchung oder Datenbank aktiviert.
- Produktive Identität, Rollen/Mandanten-/Ressourcenrechte einschließlich aller abgeleiteten Metadaten, dauerhafte Persistenz/Audit/Backup mit Restore, Aufbewahrung und Härtung für echte Daten bleiben gesonderte Gates. Namen/Dokumente sind erfundene Beispiele, keine Unternehmens-/Personenoriginale.
- Git: kein Commit, kein Push, kein Remotezugriff, kein Release. Index leer; 53 öffentliche Dateien seit HEAD geändert/neu (einschließlich der vorherigen uncommittierten Blöcke). Private Anweisungen, Sicherungen, Dateien und Screenshots bleiben ausgeschlossen. Kein Nachweis einer ChatGPT-Library-Synchronisierung.

### Öffentliche Dateien im uncommittierten Arbeitsstand

- `README.md`
- `docs/public/ARCHITEKTUR.md`
- `docs/public/MVP-01-NACHWEIS.md`
- `src/backend/Betriebsplattform.Web/ApiAnwendung.cs`
- `src/backend/Betriebsplattform.Web/ApiModelle.cs`
- `src/backend/Betriebsplattform.Web/DemoArbeitsmappe.cs`
- `src/backend/Betriebsplattform.Web/DemoDaten.cs`
- `src/backend/Betriebsplattform.Web/DemoDokumente.cs`
- `src/frontend/DESIGN.md`
- `src/frontend/src/App.tsx`
- `src/frontend/src/Arbeitsraum.css`
- `src/frontend/src/Dokumente.tsx`
- `src/frontend/src/Icon.tsx`
- `src/frontend/src/Importvorschau.tsx`
- `src/frontend/src/Kontaktkarte.tsx`
- `src/frontend/src/Rechnungen.tsx`
- `src/frontend/src/Rechnungsdetails.tsx`
- `src/frontend/src/api.ts`
- `src/frontend/src/darstellung.ts`
- `src/frontend/tests/app.test.mjs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/WebPrototypTests.cs`
- `tests/repository/test_public_layout.py`
- `docs/public/adr/0002-lokaler-demoimport-und-pdf-vorschau.md`
- `docs/public/adr/0003-zentrale-vorgangsakte-und-eingangsadapter.md`
- `docs/public/adr/0004-kontakte-kategorien-und-arbeitsumgebung.md`
- `src/backend/Betriebsplattform.Web/Akten/AktenEndpoints.cs`
- `src/backend/Betriebsplattform.Web/Akten/AktenSuche.cs`
- `src/backend/Betriebsplattform.Web/Akten/Aktenbestand.cs`
- `src/backend/Betriebsplattform.Web/Akten/Aktenmodell.cs`
- `src/backend/Betriebsplattform.Web/Akten/Arbeitsumgebung.cs`
- `src/backend/Betriebsplattform.Web/Akten/Beispielakten.cs`
- `src/backend/Betriebsplattform.Web/Akten/Dokumentpruefung.cs`
- `src/backend/Betriebsplattform.Web/Akten/Kategorienverwaltung.cs`
- `src/backend/Betriebsplattform.Web/Akten/Kontaktpflege.cs`
- `src/backend/Betriebsplattform.Web/Akten/Kontaktverwaltung.cs`
- `src/backend/Betriebsplattform.Web/Akten/Notizablage.cs`
- `src/backend/Betriebsplattform.Web/Akten/ScanOrdnerAdapter.cs`
- `src/backend/Betriebsplattform.Web/DemoImportbestand.cs`
- `src/frontend/src/Aktenansichten.tsx`
- `src/frontend/src/Aktenbereich.tsx`
- `src/frontend/src/Arbeitskontext.tsx`
- `src/frontend/src/Buero.tsx`
- `src/frontend/src/Dateieingang.tsx`
- `src/frontend/src/Dokumentauswahl.tsx`
- `src/frontend/src/Kategorien.tsx`
- `src/frontend/src/Kontakte.tsx`
- `src/frontend/src/Suche.tsx`
- `src/frontend/src/akte-api.ts`
- `src/frontend/tests/akte.test.mjs`
- `src/frontend/tests/produktreife.test.mjs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/AktenTests.cs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/DemoImportTests.cs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/ProduktreifeTests.cs`


## Enger UX-/Fach-Delta-Nachweis nach Sichtprüfung (18.10.2026)

Keine neue Produktreife-Runde: bestehende Komponenten angepasst. Getrennte serverseitig validierte Vorgangsstatus, kleine direkte Vorgangsanlage und versionierter Statuswechsel; Kategorien atomar innerhalb der Geschwister hoch/runter. Forderungen/Verbindlichkeiten aus vorhandenen Kernbeträgen getrennt projiziert; bei nicht vollständig bestimmter eigener Richtung keine irreführenden Teilsummen. Keine Fälligkeitsregel oder Änderung des Finanz-/Importkerns.

Betroffene Ansichten: Rechnungskennzahlen/natürliche Anzahl/Aussteller-Empfänger; Vorgangsübersicht/-kopf; Eingangsfilter und menschliche Dokumenttitel/Kanalprojektion; Kontakte ohne Aktiv-Wiederholung; Kategorien ohne Sortierzahlen, mit Einrückung und kompakten Aktionen; gebündelte Aktenhinweise und Aktionshierarchie; Korrespondenzbezeichnung; PDF-Breitenanpassung. Ursprüngliche Dateiherkunft und Dokumentbytes bleiben erhalten. Telefon-Untertypen mangels Modellinformationen bewusst nicht erfunden.

Gezielt erfolgreich: zunächst 3 neue Backendtests, danach 54 Tests aus ProduktreifeTests/AktenTests/WebPrototypTests/DemoImportTests wegen der tatsächlich gekoppelten Anzeige-/HTTP-/Importprojektion. Nach kleiner Quellenbeschriftung nur 23 Akten-/Delta-Tests erneut. Frontend zuerst 5 neue Delta-Tests, danach 25 per Namensfilter ausgewählte Tests der geänderten Ansichten und gekoppelten Akten-/Dialog-/PDF-Komponenten; zwei alte UI-Erwartungen an geänderte Beschriftung/Sortierung angepasst. Diese Zahlen sind überlappende Prüfungen, keine addierbaren Gesamtsuiten. Finanzkern-, XLSX-Leser-/Sicherheitsklassen, übrige Frontendtests und Repositorytests nicht erneut ausgeführt: deren Code, Abhängigkeiten und öffentliches Dateiinventar unverändert. Keine vollständige Regression behauptet.

Webprojekt Releasebuild (mit unveränderten Kernreferenzen) und Frontend strict/Vite erfolgreich; Git-Diff-Prüfung erfolgreich. Reale Edge-Prüfung nur betroffener Ansichten: Vorgang angelegt, Status unabhängig von Zahlungen gewechselt, Kategorie tatsächlich verschoben, weitere Filter eingeklappt, Hinweise über Tabs sichtbar, Quellen verständlich, PDF mit FitH und Escape-Rückweg. Acht gezielte private Screenshots, keine erneute Sammlung der21 Baselinebilder. Konsole ohne Fehler/Warnungen. Echte 200%-Zoom-/Screenreader-/native Datei- und OS-Handlerprüfungen aus der Baseline bleiben offen; keine automatische UX-Abnahme. Kein Commit/Push/Release.


### UX-/Datenqualitäts-Delta – 18.10.2026

Gezielt erweitert: reservierter Suchicon-Platz, „Zahlung bestätigt“, sichtbare Aktenbegriffe, Kategorien unter Einstellungen mit Suche/Zeilenbearbeitung/nativer Sortierung und Tastaturalternative, Kontaktanschrift/Telefonarten/Website/bevorzugte Angaben, gemeinsame Pflichtfeldkennzeichnung. Zentrale deterministische Vergleichsschlüssel für Kontakte, Akten, Suche und Firmenhinweise im begrenzten Import; Warnung und bewusste separate Anlage statt automatischem Merge. Rechnungs-/Quellkennungs- und Dateihashschutz bleiben erhalten.

Posteingang zeigt prüfbedürftige beziehungsweise verarbeitete Eingänge aus demselben Dokumentbestand. Vorab vorhandene Aktenbelege ohne Eingangsnachweis werden nicht als neue Eingänge ausgegeben. Manuelle Zuordnung und identische Wiederholungseingänge besitzen Aktivität, Zeitpunkt und Kanal; Originale bleiben erhalten. Kurzfassung/Handlungsbedarf sind ausdrücklich redaktionelle Beispielangaben oder als nicht zuverlässig ausgewertet gekennzeichnet. Keine neue OCR/KI, Fristbestätigung oder automatische Finanzwirkung.

Dokumentgrenze server-/clientseitig 20.000.000 Byte (20 MB). Office-ZIP/XML-Grenzen bleiben getrennt bei 2 MiB pro Teil und 16 MiB entpackt, bestehende Format-/Aktivinhaltprüfungen bleiben erhalten. Text/CSV: UTF-8, höchstens 64.000 Zeichen; CSV nur als Download, keine Formelauswertung. PDF, PNG/JPEG, DOCX/XLSX und TXT weiterhin innerhalb der konservativen Formatprüfung. TIFF, alte XLS-Dateien und Makroformate nicht freigegeben. Strukturierter XLSX-Import behält seine eigene 2-MiB-Grenze (UI: 2,1 MB) und bisherigen Vorlagen-/Firmenumfang.

Tatsächlich ausgeführt: 68 Backendtests aus ProduktreifeTests, AktenTests, DemoImportTests und WebPrototypTests; 58 Frontendtests aus den drei direkt gekoppelten Testdateien, nach letzter reiner Vergleichsanzeige-/Zustandskorrektur nochmals 10 relevante Eingangs-/Navigations-/Datenqualitätsfälle. Web-Release-Build und Frontend-Build erfolgreich. Red-Nachweis der neuen Backendfälle zunächst fehlende Implementierung; zwischenzeitliche UI-Assertions/Prüfkörperfehler korrigiert, keine offenen Testfehler. Keine erneute Vollregression unveränderter Finanzkern-/Importbibliothek oder Repositorytests: keine Änderung dieser Quellen, Abhängigkeiten oder Dateiinventare.

Realer Browser: Suchabstand (44 px Innenabstand, Icon endet 32 px hinter Feldbeginn), Rechnung/Zahlungsstatus/PDF-Ladeabschluss, Kontaktformular und echte Dublettenwarnung, Wechsel zum bestehenden Kontakt ohne stehengebliebenen Warntext, Aktenwarnung und Navigation, Eingangssichten und echte Zuordnung mit sichtbarer Aktivität, Kategorieneditor/Escape, Typeahead mit Pfeil/Enter. Bei nominell 720 px Ansichtsprüfung kein Seitenüberlauf und kein innerer Kontaktformular-Überlauf; dies ist kein echter 200-%-Zoomnachweis. Acht gezielte lokale Screenshots, keine automatische UX-Abnahme.

Offen: manuelle native Dateiauswahl/Datei- und Kategorien-Drag-&-Drop-Endprüfung im Browser, echter Zoom/Screenreader; inhaltsbasierte Scan-Dubletten, automatische Extraktion/Klassifikation/Zuordnung und nachträglicher Zuordnungswechsel. Bestehende begrenzte In-Memory-Demo bleibt ohne produktive Persistenz/Auth. Wiederholtes Akten-/Kontaktladen pro Suchtreffer entfernt; vollständige Metadatenlisten und In-Memory-Auswertung bleiben Skalierungsgrenzen. Serverseitige Pagination/Filter-/Sortierabfragen und Lastbudgets vor großen Beständen nötig, hier kein Produktionsumbau. Keine neue Bibliothek, kein Commit/Push/Release.


### Folge-Delta: strukturierte Kontakte und ruhige Aktenansichten (18.10.2026)
Bestehende Kontakte additiv um Vorname/Nachname/Hausnummer ergänzt; Firmenname bleibt zusammenhängend, alte Freitextanschriften bleiben ohne geratenes Zerlegen erhalten. Firma: Name/Telefon/E-Mail verpflichtend; Person: Vorname/Nachname/Telefon. UI-Stern/required/aria-required und Servervalidierung. Zentrale Dublettenhinweise berücksichtigen strukturierte Personennamen, Organisation und Anschrift; bewusste getrennte Anlage bleibt möglich. Unvollständige Ansprechpartner aus Dokumenten müssen zuerst als zentraler Personenkontakt gepflegt werden. Bei expliziter Übernahme einer neuen unstrukturierten Anschrift keine veralteten strukturierten Werte behalten; vorherige Anschrift bleibt im Verlauf.

Vorschau standardmäßig geschlossen, Inhalt erst nach Auswahl; Schließen und erneutes Öffnen desselben Zahlungsbelegs im Browser geprüft. Richtungsabhängige Zahlungsbezeichnungen aus vorhandener Perspektive, keine neue Geldlogik. Kürzere zentrale Uploadtexte, unveränderte 20-MB-Dokumentgrenze und separate XLSX-Importgrenze. Eingangsschnellsichten bleiben primär, Statusfilter sekundär, suchbare Kategorieauswahl maximal240px scrollbar; „Akte zugeordnet“, Eingangsgruppen mit Abstand, konkreter Rückweg aus Akte zum Eingang. Aktenstatus zeigt Speichern/Erfolg/Fehler, zehn Sekunden versioniertes Rückgängig. Profilbereich ausdrücklich lokal, Firmenverweis dezent, Ansprechpartner mit Kontaktdaten, kompakte Anhänge. Notizfehler: neu gespeicherte Notiz wird ausgewählt; ausgewählter Listenpunkt wiederholt nicht den vollständigen Text. Kein doppelter Datensatz erzeugt.

Nachweise: 56 Backendtests ausschließlich ProduktreifeTests/AktenTests; 67 Frontendtests der drei direkt gekoppelten Dateien app/akte/produktreife. Nach letzter reiner Iconänderung neun Folgedelta-Fälle frisch wiederholt. Backend-Releasebuild und Frontendbuild erfolgreich. Keine komplette Backend-/Repository-/Import-Fachregression: Finanzkern, XLSX-Verarbeitung, Dateivalidierung und Repositorystruktur wurden hier nicht geändert. Diff-Prüfung der Änderungen erfolgreich.

Realer Edge-Browser auf127.0.0.1:5180: Rechnung mit geschlossener Vorschau/Beleg öffnen-schließen-wiederöffnen; Notiz erstellen/gezielt auswählen; Eingangssichten/Kategoriesuche/Detail; Eingang→Akte→genauer Eingang; Status speichern und rückgängig; Firma samt strukturierter Anschrift anlegen, Person mit Firma/Funktion/Mobil/E-Mail anlegen und bearbeiten, Firmenbearbeitung erhält Hausnummer, Ansprechpartner/Firmenverweis; Korrespondenz/kompakte Anhänge/Word-Textinhalt; Archivierungsdialog und Abbrechen. Fünf lokale Screenshots unter src/backend/private/pruefungen/ux-stammdaten. Kein horizontaler Seitenüberlauf in der beobachteten Desktopansicht, keine Browser-Konsolenfehler. Dies ersetzt keinen echten Zoom-/Screenreader-/nativen Datei-Drop-Endtest. Dateiauswahl/Datei-Drag-and-Drop bleiben manuell zu prüfen; vorhandene Edge-Berechtigungen nicht verändert. Keine neue Bibliothek/OCR/KI/Makrofreigabe, keine neuen Formate/Finanzregeln/DB-Migration, keine automatische UX-Abnahme.


## MVP-01.5 – Fachworkflow, Suche, Datenqualität und Security (18.–19.10.2026)

Ausgangspunkt6eba930, beauftragter zusammenhängender Block, keine neue Anwendung/Library, keine produktive Datenbank, kein Commit/Push/Release. Zuordnung der Auftragspunkte:

| Punkte | Tatsächlicher Stand |
|---|---|
|1–4|Rechnungsnummer, Datum, Aussteller, Empfänger, Betrag und Zahlungsstatus per Kopf/Enter auf- und absteigend; Forderungen/Verbindlichkeiten über bestehende Perspektive filterbar. Betrag ausschließlich für Darstellung sortiert, kein Saldo neu berechnet. Offen amber mit Rahmen; Überfällig rot vorbereitet. Beteiligtenauswahl bei belegter Perspektive entfernt, Auswahl mehrerer vorhandener Mail-/Telefonziele erhalten; bei ungeklärter Perspektive keine falsche automatische Partei.|
|5–6|Notizen intern; Korrespondenz mit erforderlicher Richtung, externer Kontaktpartei und Kommunikationszeitpunkt. Entwurf ist unversandt, keine Mailintegration. Notizarchivierung/Bearbeiten kompakt sekundär, kein Hartlöschen.|
|7–11|Globale Suche mit mehreren Wörtern ohne Reihenfolgezwang, Teilwörtern, Umlaut-/Schreibweisennormalisierung und begrenztem Ein-Zeichen-Fehlerabstand. Explizite Typen priorisiert, maximal10 Ziele, Rechte/Module berücksichtigt. Akten-Suchabfrage serverseitig über aktuelle Dokumente/Kategorien/Absender, zentrale Kontakte und Rechnungsnummer/-beteiligte; begrenzte Suchantwort statt Dokumentinhalte zu laden. Kontakte nach Nachname/Vorname bzw. Firmenname und per Kopf sortierbar, lokaler toleranter Filter. Keine automatische Dubletten-Zusammenführung.|
|12–14|Nur interne Adresswerte als Vorschläge; kein externer Dienst. Kontaktstamm auf64rem und flexible Gruppen begrenzt, Tabellenkopf über vorhandenen Nebenschrifttoken lesbarer. Firmenarbeit mit Akten-, Rechnungs-, Dokument-, internen Notiz- und Korrespondenzverweisen, separater aktiver Bereich und20 Ergebnisse je Seite. Ansprechpartner bleiben zentral.|
|15–17|Direkte Aktenablage bleibt möglich; Posteingang als Schleuse erklärt. Kategoriefilter maximal19rem/200px Scrollfläche mit Suche. Kontrollierte Umzuordnung aus Eingang oder ausgewähltem Aktendokument: gleiche ID/Originalbytes, Quell-/Zielprüfung, erwartete Version, Verlauf mit alter/neuer Akte/Actor/Zeit und sichtbarer Speicherbestätigung. Wiederholte/veraltete/falsche Zuordnung kontrolliert abgelehnt.|
|18–20|Erfassungszeit und Bearbeitungszeit getrennt: neueste tatsächliche Aktivität statt unverändertem Eingangsdatum für „Zuletzt bearbeitet“. Beispielkontakte hatten keinen gesetzten Änderungszeitpunkt; nun bekannter Erstellungszeitpunkt als initialer Änderungszeitpunkt. UTC/ISO zur Browserzeitzone, keine manuelle Stundenkorrektur. Eingang, Absender, Kategorie, Prozessstatus und letzte Bearbeitung sortierbar.|
|21–25|Akten-Neu kontrolliert auf Offen abgebildet; übrige Bearbeitungsstatus erhalten, separate Posteingangsstatus unverändert. Aktenbetreff/Kategorie/Status/letzte Bearbeitung sortierbar. Hauptkategorie verständlich benannt; Kategorienbaum mit Einzelaufklappen und Alle-Unterkategorien-Checkbox, kompakte zwei Spalten, zeilennahes Menü.|
|26–28|Zusätzliche Negativtests für leere/überlange/ungeeignete Namen und Kontaktwerte, zulässige Unicode-Namen, fehlende Kommunikationssemantik, ungültige Ziele/Versionen, Hierarchiekreise/Archivierung, Suchgrenzen/Rechte, XSS-Textdarstellung, JSON/Content-Type/Größe/Overposting/Origin/Aktionsrechte, Traversal-/Makro-/Dateitypablehnung. Kein externer Angriffstest.|
|29–33|Bestehende Dateien/Komponenten/Nachweise weiterverwendet, keine neue Prüfmappe. Ein einzelner Screenshot im vorhandenen lokalen Nachweis nur für den neuen Umzuordnungs-Verlauf. Fach-/Security-/Persistenzgrenzen hier und in bestehender Architektur festgehalten; keine automatische Abnahme.|

Technisch nicht vorgetäuscht: Rechnungsfälligkeit und ein eigenständiger Rechnungs-Bearbeitungszeitpunkt sind im aktuellen Rechnungsmodell nicht vorhanden; daher keine entsprechenden Sortierspalten, keine Überfällig-Berechnung und keine Ableitung aus Dokumentänderungen. Aufgaben-/Fristmodell ist kein Teil der Firmenübersicht. Keine produktive Mail-, OCR-/KI-, Adress- oder Authentifizierungsintegration.

Tatsächlich ausgeführte Prüfungen: zuerst gezielte neue Fälle (roter Compiler-Ausgangspunkt für noch fehlenden Such-/Umzuordnungsservice, danach grün); einmal gesamte bestehende Backendregression200/200 erfolgreich und Frontendregression73 Fälle (71 sofort erfolgreich; zwei alte Erwartungen an den Tabellenkopf und sofortige lokale Aktenfilterung anschließend korrekt angepasst und gezielt erfolgreich geprüft). Danach ausschließlich gekoppelte Nachprüfungen: zuletzt28 Backendfälle und13 Frontendfälle erfolgreich, einschließlich neuer Fälle und Browserkorrekturen. Die neu hinzugekommenen Fälle wurden gezielt ausgeführt; keine zweite vollständige Regression behauptet. Beide abschließenden Builds erfolgreich. Repositorytests nicht wiederholt, weil Dateiinventar/Repositorylogik unverändert blieben. Diff-Prüfung erfolgreich.

Browserprüfung des realen finalen lokalen Builds: Rechnungsköpfe mit Enter zweimal sortiert, Forderung/Verbindlichkeit gefiltert, Amberfarbe geprüft, unnötige Partei-Auswahl bei bekannter Perspektive entfernt; „Rechnung Joris“ und umgekehrte Wortreihenfolge mit echten typisierten Treffern/Tastaturnavigation; Kontaktsortierung und „Kiselanker“; Firmen-Akten-/Dokumentverweise; Eingang zugeordnet und anschließend ohne Kopie in andere Akte verschoben, alte/neue Akte und Speicherfeedback geprüft, Rückweg und beziehungsbasierte Akten-Suche; Status mit Rückgängig; Kategorien einzeln/alle auf-/zugeklappt und neue Hauptkategorie tatsächlich gespeichert. Korrespondenz im Browser erfolgreich mit externer Partei, Richtung und lokalem Zeitpunkt gespeichert. Hier wurde ein realer zusätzlicher Notiz-Versionsparameter reproduziert, durch die strenge JSON-Prüfung abgewiesen und anschließend im Frontend korrekt entfernt und regressionsgetestet. Keine Konsolenfehler im abschließenden Lauf. Native Datei-Drag-and-Drop/Scanner/echterZoom/Screenreader wurden nicht erneut geprüft.

Fokussierter Security-Check (kein vollständiges Audit): bestehende Format-/Inhalts-/Pfadprüfung, React-Encoding, sichere ID-Verweise, CSP/nosniff, Loopbackbindung, eingeschränkte Origin/CSRF-Marker und modul-/ressourcenabhängige APIs geprüft. Geschlossen: mutierende Notiz-/Korrespondenz-/Zuordnungs-/Uploadaktionen prüften zuvor teils nur Leserechte; nun zusätzlich vorhandenes Verwaltungsrecht. Quell-/Zielrechte bei Umzuordnung einschließlich Firmenkontext zentral geprüft; Dokumentzähler im Aktenresultat auf lesbare Dokumente beschränkt. JSON128KiB/16 Ebenen/unbekannte Felder abweisen; falscher Content-Type explizit415 statt Fallback404. Fehlerantworten ohne Stacktrace/Pfade, umfangreiche Eingaben verständlich melden; Request-URL-Informationslogging unterdrückt. Origin5173 nur in Development, keine zusätzliche Netzwerkfreigabe. Unabhängiges Audit/echte Authentisierung/Mandantenschutz, dauerhafte revisionssichere Historie, Virenscanner, Restore und Betrieb bleiben Pilot-Gates.

Dependency-Prüfung19.10.2026: `dotnet list … package --include-transitive --vulnerable --no-restore --format json` erfolgreich ohne gemeldete verwundbare Pakete. `npm audit --json` meldet1 High-Befund in der vorhandenen reinen Entwicklungsabhängigkeit source-map-js1.2.1 ([Advisory](https://github.com/advisories/GHSA-68fv-2mgg-jv7q), Patch1.2.2). Zusätzlich aktuelles primäres [Vite-Advisory](https://github.com/vitejs/vite/security/advisories/GHSA-rq7h-c2jc-7f22) geprüft:7.3.6 im Versionsbereich, Patch7.3.7; angegebener Angriff setzt u.a. Nicht-Windows-Devserver voraus. Aktueller Betrieb: Windows, ausgelieferter ASP.NET-Build, kein Vite-Netzwerkserver. React-Server-Components-Advisory betrifft hier nicht installierte react-server-dom-Pakete, keine entsprechende Serverkomponente vorhanden. Keine pauschale Abhängigkeits-Sicherheitsgarantie. Gepatchte Pakete nicht im lokalen Cache; separate Freigabe für gezielten Download angefragt, bisher keine Antwort. Deshalb kein Update/Download/Install, kein geänderter Lockfile; Dependency-Gate bleibt ausdrücklich offen.

Vor PostgreSQL vorbereitet: stabile Ressourcen-IDs, getrennte Fachstatus, erwartete Versionen, explizite Dokumentbeziehung/-historie, begrenzte Such-/Firmen-Readmodelantworten und serverseitige Abfrageparameter. Nicht implementiert: Schema/Migration, produktive Persistenz, echte Benutzer-/Mandantenautorisierung, indexierte DB-Abfragen/Lastnachweis und transaktionales dauerhaftes Audit. Diese offenen Punkte werden nicht durch In-Memory-Tests ersetzt.

Geänderte Dateien dieses Meilensteins (26, ausschließlich bestehende Dateien):

- `docs/public/ARCHITEKTUR.md`
- `docs/public/MVP-01-NACHWEIS.md`
- `src/backend/Betriebsplattform.Web/Akten/AktenEndpoints.cs`
- `src/backend/Betriebsplattform.Web/Akten/AktenSuche.cs`
- `src/backend/Betriebsplattform.Web/Akten/Aktenbestand.cs`
- `src/backend/Betriebsplattform.Web/Akten/Aktenmodell.cs`
- `src/backend/Betriebsplattform.Web/Akten/Beispielakten.cs`
- `src/backend/Betriebsplattform.Web/Akten/Kontaktpflege.cs`
- `src/backend/Betriebsplattform.Web/Akten/Kontaktverwaltung.cs`
- `src/backend/Betriebsplattform.Web/ApiAnwendung.cs`
- `src/frontend/DESIGN.md`
- `src/frontend/src/Aktenansichten.tsx`
- `src/frontend/src/Aktenbereich.tsx`
- `src/frontend/src/Arbeitsraum.css`
- `src/frontend/src/Buero.tsx`
- `src/frontend/src/Kategorien.tsx`
- `src/frontend/src/Kontakte.tsx`
- `src/frontend/src/Kontaktkarte.tsx`
- `src/frontend/src/Rechnungen.tsx`
- `src/frontend/src/Suche.tsx`
- `src/frontend/src/akte-api.ts`
- `src/frontend/src/darstellung.ts`
- `src/frontend/tests/app.test.mjs`
- `src/frontend/tests/produktreife.test.mjs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/AktenTests.cs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/ProduktreifeTests.cs`


Dependency-Gate MVP-01.5 geschlossen (19.10.2026, gezielte Paketdownloads ausdrücklich freigegeben): Vite direkt 7.3.6 -> 7.3.7; source-map-js transitiv über Vite -> PostCSS 8.5.26 von 1.2.1 -> 1.2.2. GHSA-rq7h-c2jc-7f22 und GHSA-68fv-2mgg-jv7q mit kleinsten kompatiblen Patches behandelt. npm audit --json: 0 Advisories; npm ls: konsistent; npm test: 75/75 erfolgreich; npm run build: erfolgreich. Manifest/Lockfile geprüft: nur Vite-Pin und zwei Paketeinträge geändert, keine weiteren Paketupdates. Keine Backend-/Repositoryregression, da ausschließlich Frontend-Abhängigkeiten geändert wurden. Keine neuen Fach-/UX-Änderungen, kein Commit/Push/Release.


UX-Abnahmedelta (19.10.2026): Von/Bis als gemeinsame kompakte Gruppe; fachliche Detailmetadaten mit vorhandenen 16/17/15-px-Tokens, Firmenverweise/Ansprechpartner lesbarer; Unterkategorien sichtbar eingerückt. Umzuordnungsdialog: aktuelle Akte nur Information, debouncte vorhandene Aktensuche, kontrollierte Zielauswahl, Antwortversion für erneute Verschiebung und Speicherfeedback. Bestehende Backend-Umzuordnung unverändert; A-B-A und A-B-C ohne Kopie mit Verlauf/Versionsschutz geprüft. Firmenzahlen über separate Metadatenabfrage mit Ressourcen-/Modulfiltern, keine Tabinhalte zum Zählen geladen; Null ohne Zahl. 8 gezielte Frontendtests, 5 gekoppelte Backendtests und Frontendbuild erfolgreich. Browser ohne Screenshots: Datumsgruppe Desktop/schmalere Ansicht ohne Seitenüberlauf, Detailgrößen, Umzuordnung/Rückweg, sofortige Firmenzahlen und Kategorieeinrückung geprüft. Lokalen Server mit aktuellem Backend neu gestartet; flüchtige Beispieldaten dadurch zurückgesetzt. Keine Vollregression, kein Commit/Push.


Letztes Abnahmedelta (19.10.2026): Registertypografie zentral über Tokens: Rechnungsbestand-Titel 20 px, Filter/Sortierauswahl 17 px, Count/Erklärung/Sortierlabel 16 px; keine geänderten Buttonabstände. Erst- und Umzuordnung verwenden dieselbe aus der Erstzuordnung extrahierte Zuordnungsziel-Komponente: Kategorie, vorhandene Akte per nativer Auswahl oder neue Akte mit Betreff/Firma. Keine separate Zielsuche mit zweitem Dropdown. Aktuelle Zuordnung ist reine Information; nur aktuelle Akte ausgeschlossen. Erweiterung der vorhandenen Umzuordnung erlaubt neue Akte unter derselben Versionsprüfung, bestehender Dublettenfreigabe sowie Quellen-/Ziel-/Firmenrechten; Historie und Original bleiben erhalten. 6 gezielte Frontendtests, 3 gekoppelte Backendtests und Frontendbuild erfolgreich. Browser ohne Screenshot: 20/17/16 px tatsächlich gerendert, kein fachlicher Textbeschnitt (nur vorgesehene Screenreader-Beschriftung verborgen), kein Seitenüberlauf; Verschieben und Rückverschieben mit identischer Auswahl erfolgreich. Keine Vollregression, kein Commit/Push.


## MVP-02A continuation — 19 October 2026

Baseline remains `e588f3c58ce0004a38b04ed231d84019f5aad6d4`, branch `main`; all work is uncommitted. The interrupted run already contained the EF/Npgsql packages, relational model, initial migration, storage abstraction, initial provider/API adapter and 17 passing relational tests. Those files were retained. This continuation completed missing workflow adapters, contact-value concurrency/history, shared HTTP guards, bounded list/query/selector contracts, relationship-aware search, direct detail navigation, PostgreSQL environment feedback, negative tests and the existing documentation. No UI redesign or financial-rule change.

The initial migration contains 17 tables and 40 explicit indexes, including relationship/uniqueness indexes. PostgreSQL model/snapshot consistency, generated idempotent migration SQL, restrictive foreign keys and query LIMIT/OFFSET were checked without connecting to a database. No migration was applied to PostgreSQL and no database was installed or configured.

Executed evidence:

- Existing 17 MVP-02A tests were first re-run successfully to establish the continuation baseline.
- One broad backend regression (`dotnet test src/backend/Betriebsplattform.slnx --no-restore --verbosity quiet`): 238 passed, 0 failed. Subsequent changes were confined to the new persistence read adapters, an optional contact DTO field and their tests; no second complete regression was run.
- Final targeted `FullyQualifiedName~Mvp02A`: 33 passed, 0 failed. Relational SQLite tests cover create/read/update/archive, FK/delete/PK/check protection, category validation, real separate-context optimistic concurrency, contact-value-only version changes, synchronous saves, notes/revisions, correspondence/document references, bounded pages/counts, financial confirmations/allocations, exact-file duplicate protection, controlled IDs/provider errors, startup failure without fallback, and A→B→A / A→B→C without copying documents. A new invoice-reference search test initially failed for `RE-07`; the mismatch between normalized input and raw reference was reproduced, corrected with bounded tolerant matching, and re-tested successfully.
- Frontend regression once: 80 passed. Added persistent-mode tests then passed separately; final coupled run: 10 passed (6 MVP-02A cases plus contact consumers). Tests verify server page/filter requests, whole-stock counts/sums, deep detail routes, shared paged destination selection, return destinations and no automatically loaded preview. These use jsdom/mocked contracts, not a live PostgreSQL browser session.
- Final backend Release build: passed, zero warnings/errors. Final TypeScript/Vite build: passed. A later metadata-only list-query optimization was re-tested through the targeted persistence suite and rebuilt separately.
- Repository checks: 7 passed after correcting a new documentation link unsupported by the existing link checker. Public inventory explicitly includes the reviewed new source and migration files.
- NuGet vulnerability checks for Web and test projects, including transitive packages: no known vulnerable packages reported by the configured NuGet sources. Direct additions: EF Core/Design 10.0.11, Npgsql EF provider 10.0.3; SQLite 10.0.11 is test-only. No frontend package changes.
- Changed/new public text was scanned locally for obvious private keys, GitHub/AWS tokens and literal password/API-secret assignments: no matches. Repository checks also cover private/temporary/build/database exclusions and personal absolute paths. These checks are not a guarantee of absence of all possible secrets.

External gate: no installed/reachable local PostgreSQL instance was available; no `psql`/`pg_isready`, installation/service or listener at 127.0.0.1:5432 was found. The prior check was reused, not repeatedly rerun. Therefore PostgreSQL-18 migration application, real-provider CRUD/FKs/concurrency/rollback, restart persistence and PostgreSQL-backed browser workflows are NOT claimed as passed. SQLite is test-only and cannot replace those checks. Startup-unavailable behavior is automated and sanitized.

Current limits: explicit Demo remains in memory (including restricted XLSX import and scan-folder intake); PostgreSQL does not seed or silently fall back. Those two adapters are deliberately unavailable in PostgreSQL mode. Financial persistence is exercised through the existing domain adapter, not a newly exposed payment-write API. Production storage/backup/restore, database-level permissions/authenticated actors, deployment and large-data/detail-history pagination remain gates. Case details and fuzzy candidates have documented bounds; no 100,000-record performance claim. The local development file adapter may leave an orphan after failed metadata commit; it is not a production archive. Original bytes remain separate from relational data. No real data, screenshots, commits, pushes or releases.

Changed/new public files (including the interrupted work):

- `README.de.md`
- `README.md`
- `docs/public/ARCHITEKTUR.md`
- `docs/public/MVP-01-NACHWEIS.md`
- `docs/public/adr/0001-rechnungs-und-zahlungskern.md`
- `src/backend/Betriebsplattform.Web/Akten/Aktenmodell.cs`
- `src/backend/Betriebsplattform.Web/Akten/Kontaktpflege.cs`
- `src/backend/Betriebsplattform.Web/ApiAnwendung.cs`
- `src/backend/Betriebsplattform.Web/ApiModelle.cs`
- `src/backend/Betriebsplattform.Web/Betriebsplattform.Web.csproj`
- `src/backend/Betriebsplattform.Web/Persistenz/Arbeitsaktionen.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/Datensaetze.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/DokumentDaten.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/Dokumentdateien.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/FinanzDaten.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/FirmenAbfragen.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/FirmenDaten.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/FirmenDbContext.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019001700_InitialRelational.Designer.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019001700_InitialRelational.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/Migrationen/FirmenDbContextModelSnapshot.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/Persistenzkonfiguration.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/Persistenzverwaltung.cs`
- `src/backend/Betriebsplattform.Web/Persistenz/PostgreSqlHost.cs`
- `src/backend/Betriebsplattform.Web/Program.cs`
- `src/backend/Betriebsplattform.Web/Webschutz.cs`
- `src/frontend/src/Aktenansichten.tsx`
- `src/frontend/src/Aktenbereich.tsx`
- `src/frontend/src/App.tsx`
- `src/frontend/src/Arbeitskontext.tsx`
- `src/frontend/src/Buero.tsx`
- `src/frontend/src/Kontakte.tsx`
- `src/frontend/src/Rechnungen.tsx`
- `src/frontend/src/akte-api.ts`
- `src/frontend/src/api.ts`
- `src/frontend/tests/produktreife.test.mjs`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/Betriebsplattform.Rechnungen.Tests.csproj`
- `tests/backend/Betriebsplattform.Rechnungen.Tests/Mvp02APersistenzTests.cs`
- `tests/repository/test_public_layout.py`

### PostgreSQL live-gate availability check — 19 October 2026

POSTGRESQL-LIVE-GATE: NICHT BESTANDEN

Reason: a real PostgreSQL 18 runtime is not available on this machine. Targeted checks found no psql/pg_isready/postgres/docker commands in PATH, no PostgreSQL or Docker service, no matching installation in the standard directories or checked Windows uninstall registrations, and no successful connection to 127.0.0.1:5432 within 1.5 seconds. The standard Docker Desktop executable locations were also absent. This is an environment prerequisite, not a demonstrated persistence-code failure.

No installation, download, database creation, migration application or credentials change was performed. Consequently live schema inspection, CRUD/FK/constraint checks, real PostgreSQL concurrency/rollback/reassignment/restart persistence and PostgreSQL-backed API/frontend smoke checks remain unexecuted. Existing tests/builds were not repeated solely for this availability check; no product code changed.

Next user action: install PostgreSQL 18 for Windows x86-64 using the installer linked from the official PostgreSQL Windows download page (the project-recommended installer is hosted by EDB). Select PostgreSQL Server and Command Line Tools; pgAdmin is optional and Stack Builder/additional packages are unnecessary. Keep the instance local-only, retain credentials privately and do not add firewall/router access. A separate synthetic firmenzentrale_dev database and secure application configuration will be prepared when this gate resumes. Do not put passwords in the repository or chat. No commit, push or release.

### PostgreSQL live-gate resume: local authentication prerequisite

POSTGRESQL-LIVE-GATE: NICHT BESTANDEN

The native PostgreSQL 18.6 client is installed under the PostgreSQL 18 installation, service postgresql-x64-18 is Running and 127.0.0.1:5432 is reachable. Branch main and the existing uncommitted MVP-02A work were preserved. A noninteractive local connection to database postgres as the installer role postgres was rejected because no password was supplied. No application connection-string environment variable, password environment variable or project User Secrets file is configured. No credentials were printed, guessed or stored; authentication rules were not changed.

Database creation, migration and all real-provider live tests remain unexecuted until the user provides local configuration through User Secrets or environment variables. This is an authentication prerequisite, not a demonstrated EF/Npgsql failure. No application/test code changed and no tests/builds were repeated without a usable connection. The next action is to configure ConnectionStrings:FirmenZentrale locally for firmenzentrale_dev, with credentials entered privately; then resume this same live gate. No commit, push or release.


### PostgreSQL 18 live gate completed — 19 October 2026

**POSTGRESQL-LIVE-GATE: BESTANDEN**

This result supersedes the earlier availability/authentication blocker entries above. Native PostgreSQL server/client 18.6 (server version number180006), local service running, loopback127.0.0.1:5432. Created the separate synthetic database `firmenzentrale_dev` after checking it did not exist; existing databases were not reset or dropped. Credentials were read privately from the configured project User Secrets and passed only in process environment/configuration, never printed or committed. No authentication, firewall, router or public exposure settings were changed.

The existing initial migration `20261019001700_InitialRelational` was applied successfully and reapplication was idempotent. No migration was regenerated or added. Actual PostgreSQL catalog/information_schema metadata was matched to the EF relational design model:17 business tables plus EF history,17 primary keys,40 explicit indexes (unique flags checked),30 foreign keys with RESTRICT delete rules,26 CHECK constraints, column NOT NULL declarations and integer concurrency columns/tokens. Pending migrations/model changes: none.

Added one explicit opt-in PostgreSQL integration scenario in the existing `Mvp02APersistenzTests.cs`; it requires `FZ_POSTGRES_LIVE=1` and a privately configured `ConnectionStrings__FirmenZentrale`. The test refuses a database other than `firmenzentrale_dev`, a non-loopback host, nonstandard port or a server outside major18. It is skipped in ordinary runs, not silently passed without a server. No public test needs local private helper files.

Real-provider evidence, successful scenario (1 integration test, several asserted phases):

- Created/reloaded company, person/company relationship, separate address fields and contact values, category, cases, invoice, payment/confirmation/allocation, document metadata/current relationship, note/revisions/archive and external correspondence with central attachment references. Separate DbContexts verified persisted results. Invoice retained120 EUR total,40 EUR confirmed allocation,80 EUR open.
- PostgreSQL rejected nonexistent foreign IDs (23503), missing required name (23502), overlong varchar (22001), negative amount/self-parent CHECK (23514), duplicate normalized category/current document assignment (23505) and deletion of a referenced contact (23001). Historical rows remained. Schema unique/index protections were inspected in addition to behavioral negatives.
- Two independent real PostgreSQL contexts read versionX; B savedX+1; stale A received `DbUpdateConcurrencyException`; B's state was retained. Real HTTP stale status update returned409 without SQL/provider/connection details.
- Reassignment A→B→A→B→C kept one current relationship, one unchanged original document/hash/storage key, incremented versions and old/new case history with UTC timestamps. Invalid/duplicate relationships remained protected.
- Injected an execution failure at the document-history INSERT after a new case had already been inserted inside the same operation. A fresh context verified full transaction rollback: no new case, no changed current relationship/version and no extra history.
- Started a real PostgreSQL-backed ASP.NET host; contacted metadata/list/count/detail, financial, note and reassignment endpoints. Compared pre-/post-start row counts: no implicit seeds. Restricted demo import remained blocked. Stopped/disposed the host completely and created a new host; records, saved note and status persisted.
- SQL command-shape interception (no parameter values) verified actual LIMIT/OFFSET and WHERE/ORDER BY in the exercised PostgreSQL queries. No obvious per-row N+1 loop was observed in the bounded list/read paths. This small synthetic dataset is not a performance benchmark or production-scale query-plan approval.
- An unreachable loopback PostgreSQL connection (port1) caused the registered startup readiness check to fail with a sanitized error and no Demo/InMemory fallback.

Real Edge browser smoke on127.0.0.1:5180: PostgreSQL environment indicator; invoice overview/detail and partial-payment amounts; central company/person details and company-work counts/links; target case with a closed-by-default document preview; deliberate original text preview; shared destination selection and actual saved C→B reassignment; inbox detail with current relationship and full history. Created `PostgreSQL-Browserprüfung` through the real note dialog. Then terminated the host process, confirmed no listener remained on5180, started a fresh host process and reloaded the invoice: the browser-created note and earlier host-restart note were still present. No screenshots, UI changes, console-injection scripts or browser permission changes.

Necessary product correction: real RESTRICT deletion returned SQLSTATE23001, which was not included in the existing generic FK-conflict mapping. Added `PostgresErrorCodes.RestrictViolation` to the controlled409 mapping and its regression case. No other product/mapping/schema/SQL correction was required. Two initial assertions in the new live test were corrected before the successful run: historical revision-version fields are not concurrency tokens, and RESTRICT deletion's expected code is23001. No protection was weakened to make a test pass.

Final checks: PostgreSQL live integration scenario1/1 passed; directly coupled non-live MVP-02A tests34/34 passed; backend Release build passed with0 warnings/0 errors. No full backend/frontend regression or frontend build was repeated because the only product change was this backend error-code mapping. Existing frontend build was served and checked live. `git diff --check` passed; repository checks cover updated Markdown/inventory. Current-step files: `Persistenzkonfiguration.cs`, existing `Mvp02APersistenzTests.cs`, this evidence, existing architecture/ADR and EN/DE README status, plus the existing private development plan. Local runtime originals and a credential-free local execution helper remain under ignored backend `private/`.

Synthetic live-test records remain intentionally in `firmenzentrale_dev`; failed test attempts also left only synthetically named records. No cleanup, reset or deletion of business records was performed. The temporary smoke host is stopped at completion; the installed database service remains untouched. Productive file storage, backup with restore test, authenticated actors/database-level permissions, deployment and realistic-load/detail-history pagination remain future pilot gates. No real data, commit, push, tag or release.


## MVP-02B1 — durable local document storage (19 October 2026)

Implemented and locally verified, without pilot approval. Reused the existing file adapter/metadata model and migration: explicit external local root, startup read/write/removal probe, random keys, atomic same-directory staging/rename without overwrite, normal DB-failure compensation and guarded ambiguous-commit handling. Retrieval verifies SHA-256/size and streams the same read-only handle. Device/invalid filenames are rejected. No packages, migration, financial logic, frontend or permission model changed. Architecture/ADR and bilingual README status were updated; no new public document was created.

Test-first evidence: the initial three filename cases produced two expected failures (`CON.txt`, `brief?.txt`) and one pass; the validator correction passes them. An integration assertion was corrected to expect EF's `DbUpdateException` wrapper around the injected I/O error; rollback/compensation protections were retained. Final command from `src/backend`: `dotnet test ../../tests/backend/Betriebsplattform.Rechnungen.Tests/Betriebsplattform.Rechnungen.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Mvp02B1StorageTests|FullyQualifiedName~Mvp02APersistenzTests|FullyQualifiedName~AktenTests" --verbosity quiet`: **75 passed, 1 opt-in live test skipped**, exit 0. This includes 22 non-live MVP-02B1 cases and directly coupled existing persistence/upload/case tests. Cases cover traversal/absolute/UNC keys and names, device names, MIME/extension/content mismatch, empty/oversized files, unsupported macro formats, blocked storage, no fallback, hash mismatch/missing originals, cancellation, no overwrite, restart, DB failure compensation, stale/invalid reassignment and A→B→A→B→C without file copies. A post-commit injected response failure proves the committed original remains readable.

The opt-in `Mvp02B1StorageTests.PostgreSql18StorageLiveUploadNeustartUmordnungUndKompensation` ran separately with `FZ_POSTGRES_LIVE=1`, a process-only connection supplied privately and `FZ_STORAGE_LIVE_ROOT` pointing to a separate external LocalAppData development directory: **1 passed**, exit 0. It used the existing PostgreSQL 18.6 `firmenzentrale_dev` migration, created only synthetic records, verified metadata/hash/file contents, injected a DB write failure after the file step with full rollback/removal, exercised reassignment/history/versions, uploaded through actual Kestrel HTTP, stopped/disposed the host and started a new host, then read the retained bytes. Actual PDF attachment/PNG inline and text responses, unknown/manipulated IDs, missing-file sanitized 404 and retrieval after reassignment passed. Existing conservative uploaded-PDF download policy was preserved; no new PDF inline approval was introduced. Synthetic test records and their external run-specific directories remain for development inspection. No User Secrets or credentials were changed, printed or stored in public files.

`dotnet build Betriebsplattform.slnx --no-restore --configuration Release --verbosity quiet`: passed, 0 warnings/errors. Repository checks: **7 passed** (`python -B -m unittest discover -s tests/repository -v`); they first detected the temporary live-test starter, which was removed before the successful repeat. `git diff --check` passed. No full backend regression, frontend test/build, manual browser rendering, screenshot, real-data test, destructive power-loss experiment or NTFS ACL/race test was performed. Frontend and response DTOs are unchanged; real response bytes/type/disposition and host restart were checked through HTTP. No new dependencies or public runtime/binary files.

Remaining boundaries: production authentication/resource authorization, malware scanning, coordinated PostgreSQL/filesystem backup and restore, orphan reconciliation after hard termination or inaccessible compensation, OS service-account access controls, deployment and LAN security. Filesystem and DB are not a distributed transaction; uncertain commit outcome preserves the original and returns a controlled maintenance/conflict response. Existing synthetic MVP-02A files are not automatically relocated. Suggested MVP-02B2 is coordinated backup/restore and measured crash recovery, separately authorized. No commit, push or release in this block.

Final fixture check: synthetic live PDF comments and PNG text chunks now include the run identifier, preserving valid bytes and global duplicate protection while allowing retained test records across repeated runs. The final targeted Storage class ran with the live opt-in enabled: **23/23 passed, no skips**, exit 0. The 75-case coupled regression above remains the preceding unchanged-product regression.


## MVP-02B2–02B4 — operations/security verification (19 October 2026)

Preserved all uncommitted B1 work. Implemented offline database/files/keyring backup and manifest verification, isolated restore, startup/CLI inventory without deletion, framework cookie/CSRF/password authentication, individual users/admin/employee roles, database-side resource filtering, administrator management, authenticated audit, restricted PostgreSQL roles and local publish packaging. Added one additive `PilotSecurity` migration (three tables); no new packages, real data, public deployment, installer, firewall or OS-account changes. English/German status and existing architecture/ADR updated. Visible development hints are centralized; internal fixture IDs remain.

Ran one broad backend regression: **271 passed, 1 failed, 4 opt-in/helper cases skipped (276 total)**. The failure was a historical pooled test factory incompatible with the new request-aware constructor, not a database fallback: replaced only that fixture with a non-pooled factory and retained its unreachable-DB assertions. Targeted rerun of that case plus security/recovery: **9 passed, 2 opt-in/helper cases skipped**. New deterministic third-party-payment test first failed (30 EUR allocated was incorrectly hidden as zero), then passed after preserving authorized allocation facts and using permission-aware left joins for hidden payer names. Final directly coupled persistence/security suite: **43 passed, 2 opt-in/helper cases skipped**. Existing HTTP/static delivery suite after publish changes: **8 passed**. No second broad backend regression was run.

Ran the frontend regression once: **83 passed, 5 failed (88 total)**. Causes: DOM-free transport tests needed a base URL; one fixture assumed a plain header object instead of standard Headers; two expected the old global hint/profile labels. Fixed the transport and updated only those fixture expectations. Targeted failed/related login cases: **8 passed**, including the three new login/CSRF/logout tests. No second full frontend run. The base-URL guard still forbids external API targets; no LocalStorage auth tokens.

The explicit PostgreSQL live scenario `Mvp02BSicherheitTests.PostgreSql18BackupRestoreAuthRechteUndNegativfaelle` passed against native PostgreSQL 18 with only new `firmenzentrale_b2_*` / `firmenzentrale_restore_*` databases and external LocalAppData test roots. It used the normal private configuration provider internally, without displaying or changing existing Secret files. Runtime passwords were generated only in test-process memory. Preserved `firmenzentrale_dev` business contents. Isolated test databases/roles/files remain for inspection; generated secrets are not reported.

Live assertions: additive migrations and synthetic companies/cases/invoices/payments/notes/document metadata; admin and two employee cookies; unauthenticated API denial, forbidden administration/category mutation, inaccessible case/contact/document and filtered search, strict overposting rejection, CSRF rejection, actual Secure/HttpOnly/Strict cookies, bad password/lockout, inactive login and immediate session rejection, expired signed ticket and rate-limit 429; two actors stale-update 409 and correct note actor; forbidden runtime schema creation and refused superuser host startup. Backup refuses an active host. After stopping it, actual pg_dump backup and transactionally restored isolated DB/files were verified. Restored admin and employee logins, allowed/denied resources, relationships, invoice and note rows, original bytes/hash and new session keys/stamps passed. Missing/mutated archive documents, wrong archive hash, unsupported/incomplete manifest, unwritable/existing targets and a genuinely corrupt pg_restore input were rejected. No success marker is written for incomplete restore.

Two normal tests launch and kill only their own isolated child test processes: one holds a partially written staging file; one stops after the real adapter finalized a file but before DB metadata. Restart inventory detects the remnants/orphan without deleting them or modifying an existing valid original. This is not a physical power-loss/OS failure experiment. Additional recovery tests cover missing/mismatched originals and unknown files.

Local Release publish includes the built frontend in `wwwroot`. With `FZ_PILOT_LIVE=1` and `FZ_PUBLISHED_HOST` pointing to the ignored publish artifact, the final live scenario also started that actual host as a separate Production process using the restored DB, restricted runtime role, external storage and an encrypted temporary test certificate. It authenticated, served the frontend and original, then was terminated without affecting the retained DB. Windows fixture fixes: the TLS test certificate needed a SAN and a Windows-compatible key load; the exported temporary PFX uses the original exportable test certificate. No certificate was installed or globally trusted. No passwords were logged, and the temporary PFX was removed.

Builds passed: backend Release **0 warnings/0 errors**, frontend TypeScript/Vite, local framework-dependent publish. Dependency checks: `npm audit --json` reported **0 advisories**; `dotnet list Betriebsplattform.slnx package --vulnerable --include-transitive --no-restore --format json` reported no vulnerable packages. These checks do not certify Windows/shared-runtime patch state. No dependency changes. Existing public inventory was extended only for reviewed new source/test/migration files; repository/diff checks are performed after this documentation update.

Security review is single-agent and bounded, not an independent penetration test or production assurance. No real data, manual browser/certificate-trust check, LAN client, firewall change, service-account ACL test, malicious-virus execution, actual Defender invocation or physical power-loss test. Defender service is running and its tool exists; Microsoft's documented elevated-process requirement makes actual integration a separate gate. Pilot uploads fail closed at the scanner hook until provisioned. Backup manifests provide corruption checks, not authenticity against replacement; protected/independent/encrypted backup media and operator procedures remain necessary.

**MVP-02B SECURITY/PILOT-GATE: NICHT BESTANDEN.** Concrete blockers: the existing PostgreSQL listener binds `0.0.0.0:5432` and `[::]:5432` (reachability through firewall/HBA not established); production certificate/trust/renewal, actual OS/backup permissions and retention, approved malware integration, human resource-grant matrix and independent final security approval are outstanding. No network/system security setting was changed. No commit, push, tag or release.

Final repository checks: **7/7 passed** after explicitly reviewing/allowlisting the Microsoft Defender primary-source link. Final backend Release build: **0 warnings/0 errors**. Final PostgreSQL/auth/restore/published-host scenario: **1/1 passed** after the financial permission correction. git diff --check passed; public private-path/obvious-secret checks passed. Existing secret configuration was not displayed or modified. No full suites were repeated.


## Startup stability and multi-user readiness ? 19 October 2026
Baseline main cb19e6496380720ef5499c3e3fc065db33b5ece1 with 31 modified tracked / 14 untracked MVP-02B files was preserved. A controlled child-host probe using a static-assets manifest with a nonexistent content root reproduced refused startup. The unused SDK asset pipeline is now disabled, its obsolete build-output manifests removed, and the existing Vite build copied beside the executable. No user-created source wwwroot is required. Added HTTP test starts with an isolated content root lacking wwwroot and verifies HTML, JavaScript, deep-link fallback and API 404.

No previous live-update transport existed. Shared bounded polling now refreshes visible lists/case metadata every 15 seconds and on focus/online/visibility return, without remounting forms. Hidden/offline views skip polling. List failures discard stale rows, contact editors pause detail refresh, note drafts retain their original version and values, and opened text previews are not reloaded solely because metadata object identity changed. This is eventual refresh, not immediate push or a durable event feed. Existing server-side authorization and 409 logic are unchanged.

Executed: targeted WebPrototypTests 9/9; final full backend regression 274 passed, 4 opt-in/helper cases skipped, total 278; full frontend regression 91/91; TypeScript/Vite build passed; backend Release build passed with 0 warnings/0 errors after repeating a sandbox-denied test-output write with the proper workspace permission. The skipped cases are PostgreSQL schema/live persistence, PostgreSQL storage live, PostgreSQL auth/backup/restore live, and the isolated crash-child helper (its parent tests execute their child separately). No new live migration or database setup was authorized or performed. Earlier PostgreSQL evidence is retained, not represented as rerun here.

New DOM tests verify list refresh with an unchanged draft, offline/online reconciliation, denied-list removal, administrator account creation through its actual form, and a foreign note appearing while the real note dialog remains open with unchanged subject/body. Existing regression includes contact stale-version 409, EF two-context conflicts, resource filtering and authentication/session negatives. The DOM tests use synthetic responses and controlled timers, not two physical PCs or real production sessions. A jsdom focus-polyfill warning was addressed using the existing test-harness convention. No screenshots or new packages.

Actual Release host process in explicit Demo mode: GET / => 200 with root element; linked built JavaScript => 200; /api/auth/session => 200. Host stopped afterwards. No PostgreSQL secrets were read/displayed and no operating-system/network/certificate configuration changed. Admin-created accounts use framework-hashed start passwords; secure handover, forced first-password change, MFA and account recovery are not established by the form test. Pilot gate remains NOT PASSED.

Manual two-PC acceptance, only after separately approved LAN HTTPS/certificate and operator gates: host application and PostgreSQL on office PC A, browser on PC B; no third server. Use two individual fictional users and a shared authorized resource. (1) A creates a company/case; B sees it within a polling interval or after focus return. (2) A keeps a note draft open while B saves another note; A sees the count/list update and keeps its draft. (3) Both edit the same contact version; first save succeeds, second receives a conflict with its draft retained. (4) Withhold/revoke another resource and confirm no list/search/download access; deactivate a user and confirm session rejection. (5) Disconnect/reconnect B and verify reconciliation without F5. (6) Administrator creates an employee, securely hands over its initial credential and verifies least-privilege login/logout. No actual two-PC or manual browser acceptance is claimed.


## Compact follow-up: local checkpoint, five-second refresh and own password ? 19 October 2026
Preserved and inspected all 51 pending public files. Seven repository checks passed. A Git-excluded local ZIP of those files and a verified history bundle were created; private configuration/User Secrets/runtime files were not staged. One whitespace-only EOF issue discovered by staged diff-check was normalized. Local checkpoint f54ecd7 commits the previously completed work in 51 files; no push.

The new follow-up remains uncommitted. Central ABGLEICH_INTERVALL_MS is 5000. List/invoice background reads reuse an outstanding request instead of aborting it at each tick; small detail reads have pending guards, hidden/offline checks remain, and the case header reuses its workspace metadata instead of polling the same full response twice. Note drafts retain their own version and content; contact editing pauses detail refresh. SignalR is deferred.

Added an authenticated own-password endpoint and profile dialog using existing password hashing, authorization, CSRF, rate limiting, concurrency and audit. Current password is mandatory; the new password follows the existing 14-to-128-character nonblank policy and must differ. Only the signed-in account can change; no account ID or role is accepted from the client. A new security stamp revokes older cookies on their next request while the current reauthenticated session is renewed without extending its existing lifetime. No periodic forced password change. No passwords are logged or stored in plaintext; the new request DTO masks its ToString.

Targeted checks: four backend tests passed, including an actual isolated HTTPS/cookie integration with two sessions, wrong current password, missing CSRF, overposting denial, current-session continuity, other-session 401, rejected old login and successful new login. Synthetic SQLite only; no PostgreSQL migration. The HTTP test first exposed nullable DateTimeOffset lockout comparison unsupported by the SQLite test mapping; the existing SQLite-only UTC converter now includes nullable properties. PostgreSQL mapping/schema is unchanged. Eight frontend authentication/refresh tests passed and eight directly coupled list/error tests were exercised; after eliminating the duplicate case read, one case-header test found a missing callback, corrected and rechecked with the note-draft case (2/2). No complete regression was repeated; earlier 274-backend/91-frontend evidence remains historical. Final build/diff results are recorded after execution below.

Final results: frontend TypeScript/Vite build passed; backend Web Release build passed with 0 warnings/0 errors; git diff --check passed. Four targeted backend cases are green. Sixteen distinct frontend cases were exercised; the one intermediate case-header failure and the directly coupled note-draft case passed their targeted rerun. No full suite, PostgreSQL live gate, LAN test, installation or deployment was repeated. The new 14 tracked-file changes remain unstaged/uncommitted after checkpoint f54ecd7. Pilot approval remains withheld.

## Employee first access — 19 October 2026

Preserved HEAD f54ecd7 and the 14 existing modified files; made a byte-verified, Git-excluded local backup before editing. No commit or push. Administrators now create accounts with a role and receive a single-display code for personal handover, without choosing an employee password. Give the employee their login, application address and code; they choose “Zugang einrichten”, enter the code and confirm their own password, then return to login. Pending accounts cannot log in. Existing configured accounts and own-password changes remain supported. Pending invitations can be revoked or replaced; configured accounts cannot use this feature as password recovery.

Codes contain 256 random bits, expire after 24 hours, travel only in POST bodies and are stored as SHA-256 digests. Existing framework password hashing, administrator authorization, CSRF, IP rate limiting, session stamps and audit are reused. Consumption, password assignment and audit commit atomically with EF's existing version predicate. Reissue, revocation or deactivation invalidates an earlier code; stale competing updates cannot activate it. Deactivation also clears pending invitations. Remote first-access requests require HTTPS; this application still retains its existing loopback host boundary. Trusted LAN TLS is a separate unresolved pilot gate.

Eight new backend cases passed in targeted subruns: actual synthetic HTTPS/cookie create–configure–login–password-change flow with pending-login rejection, unauthorized/forbidden operations, CSRF, rate limit, secret-free account projection and protection of the last configured administrator; invalid/expired/reused/deactivated/revoked/replaced codes; two synchronized redemption attempts with exactly one success and one audit; three stale-read races against reissue/revocation/deactivation; transport guard; additive migration preserving existing accounts. Tests use SQLite plus generated PostgreSQL migration SQL and a snapshot/model consistency check. The Windows HTTPS fixture required permitted execution outside the sandbox for its temporary test key; no trust store or system configuration was changed. Initial missing-feature test was red; final targeted cases are green. No PostgreSQL live migration or provider concurrency test was run.

Three frontend cases passed using TypeScript compilation and Node/jsdom: administrator creation/copy/close/reissue/revoke, employee confirmation/error/success/return to login, and existing own-password change. No real-browser visual, LAN, load or complete-regression claim. Frontend `npm.cmd run build` and backend Web `dotnet build -c Release --no-restore` passed (backend: zero warnings/errors). No dependencies added. The additive `MitarbeiterErstzugang` migration is prepared, **not applied to an existing database**; it must be deliberately applied before using the new build against PostgreSQL. MFA, recovery and deployment remain outside this block; MVP-02B remains blocked pending full security acceptance.

Files changed by this block (earlier unrelated edits preserved):

- Backend: `Sicherheit/Anmeldung.cs`, `Sicherheit/Pilotzugriff.cs`, new `Sicherheit/Erstzugang.cs`, `Persistenz/Pilotmodell.cs`, `Persistenz/Migrationen/FirmenDbContextModelSnapshot.cs`, new `20261019170000_MitarbeiterErstzugang.cs` and its `.Designer.cs`.
- Frontend: `src/Anmeldung.tsx`, `src/Benutzerverwaltung.tsx`, new `src/Erstzugang.tsx`, `tests/anmeldung.test.mjs`.
- Tests: new `tests/backend/Betriebsplattform.Rechnungen.Tests/ErstzugangTests.cs`; this existing public evidence file. Local operational notes remain Git-excluded.

## Isolated PostgreSQL first-access gate — 19 October 2026

One new opt-in integration test, `ErstzugangPostgreSqlTests.IsolierteMigrationHttpsErstzugangNeustartUndKonkurrenz`, passed against the actual local PostgreSQL 18 server. It creates a uniquely named `fz_erstzugang_test_<GUID>` database from `template0`, connecting for creation to the `postgres` management database. It never opens the main development database. Before each migration it checks both the configured and actual database identity against the freshly generated test name. It first applies the previous `PilotSecurity` schema, inserts two synthetic existing accounts and a resource grant, then applies `MitarbeiterErstzugang`. Existing columns, constraints, indexes, password hashes, roles, security stamps and the grant were preserved. The intended new NOT NULL constraint is explicitly allowed; no other constraint change is accepted. Both old accounts default to configured, with no invitation secret or expiry. No migration remains pending in this isolated database.

The existing complete application host ran over actual loopback HTTPS with a separate restricted test runtime role. The administrator logged in and created an employee with the employee role; the code digest and expiry were verified in PostgreSQL. Pending login was rejected, employee activation succeeded, reuse failed, regular login worked, and attempts to grant administrator rights or create an administrator returned 403. Resource access was denied before an administrator's explicit grant, granted immediately afterward, and another contact remained hidden. After stopping and disposing the host, a fresh host and fresh database contexts verified employee login, the persisted resource grant, the legacy employee login, consumed invitation fields and the framework password hash. A synchronized two-context PostgreSQL activation race produced exactly one success and one audit entry.

Executed: process-local `FZ_ERSTZUGANG_POSTGRES=1`, `dotnet test tests/backend/Betriebsplattform.Rechnungen.Tests --no-restore --filter FullyQualifiedName~ErstzugangPostgreSqlTests --logger "console;verbosity=normal" --logger "trx;LogFileName=erstzugang-postgresql.trx" --results-directory private/pruefungen/erstzugang-postgresql`. Final result: 1 passed, 0 failed/skipped. The first attempt exposed a test expectation error: PostgreSQL 18 adds a NOT NULL constraint for `Eingerichtet`; only that assertion was corrected and the affected integration test rerun. No product or migration correction was necessary. Successful prior SQLite/UI tests, including invalid/expired/deactivated codes, were not repeated. The build required by this focused test passed; no separate frontend build or full regression was needed. Diff/whitespace checks passed, including the new test file.

Only the new integration test, this evidence file and Git-excluded operational notes changed in this block. The local TRX records the generated test database/role names; synthetic databases remain available for inspection. Test hosts were stopped and disposed. No package installation, main-database migration, system/network/trust change, commit, push or deployment. This is an automated HTTP/database end-to-end result; actual browser acceptance, manual/LAN checks and full security acceptance remain open. **MVP-02B remains blocked for a real-data pilot.**

## Authenticator MFA — 19 October 2026

Profile → Sicherheit now supports manual Authenticator enrollment (fresh password, setup key, confirmed six-digit code), status, eight single-use recovery codes, code replacement and secure deactivation. No QR library/service was added. RFC 6238 uses SHA-1, 30-second periods and ±1 period tolerance; the highest accepted period persists with optimistic concurrency. Active/pending secrets use user-bound ASP.NET Data Protection and the existing persistent Windows-DPAPI keyring. Pending enrollment expires after ten minutes. Password verification starts a five-minute Secure/HttpOnly/SameSite=Strict challenge, with no business authorization until MFA succeeds. Recovery codes contain 128 random bits each; only hashes persist. Security changes revoke other sessions and renew the proven current session without extending its lifetime. Password reset preserves MFA. Five failed attempts lock for fifteen minutes; failed attempts are counted atomically even under concurrency.

After recovery login, a protected cookie claim plus a hashed, five-minute, single-use server record allows one settings change with another password confirmation. This also supports replacing codes after the last recovery code was used. It is not a permanent MFA bypass. Without any factor/recent recovery proof there is no emergency reset. Role-mandatory MFA and an administrator emergency process remain separate future work.

Executed targeted checks: six published RFC 6238 SHA-1 vectors; three MFA lifecycle/expiry/lockout/last-code cases; actual HTTPS enrollment, no access before MFA, rejected well-formed wrong codes/replay, recovery login, step-up replacement, password-reset preservation, password change, safe deactivation and legacy password-only login, including host restart with the persistent protected keyring. The existing isolated PostgreSQL gate checked the additive ten-column migration, snapshot consistency, unchanged existing structures/accounts/rights, exactly one successful TOTP/recovery consumption under concurrent requests, and lockout after five parallel failures. **11 distinct backend cases passed in filtered runs.** Five directly coupled Node/jsdom frontend cases passed, including three MFA cases. Final TypeScript/Vite and backend Web Release builds passed (zero backend warnings/errors). An esbuild sandbox directory-access failure was resolved by an approved local build execution, without configuration/dependency changes. Diff/whitespace checks include new files. No complete regression or manual browser/LAN test was run. Local HTTPS/PostgreSQL TRX: `private/pruefungen/mfa/mfa-https-postgresql.trx`.

The existing 27-file working state was protected with a verified incremental local backup, reusing unchanged earlier backups. Changes in this block:

- Backend: `Anmeldung.cs`, `Erstzugang.cs`, `Pilotzugriff.cs`, `Pilotmodell.cs`; new `MfaDienst.cs`, `MfaEndpunkte.cs`, `Totp.cs`; model snapshot and new `20261019200000_AuthenticatorMfa.cs` / `.Designer.cs`.
- Frontend: `App.tsx`, `Anmeldung.tsx`, new `MfaAnmeldung.tsx`, `MfaSicherheit.tsx`, and `tests/anmeldung.test.mjs`.
- Tests: new `MfaTests.cs`, `MfaHttpTests.cs`, extended `ErstzugangPostgreSqlTests.cs`; this evidence file and ignored operational notes.

The main database was not opened/migrated; deliberate target migration is still required. Actual Authenticator-app/device usability, accessibility, multi-browser/two-PC/LAN behavior, operational key backup/restore and final security acceptance remain manual gates. No commit/push/deployment/system-security change or new dependency. **MVP-02B remains blocked for real customer data.**

### 19.10.2026 — regulären Start und sichtbare Kontenfunktionen integriert

Ursache: Das lokale Web-Startprofil aktivierte weder PostgreSQL noch Authentifizierung. Der bisherige Standard war Demo, einschließlich fester Beispielidentität und nicht interaktivem Profilcontainer. Das Root-Frontend hatte bereits eine Anmeldesperre; der Demo-Session-Endpunkt meldete jedoch ausdrücklich deaktivierte Authentifizierung. Zusätzlich fehlte der automatische Frontendbuild im Webprojekt; eine veraltete Auslieferung war möglich. Welches Bundle der frühere Screenshot genau zeigte, ist nicht nachgewiesen.

Der Programmeinstieg verlangt jetzt Anmeldung oder einen ausdrücklich getrennten Demostart. Unvollständige Sicherheitskonfiguration liefert einen sichtbaren Einrichtungshinweis und gesperrte Geschäfts-APIs, ohne Datenbankverbindung. Vollständig konfigurierter Kontenbetrieb verwendet weiterhin den vorhandenen HTTPS-/PostgreSQL-Host und dessen Startprüfungen; fehlende Migrationen werden nicht automatisch angewendet. Profilmenü mit nativer Tastaturbedienung/Escape, Profil/Sicherheit, Passwortänderung, vorhandene MFA-/Recovery-Dialoge und Abmeldung sind verbunden. Administratoren erreichen die bestehende Verwaltung über Einstellungen → Benutzer & Berechtigungen; Mitarbeiter erhalten bei direkter Route keine Verwaltung. Der explizite Demostart kennzeichnet die Beispielidentität klar.

Webprojekt-Build: Frontendquellen als Aktualitätseingaben, inkrementeller TypeScript-/Vite-Build vor Asset-Erfassung, anschließend Kopie der aktuellen Dateien in `wwwroot`. Fehlende Asset-URLs liefern 404 statt HTML. Release-Index und sämtliche referenzierten Assets wurden bytegleich mit `dist` geprüft. Ein echter Release-Prozess ohne Startargumente lieferte den aktuellen Index, den Einrichtungshinweis und HTTP 503 für die Geschäfts-API. Keine Visual-Studio- oder manuelle Browserabnahme.

Ausgeführt: fünf gezielte React/jsdom-Integrationstests der regulären Root-Komposition; zwei neue Backend-Starttests und vorhandener HTTP-Frontend/Asset-Test; erweiterter HTTPS-MFA-Test einschließlich direkter Administrator-API-Verweigerung und Logout. TypeScript-Testcompile und Release-Webbuild mit automatischem Vite-Build erfolgreich, ohne neue Abhängigkeiten. Sandboxfehler von esbuild und Windows-Test-PFX-Import wurden durch begrenzt freigegebene Build-/Testausführung behandelt; keine Zertifikatinstallation oder Systemkonfiguration. Der neue Test verwendete zunächst einen falschen Fixture-Pfad und einen expliziten Rollen-Selektor für einen nativen Dialog; beide Testfehler sind korrigiert. Der dynamische Asset-Kopiervorgang wurde nach einem konkreten MSBuild-Metadatenfehler berichtigt.

Noch offen: tatsächliche Hauptdatenbank-/HTTPS-Bereitstellung einschließlich geprüfter Migrationen, Dateiablage, Schlüsselablage und initialem Administrator. Die Hauptdatenbank wurde weder geöffnet noch migriert; ihr Migrationsstand ist nicht bestätigt. Neue QR-Komponente, Rollen-MFA-Pflicht, administrativer MFA-Notfallreset einschließlich Vieraugen-/Letzter-Administrator-Verfahren sowie das zentrale Aufgabenmodul wurden in diesem Integrationspaket nicht implementiert. Keine neuen Schemaänderungen. Die bestehende manuelle Authenticator-Einrichtung und Recovery bleiben erhalten. Diese offenen Teilpakete sind keine fertigen Funktionen.

Zentrale spätere manuelle Abnahmeliste — jetzt ausdrücklich nicht ausgeführt:

- Visual-Studio-Play mit frischem Frontend, Login/Logout und Wechsel zwischen Konten; Administrator-/Mitarbeiteransicht, Profilmenü und direkte geschützte URLs.
- Erstzugang, Passwortwiederherstellung, Passwortänderung, Authenticator und später QR-Code; Recovery und später kontrolliertes MFA-Notfallverfahren.
- Nach Implementierung: Aufgaben erstellen/zuweisen/erledigen/wieder öffnen, Fristen/Wiedervorlagen und Kontextaktionen aus Rechnungen, Kontakten und Akten.
- Mehrbenutzerverhalten, Smartphone/Tablet, Tastatur/Fokus/Zoom/Screenreader, LAN mit vertrauenswürdigem HTTPS.
- Koordinierter Datenbank-, Datei-, Schlüssel- und Backup-Restore; abschließende Sicherheits- und Pilotabnahme. **MVP-02B bleibt für Echtdaten gesperrt.**

### 19.10.2026 — Featurepakete A bis H umgesetzt und gezielt geprüft

Die Fortsetzung erweitert die vorhandene Anwendung und Kontenverwaltung. Die unten genannten Funktionen sind in der regulären React-Komposition und Navigation verbunden. Die weiterhin fehlende lokale PostgreSQL-/HTTPS-Betriebseinrichtung einschließlich freigegebener Hauptdatenbankmigration bleibt eine getrennte Startgrenze. Es wurde kein Demo-Ersatz für diese Funktionen angelegt.

| Paket | Technisches Ergebnis | Automatisierter Nachweis | Weiterhin offen |
| --- | --- | --- | --- |
| A – QR-Code | Lokale SVG-Erzeugung aus TOTP-URI; manueller Schlüssel bleibt, Anzeige nur bei Einrichtung und bis Ablauf | QR-Daten rückdekodiert, Format/Fehlerkorrektur geprüft; Root-Einrichtung mit Aktivierung und einmaliger Codeanzeige | Tatsächlicher App-/Smartphone-Scan |
| B – Rollen-MFA | Persistente Richtlinie unter Einstellungen → Sicherheit; starke Bestätigung; begrenzter Einrichtungszugang, auch für bestehende Sitzungen | Rollen-/Vorbereitungsfälle und echte HTTP-Sperren, direkte API-Verweigerung, Einrichtung bis Geschäftsfreigabe | Menschliche Rollen-/Kontenabnahme |
| C – Notfallreset | Mitarbeiterprozess sowie getrennte Administrator-Anträge, unabhängige Zustimmung und einmalige Durchführung; zehn Minuten gültig | Berechtigung, Selbstreset-Verweigerung, Vieraugenablauf, Ablauf/Replay, Faktoren-/Sitzungswiderruf und erneute Pflicht geprüft; Mitarbeiterprozess über HTTPS | Betreiber-Recovery für den letzten verfügbaren Administrator bleibt gesperrt |
| D – Aufgabenbackend | Zentrale Aufgaben, vier Status/Prioritäten, Zuständigkeit, optionale Ressourcen-FKs, Versionierung und Änderungsverlauf | Rechte, Zuweisung, Lesen/Ändern, Freigabeentzug, Konflikte; SQLite und isoliertes PostgreSQL | Manuelle Mehrbenutzerabnahme |
| E – Aufgabenoberfläche | Hauptnavigation, Listenansichten, Suche/Filter/Sortierung/Pagination, Editor, Statuswechsel und Archivierung | Reguläre Root-Anwendung: Anlegen, Bearbeiten, Statuswechsel; Entwurf bleibt bei Abgleich und Konflikt erhalten | Geräte-/Bedienungsabnahme |
| F – Fristen/Wiedervorlagen | Kalenderdatum mit optionaler Uhrzeit; Heute/Morgen/Zukunft/Überfällig; ohne dauernden Hintergrunddienst | Fälligkeit, Zeitzone, doppelte Stunde der Zeitumstellung und dauerhafte Speicherung geprüft | Menschliche Terminabnahme |
| G – Kontextverknüpfungen | Rechnung → Aufgabe, Akte → Aufgabe, Kontakt → Wiedervorlage; Rücknavigation; zentrale Datensätze | Ressourcenrechte/FKs und Root-Routen mit korrekten Bezügen; reale Quellenanbindung geprüft | Gesamtabnahme der Kontextabläufe |
| H – Arbeitsplatz | Eigene offene, heute fällige und überfällige Aufgaben samt Link; vorhandener Datenabgleich | HTTP-Zähler und Root-Arbeitsplatznavigation geprüft | Menschliche Arbeitsplatzabnahme |

Notfallreset verlangt Passwort und einen frischen eigenen zweiten Faktor des handelnden Administrators. Eine organisatorisch bestätigte Identitätsprüfung mit dokumentiertem Grund ist verpflichtend; die Anwendung beweist die Identität damit nicht technisch. Bei Administratorkonten sind Antragsteller und Genehmiger zwei andere aktive Administratoren; Zustimmung führt den Reset noch nicht aus. Freigabe und Zielkontostand sind versioniert und gebunden, Durchführung und Zustandsänderungen werden gemeinsam gespeichert und auditiert. Es gibt keinen Selbstreset und keinen letzten-Administrator-Bypass. Beim Reset werden auch das alte Passwort und alte Sitzungen ungültig. Der vorhandene Passwort-Wiederherstellungsmechanismus liefert einen einmalig angezeigten, persönlich zu übergebenden Code; gespeichert wird dessen Hash. Erst nach persönlichem Passwortreset folgt die weiterhin zwingende MFA-Neueinrichtung. So reicht ein gestohlenes altes Passwort nach dem Notfallreset nicht zur Einrichtung eines neuen Faktors. Die Benutzerübersicht aktualisiert den Status „MFA-Neueinrichtung erforderlich“ unmittelbar.

Aufgaben sind für Mitarbeiter auf Ersteller/Zuständigkeit und vorhandene Ressourcenfreigaben begrenzt; Administratoren behalten ihre vorhandenen Rechte. Auch die ausgewählte zuständige Person muss den Bezug sehen dürfen. Ressourcenfreigabeentzug sperrt nachfolgende Aufgabenabrufe. Der Verlauf wird unverändert ergänzt; die eingeschränkte Datenbank-Laufzeitrolle erhält keine Änderungs-/Löschrechte darauf. Die Anwendungszeitzone ist explizit konfigurierbar, Standard `Europe/Berlin`; Kalenderaufgaben ohne Uhrzeit werden erst nach Ende ihres Tages überfällig. Ungültige Ortszeiten werden abgelehnt, bei doppelten Uhrzeiten gilt der spätere Zeitpunkt. Keine automatische rechtliche Fristberechnung und kein Versand oder finanzieller Folgevorgang.

Neue additive Migration: `20261019210000_MfaRichtlinieUndAufgaben` mit vier neuen Tabellen und einem Benutzerflag. Sie wurde nur auf einer frisch erzeugten synthetischen PostgreSQL-18-Datenbank angewendet. Bestandskonto, Passwort-Hash und Rolle blieben erhalten; Modell/Snapshot stimmen überein, Schreiben mit eingeschränkter Laufzeitrolle und konkurrierender Versionskonflikt bestanden. Die Hauptdatenbank wurde nicht geöffnet oder migriert. Vor späterem Betrieb müssen die freigegebene Zielmigration und die Berechtigungen der vorhandenen Laufzeitrolle ausdrücklich geprüft/aktualisiert werden.

Tatsächlich ausgeführt: sechs unterschiedliche gezielte Backendtests (Rollenrichtlinie, Notfallprozess, zwei Aufgabenfachtests, PostgreSQL-Migration/Persistenz, erweiterter HTTPS-Ablauf) und sechs gezielte Frontendtests (QR, Root-MFA, Aufgabeneditor, zwei Kontext-/Arbeitsplatztests sowie bestehender MFA-Sicherheitsdialog). Wiederholungen erfolgten bei Änderungen oder Fehlern, keine Gesamtregression. TypeScript-/Vite-Build und Release-Webprojekt-Build erfolgreich. Der endgültige Abschlussbuild wurde nach einer Kontingentunterbrechung fortgesetzt; die bestehende automatische Frontend-Einbindung bleibt aktiv. Ein zu kurzer PostgreSQL-Verlaufstext und ein Konfliktfall mit unverändertem Entwurf wurden gezielt korrigiert. Der lange HTTP-Test verwendet für unabhängige Abläufe einen echten Hostneustart statt gelockerter produktiver Rate-Limits. Keine neuen Produktabhängigkeiten, kein Commit/Push/Deployment.

Die obige zentrale manuelle Abnahmeliste gilt jetzt für die implementierten Funktionen. Ergänzend ausdrücklich prüfen: MFA-Pflicht je Rolle einschließlich Rollenwechsel und bestehenden Sitzungen; organisatorische Identitätsprüfung, unabhängige Freigabe, persönliche Codeübergabe und Ablauf des Notfallverfahrens; Aufgaben zuweisen/archivieren, gleichzeitige Bearbeitung und automatische Aktualisierung. Windows-/Linux-Mehrbenutzerbetrieb und Schlüssel-/Datenbank-Restore bleiben spätere Betriebsgates. Keine manuelle Browser-, Smartphone-, LAN- oder Pilotabnahme durchgeführt. **MVP-02B bleibt für echte Kundendaten gesperrt.**


## 19.10.2026 - Startbare lokale Entwicklungsvorschau und gezielte Korrekturen
Visual-Studio-Profil `Betriebsplattform.Web` startet explizit die **Lokale Entwicklungsvorschau** auf `http://127.0.0.1:5180`. Alternativ: `dotnet run --project src/backend/Betriebsplattform.Web --launch-profile Betriebsplattform.Web`. Der bestehende inkrementelle Webbuild erzeugt aktuelle TypeScript/Vite-Assets. Zweites Profil: `Persoenliche Anmeldung (HTTPS-Konfiguration erforderlich)`; fehlende echte Betriebskonfiguration bleibt gesperrt.
Vorschau nur Development plus expliziter Modus, nur Loopback/zulassiger lokaler Hostname, kein DbContext und keine PostgreSQL-Verbindung. Geschaeftsansichten verwenden den vorhandenen synthetischen Demo-Bestand. Aufgaben/Profil/Benutzer/MFA sind ein getrennter fluechtiger Browser-Fixtureadapter, deutlich als Simulation gekennzeichnet. Keine echten Passwoerter eingeben. Konten-/Admin-/Aufgaben-Serverendpunkte sind dort gesperrt. Nicht simulierte Kontenaktionen und Notfallreset liefern einen ausdruecklichen Einschraenkungshinweis. Neuladen setzt Browserdaten zurueck; Hostneustart setzt Geschaefts-Beispielbestand zurueck. Vorschauerfolge belegen keine echte Authentifizierung, MFA-Sicherheit oder PostgreSQL-Persistenz.
Separater bereits gebauter Rueckfallstand im privaten Sicherungsbereich, Startbefehl und Laufzeitvoraussetzungen im privaten Entwicklungsplan. Er wurde unabhaengig gestartet und danach nicht mehr veraendert; keine Quellbuild-/Node-/Datenbankabhaengigkeit beim Start. Debugbuild/Publish/HTTP/aktuelle Assets und Loopback geprueft; kein manueller Visual-Studio- oder Browsernachweis.
Produktionskorrekturen: Historieninhalte werden je altem Stand gegen Ressourcenfreigaben und historische Zustaendigkeit geprueft; fehlende/unguelle Snapshots werden redigiert. Verlauf jetzt20/Seite inklusive UI-Navigation. Mitarbeitersuche und Seiten nach Berechtigungsfilter ohne200er-Abbruch. Regression fuer Bezugs-/Zustaendigkeitswechsel,103Verlaufseintraege,203synthetischeKonten, Archiv/Wiederoeffnen und ungueltigeSommerzeit. Echte Aufgabenendpunkte mitSQLite und synthetischer Testidentitaet pruefen Filter, Sortierung, Seiten und Kalendergrenzen; dies ist kein Authentifizierungsnachweis.
Vorhandene MFA-CAS-Mechanismen wurden durch parallele PostgreSQL-Anfragen nachgewiesen: genau eine Richtlinienaenderung, eine unabhaengige Genehmigung und eine Reset-Ausfuehrung; MFA-Pflicht/Neueinrichtung und Audit bleiben erhalten. Ausschliesslich frische isolierte PostgreSQL18-Testdatenbank, keine Hauptdatenbankmigration und keine neue Migration erforderlich. Kein letzter-Administrator-Bypass.
Nachweise: `private/pruefungen/vorschau/` mit TRX, TAP, HTTP-Ergebnissen, Buildlog, Quellhashmanifest und Zuordnung. Sechs verschiedene gezielte Backendfaelle und drei Frontendfaelle bestanden; keine Browser-/LAN-/Gesamtproduktabnahme. Ein anfangs zu breiter Testfilter erfasste zusaetzlich40 bestehende Importvorschaufaelle (bestanden); der einzige fehlgeschlagene neue HTTP-Fall hatte eine falsche Test-URL und wurde gezielt korrigiert/wiederholt. Build ohne Warnungen/Fehler, Diff einschliesslich neuer Dateien geprueft.
Offen bleiben menschliche VS-/Browser-/Smartphone-/Mehrbenutzer-/LAN-/Restore-Abnahme, echter HTTPS-/PostgreSQL-/Keyring-/Erstkonto-Betrieb und letzter-Administrator-Betreiberrecovery. Kein umfassender React-HTTP-PostgreSQL-End-to-End-Nachweis behauptet. MVP-02B bleibt fuer echte Kundendaten GESPERRT. Kein Commit/Push/Deployment.


### 19.10.2026 - Ruhige Produktoberflaeche in der lokalen Vorschau
Gelber globalerVorschaubalken entfernt; einmalige Footer-Badge LokaleVorschau/Beispieldaten mitDetails inEinstellungen. Beispielrollenwechsel nurdort, StandardAdmin und normaleerfundeneNamen. Profilmenue: MeinProfil, Sicherheit, Admin-Benutzerverwaltung; echteAbmeldung nurimKontenbetrieb. BestehendeglobaleSuche behalten, auf320px begrenzt. Einstellungen nachAdministration/Arbeitsplatz geordnet. Aufgaben mitvierSchnellansichten/kompakterSuche, Zusatzfilter eingeklappt; ruhigeStatus-/Terminanzeigen. Dialog mitGrunddaten, optionalemBezug, Zustaendigkeit, Planung undklarenSpeichern/Abbrechen-Aktionen. KeinezweiteUI oderBackend-/Authaenderung. ViergezielteReact/jsdomFaelle bestanden, TypeScript/Vite/Web-Debugbuild undHTTP/aktuelleAssets geprueft. KeinevisuelleBrowser-/VS-/LAN-/Gesamtproduktabnahme. BestehenderStartsnapshot19Dateien unveraendert. Nachweise private/pruefungen/ui-ux; MVP-02B weiterfuerEchtdatengesperrt.


## Dokumentpipeline 21.10.2026 – abgegrenzter Entwicklungsnachweis

BESTANDEN: zehn tatsächliche fiktive Paket-PDFs mit Originalhash/Seiten und33 ausgewählten Soll-Ist-Feldvergleichen; sieben Text-PDFs direkt, zwei echte Scan-PDFs per lokalerOCR, gemischtesPDF nur Seite2perOCR. Quellen sind tatsächliche PDF-Grundlinien beziehungsweise OCR-Rahmen. Eine ergänzende echte90°-PDF-Prüfung und beschädigteDatei wurden kontrolliert behandelt. Ausstellerrollen bleiben ohne ausreichenden Anker zunächst unbekannt.

BESTANDEN: Edge/HTTPS/echteAnmeldung/ProduktAPI/isoliertesPostgreSQL18 mit tatsächlichenMonats-PDFs, Feldkorrektur, versioniertemMuster und unabhängigerFolgerechnung. AktuelleNummer/Betrag statt alterWerte; Layoutänderung stopptMusterautomatik. Explizite administrativeKlassifizierungsfreigabe und Widerruf, Quellen-/Rollen-/CSRF-Negativtests sowie wiederangezeigteDaten nachBrowserreload/Prozess-/Hostneustart. Dateiannahme erfolgte durch einen geschlossenen isoliertenFixtureeinlass, kein produktiverUpload-E2E.

BESTANDEN: Backend337Tests/8bewussteSkips, Frontend118/118 einschließlich echtemHTTP-Vorschauablauf, Repository7/7; Debug/Release undTypeScript/Vite. PersistenterScanordner mit echterDatei, Wiederaufnahme undDubletten inSQLite undisoliertemPostgreSQL18. ProduktiveMalwaregrenze weiterhin geschlossen. WIAinventar0Geräte, keineAufnahme. Ein beaufsichtigterBrowserlauf lief ins45-Minuten-Limit; Fortsetzung mitderselbenTestDB bestand. Diese Entwicklungsnachweise ersetzen keine unabhängigePraxis-/Hardware-/UX-/Pilotabnahme.

TEILWEISE: nurRechnung/Behördenbrief/Versicherung mitrobustenbekanntenAnkern, nurinterneKlassifizierung automatischfreigebbar; keine Stufe4, E-Rechnungs-/KI-/Scanner-Companion-Module. SchutzvorEntwurfsverlust technischergänzt; AblehnungdesnativenDialogs imBrowser durchSteuerungsgrenze offen. Grenzen undKonsequenzen in [ADR0005](adr/0005-lokale-dokumentanalyse-und-kontrollierte-muster.md), lokaler optionalerAufbau in [CONTRIBUTING](../../CONTRIBUTING.md). KeineKundendaten, Installationen, HauptDBmigrationen, Commits/Pushes oderLibrary-Synchronisierung.


## Gezielte Dokumenten-UX – 21.10.2026

BESTANDEN: bestehende Prüfoberfläche fachlich gruppiert, kurze Felder begrenzt, lokale Bestätigen-/Fundstellenaktionen kompakt, Kontext/Zuordnung/Prüfentscheidung klar getrennt. Sichtbarer Dateieingang mit ehrlich deaktivierter Scanneraufnahme; vorhandene geschützte Scanquellenstatusabfrage bleibt erreichbar. Inline-Verwerfentscheidung erhält Entwurf und Fokus; reine Zoomänderung erzeugt keine Datenänderungswarnung. Keine Backend-, Erkennungs-, Rechte- oder Modelländerung.

Echter authentifizierter Edge-Browser mit isoliertem PostgreSQL: Original/Markierungen, Dokumentzoom200%, Speicherung/erneutes Öffnen und Tastaturablauf geprüft. Responsive Sichtprüfung bei ungefähr1440/1024/768/375CSS-Pixeln, kein gemessener Seitenüberlauf. 118Frontendtests einschließlichHTTP,18gezielteBackendtests mit echtenPDF/OCR-/Muster-/Korrektur-/Rechtefällen,7Repositorytests sowie Debug/Release/TypeScript/Vite bestanden. Kein neuer pauschaler Gesamtbackendnachweis.

AUSSTEHEND: unabhängige externe PDF-Dateien noch nicht lokal vorhanden; keine Ersatzdownloads. Menschliche Design-/Fachabnahme und Produktions-/Scannerfreigaben bleiben offen. Dokumentinterner200%-Zoom ersetzt keinen vollständigen Browserzoom-/Screenreader-/WCAG-Audit. Die flüchtige Vorschau ersetzt keine PostgreSQL-Persistenz.


## UX-, Navigations- und Druckfortsetzung – 22.10.2026

BESTANDEN: gemeinsame Aufgabenaktion bleibt bei Rechnung, Akte und Kontakt über dem Ursprung; Abbrechen erhält im Browser Route, Datensatz, aktiven Akten-Tab und Fokus. Globale Aufgaben-/Posteingangszähler liegen im Arbeitsplatz. Die Rechnungsrichtung stammt aus der bestehenden Unternehmensperspektive, Beträge weiter aus dem Backend. Firmenauswahl lädt fehlende Vorschaukontakte und begrenzt Suchergebnisse auf 20 pro Seite. Kontaktanschriften sind kompakt und responsiv. PDF-/TXT-Originalvorschau ist auch ohne Positionsanalyse direkt in der Prüfung erreichbar; unbekannte Positionen werden nicht erfunden. Eine nach Musterwiderruf veraltete Bestätigung wird am Feld kenntlich und verlangt bewusste erneute Prüfung.

BESTANDEN: 126 Frontendtests einschließlich bestehendem HTTP-Vorschaupfad, 27 gezielte Backendtests mit echten PDF-/OCR-Dateien, isolierten PostgreSQL-Gates, Scanordner und lesender WIA-Abfrage. Debug/Release sowie TypeScript/Vite bestanden. Begleiteter realer HTTPS-/Browser-/PostgreSQL-Lauf mit Speichern, Neuladen, Hostneustart und 15 Sicherheitsfällen bestanden; keine zusätzliche Aufgabe oder Originaländerung. Diese Ebenen sind getrennte Nachweise und keine Produktivfreigabe.

TEILWEISE: kontextnahe Druckaktionen für vorhandene Rechnungs-, Akten-, Kontakt-, Aufgaben-, Original- und Korrespondenzansichten; A4-Regeln und gezielte Bereichsauswahl. Aufruf und Zustandsbereinigung sind automatisiert geprüft. Ein längerer fiktiver Brief wurde im Browser erfasst; der Druckaufruf blockierte die verfügbare Steuerung. Druckvorschau, PDF-Ausgabe und echte Papierausgabe sind deshalb nicht abgenommen. Es wurde kein Druckauftrag bestätigt.

TEILWEISE: Scannereinrichtung mit echter administrativer WIA-Hostsuche über HTTPS. Herstellerübergreifende Geräteauswahl wurde auf UI-Ebene mit simulierten Profilen geprüft; dies ist kein Adapter-/Hardware-Kompatibilitätsnachweis. Direkte Aufnahme, Client-Companion, TWAIN, Mehrseiten-/Duplex-Hardware und Kundeninstaller sind NICHT IMPLEMENTIERT beziehungsweise NICHT GETESTET. Produktions-Malwaregate bleibt gesperrt. Ein positiver lokaler Defender-Einzeldateitest ist keine produktive Freigabe. Grenzen, Alternativen und Revision stehen in [ADR 0005](adr/0005-lokale-dokumentanalyse-und-kontrollierte-muster.md).

Browsermaße: Dokumentprüfung mit geladener echter PDF-Seite und 18 Markierungen bei 1440/1024/767/375 CSS-Pixeln ohne gemessenen Seiten-/Dialogüberlauf; schmal einspaltig. Dokumentzoom 200 % geprüft, kein vollständiger Browserzoom-/WCAG-Nachweis. Kontaktformulare bei denselben Breiten ohne gemessenen Überlauf; Hausnummer/PLZ jeweils etwa 100 px, Ort responsiv. Menschliche UX-, Hardware- und Druckabnahme bleibt offen.


Zusätzlicher Reviewnachweis: eingebettete Vorschau- und Blätterbuttons konnten durch den HTML-Standardtyp ein Elternformular absenden. Reproduzierbare rote Tests, danach explizites `type="button"` für Vergrößern, Rückkehr, PDF-Wiederholung und gemeinsame Pagination; gezielte Tests und gesamte Frontendregression erneut bestanden. Serverseitige Validierung/Autorisierung wurde nicht geändert.


## Qualitäts-/Architekturprüfung 25.10.2026

Aktueller Arbeitsbaum geprüft, vorhandene lokale Änderungen erhalten. Abschluss: 413 Backendtests erfolgreich, 19 opt-in/Helfertests im normalen Lauf übersprungen; neun davon in getrennten isolierten PostgreSQL-/HTTPS-/Restore-/Browsergates erfolgreich ausgeführt. 171 Frontendtests einschließlich realer HTTP-Vorschaukette, 7 öffentliche Repositorytests und 25 optionale lokale Harnessprüfungen erfolgreich. Debug/Release erfolgreich mit 0 .NET-Warnungen/Fehlern; TypeScript/Vite erfolgreich mit bestehender Warnung zum 521,58-kB-Hauptbundle. npm-/NuGet-Advisoryabfragen meldeten keine bekannten anfälligen Pakete gemäß den abgefragten Quellen. Keine Produktions-/Lizenz-/Sicherheitsfreigabe.

Behoben: unverständlicher Antwortlesefehler nach HTTP-Headern, veraltete synthetische Kontaktfixtures, HTTP-Zielaktenauswahl und instabile feste Navigationswartezeiten. Verweigerte Akten-/Notizänderungen werden zusätzlich durch echten PostgreSQL-Zustandsvergleich abgesichert. Architektur-/Teststrukturentscheidung und Alternativen stehen in der Architekturübersicht; keine Projektaufteilung oder neue Abhängigkeit erzwungen.

Echter betreuter Browserlauf mit neuem synthetischem Konto, HTTPS, neuer isolierter PostgreSQL-Datenbank und Hostneustart: Aufgabe genau einmal angelegt und danach wieder angezeigt. Authentifizierte Aufgabenansicht bei 1366×768, 1440×900 und 1920×1080 ohne horizontalen Seitenüberlauf. Vorschauprofil mit tatsächlichen Profilargumenten und bytegleichen aktuellen Assets geprüft. Echter 200%-Browserzoom, Screenreader, vollständige Sichtabnahme aller Ansichten, Hardware und reale Modellinferenz bleiben offen. Keine bestehende Kundendatenbank oder Sicherung verändert; neue Testbestände bleiben erhalten. Historische Abschnitte dieses Dokuments sind keine Nachweise für spätere Quellstände.
# Kontaktberechtigungen – durchgängiger Pilot, 29.10.2026

Auftrag: bestehende Mitarbeiter-/Administratorrollen und Datenfilter erhalten; nur Kontakte als vollständigen ersten Bereich implementieren. Ausgangsstand: umfangreiche vorhandene Änderungen wurden erhalten. Bisherige Ressourcenfreigaben unterschieden keine Kontaktaktionen; keine Bereichsprofile oder Zuweisungsmatrix vorhanden.

Implementiert: [ADR 0007](adr/0007-kontaktberechtigungen.md), zentraler Aktionskatalog, versionierte Vorlagen Büromitarbeiter/Teamleitung, explizite Abweichungen, starke Admin-Zuweisung, unveränderlicher Berechtigungsaudit und Matrix. Unterstützt: Lesen/Erstellen/Bearbeiten/Archivieren; kein endgültiges Löschen. Neue Konten ohne Freigabe verweigert, vorhandene Mitarbeiter durch additive Migration als ausdrücklich gekennzeichneten Bestand übernommen. Ressourcenfreigaben bleiben separat. Migration und Laufzeitrechte wurden ausschließlich auf neuen synthetischen PostgreSQL-Testdatenbanken geprüft, nicht auf vorhandene Bestände angewendet.

Ausgeführt nach letzter Produktänderung:

- Backend: `dotnet test tests/backend/Betriebsplattform.Rechnungen.Tests --no-build --no-restore --logger "console;verbosity=minimal"`: 428 bestanden, 21 bewusst übersprungene externe/Hardware-/Livegates, Exit 0. Build davor durch gezielte Tests mit `--no-restore -p:BuildFrontend=false`. Keine neuen Pakete.
- Separat: prozesslokal `FZ_KONTAKTRECHTE_POSTGRES=1`, Filter `FullyQualifiedName~KontaktrechtePostgreSqlTests|FullyQualifiedName~KontaktberechtigungenTests`: 4 bestanden, 0 übersprungen, Exit 0. Tatsächliche PostgreSQL-18-Migration vom letzten Schema `TerminadresseUndErinnerung`, erhaltene Rollen/Hashes/Stamps/Freigaben, restricted runtime role, echte HTTPS-Kontakt-/Admin-APIs, starke Bestätigung, erlaubte Create/Update- und verweigerte Aktionen, Ressourcenbegrenzung, unmittelbarer Entzug, Vorlagen-/Audit-Datenbankschutz. Neue GUID-Datenbanken `fz_kontaktrechte_test_*` und Testrollen bleiben zur Inspektion erhalten; Hauptdatenbank nie geöffnet. Unit-/Migrationsfälle zusätzlich mit synthetischem SQLite. Kein Browser-E2E daraus ableiten.
- Frontend: `npm.cmd test`: 182 bestanden, 1 optionaler HTTP-Test übersprungen, Exit 0; React/jsdom und synthetische API-Antworten. `npm.cmd run build`: TypeScript/Vite erfolgreich, bekannter Hinweis Hauptbundle >500 kB.
- Repository: `python -B -m unittest discover -s tests/repository -v`: 7 bestanden, Exit 0. Neue öffentliche Dateien gezielt inventarisiert; keine private Konfiguration veröffentlicht. Diff-Whitespace-Prüfung bestanden.

Prüfstand SHA-256: `Sicherheit/Kontaktberechtigungen.cs` = `C820C25A6FBAFE52D6D43E095C1D43954F26891C95577FB357151EAFF2F0CB1E`; `Persistenz/Berechtigungsmodell.cs` = `BAF7772A3744AAC2C8397C8BEB83916954112B1C27C8B48386A88130731A88C4`; neue Migration = `6C3A539C92B3F3FA98D38EEB4FE317D7033BF128F6163E84ED4B51B6FD77ABFB`; `src/frontend/src/Kontaktrechte.tsx` = `5F6F6D8B3C30BA32768E1DBA4BD1B2B437F7204BB5D8EAB07B0B0818D0DAF55B`.

Reflexion: Der erste Test war mangels Katalog rot. Ein SQLite-Migrationsprüfer musste generiertes SQL direkt statt über EF-Formatplatzhalter ausführen, weil Berechtigungs-JSON geschweifte Klammern enthält. Bestehende UI-Fixtures wurden um den zusätzlichen lesenden Matrix-Aufruf ergänzt. Die separate Gegenprüfung stellte sicher, dass eine Rechte-Transaktion keine gespeicherten MFA-Fehlversuchszähler zurückrollt; dafür besteht ein positiver/negativer Regressionstest. Kein zweiter Agent/Provider eingesetzt. Nicht geprüft: tatsächliche Browserdarstellung/Screenreader und produktive Übernahme. Nächster Schritt: bewusst freigegebene Migration plus `runtime-grants` auf dem vorgesehenen Bestand, anschließend persönliche Abnahme. Andere Bereichsmatrizen und zentrale Datenbereichsverwaltung bleiben offen. Kein Commit, Push oder Deployment; keine Library-Synchronisierung.
