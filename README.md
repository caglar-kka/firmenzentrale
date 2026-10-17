# Betriebsplattform

Eine geplante, branchenneutrale Betriebssoftware zur nachvollziehbaren Verwaltung von Rechnungen und Zahlungen.

## Entwicklungsstand

Das Projekt befindet sich in der Vorbereitung. Es gibt noch keine ausführbare Produktanwendung. Die Dokumentation beschreibt das Zielbild und behauptet keine bereits implementierten Funktionen.

## Erste Produktstrecke

- Rechnung mit Aussteller, Empfänger, Datum und Betrag erfassen.
- Zahlung mit Zahler, Zahlungsempfänger, Datum und Beleg nachvollziehen.
- Zahlungen und Teilzahlungen Rechnungen zuordnen und den offenen Betrag ermitteln.
- Herkunft und Änderungen nachvollziehbar halten.
- Anschließend eine kleine Excel-Importvorschau mit Prüfbericht entwickeln.

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

Aktuell gibt es keine Installations- oder Startbefehle für eine Anwendung. Eine überprüfte Startanleitung folgt mit dem ersten ausführbaren Produktstand.

- [Architektur und geplante Struktur](docs/public/ARCHITEKTUR.md)
- [Mitarbeit und Git-Regeln](CONTRIBUTING.md)

## Vertraulichkeit und Lizenz

Keine echten Firmen-, Kunden-, Personal-, Zahlungs- oder Zugangsdaten in Quellcode, Tests, Issues oder Anhänge aufnehmen. Die Datenbank ist nicht für einen öffentlichen Zugriff vorgesehen.

Eine Lizenz wurde noch nicht ausgewählt. Dieses Projekt erteilt derzeit keine ausdrückliche Open-Source-Lizenz.
