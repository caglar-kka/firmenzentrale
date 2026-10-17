# Architektur und Projektstruktur

## Zielbild – noch nicht implementiert

Ein React-Frontend spricht mit einem ASP.NET-Core-Backend. Das Backend bündelt die fachlichen Module in einem modularen Monolithen und greift über EF Core und Npgsql auf PostgreSQL zu. Geschäftsdaten bleiben in der vorgesehenen lokalen Kundenumgebung. Ein späterer mobiler oder hybrider Zugang benötigt eine eigene Sicherheits- und Betriebsentscheidung.

Der Stack ist festgelegt: React, TypeScript (strict), Vite; C# und .NET 10; PostgreSQL 18 und EF Core 10. Zusätzliche Infrastruktur und Bibliotheken werden erst bei belegtem Bedarf ausgewählt.

## Fachliche Grenzen

Rechnung, Zahlung und Zuordnung erhalten klare Verantwortlichkeiten. Aussteller, Empfänger, Zahler, Zahlungsempfänger, Datum, Betrag, Beleg und Herkunft müssen nachvollziehbar sein. Regeln für Teil-, Sammel- und Überzahlungen sowie Storno werden vor ihrer Implementierung fachlich festgelegt und mit synthetischen Tests geprüft.

Excel-Dateien sind nicht vertrauenswürdig. Geplant sind Format- und Größenprüfung, keine Makroausführung, unveränderte Originalwerte, Importvorschau, freigegebenes Mapping, Prüfbericht sowie Duplikat- und Wiederholungsschutz. Vorschau und Import dürfen keine ungeprüften finanziellen Folgewirkungen haben.

## Geplante Verzeichnisstruktur

```text
README.md
CONTRIBUTING.md
docs/public/       Öffentliche Produkt- und Architekturdokumentation
src/backend/       Geplantes ASP.NET-Core-Backend
src/frontend/      Geplantes React-Frontend
tests/repository/  Prüfungen der öffentlichen Ablage
tests/backend/     Geplante Fach- und Integrationstests
tests/frontend/    Geplante Oberflächentests
```

Die Produktverzeichnisse entstehen erst mit der jeweiligen Implementierung. Es werden noch keine leeren Projekte, Abhängigkeiten oder Datenbanken eingerichtet.

## Öffentliche und private Ablage

Öffentliche Produktquellen und Tests bleiben von privaten lokalen Arbeitsunterlagen getrennt. Statt persönliche Playbooks zu bereinigen und mitzuveröffentlichen, werden sie vollständig von Git ausgeschlossen. Dadurch bleiben lokale Arbeitsabläufe erhalten; ein öffentlicher Klon enthält diese optionalen Unterlagen nicht.

Diese Trennung folgt dem Auftrag zur öffentlichen Vorbereitung. Sie benötigt erneut eine Prüfung, sobald öffentliche Tests von privaten Dateien abhängen, neue Root-Verzeichnisse hinzukommen oder gemeinsame Entwickleranweisungen veröffentlicht werden sollen. Dann sind eigenständige, neutrale Anweisungen zu erstellen und ohne private Unterlagen zu testen.

## Vor einem produktiven Einsatz

Benutzer- und Rollenrechte, Audit, sichere Updates, Monitoring, Backups mit nachgewiesenem Restore-Test sowie Betriebs- und Krisenabläufe sind erforderlich. Diese Fähigkeiten sind bisher nicht nachgewiesen.
