# ADR 0002 – Begrenzter lokaler Demoimport und interne PDF-Vorschau

Datum: 18.10.2026. Status: für die ausdrücklich beauftragte synthetische Demo umgesetzt; visuelle Abnahme offen.

## Kontext

Die bestehende Anwendung hatte nur synthetische Importbeispiele und PDF-Links im neuen Tab. Benötigt werden eine tatsächlich bedienbare vertikale Importstrecke und Dokumentlesen im bestehenden Arbeitsraum. Produktive Datenbank, Authentifizierung, echte Firmendaten, zusätzliche Pakete und externe Dienste sind nicht freigegeben.

## Entscheidung

MVP-02 bleibt der einzige XLSX-Leser mit seinen festen Blatt-/Spalten- und Sicherheitsgrenzen. Eine lokale Prozessinstanz verwahrt Originalbytes, Vorschau, bestätigtes Firmenmapping, ausgewählte Zeilen und UTC-Entscheidungszeit. Explizite Übernahme gültiger Rechnungen ergänzt genau den regulären Demobestand; die gemeinsame serverseitige Projektion verwendet den Rechnungskern. Zahlungszeilen bleiben im Prüfbericht und ohne finanzielle Wirkung. Prüfung und Übernahme laufen unter demselben Lock; Wiederholungen und fachliche Dubletten werden erkannt.

Die beiden unveränderten selbst erzeugten Demo-PDFs erscheinen mit dem Browserviewer in einem Same-Origin-Frame und optional einem nativen Dialog. Nur ihre Antworten erlauben Einbettung im eigenen Ursprung. Keine PDF-Bibliothek, kein Bearbeitungs- oder Rückschreibendpunkt. Lokale Notizen liegen begrenzt im sessionStorage des Tabs und sind ausdrücklich Demo.

## Alternativen

Nur statische Beispiele würden keine Dateiübernahme prüfen und erfüllen den Auftrag nicht. Ein allgemeiner Excel-Parser mit beliebigem Blatt-/Spaltenmapping würde den ersten vertikalen Ausschnitt unnötig erweitern. Eine Datenbank würde eine nicht freigegebene Persistenz-/Betriebsentscheidung vorwegnehmen. Ein externer PDF-Dienst oder neues Viewerpaket ist für zwei feste Testdateien nicht nötig. Ausschließlich neue PDF-Tabs erfüllen die gewünschte interne Arbeitsoberfläche nicht.

## Folgen und Grenzen

Schneller, lokal vollständig testbarer Demonstrationsweg ohne neue Abhängigkeiten. Der Server verliert Daten und Prüfverlauf beim Neustart; Notizen können beim Schließen des Tabs verloren gehen. Keine Mandanten-/Benutzerautorisierung, dauerhafte Idempotenz, Buchhaltung oder produktive Auditgarantie. Die Zustimmung „synthetisch“ ist eine Nutzerbestätigung, kein technischer Beweis über den Inhalt. Maximal 20 Dateien und 5.000 importierte Rechnungen, feste XLSX-Vorlage. Native PDF-Werkzeuge können eine lokale Kopie bearbeiten, ändern aber nicht das Serveroriginal.

## Neuprüfung

Vor echten Dateien, mehreren Firmenkontexten/Benutzern, persistenter Übernahme, weiteren Blatt-/Spaltenformaten oder fremden PDFs: Autorisierung, Dateisicherheit, Datenvertrag, Wiederholungsschutz, Audit, Backups/Restore, Aufbewahrung, Fehlerwiederanlauf und Viewer erneut fachlich und technisch entscheiden. Zusätzliche Bibliotheken bleiben gesondert freizugeben. Die menschliche Sichtabnahme bleibt unabhängig von Tests offen.

Aktuelle Ergänzung 18.10.2026: Die sessionStorage-Notizablage und der feste Dokumentenkatalog wurden durch die zentrale lokale Vorgangsakte weiterentwickelt; Alt-Tab-Notizen bleiben lesbar. Siehe [ADR 0003](0003-zentrale-vorgangsakte-und-eingangsadapter.md). Die damalige Entscheidung bleibt als Historie erhalten.
