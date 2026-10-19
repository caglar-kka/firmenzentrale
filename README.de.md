[English](README.md) | [Deutsch](README.de.md)

# FirmenZentrale

Eine modulare Betriebs- und Arbeitsplattform für kleine und mittlere Unternehmen im DACH-Raum.

**Vorläufiger Produktname – kann sich mit der Weiterentwicklung des Produkts ändern.**

## Überblick

FirmenZentrale bündelt Dokumente, digitale Akten, Rechnungen, Kontakte, Korrespondenz und Geschäftsabläufe in einer zentralen Arbeitsoberfläche. Zielgruppe sind kleine und mittlere Unternehmen in Deutschland, Österreich und der Schweiz.

Die Anwendung ist derzeit auf Deutsch ausgerichtet. Der Einstieg in die Repository-Dokumentation ist primär [Englisch](README.md), ergänzt durch diese deutsche README; die technische Dokumentation bleibt überwiegend deutsch. Aktuelle Beispiele verwenden `de-DE` und EUR. Daraus folgen weder länderspezifische Steuerkonformität noch CHF-Unterstützung oder internationale Produktreife.

## Aktueller Stand

Lokaler Prototyp in Entwicklung. MVP-02A ergänzt eine ausdrücklich aktivierbare PostgreSQL-Persistenzgrundlage neben dem bisherigen Demo-Modus. Ausschließlich erfundene/synthetische Daten werden unterstützt. Demo-Daten bleiben flüchtig; ein stiller Rückfall von PostgreSQL auf Demo ist ausgeschlossen. Migration, Schema, CRUD, Constraints, Concurrency, Transaktions-Rollback, Neustartpersistenz und API-/Browser-Smoke wurden lokal mit PostgreSQL 18.6 und synthetischen Daten erfolgreich geprüft. Lokales Backup/Restore, Framework-Cookie-Anmeldung und Ressourcenfreigaben sind inzwischen implementiert und getestet. Eine produktive Pilotfreigabe besteht weiterhin nicht; maßgeblich sind die aktuellen Sicherheitsgates.

## Kernfunktionen

- Digitale Akten mit Dokumenten, versionierten internen Notizen und dokumentierter externer Korrespondenz. Das Erfassen einer Korrespondenz versendet nichts.
- Posteingang, direkte Ablage in eine bekannte Akte, kontrollierte Dateiauswahl/Drag & Drop, Dateidublettenprüfung sowie Umzuordnung mit Verlauf und Versionsprüfung.
- Lesende Vorschau unterstützter Dokumente erst nach Auswahl; Originalrechnung und Zahlungsbeleg bleiben getrennt.
- Rechnungen, Teilzahlungen, bestätigte Zuordnungen und offene Beträge mit Unterscheidung eingehender und eigener Zahlungen. Ein manueller Status ist kein Banknachweis.
- Zentrale Firmen, Personen und Ansprechpartner, strukturierte Kontaktdaten, entscheidungspflichtige Änderungsvorschläge und Dublettenhinweise ohne automatische Zusammenführung.
- Tolerante globale Suche, Listenfilter/-sortierung und konfigurierbare Geschäftskategorien.
- Begrenzter XLSX-Ablauf mit Vorschau, Bestätigung, Übernahme und Bericht für die bestehende Vorlage. Gültige Rechnungszeilen werden ausdrücklich in den flüchtigen Bestand übernommen; Zahlungszeilen bleiben zur Prüfung.

## Tech-Stack

| Bereich | Technologie / Stand |
| --- | --- |
| Frontend | React, TypeScript (strict), Vite |
| Backend | ASP.NET Core, C#, .NET 10 |
| Persistenz | Demo im Arbeitsspeicher; optional PostgreSQL 18 / EF Core 10.0.11 / Npgsql-Provider 10.0.3 |
| Struktur | Modularer Monolith; lokal zuerst |

## Architektur

Ein React-Frontend und ein ASP.NET-Core-Host verwenden zentrale Fachdienste für Rechnungen/Zahlungen, Akten, Dokumente, Kontakte und Import. Der Posteingang ist eine Eingangsschleuse, kein dauerhaftes Archiv und kein Pflichtweg: Bei bekannter Zielakte ist eine direkte Ablage möglich. Interne Notizen und externe Korrespondenz sind fachlich verschieden.

Technische Namespaces und Projektpfade behalten vorerst ihre internen Namen. Eine produktive Datenbankintegration oder allgemeine Workflow-Engine wird damit nicht behauptet. Siehe [Architekturdokumentation](docs/public/ARCHITEKTUR.md) und [ADRs](docs/public/adr/0001-rechnungs-und-zahlungskern.md).

## Erste Schritte

Voraussetzungen: .NET-10-SDK, Node.js 24, npm und Git. Aus der Repository-Wurzel:

```powershell
dotnet restore src/backend/Betriebsplattform.slnx
Push-Location src/frontend
npm.cmd ci --ignore-scripts --no-audit --no-fund
npm.cmd run build
Pop-Location
```

Restore/Installation benötigen Registry-Zugriff, sofern nicht alle Pakete im lokalen Cache liegen. Bei vollständigem Cache den npm-Befehl um `--offline` ergänzen. Dies ersetzt weder die Abhängigkeitsprüfung noch die separat dokumentierten Sicherheitsprüfungen. Testbefehle stehen in der [Anleitung zur Mitarbeit](CONTRIBUTING.md).

## Lokal ausführen

Nach Frontend-Build und .NET-Restore:

```powershell
dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release
dotnet run --project src/backend/Betriebsplattform.Web --configuration Release --no-build --no-restore --no-launch-profile
```

`http://127.0.0.1:5180` öffnen. Beenden mit `Strg+C`. Der Host bindet an IPv4-Loopback; öffentlicher Betrieb sowie LAN-/Mobilzugriff sind nicht freigegeben.

Für den optionalen synthetischen Scan-Ordner den Startbefehl um `-- --scan-demo` ergänzen und nur synthetische Dateien in `src/backend/private/scan-eingang` ablegen. Der Adapter liest stabile Dateien, prüft Dubletten und lässt Quelldateien unverändert. Im Posteingang **Aktualisieren** verwenden. Direkte Scannersteuerung und OCR sind nicht implementiert.

Konfiguration, bewusste Migration und Live-Test-Nachweise des optionalen PostgreSQL-Entwicklungsmodus stehen in der [Architektur](docs/public/ARCHITEKTUR.md). Zugangsdaten gehören nicht ins Repository. Standard bleibt Demo.

## Entwicklungsdaten & Sicherheit

Keine echten Kunden-, Unternehmens-, Personal-, Finanz- oder Zugangsdaten in Anwendung, Repository, Issues oder Anhängen verwenden. Originaldokumente bleiben unverändert; unsichere Kontaktänderungen, Zuordnungen und Importzeilen verlangen menschliche Prüfung. Makros werden nicht ausgeführt und Excel-Statuswerte bestätigen keine Zahlung.

Aktenanhänge unterstützen derzeit PDF, JPG/JPEG, PNG, DOCX, XLSX, TXT und CSV unter serverseitiger Typ-/Inhalts-/Größenprüfung (maximal 20 MB; teils strengere Inhaltsgrenzen). Die Vorschau hängt vom Format ab. Excel als Dokument anhängen ist vom strukturierten Datenimport getrennt.

Der strukturierte Import unterstützt ausschließlich die synthetische, makrofreie MVP-02-XLSX-Vorlage: maximal 2 MiB, ein sichtbares Blatt `ISD-Testdaten` und die neun Spalten `DatensatzId`, `Art`, `Nummer`, `Von`, `An`, `Datum`, `Betrag`, `Waehrung`, `Beleg`. Beliebige Arbeitsmappen werden nicht unterstützt. `ISD` bleibt eine Quellen-/Vorlagenbezeichnung, keine bestätigte Rechnungsart und kein verifizierter vollständiger Firmenname. Erzeugte Testdateien, Uploads und lokale Laufzeitdaten bleiben von Git ausgeschlossen. Ein öffentlicher Datenbankzugriff ist nicht eingerichtet.

## Aktuelle Grenzen

- Individuelle Cookie-Anmeldung und Ressourcenrechte sind implementiert; produktive Bereitstellung, Betriebskontrollen und Mandantentrennung sind nicht freigegeben. Das aktuelle Security-/Pilot-Gate ist NICHT BESTANDEN.
- Demo-Datensätze, Importe und Historien bleiben flüchtig. PostgreSQL startet ohne Beispieldaten; begrenzter XLSX-Import und Scan-Ordner bleiben im Demo-Modus. Dauerhafte lokale Dateiablage und koordiniertes Offline-Backup/Restore sind implementiert; Betreiberrechte, unabhängige Sicherungsmedien/Aufbewahrung und Produktivfreigabe bleiben offen.
- Keine produktive E-Mail-Integration oder Versandfunktion, direkte Scannerintegration, allgemeine OCR oder KI-Klassifikation.
- Keine vollständige Buchhaltung, automatischen Finanzbuchungen, allgemeine Excel-Migration oder länderspezifische Steuerimplementierung.
- Vertrauenswürdiges HTTPS, Betriebssystem-/Backuprechte, Malware-Anbindung, Netzwerkeinschränkung, unabhängige Sicherheitsprüfung und Betriebsüberwachung bleiben Pilot-Gates. Pilotuploads bleiben ohne eingerichtete Prüfung gesperrt. Bestehende Nachweise belegen keine Produktivreife.

## Dokumentation

- [Architektur](docs/public/ARCHITEKTUR.md)
- ADRs: [Rechnungs-/Zahlungskern](docs/public/adr/0001-rechnungs-und-zahlungskern.md), [Demoimport und PDF-Vorschau](docs/public/adr/0002-lokaler-demoimport-und-pdf-vorschau.md), [Akten und Eingangsadapter](docs/public/adr/0003-zentrale-vorgangsakte-und-eingangsadapter.md), [Kontakte und Kategorien](docs/public/adr/0004-kontakte-kategorien-und-arbeitsumgebung.md)
- [Mitarbeit](CONTRIBUTING.md)
- [Entwicklungsnachweise und Grenzen](docs/public/MVP-01-NACHWEIS.md)
- [Frontend-Designregeln](src/frontend/DESIGN.md)

## Roadmap / Nächster Meilenstein

MVP-02B2–02B4 ergänzt echtes isoliertes Backup/Restore, Bestandsprüfung beim Wiederanlauf, individuelle Cookie-Anmeldung, Ressourcenfreigaben, eingeschränkte Datenbank-Laufzeitrechte und eine lokale Publish-Grundlage. Die PostgreSQL-/Auth-/Restore-Prüfungen mit Beispieldaten sind bestanden; ein Echtdatenpilot ist nicht freigegeben. Die bestehende PostgreSQL-Wildcardbindung muss geprüft/eingeschränkt werden; vertrauenswürdiges HTTPS, Betriebssystem-/Backuprechte, Malware-Prüfung und abschließende menschliche Sicherheitsfreigabe bleiben offen. Vorgehen siehe bestehende Architektur-/Betriebsdokumentation. Keine produktive Datenmigration ist freigegeben.

## Lizenz

Eine Open-Source-Lizenz wurde noch nicht ausgewählt. Die öffentliche Verfügbarkeit erteilt allein keine ausdrückliche Open-Source-Lizenz.
