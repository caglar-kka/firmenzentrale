[English](README.md) | [Deutsch](README.de.md)

# FirmenZentrale

Eine modulare Betriebs- und Arbeitsplattform für kleine und mittlere Unternehmen im DACH-Raum.

**Vorläufiger Produktname – kann sich mit der Weiterentwicklung des Produkts ändern.**

## Überblick

FirmenZentrale bündelt Dokumente, digitale Akten, Rechnungen, Kontakte, Korrespondenz und Geschäftsabläufe in einer zentralen Arbeitsoberfläche. Zielgruppe sind kleine und mittlere Unternehmen in Deutschland, Österreich und der Schweiz.

Die Anwendung ist derzeit auf Deutsch ausgerichtet. Der Einstieg in die Repository-Dokumentation ist primär [Englisch](README.md), ergänzt durch diese deutsche README; die technische Dokumentation bleibt überwiegend deutsch. Aktuelle Beispiele verwenden `de-DE` und EUR. Daraus folgen weder länderspezifische Steuerkonformität noch CHF-Unterstützung oder internationale Produktreife.

## Aktueller Stand

Lokaler Prototyp in Entwicklung. MVP-01.5 konsolidiert bestehende Fachabläufe, Suche, Datenqualität und gezielte Sicherheitskontrollen. Ausschließlich erfundene/synthetische Daten werden unterstützt. Produktive Authentifizierung und PostgreSQL-Persistenz fehlen; Arbeitsdaten liegen im Arbeitsspeicher und können bei einem Neustart verloren gehen.

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
| Persistenz | In-Memory-Prototyp; PostgreSQL 18, EF Core 10 und Npgsql sind geplant |
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

## Entwicklungsdaten & Sicherheit

Keine echten Kunden-, Unternehmens-, Personal-, Finanz- oder Zugangsdaten in Anwendung, Repository, Issues oder Anhängen verwenden. Originaldokumente bleiben unverändert; unsichere Kontaktänderungen, Zuordnungen und Importzeilen verlangen menschliche Prüfung. Makros werden nicht ausgeführt und Excel-Statuswerte bestätigen keine Zahlung.

Aktenanhänge unterstützen derzeit PDF, JPG/JPEG, PNG, DOCX, XLSX, TXT und CSV unter serverseitiger Typ-/Inhalts-/Größenprüfung (maximal 20 MB; teils strengere Inhaltsgrenzen). Die Vorschau hängt vom Format ab. Excel als Dokument anhängen ist vom strukturierten Datenimport getrennt.

Der strukturierte Import unterstützt ausschließlich die synthetische, makrofreie MVP-02-XLSX-Vorlage: maximal 2 MiB, ein sichtbares Blatt `ISD-Testdaten` und die neun Spalten `DatensatzId`, `Art`, `Nummer`, `Von`, `An`, `Datum`, `Betrag`, `Waehrung`, `Beleg`. Beliebige Arbeitsmappen werden nicht unterstützt. `ISD` bleibt eine Quellen-/Vorlagenbezeichnung, keine bestätigte Rechnungsart und kein verifizierter vollständiger Firmenname. Erzeugte Testdateien, Uploads und lokale Laufzeitdaten bleiben von Git ausgeschlossen. Ein öffentlicher Datenbankzugriff ist nicht eingerichtet.

## Aktuelle Grenzen

- Keine produktive Authentifizierung, Mandantentrennung, dauerhafte PostgreSQL-Persistenz oder produktive Bereitstellungsfreigabe.
- Datensätze, Importe und Historien im Arbeitsspeicher sind flüchtig und kein dauerhaftes Geschäftsarchiv.
- Keine produktive E-Mail-Integration oder Versandfunktion, direkte Scannerintegration, allgemeine OCR oder KI-Klassifikation.
- Keine vollständige Buchhaltung, automatischen Finanzbuchungen, allgemeine Excel-Migration oder länderspezifische Steuerimplementierung.
- Produktive Backups/Wiederherstellung, Rechteverwaltung, Betriebsüberwachung sowie weitere Barrierefreiheits-/Sicherheitsprüfungen bleiben Pilot-Gates. Bestehende Prüfungen belegen keine Produktivreife.

## Dokumentation

- [Architektur](docs/public/ARCHITEKTUR.md)
- ADRs: [Rechnungs-/Zahlungskern](docs/public/adr/0001-rechnungs-und-zahlungskern.md), [Demoimport und PDF-Vorschau](docs/public/adr/0002-lokaler-demoimport-und-pdf-vorschau.md), [Akten und Eingangsadapter](docs/public/adr/0003-zentrale-vorgangsakte-und-eingangsadapter.md), [Kontakte und Kategorien](docs/public/adr/0004-kontakte-kategorien-und-arbeitsumgebung.md)
- [Mitarbeit](CONTRIBUTING.md)
- [Entwicklungsnachweise und Grenzen](docs/public/MVP-01-NACHWEIS.md)
- [Frontend-Designregeln](src/frontend/DESIGN.md)

## Roadmap / Nächster Meilenstein

MVP-02A ist der nächste geplante Meilenstein in Richtung kontrollierter PostgreSQL-Persistenz. Schema/Migrationen, dauerhafte Transaktionen/Audit, Zugriffsgrenzen und Backup/Wiederherstellung müssen gesondert implementiert und geprüft werden. Diese README meldet sie nicht als fertig und erteilt keine Freigabe zur Migration produktiver Daten. Optionale Integrationen und weitere Module bleiben spätere Aufgaben.

## Lizenz

Eine Open-Source-Lizenz wurde noch nicht ausgewählt. Die öffentliche Verfügbarkeit erteilt allein keine ausdrückliche Open-Source-Lizenz.
