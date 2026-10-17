# Mitarbeit und Git-Regeln

## Kleine, prüfbare Änderungen

Die allgemeinen Entwicklungsregeln stehen in der öffentlichen AGENTS.md. Sie und diese Anleitung benötigen keine persönlichen Skills. Eine optionale lokale AGENTS.override.md wird von Git ausgeschlossen; bei ihrer Verwendung müssen auch die öffentlichen Projektanweisungen ausdrücklich gelesen werden.

Vor einer Änderung Ziel, fachliche Regeln, betroffene Dateien und Akzeptanzfälle klären. Kleine Änderungen mit verständlichen Commit-Nachrichten bevorzugen. Bei klarer Fachlogik zuerst einen tatsächlich fehlschlagenden Test ausführen und nach der Umsetzung erneut prüfen. Fehler zuerst reproduzieren und ihre Ursache untersuchen.

Für die spätere Zusammenarbeit sind kurze Aufgabenbranches und Pull Requests vorgesehen. Ein Pull Request beschreibt Problem, Änderung, tatsächlich ausgeführte Prüfungen und verbleibende Grenzen. Branchschutz und CI sind noch nicht eingerichtet.

## Daten und Konfiguration

Nur synthetische Beispieldaten verwenden. Keine Kundendaten, Originaldateien, Zugangsdaten, lokalen Datenbanken oder persönlichen Projektunterlagen veröffentlichen. Auch Dateinamen, Screenshots, Logs und Fehlermeldungen können vertrauliche Informationen enthalten.

Die `.gitignore` lässt nur ausdrücklich vorgesehene Root-Dateien und die Bereiche `src/`, `tests/` und `docs/public/` zu. Innerhalb dieser Bereiche gelten weitere Ausschlüsse für sensible Formate und Konfigurationen. Neue Ausnahmen benötigen eine Inhaltsprüfung. Die Regeln erkennen keine Geheimnisse in beliebigem Quelltext und ersetzen keine Prüfung.

Private lokale Entwicklungsunterlagen gehören nicht zum öffentlichen Projekt. Ein öffentlicher Klon setzt keine persönlichen Skills oder Playbooks voraus. Produktprüfungen müssen später ohne solche Unterlagen ausführbar sein.

## Vor einem Commit und vor Veröffentlichung

1. Nur konkret geprüfte Dateien mit `git add -- <Dateipfad>` auswählen; kein pauschales `git add .` und kein `git add -f` für ignorierte Dateien.
2. Mit `git status --short`, `git diff --cached --stat`, `git diff --cached` und `git diff --cached --check` den tatsächlichen Commit-Inhalt prüfen.
3. Mit `git ls-files` die vollständige Dateiliste prüfen. `.gitignore` entfernt keine bereits erfassten Dateien und bereinigt keine Historie.
4. Relevante Tests frisch ausführen; nicht ausgeführte Tests und fehlende Abdeckung benennen.
5. Vor einer Veröffentlichung die gesamte vorgesehene Historie, Metadaten und Inhalte auf private Informationen und Secrets prüfen. Git-Autorname und E-Mail bewusst wählen; keine globalen Einstellungen durch Projektskripte ändern.
6. Push, Veröffentlichung und externe Datenübertragung benötigen eine gesonderte Freigabe. Die Lizenzentscheidung bleibt offen.

Wenn ein Geheimnis versehentlich veröffentlicht wird, reicht das Löschen der Datei nicht aus: Veröffentlichung stoppen, betroffene Zugänge sperren beziehungsweise erneuern und die Bereinigung der Historie gesondert planen. Keine echten Geheimnisse in öffentlichen Issues melden.

## Öffentliche Repository-Prüfung

Voraussetzungen: Python 3 und eine vorhandene Git-Installation. Aus dem Projektordner:

```powershell
python -B -m unittest discover -s tests/repository -v
```

Falls Git nicht im Suchpfad liegt, kann für diesen Testprozess die Variable `BETRIEBSPLATTFORM_GIT` auf die vorhandene `git.exe` gesetzt werden. Die Tests verwenden ein isoliertes temporäres Git-Repository und synthetische Pfade. Sie erstellen keinen Commit und initialisieren nicht den Projektordner.
