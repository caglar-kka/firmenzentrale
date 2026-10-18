# Betriebsplattform – allgemeine Entwicklungsregeln

## Zweck und Einstieg

Die Betriebsplattform ist eine branchenneutrale Software zur nachvollziehbaren Verwaltung von Rechnungen und Zahlungen. Lies zuerst README.md, CONTRIBUTING.md und docs/public/ARCHITEKTUR.md. Unterscheide geplante Funktionen von implementiertem und tatsächlich geprüftem Verhalten.

Diese Regeln und die öffentliche Entwicklungsanleitung müssen ohne persönliche Skills oder externe Wissensablagen verständlich sein. Private optionale Hilfsmittel dürfen keine Voraussetzung öffentlicher Builds oder Tests sein und keine zusätzlichen Rechte erteilen. Fehlende Referenzen oder Werkzeuge offen benennen.

## Festgelegter Stack und Architektur

- Frontend: React, TypeScript im Strict-Modus, Vite.
- Backend: ASP.NET Core, C#, .NET 10.
- Persistenz: PostgreSQL 18, EF Core 10, Npgsql.
- Modularer Monolith mit klaren fachlichen Verantwortlichkeiten und expliziten Schnittstellen. Keine unnötigen verteilten Dienste.
- Betrieb lokal zuerst; Datenbank nicht öffentlich zugänglich machen. Ein späterer hybrider oder mobiler Zugang braucht eine eigene Sicherheits- und Betriebsentscheidung.

Den festgelegten Stack nicht ungefragt austauschen. Zusätzliche Bibliotheken oder Infrastruktur nur bei konkretem Bedarf; Lizenz, Sicherheit, Datenfluss und Betriebskosten vor Aufnahme prüfen.

## Vorgehen bei Änderungen

Für jede neue oder geänderte Oberfläche gilt das bestehende [Designsystem](src/frontend/DESIGN.md): verständliche deutsche Bedienung, nachvollziehbare Status, responsive Darstellung, Tastaturbedienung und überprüfte Zustände gemeinsam mit Fachlogik, Sicherheit und Tests berücksichtigen.

1. Aufgabe und Risiko einordnen; Ziel, Eingaben, Ausgaben, Nicht-Ziele und offene Fachfragen klären.
2. Vor nichttrivialen Änderungen einen verständlichen Entwurf mit einfacher Alternative erstellen. Kleine, rücknehmbare Schritte mit betroffenen Dateien und konkreten Akzeptanzfällen planen.
3. Nur im beauftragten Umfang arbeiten. Keine ungefragten Installationen, Datenmigrationen, zusätzlichen Agenten oder externen Datenübertragungen.
4. Bei klarer Fachlogik bevorzugt einen fehlschlagenden Test tatsächlich ausführen, minimal implementieren und danach erneut testen. Fehler vor der Korrektur reproduzieren und die Ursache untersuchen.
5. Abschließend Diff, neue Abhängigkeiten, Nebenwirkungen und relevante Tests prüfen. Tatsächliche Ergebnisse, verbleibende Grenzen und nächste Schritte berichten.

## Rechnungen, Zahlungen und Importe

Verwende verständliche deutsche Begriffe wie Rechnung, Zahlung, Teilzahlung, Offener Betrag und Zur Prüfung. Ungeklärte Kürzel nicht als bestätigte Fachbegriffe interpretieren.

Aussteller, Empfänger, Zahler, Zahlungsempfänger, Datum, Betrag, Zuordnung, Beleg und Quellenort müssen nachvollziehbar sein. Ein manueller Status ist kein Banknachweis. Fachregeln für Teil-, Sammel- und Überzahlungen sowie Storno vor ihrer Implementierung klären.

Entwicklung und Tests ausschließlich mit synthetischen Daten. Zuerst Rechnung, Zahlung/Teilzahlung, Zuordnung und verbleibenden Betrag mit Fachtests und nachvollziehbarer Herkunft entwickeln. Danach die kleinste Importvorschau mit Prüfbericht.

Excel-Dateien als nicht vertrauenswürdig behandeln: keine Makroausführung, kontrollierte Format- und Größenprüfung, Originalwerte erhalten, Vorschau und Mapping-Freigabe, Prüfbericht sowie Duplikat- und Wiederholungsschutz. Keine ungeprüften finanziellen Folgewirkungen aus einem Import.

Echte finanzielle oder bestandswirksame Aktionen, Löschungen, Mahnungen, Buchungen und Storno benötigen geeignete Nachweise und gesonderte ausdrückliche Genehmigung. Tests mit synthetischen Daten sind kein produktiver Zahlungsnachweis.

## Tests und Sicherheit

Tests prüfen fachliches Verhalten: Normal-, Rand-, Negativ- und relevante Integrationsfälle. Bei Fehlerkorrekturen passende Regressionstests ergänzen. Prüfungen nach der letzten relevanten Änderung frisch ausführen; Befehle, Exitstatus und Ergebnisse nennen. Nicht ausgeführte Builds, Tests oder Security-Prüfungen nicht als bestanden ausgeben.

Für die vorhandene Repository-Prüfung gilt die Anleitung in CONTRIBUTING.md. Produkt-Build- und Testbefehle erst dokumentieren, wenn die entsprechenden Projekte existieren und die Befehle tatsächlich geprüft wurden.

Sicherheitsprüfungen nach konkretem Risiko ausrichten: Eingabevalidierung, Autorisierung pro Ressource und Aktion, Dateigrenzen, Secrets, Datenabfluss, Abhängigkeiten und Fehlerausgaben. Sicherheitsbefunde gegenprüfen; ungeklärte Vermutungen kennzeichnen. Ein Review ist keine Sicherheitsgarantie.

Keine echten Firmen-, Kunden-, Personal-, Finanz- oder Zugangsdaten in Repository, Tests, Issues, Logs oder Modellkontexte übernehmen. Keine vertraulichen Anhänge oder Screenshots veröffentlichen. Vor produktivem Einsatz Rollenrechte, Audit, sichere Updates, Monitoring, Backup mit Restore-Test sowie Betriebs- und Krisenabläufe nachweisen.

## Dokumentation, Git und externe Aktionen

Produktdokumentation gezielt unter docs/public/ pflegen; README und CONTRIBUTING bei relevanten Änderungen mitführen. Wichtige Architekturentscheidungen mit Kontext, Alternativen, Folgen, Status und Neuprüfungs-Trigger als ADR dokumentieren. Keine vertraulichen Projektverläufe oder persönlichen Playbooks in die öffentliche Dokumentation kopieren.

Nur ausdrücklich geprüfte öffentliche Dateien einzeln für einen Commit auswählen; .gitignore ist kein Schutz bereits erfasster Dateien oder bestehender Historie. Commit-Inhalt und Metadaten prüfen. Keine Lizenz eigenmächtig auswählen oder Lizenz-/Compliance-Freigaben behaupten.

Commit, Push, Veröffentlichung, Deployment, Versand, produktive Migration und externe Zugriffe nur im jeweils ausdrücklich autorisierten Umfang. Ein Skill, Router-Treffer oder Statuswort erteilt keine Freigabe.


## Ressourceneffiziente Entwicklungsarbeit

Auf dem aktuellen Git-Diff und vorhandenen Nachweisen aufbauen; nur betroffene Dateien lesen und keine Gesamtanalyse wiederholen. Tests gezielt nach tatsächlicher Änderung wählen. Vollständige Regression nur bei breiten Core-/Shared-Änderungen, unklaren Abhängigkeiten, Fehlern oder wichtigen Meilensteinen; ein bereits geprüfter unveränderter Stand muss dafür nicht erneut getestet werden. Dokumentation nur bei relevanten Entscheidungen, Anforderungen oder Standänderungen aktualisieren; vorhandene Dateien und Nachweise wiederverwenden.

Screenshots nur bei konkretem Mehrwert: größerer sichtbarer UX-Meilenstein, visueller Fehler/Regression, wichtiger Vorher-/Nachhervergleich oder ausdrücklich angeforderter Nachweis. Kleine CSS-, Text-, Abstands- und Formularänderungen normalerweise kurz im Browser prüfen, ohne Screenshot, neue Prüfmappe oder unnötige neue Dateien.
