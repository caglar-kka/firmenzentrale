# Betriebsplattform

Eine branchenneutrale Betriebssoftware in Entwicklung zur nachvollziehbaren Verwaltung von Rechnungen und Zahlungen.

## Entwicklungsstand

MVP-01 enthält den getesteten Rechnungs- und Zahlungskern, MVP-02 die begrenzte XLSX-Importvorschau. MVP-03 verbindet beide mit einem lokalen ASP.NET-Core-Host und einer React-/TypeScript-/Vite-Oberfläche: Rechnungen, Teilzahlungen, offene Beträge und Prüfmeldungen mit Originalwerten und Quellen. Ausschließlich synthetische Beispiele; keine echte Buchhaltung oder Datenbankintegration.

ISD wird vorläufig als Firma/Geschäftspartner behandelt. Der vollständige Firmenname ist nicht bestätigt; die Rolle ergibt sich aus dem konkreten Vorgang. Originalbezeichnungen wie `ISD-Testdaten` bleiben erhalten.

## Erste Produktstrecke

- Rechnung mit Aussteller, Empfänger, Datum und Betrag erfassen.
- Zahlung mit Zahler, Zahlungsempfänger, Datum und Beleg nachvollziehen.
- Zahlungen und Teilzahlungen Rechnungen zuordnen und den offenen Betrag ermitteln.
- Herkunft und Änderungen nachvollziehbar halten.
- XLSX-Testvorlage mit SHA-256, Blatt-/Zeilen-/Zellquellen und Prüfbericht lesen, ohne Buchungen oder Zahlungsbestätigungen.

Entwicklung und Tests verwenden ausschließlich synthetische Daten. Ein manuell gesetzter Status ersetzt keinen Zahlungsbeleg. Importierte Dateien dürfen keine ungeprüften finanziellen Aktionen auslösen.

## Festgelegter Tech-Stack

| Bereich | Technologie |
|---|---|
| Frontend | React, TypeScript (strict), Vite |
| Backend | ASP.NET Core, C#, .NET 10 |
| Datenbank | PostgreSQL 18, EF Core 10, Npgsql |
| Architektur | Modularer Monolith |
| Betrieb | Lokal zuerst, später optional hybrid |

## Einstieg

Mit installiertem .NET-10-SDK aus der Repository-Wurzel:

```powershell
dotnet restore src/backend/Betriebsplattform.slnx
dotnet test src/backend/Betriebsplattform.slnx --no-restore
```

Restore benötigt entweder die Testpakete im lokalen NuGet-Cache oder einen ausdrücklich erlaubten Paketdownload. Fachkern, Importmodul und Host benötigen keine externen NuGet-Pakete.

## Lokalen Prototyp starten

Voraussetzungen: .NET-10-SDK, Node 24 und npm sowie die festgelegten Pakete im lokalen Cache. Aus der Repository-Wurzel nach dem .NET-Restore:

```powershell
Push-Location src/frontend
npm.cmd install --offline --ignore-scripts --no-audit --no-fund
npm.cmd test
npm.cmd run build
Pop-Location
dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release
dotnet run --project src/backend/Betriebsplattform.Web --configuration Release --no-build --no-restore --no-launch-profile
```

Danach im Browser `http://127.0.0.1:5180` öffnen. Beenden mit `Strg+C` im Backend-Terminal. Die Anwendung bindet ausschließlich IPv4-Loopback. Sie zeigt feste künstliche Rechnungen und drei lokal erzeugte XLSX-Prüffälle; eigene Dateien werden nicht hochgeladen. Offene Beträge werden ausschließlich im Backend berechnet. Eine Zahlung zur Prüfung bleibt ohne Saldenwirkung.

Der Offline-Befehl bricht bei fehlenden Cachepaketen ab; vor Downloads ist eine eigene Freigabe erforderlich. Keine öffentlichen Entwicklungsserver oder Datenbanken starten. Dieser Prototyp hat keine Anmeldung und ist nicht für reale Daten oder LAN-/Mobilzugriff freigegeben.

Die Tests erzeugen ausschließlich künstliche Daten. Nach `dotnet test` liegen die Beispieldatei `MVP-02-synthetisch.xlsx` und der lesbare `MVP-02-Pruefbericht.txt` unter `tests/backend/Betriebsplattform.Rechnungen.Tests/bin/Debug/net10.0/`. Diese erzeugten Dateien bleiben von Git ausgeschlossen. Der Bericht zeigt eine Rechnung, eine unbestätigte Zahlung und einen fehlerhaften Betrag. Ein schreibender Import ist nicht enthalten.

- [Architektur und geplante Struktur](docs/public/ARCHITEKTUR.md)
- [Mitarbeit und Git-Regeln](CONTRIBUTING.md)
- [Fachregeln und Grenzen von MVP-01](docs/public/adr/0001-rechnungs-und-zahlungskern.md)
- [Entwicklungsnachweise MVP-01 bis MVP-03](docs/public/MVP-01-NACHWEIS.md)

## Vertraulichkeit und Lizenz

Keine echten Firmen-, Kunden-, Personal-, Zahlungs- oder Zugangsdaten in Quellcode, Tests, Issues oder Anhänge aufnehmen. Die Datenbank ist nicht für einen öffentlichen Zugriff vorgesehen.

Eine Lizenz wurde noch nicht ausgewählt. Dieses Projekt erteilt derzeit keine ausdrückliche Open-Source-Lizenz.

## Design- und UX-Sprint

Die Rechnungsübersicht bietet eine kompakte Tabelle mit Suche nach Nummer, Partnern, Beleg oder Quelle, Zahlungsstatusfilter und Nummern-/Datums-/Partnersortierung. Die Rechnungsnummer öffnet Zahlungen und Herkunft. Bestätigte Zuordnungssummen und offene Beträge kommen aus dem Backend. Fälligkeiten wurden nicht erfasst; es gibt keine erfundenen Überfälligkeitsanzeigen.

Die Excel-Vorschau zeigt einen Prüfablauf, eine Datensatztabelle mit Fehler-/Warnfilter und erreichbaren Originalwerten. Datei-Hash und Vollbericht sind einklappbar. Kein Upload und keine Buchung. Bei Tabletbreite scrollen Tabellen in einem eigenen Bereich (auch per Links-/Rechts-Pfeiltaste bei Fokus); auf schmalen Bildschirmen stehen beschriftete Datensätze untereinander.

Verbindliche Oberflächengrundlage: [Designsystem](src/frontend/DESIGN.md). Regionale Darstellung ist für DE/AT/CH vorbereitet; aktiv bleibt de-DE/EUR. CHF-Verarbeitung ist nicht implementiert. Nachweise und offene Accessibility-Prüfungen stehen im bestehenden [Entwicklungsnachweis](docs/public/MVP-01-NACHWEIS.md). Bei bereits vorhandenen Paketen ist keine erneute Installation vor Test und Build erforderlich.
