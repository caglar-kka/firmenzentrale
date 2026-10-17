# ADR 0001 – Kleiner Rechnungs- und Zahlungskern

Status: umgesetzt im beauftragten synthetischen MVP-01-Umfang, 17.10.2026.

## Kontext und Entscheidung

Die erste Produktstrecke soll Geldbeträge nachvollziehbar berechnen und Fehler verhindern, bevor Oberfläche, Import oder Persistenz hinzukommen. Die Solution unter `src/backend/` besteht aus einer .NET-10-Klassenbibliothek und einem xUnit-Testprojekt. Sie bildet das erste Fachmodul des vorgesehenen modularen Monolithen. Der festgelegte Gesamtstack bleibt bestehen.

Ein `Rechnungskern` verwaltet die Zusammenhänge in einem einzelnen In-Memory-Kontext. Rechnung und Zahlung sind unveränderliche Eingangsdaten; nur der Kern erstellt Bestätigungen und Zahlungszuordnungen. Es gibt keine frei setzbaren Salden oder Zahlungsstatus. Beleg und Quellenort sind unveränderliche Referenztexte, keine geöffneten Dateien. Die Beteiligten werden als stabile, exakt übereinstimmende Referenztexte geführt; eine Stammdatenverwaltung folgt später.

## Fachvertrag für diesen MVP

- Ausschließlich EUR. Positive `decimal`-Beträge auf volle Cent; zusätzliche Nachkommastellen werden abgewiesen und nicht still gerundet. Obergrenze: 792281625142643375935439503,35 EUR (`decimal.MaxValue / 100`), damit auch am oberen Rand jeder Cent exakt darstellbar bleibt. Keine Steuern, Wechselkurse oder Gutschriften.
- Rechnung: ID, Nummer, Aussteller, Empfänger, Rechnungsdatum, Betrag, Beleg und Quellenort. Zahlung: ID, Zahler, Zahlungsempfänger, Zahlungsdatum, Betrag, Beleg und Quellenort. IDs und Pflichttexte sowie fehlende Datumswerte werden geprüft.
- Eine erfasste Zahlung ist zunächst zur Prüfung und hat keinen verfügbaren Zuordnungsbetrag. Bestätigung erfordert eine gesonderte Nachweisreferenz, prüfende Person und Erfassungszeit. Bestätigung allein reduziert keine Rechnung.
- Der Aufrufer trägt die Verantwortung für die tatsächliche Belegprüfung und spätere Autorisierung. Weder eine Nachweisreferenz noch ein manueller Text wie `OK` beweist einen Bankumsatz. Die Testbestätigungen sind vollständig erfunden.
- Nur bestätigte Zahlungen können zugeordnet werden. Jede Zuordnung speichert ID, beide Referenzen, Betrag, Nachweis, erfassende Person und Erfassungszeit. Datum und Zeit werden vom Aufrufer geliefert; keine versteckte Systemuhr.
- Offener Betrag = Rechnungsbetrag minus gültige Zuordnungen. Verfügbarer Betrag = bestätigter Zahlungsbetrag minus gültige Zuordnungen. Beide bleiben nichtnegativ.
- Eine Rechnung kann mehrere unterschiedliche Zahlungen erhalten. Eine Sammelzahlung kann mehrere Rechnungen bedienen. Zahler und Rechnungsempfänger dürfen verschieden sein, etwa bei Zahlung durch Dritte; Zahlungsempfänger und Rechnungsaussteller müssen identisch sein. Abtretungen und abweichende Zahlungsempfänger sind nicht modelliert.
- Eine Überzahlung wird bis zum offenen Rechnungsbetrag zugeordnet. Der Rest bleibt an der Zahlung sichtbar und wird weder automatisch erstattet noch als Gutschrift gebucht.
- Zuordnungs-ID und das Paar aus Zahlungs-ID und Rechnungs-ID sind eindeutig. Wiederholungen werden ohne Saldoänderung abgewiesen. Eine zweite Zuordnung desselben Paars mit anderem Betrag ist ebenfalls gesperrt; Korrekturen erfordern einen späteren expliziten Ablauf.
- Doppelte Rechnungs-/Zahlungs-IDs oder wiederholte Bestätigungen überschreiben keine Originale. Fachliche Dubletten mit neuen IDs werden noch nicht erkannt. Nummern sind keine global eindeutigen Schlüssel.
- Ungültige Eingaben, unbekannte Referenzen, falsche Zahlungsempfänger und Überschreitungen werden vor jeder Änderung abgewiesen. Die Prüf- und Schreibfolge setzt serielle Aufrufe voraus.

## Alternativen und Folgen

Lose Rechenfunktionen wären kleiner, könnten aber dieselbe Zahlung über mehrere Rechnungen hinweg nicht zuverlässig vor Mehrfachverwendung schützen. Getrennte Application-/Domain-/Infrastructure-Projekte und ein sofortiger API-/Datenbankaufbau würden den ersten Nachweis unnötig vergrößern. Deshalb zunächst ein Fachprojekt ohne externe Laufzeitpakete.

Die Listen sind Momentaufnahmen und die Datensätze unveränderlich. Das schützt den internen Zustand vor Änderungen durch Aufrufer. Es gibt noch kein dauerhaftes oder manipulationsgeschütztes Audit, keine Mandantentrennung, keine Rechteprüfung, keinen Schutz paralleler Aufrufe und keinen Wiederholungsschutz über Prozessneustarts hinweg. Die Daten gehen beim Verwerfen des Kerns verloren. Dieser Stand darf nicht produktiv für Zahlungen verwendet werden.

## Neuprüfung

Vor API, Persistenz oder parallelen Aufrufen: atomare Transaktionen, eindeutige Datenbankbedingungen, Nebenläufigkeit, Autorisierung, Mandantentrennung und dauerhafte Auditdaten entwerfen und testen. Vor Import: stabile externe Identitäten und fachliche Dublettenerkennung klären. Vor Storno, Korrektur, Abtretung oder weiteren Währungen die Fachregeln ausdrücklich erweitern. Vor produktivem Einsatz Belegprüfung und Betriebsgates nachweisen.

## Ergänzung MVP-02 – begrenzte Excel-Importvorschau

Auftrag: ausschließlich künstliche ISD-Testdaten lokal als Vorschau lesen, ohne Buchungen oder Zahlungsbestätigungen. ISD ist weiterhin eine ungeklärte Originalbezeichnung. Die Testvorlage ist eine neue synthetische Vereinbarung, keine Interpretation einer echten ISD-Datei.

Umsetzungsschritte: zuerst echte XLSX-Pakete in Tests erzeugen und rote Akzeptanztests ausführen; dann begrenztes ZIP-/XML-Lesen, unveränderte Zellwerte mit SHA-256/Blatt/Zeile/Zelle, explizites Unternehmensmapping und Validierung; danach Dublettenprüfung und Textbericht; abschließend Negativtests, Regression, Build und öffentliche Dateiliste prüfen. Betroffen sind ein neues Importmodul, das vorhandene Testprojekt, die Solution und die bestehenden öffentlichen Dokumente. Der Rechnungskern bleibt unverändert.

Kleinste Alternative: eine zusätzliche allgemeine Excel-Bibliothek. Lokal ist keine vorhanden. Für das enge Testformat wird stattdessen ein explizit begrenzter XLSX-Leser mit .NET-Bordmitteln umgesetzt; kein allgemeiner Excel-Importer. ZIP-Einträge werden nur begrenzt im Speicher dekomprimiert und nie auf das Dateisystem extrahiert. DTDs und externe Beziehungen werden abgewiesen. Ressourcen werden begrenzt. Nicht unterstützte aktive Paketbestandteile und wesentliche Blattmerkmale werden sichtbar abgewiesen; eine vollständige OpenXML-Schemavalidierung ist nicht enthalten. Ein breiteres Excel-Format erfordert später eine erneute Bibliotheks-/Lizenz-/Securityentscheidung.

Die synthetische Vorlage hat genau ein sichtbares Blatt `ISD-Testdaten` mit Spalten `DatensatzId`, `Art`, `Nummer`, `Von`, `An`, `Datum`, `Betrag`, `Waehrung`, `Beleg`. `Art` enthält ausdrücklich `Rechnung` oder `Zahlung`. `Von`/`An` bezeichnen bei Rechnungen Aussteller/Empfänger und bei Zahlungen Zahler/Zahlungsempfänger. Das Profil ordnet Quelltexte ausdrücklich stabilen Unternehmensreferenzen zu. `Datum` ist ISO-Text bzw. ein ISO-Datumswert, kein implizit interpretiertes Excel-Seriendatum. Nur EUR wird akzeptiert. Zahlenzellen verwenden Dezimalpunkt, Betragstexte deutsches Dezimalkomma ohne Tausendertrennzeichen; keine stillschweigende Rundung.

Ergebnisse sind unveränderliche Vorschauzeilen mit Originalzellen, normalisierten Werten, geprüften ungespeicherten Fachobjekten und adressierten Meldungen. Dateifehler geben keine Teilfreigabe. Wiederholungsschutz wird als Vergleich mit ausdrücklich übergebenen früheren Hashes/Quellschlüsseln umgesetzt; die Vorschau führt selbst keine Historie. Fachliche Rechnungsdubletten werden zusätzlich anhand Aussteller und Nummer erkannt. Ohne Beleg-/Mappingfreigabe gibt es keinerlei Übergabe in einen bestehenden Rechnungskern.

## Ergänzung MVP-03 – bedienbarer lokaler Entwicklungsprototyp

Status: im freigegebenen synthetischen Umfang umgesetzt, 17.10.2026. Die Nutzerklarstellung präzisiert die frühere ISD-Annahme: ISD wird vorläufig als Firma/Geschäftspartner behandelt, ohne bestätigten vollständigen Firmennamen. Keine pauschale Vorgangsrolle; Originalbezeichnungen und Quellen bleiben unverändert. Die Hinweis-/Berichtstexte wurden angepasst und unterschiedliche Rollen mit Regressionstests belegt.

Entscheidung: ein kleiner ASP.NET-Core-Host referenziert die vorhandenen Fachmodule und liefert unveränderliche synthetische Ansichten an React/TypeScript/Vite. Das Frontend zeigt serverseitige Salden; keine zweite Geschäftslogik. Der Host liefert auch den gebauten Frontendstand aus. Eine direkte reine HTML-Oberfläche wäre kleiner, würde aber den festgelegten React-Stack nicht als zusammenhängenden Ausschnitt prüfen. Ein generischer Upload, neue Persistenz- oder weitere UI-Frameworks würden den Scope vergrößern und bleiben ausgenommen.

Folgen: genau ein lokaler Origin im gebauten Betrieb; feste Loopback-Bindung, Host-/Originprüfung und unveränderliche Ansichten verhindern die unbeabsichtigte Öffnung des bisherigen veränderlichen Kerns für parallele Webanfragen. Importvorschauen laufen getrennt und ohne Buchung. Direkte Frontendpakete und Auflösung sind fixiert; offline aus vorhandenen Caches aufgebaut. jsdom und Node-Test-Runner genügen für die ersten Frontendtests.

Grenzen und Neuprüfung: vor echtem Upload, Persistenz, Rollenrechten, parallelen fachlichen Änderungen oder Fernzugriff erneut Transaktions-, Audit-, Betriebs- und Sicherheitskonzept prüfen. Vor Paketupdates oder Veröffentlichung Lizenz- und aktuelle Advisoryprüfung durchführen. Der aktuelle Host-/Frontendpfad setzt das Repository-Layout voraus; keine geprüfte Publish-/Installationsstrecke.
