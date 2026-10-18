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

## Integrierter Arbeitsraum

Die vorläufig akzeptierte Graphit-/Off-White-/Indigo-Gestaltung ist die reguläre Oberfläche unter `http://127.0.0.1:5180/#rechnungen`. Kein Vorschauparameter erforderlich. Rechnungsbestand mit Suche, Zahlungsfilter und Sortierung; Rechnungsnummer öffnet die eigene Zahlungs-/Belegansicht. Der sichtbare Button „Zurück zur Rechnungsübersicht“ und der Breadcrumb erhalten Suchbegriff, Filter, Sortierung und den Tastaturfokus.

Aussteller/Empfänger und Zahler/Zahlungsempfänger bleiben ausdrücklich getrennt. Bestätigte Zuordnung und offene Beträge kommen aus dem Backend, Fälligkeiten sind nicht erfasst. Die Importvorschau bewahrt Originalwerte und Zell-/Dateiquellen; keine Buchung oder Freigabeaktion. Tablet- und Quelltabellen besitzen eigene Tastatur-Scrollbereiche, mobil beschriftete Datensätze.

Es läuft eine Anwendung mit gemeinsamen Komponenten und Tokens. Frühere A/B/C-Quellen und Tests wurden privat erhalten; historische Importdateien sind nur Kompatibilitätsverweise. Verbindliche UI-Quelle: [Designsystem](src/frontend/DESIGN.md). Aktuelle Prüfungen und Grenzen: [Entwicklungsnachweis](docs/public/MVP-01-NACHWEIS.md).

Die vollständige Dokumenten- und Belegakte bleibt [fachlich vorbereitet](docs/public/ARCHITEKTUR.md); DOK-01 implementiert jetzt ausschließlich einen lesenden synthetischen PDF-Ausschnitt. Rechnung SYNTH-2026-002 besitzt eine echte künstliche Originalrechnung und einen PDF-Zahlungsbeleg an der 300-Euro-Zahlung. Öffnen im neuen Browser-Tab, zum Zurückkehren den PDF-Tab schließen. Keine Uploads oder PDF-Veränderungen. Alle Dateien sind öffentliche synthetische Demo-Inhalte ohne produktiven Zugriffsschutz. Name/Logo bleiben Arbeitstitel. Regionale Vorbereitung DE/AT/CH unverändert, aktiv de-DE/EUR; keine zusätzliche CHF-/Steuerlogik.

Die Rechnungsdetails zeigen außerdem rollenbezogene synthetische Kontakte mit E-Mail-/Telefonlinks, soweit vorhanden. Diese übergeben an eine konfigurierte lokale Anwendung; sie senden keine Nachricht und bestätigen keinen Anruf. Testadressen/-nummern sind nicht erreichbar. Fehlende Angaben erscheinen ohne Scheinaktionen. Die [Architektur](docs/public/ARCHITEKTUR.md) enthält die fachliche Matrix beider Zahlungsrichtungen und den gemeinsamen Vorgangs-/Dokumenten-/Kommunikationsplan; andere Verpflichtungen und Ratenpläne sind noch nicht implementiert. Die aktuelle Gesamtbestandssumme trennt Forderungen und Verbindlichkeiten noch nicht.


DOK-01 priorisiert Desktop: kompakte Kontaktzeilen mit Mail-/Telefonicons und aufklappbaren Kontaktdetails, natürliche Rechnungsbegriffe und explizite Demo-Unternehmensperspektive. Bei Rechnung 002: eigene Zahlungen 300,00 + 200,00 EUR, noch zu bezahlen 460,00 EUR. Kein neuer Geldfluss und keine Bearbeitungshistorie erfunden. Fehlende PDF liefert eine verständliche 404-Seite; Metadatenfehler können erneut geladen werden. Unbekannte Dokumentkennungen öffnen keine beliebigen Dateien.
