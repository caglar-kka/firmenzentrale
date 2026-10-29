# ADR 0006 – Begrenzte lokale Assistenz mit deterministischen Quellen

Datum: 22.10.2026. Status: für den autorisierten Entwicklungsumfang umgesetzt; Modellbetrieb und Produktivfreigabe offen.

## Kontext und Entscheidung

Der freigegebene Handoff verlangt eine gemeinsame lokale Assistenz, ohne Geschäftsdaten an Cloudmodelle zu senden. Die vorhandenen Aufgaben-, Ressourcen- und Dokumentrechte bleiben die Datenzugriffsgrenze. Eine reine dekorative KI-Karte genügt nicht.

Die Anwendung ermittelt Quellen deterministisch im authentifizierten Backend. Optionale lokale Inferenz bekommt ausschließlich die begrenzte, erneut berechtigte Auswahl. Ein Providerinterface trennt Ollama von Datenabfragen und Oberfläche. Keine Modellwerkzeuge und keine automatische Aktion. Vorschläge bleiben editierbar; eine Aktennotiz benötigt ausdrückliche Freigabe und den bestehenden autorisierenden Schreibweg.

## Alternativen

Reine strukturierte Suche ist die einfachste zuverlässige Stufe und bleibt ohne Modell vollständig nutzbar. Eine Cloud-API widerspricht der Nutzerentscheidung. Ein autonomer Agent, eine Vektordatenbank oder ein neuer Workflowkern sind für diese ersten Fälle nicht erforderlich. Die lokale, optionale Textgenerierung ergänzt die vorhandenen Fachfunktionen, statt sie zu ersetzen.

## Folgen und Grenzen

Keine Schemaänderung oder zusätzliche Bibliothek. Das Modell bleibt standardmäßig deaktiviert. Hostbetrieb, Cloud-Abschaltung, tatsächliches kleines lokales Modell und Hardwarebudget müssen vor Aktivierung geprüft werden. Vertragstests prüfen Fehler, Quellenvalidierung und Grenzen; sie beweisen keine tatsächliche Inferenz oder Prompt-Injection-Resistenz eines Modells. Die isolierte HTTPS-/SQLite-Strecke beweist Anmeldung, CSRF, Ressourcenrechte und Offlineverhalten, keinen PostgreSQL-Browser-End-to-End-Lauf.

Konfiguration, Größen-/Zeit-/Parallelitätsgrenzen und Datenfluss stehen in der [Architektur](../ARCHITEKTUR.md). Neu prüfen bei vorhandenem Modell, unzureichender Quellenabdeckung, anderem Provider, neuer Aktion oder geplanter produktiver Nutzung. Keine Freigabe weiterer Datenübertragungen aus dieser Entscheidung ableiten.
