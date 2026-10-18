# ADR 0004 – Zentrale Kontakte, Kategorien und Arbeitsumgebung

Datum: 18.10.2026. Status: im freigegebenen lokalen Produktreife-Block umgesetzt; fachliche und visuelle Abnahme durch den Auftraggeber offen. Ergänzt [ADR 0003](0003-zentrale-vorgangsakte-und-eingangsadapter.md).

## Entscheidung und Alternativen

Kontaktpflege verwahrt Firmen und Personen mit stabilen IDs. Ansprechpartner sind verknüpfte Personen desselben Bestands. Rechnungsprojektion, Vorgänge, Dokumentabsender, Suchtreffer und Kontaktvorschläge lösen diese IDs über denselben Bestand auf. Eine zweite Kontaktverwaltung je Fachansicht wurde verworfen. Mehrere aktive Telefon-/E-Mail-Werte besitzen jeweils einen Hauptwert. Änderungen verlangen die aktuelle Version, bewahren frühere Werte mit Benutzer/Zeit und können ähnliche Kontakte erst nach ausdrücklicher Bestätigung speichern. Archivierung erhält alle Verweise; eine harte Löschfunktion wird in dieser Strecke nicht angeboten.

Eine Dokumententscheidung setzt einen neuen Hauptwert oder ergänzt einen Wert. Die vorherige Telefonnummer/E-Mail bleibt erhalten. Ansprechpartner aus einem Brief werden zusätzlich als Person zur Firma angelegt; ein einzelnes Schreiben erklärt vorhandene Personen nicht für ungültig. Anschriftsersatz verlangt eine ausdrückliche Entscheidung und erhält den vorherigen Wert im Verlauf. Keine automatische KI-/OCR-Erkennung: nur tatsächlich extrahierte, beschriftete Textangaben liefern Vorschläge.

Kategorienverwaltung hält gemeinsame Geschäftsrubriken mit stabilen IDs, Anzeigename, Beschreibung, Reihenfolge, Version und Archivstatus. Maximal eine Unterkategorieebene; Kinder erscheinen nach ihrem Elternknoten. Umbenennung ändert keine Verknüpfung. Archivierte Rubriken bleiben bei vorhandenen Zuordnungen lesbar. Sonstiges/Ungeklärt bleiben als Fallback verfügbar. Zahlungs- und Bearbeitungsstatus werden separat im Fachcode definiert und können hier nicht neu konfiguriert werden. Alternative frei eingegebene Kategorien pro Dialog wurde wegen Dubletten und inkonsistenter Semantik verworfen.

Arbeitsumgebung konfiguriert die globale Beispieldatenkennzeichnung und Diagnoseausgabe zentral. Fachcode hängt nicht von sichtbaren Wörtern wie SYNTH, Demo oder synthetisch ab. Interne synthetische Kennzeichnungen, Herkunftswerte und stabile Test-IDs bleiben erhalten. Alle vorbefüllten Namen sind erfunden, E-Mail-Adressen verwenden .invalid. Die Beispieltelefone stammen aus den Berliner/Hamburger Medienbereichen der [Bundesnetzagentur](https://www.bundesnetzagentur.de/DE/Fachthemen/Telekommunikation/Nummerierung/DramaNumbers/artikel.html) (am 18.10.2026 geprüft). Es wurde kein Kontakt angerufen oder angeschrieben.

## HTTP, UI und Modulgrenzen

IArbeitsrechte entscheidet serverseitig über Kontakte.Verwalten, Kategorien.Verwalten und Diagnose.Lesen. Die feste lokale Beispielidentität hat diese Verwaltungsrechte; das ist ausdrücklich keine produktive Anmeldung oder Rollenverwaltung. ILesezugriff und Arbeitsmodule bleiben eigene Ebenen. Die normale Oberfläche zeigt keine technischen Rohdaten. Dokument-Prüfdaten sind nur bei explizitem Servermodus --diagnose plus Diagnose.Lesen verfügbar. Dies ist eine lokale Diagnosegrenze, kein Authentifizierungsersatz.

Hashrouten halten Bereich und Datensatz getrennt. Jeder Sidebarpunkt öffnet die jeweilige Übersicht, Browser Back/Forward liest dieselbe Route. Listenfilter bleiben beim direkten Detailrückweg innerhalb der gemounteten Bereichskomponente erhalten; kein allgemeines dauerhaftes Workspace-Profil. Büroarbeitslisten paginieren je 20 Einträge. Aktenlisten und Dokumentauswahl haben eigene begrenzte Scrollflächen. Nicht erfasste Aufgaben/Fristen oder Ereignisse werden nicht erfunden.

Korrespondenz hält Betreff, Inhalt, Art, Autor/Zeit und mehrere Dokument-IDs. Die durchsuchbare Auswahl liest den aktuellen Aktenbestand; Upload im Dialog verwendet denselben Dokumenteneingang und stellt die neuen Verweise direkt bereit. Keine Dateikopie und keine zweite Uploadimplementierung. Es gibt keinen Versand. Notizarchivierung bleibt reversibel mit Version und Verlauf.

## Folgen, Grenzen und Neuprüfung

Die Dienste bleiben im vorhandenen modularen Monolithen, ohne neue Bibliothek, Datenbank oder Infrastruktur. Feste lokale Limits und In-Memory-Speicherung begrenzen die Entwicklungsszenarien. Ein Neustart verwirft Änderungen. Finanzkern, MVP-02-Leser und Zahlungsnachweise bleiben fachlich unverändert. Der XLSX-Import unterstützt weiterhin ausschließlich die dokumentierte Vorlage, keine beliebigen Mappen.

Vor produktiven Kontakten/Dokumenten: authentisierte Benutzeridentität, Mandanten-/Ressourcenrechte einschließlich abgeleiteter Namen/Verweise und Schreibaktionen, dauerhafte Audit-/Versionsablage, Restore, Aufbewahrung und konkurrierende Benutzer prüfen. Die vorhandene Policyvorbereitung ist kein vollständiger Mehrbenutzer-Sicherheitsnachweis. Neue UI-/API-Felder müssen dieselben zentralen Rechte und Dienste verwenden. Weitere Kategorienebenen, produktive Persistenz, zusätzliche Module, OCR/Graph und Scannerhardware lösen eine erneute Architektur-/Securityprüfung aus. Keine automatische UX-Abnahme.
