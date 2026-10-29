# ADR 0007 – Versionierte Mitarbeiterrechte im Pilotbereich Kontakte

Datum: 29.10.2026. Status: im beauftragten Pilotbereich implementiert; persönliche Abnahme und Übernahme in bestehende Bestände ausstehend. Keine Produktionsfreigabe.

## Kontext und Entscheidung

Die Sicherheitsrollen bleiben `Mitarbeiter` und `Administrator`. Büromitarbeiter und Teamleitung sind unveränderliche, versionierte Berechtigungsvorlagen für Mitarbeiter. Der Pilot ergänzt ausschließlich Kontakte einschließlich Firmen/Ansprechpartnern; andere Module werden nicht umgestellt. Die frühere Architekturplanung bleibt für diese weiteren Bereiche ein Vorschlag.

`Berechtigungskatalog` kennt zunächst Kontakte.Lesen, Erstellen, Bearbeiten und Archivieren. Unbekannte Aktionen, insbesondere Löschen und eine aus einer Mitarbeitervorlage abgeleitete Sicherheitsadministration, werden verweigert. Kontakte besitzen keinen endgültigen Löschweg; Archivieren wird in der Matrix ehrlich als Archivieren angeboten. Büromitarbeiter v1 erlaubt Lesen/Erstellen/Bearbeiten, Teamleitung v1 zusätzlich Archivieren. Keine Vorlage enthält Administratorrechte.

Persistiert werden `Rechtevorlagenversionen`, bereichsbezogene `BenutzerRechteprofile` und unveränderliche `Berechtigungsaenderungen`. Eine feste Vorlagen-ID bindet die verwendete Version. Explizite individuelle Erlaubnisse/Verweigerungen überschreiben für genau diese Aktion den Vorlagenwert; eine fehlende Erlaubnis ist keine Freigabe. Änderungen einer späteren Vorlagenversion wirken nicht automatisch auf vorhandene Profile. Schreibaktionen benötigen zusätzlich Lesen; inkonsistente Auswahlen werden beim Speichern abgelehnt.

Die Entscheidung lautet weiterhin: aktive eingerichtete Identität UND bestehende Authentifizierung/MFA UND Aktionsrecht UND vorhandene Ressourcen-/Kontextrechte UND bestehende Fachregeln. Ein Kontaktbereichsrecht fügt keine Ressourcenfreigaben hinzu. Firmenvererbung, historische Bezugsprüfungen, persönliche Eigentumsrechte und die vorhandenen anderen Module bleiben erhalten. Referenznamen in eigenständig berechtigten Rechnungen/Akten sind nicht mit dem Zugriff auf die Kontaktverwaltung gleichgesetzt.

## Durchsetzung und Administration

`Anmeldung.Pipeline` prüft alle Kontaktpfade einschließlich Liste, Detail, Arbeitszahlen/Arbeit, Anlegen, Ändern, Archivieren und Kontaktvorschläge. Unbekannte Schreibpfade/-methoden erhalten keine implizite Aktion. `FirmenDaten` prüft Kontaktansichten und Kontaktmutationen zusätzlich auf Dienstebene. Globale Suche liefert ohne Kontakt-Leserecht keine Kontakttreffer; der Kontaktkontext der Assistenz verlangt ebenfalls Lesen. Bestehende Datenfilter gelten vor Ausgabe unverändert weiter. Es gibt keine neue Datenbereichsverwaltung in diesem Pilot.

`GET/POST /api/admin/benutzer/{id}/kontaktrechte` verlangt die bestehende Administratorrolle; Zuweisungen sind ausschließlich für andere Mitarbeiterkonten möglich. Speichern verlangt zusätzlich `MfaDienst.StarkPruefen`, CSRF und die Profilversion. Ein Mitarbeiter kann diesen Weg nicht zur Selbstprivilegierung verwenden. Die bestehende Admin-/MFA-Rollenprüfung und der Schutz des letzten Administrators werden nicht ersetzt.

Die starke Bestätigung wird vor der Rechte-Transaktion abgeschlossen, damit MFA-Fehlversuchszähler und Einmalfaktorverbrauch auch bei anschließendem Konflikt erhalten bleiben. Profiländerung und Berechtigungsaudit sind anschließend eine gemeinsame Transaktion. Der Audit enthält Akteur, Zielkonto, Bereich, Zeitpunkt und Vorher-/Nachher-Zustand samt wirksamen Aktionsrechten; keine Passwörter oder Codes. Versionskonflikte überschreiben keine fremde Änderung. Die nächste Anfrage liest die aktuellen Rechte aus der Datenbank, ohne neue langlebige Rechteclaims.

Die Laufzeitrolle erhält SELECT für Vorlagenversionen, aber kein INSERT/UPDATE/DELETE; Berechtigungsaudits dürfen eingefügt, nicht geändert/gelöscht werden. Die EF-Schreibsperre ergänzt diesen Datenbankschutz. Profile benötigen INSERT/UPDATE, keinen DELETE-Weg.

## Kontrollierte Migration und Bestandsübernahme

Migration: `20261029153257_Kontaktberechtigungen`. Sie erstellt nur die drei neuen Tabellen, Constraints/Indizes und zwei Vorlagenversionen. Bereits vorhandene Mitarbeiter erhalten einmalig ein Profil `Bestand` mit dem Snapshot der bisher vorhandenen Kontaktaktionen. Rollen, Passworthashes, Sicherheitskennungen, Aktivstatus und Ressourcenfreigaben bleiben unverändert. Die vorherigen Ressourcen- und globalen Schreibkontrollen werden nicht erweitert.

Neue Mitarbeiter werden nach der Migration nicht automatisch in diesen Bestandsmodus aufgenommen: Ohne Profil gibt es keine Kontaktaktionen. Bestehende Profile werden nur nach ausdrücklicher Bestandsablösung und starker Bestätigung durch die neue Auswahl ersetzt. Die UI erklärt diese Übernahme, zeigt Vorlagenherkunft und individuelle Abweichungen und speichert erst auf ausdrückliche Aktion. Administratoren bleiben auf dem getrennten geschützten Rollenweg; der Pilot erzeugt keine dritte Sicherheitsrolle.

**Vor Nutzung in einem bestehenden Bestand:** Anwendung kontrolliert stoppen, geprüftes Backup/Restoreverfahren verwenden, Migrationsskript/Bestandsübernahme prüfen, Migration mit der getrennten Schema-/Administrationsidentität anwenden und anschließend den bestehenden bewussten Operatorweg `runtime-grants` für die ausgewählte Laufzeitrolle ausführen. Kein automatisches Schema-Update beim Programmstart. Danach Startprüfung, bestehende Mitarbeiter und Ressourcenfilter sowie die Matrix abnehmen. Der beauftragte Entwicklungsdurchlauf migriert keine bestehende Entwicklungs- oder Geschäftsdatenbank.

Ein PostgreSQL-Downgrade verweigert den Tabellenabbau, sobald explizite Profile oder Berechtigungsaudits existieren. Ein unkontrollierter Rückbau könnte sonst entzogene Rechte wiederherstellen. Dann sind eine geprüfte Vorwärtskorrektur beziehungsweise ein bewusst freigegebener Restore erforderlich.

## Alternativen und Konsequenzen

- Nur Frontend-Häkchen: verworfen, da direkte APIs weiterhin zugänglich wären.
- Drei neue Sicherheitsrollen: verworfen; Teamleitung soll keine Administratorrolle werden.
- Alle Mitarbeiter automatisch auf Büromitarbeiter umstellen: verworfen, weil dies Archiveinschränkungen oder bislang nicht erlaubte Aktionen unkontrolliert ändern könnte.
- Bestehende Einzelressourcen durch globale Bereichsrechte ersetzen: verworfen, da dies geschützte Daten freischalten würde.

Der Pilot benötigt eine bewusst angewendete Schema-/Laufzeitrechteänderung. Ohne eigenes MFA kann ein Administrator die Matrix lesen, aber nicht speichern; eine lokale Admin-Ausnahme ersetzt keine starke Bestätigung. Die Vorschau simuliert diese neue Sicherheitsfunktion nicht. Die 100-Konten-Grenze der vorhandenen Mitarbeitersuche bleibt bestehen.

## Verifikation und Revisionsauslöser

Tests prüfen Katalog/Default-Deny, Vorlagen und individuelle Abweichungen, alte/neue Konten, starke Bestätigung einschließlich persistierter Fehlversuchszähler, Selbstschutz, Versionskonflikte, Audit-/Vorlagenunveränderlichkeit sowie die tatsächliche Migration und Kontakt-APIs auf einer neuen isolierten PostgreSQL-18-Datenbank mit eingeschränkter Laufzeitrolle. Frontendtests prüfen Vorschau ohne Schreibzugriff, Speichern, Herkunft, Konflikt und fehlende API. Nachweise und Grenzen stehen im bestehenden Entwicklungsnachweis.

Neu bewerten bei zusätzlichem Pilotbereich, zentraler Datenbereichsverwaltung, delegierter Rechteverwaltung, Vorlagenpflege außerhalb versionierter Migrationen oder einem tatsächlichen Löschweg. Diese Erweiterungen sind nicht durch dieses ADR freigegeben.
