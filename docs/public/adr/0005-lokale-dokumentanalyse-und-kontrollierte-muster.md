# ADR 0005 – Lokale Dokumentanalyse und kontrollierte Muster

Status: im beschriebenen Entwicklungsumfang umgesetzt, keine Produktivfreigabe. Datum: 21.10.2026.

## Kontext und Entscheidung

Der Posteingang benötigt echte PDF-Verarbeitung, nachprüfbare Fundstellen und wiederverwendbare Korrekturen. Bestehender modularer Monolith, Originalspeicher, Kontenrechte, Eingangsworkflow und Aufgaben bleiben die Grundlage. Keine zweite Dokumentverwaltung, Queue-Plattform oder KI-Infrastruktur.

Explizit konfigurierte vorhandene lokale Ghostscript- und Tesseract-Prozesse liefern Text beziehungsweise deutsche OCR mit Positionsdaten. Kein automatischer Download. Digitale Seiten verwenden Text; nur benötigte Bildseiten werden mit 200 dpi für OCR gerendert. Vorschauseiten entstehen getrennt bei Bedarf. Textgrundlinien sind keine Glyphenrahmen. Eine reine Textmengenheuristik ist noch keine umfassende Textebenen-Qualitätsbewertung.

Analyse und Grundfelder werden mit Originalhash und Verfahrensversion gespeichert. Korrekturen bleiben in der bestätigten Eingangsentscheidung und deren Audit. Versionierte Muster verwenden aktuelle Textanker oder Zeilenstrategien, keinen gespeicherten alten Rechnungswert. Aktivierung verlangt einen unabhängigen bestätigten Beleg mit anderem Original, Rechnungsdatum und Rechnungsnummer. Organisations-/Absenderrechte und Quellenversionen werden erneut geprüft. Freigabe ist widerrufbar. Die einzige freigebbare Geschäftsaktion ist interne Klassifizierung; alle anderen Aktionen bleiben menschlich.

Scanordner verwendet vorhandenen Eingang, persistente Aufträge, exklusive Dateilesung, geprüfte lokale Pfade, Inhaltsidentität und Leasing. Quellenoriginale werden nicht verschoben oder gelöscht. Nach unerwartetem Abbruch kann die Wiederaufnahme bis zum Leasingablauf von 15 Minuten warten. Eine fehlende Malwarefreigabe blockiert auch Scannerdateien.

## Alternativen und Folgen

Nur TXT-Vorschläge hätten echte PDF-/Scan-Dateien nicht nachgewiesen. Eine neue Parserbibliothek oder OCR-Installation hätte zusätzliche Abhängigkeiten und Freigaben benötigt. Pauschale Rasterisierung/OCR verschwendet Ressourcen und verschlechtert digitale Originale. Gespeicherte absolute Positionen allein wären bei anderen Layouts zu fragil. Eine eigene ML-/Agentenplattform ist für die begrenzten Ankerstrategien unnötig.

Native Prozesse besitzen Zeit-/Speicher-/Ausgabegrenzen und eine begrenzte Parallelität. Das ersetzt keine Betriebssystem-Sandbox. Ghostscript-Lizenz, native Parserhärtung, Aktualisierung und Verteilung müssen vor echtem Kundenbetrieb gesondert entschieden werden. Kein Paket wird mitgeliefert. Strukturierte E-Rechnungen, direkte Bilddatei-OCR, beliebige Tabellenlayouts, alle Dokumentklassen und unbeaufsichtigte Routineautomatisierung bleiben offen. Direkte WIA-Aufnahme benötigt reale Hardware und einen gesondert abgesicherten lokalen Adapter.

## Nachweis und Revision

Reale fiktive PDF-Binärdateien mit Manifest und Sollwerten, echte lokale OCR, SQLite-/isolierte PostgreSQL-Tests und authentifizierter Edge-Browser mit Persistenz/Neustart wurden getrennt geprüft. Dies ist keine unabhängige Praxisvalidierung und keine Sicherheitszertifizierung. Erweiterung erst nach unabhängigen rechtmäßig verwendbaren Dokumentbeispielen, Produktions-Malwareadapter, Lizenz-/Betriebsentscheidung und menschlicher Fachabnahme.

Primärquellen: [Ghostscript-Geräte](https://ghostscript.readthedocs.io/en/latest/Devices.html), [Tesseract-CLI/TSV](https://tesseract-ocr.github.io/tessdoc/Command-Line-Usage.html), [Ghostscript-Lizenzierung](https://www.ghostscript.com/licensing/index.html). Dokumentation online geprüft; tatsächlich lokal geprüft wurden Ghostscript 10.07.0 und Tesseract 5.5.2, keine automatische Aktualisierung.


## Ergänzung 22.10.2026 – Herstellerunabhängiger Eingang und Drucken

**Entscheidung:** vorhandene Dokumentenpipeline und Scanordner beibehalten; Windows-WIA als ersten direkten Geräteadapter priorisieren. Kein Hersteller oder feste Gerätekennung in der Fachlogik. Ein Druckerfund beweist keine Scannerfunktion. TWAIN bleibt ein eigener, derzeit nicht implementierter Adapter; WSD/WIA-Netzwerkgeräte werden nur berücksichtigt, wenn sie Windows tatsächlich als Scanner meldet. eSCL/AirScan und TWAIN Direct sind spätere Optionen, keine zugesagte Unterstützung.

**Ist:** `WindowsScanner` liefert ein echtes, begrenztes Host-Inventar; die administrative HTTPS-Oberfläche kann dieses abfragen und Geräte auswählen. Fähigkeiten wie ADF, Duplex, Auflösung und Mehrseitenaufnahme sind noch nicht verifiziert und werden nicht als Optionen angeboten. Die bestehende direkte Aufnahmemethode verweigert weiterhin mit 503. Der Einrichtungsdialog trennt Host, Browserarbeitsplatz und Scanordner und führt zur Windows-/Herstellereinrichtung. Er installiert nichts.

**Zielgrenze, noch nicht implementiert:** ein optionaler Windows-Client-Companion arbeitet in der interaktiven Sitzung des jeweiligen Arbeitsplatzes. Sein Geräteadapter muss Inventar, aktuelle Gerätekennung, tatsächlich gemeldete Fähigkeiten, ausdrücklich gestarteten Auftrag, Status, Abbruch und begrenzte Seitenresultate anbieten. Keine allgemeinen Pfade oder Kommandos aus Browserdaten. Der Companion benötigt eine gesondert nachgewiesene Benutzer-/Host-Kopplung mit kurzlebigen, einmaligen, zielgebundenen Aufträgen, strikter Originprüfung, Transportverschlüsselung und Widerruf. Localhost-Bindung allein genügt nicht. Scanresultate bleiben bis zur bewussten Übernahme temporär; danach durchlaufen sie denselben berechtigungs- und malwaregeprüften Eingang. Installation, Signierung, Updates und Mehrbenutzer-/Treiberabnahme sind offene Liefergrenzen. Es existiert kein fertig installierbarer Companion.

**Alternativen/Folgen:** eine Webserver-WIA-Aufnahme wäre auf die Host-Hardware und dessen Windows-Sitzung beschränkt und löst keine Client-Scanneranbindung. Ein ungeschützter lokaler HTTP-Dienst oder ein Canon-Sonderpfad wird nicht eingeführt. Ohne sicheren Companion bleibt die vorhandene Scan-App mit Dateiauswahl beziehungsweise freigegebenem Scanordner der praktische Weg. Keine neuen Netzwerkfreigaben. Eine echte Aufnahme benötigt erreichbare Hardware und bewusste Hardwarefreigabe; Simulationen ersetzen dies nicht.

**Drucken:** bewusste Benutzeraktion über `window.print()` beziehungsweise die native Original-PDF-Anzeige. Betriebssystem und Benutzergerät bestimmen USB-/WLAN-/Netzwerkdrucker und Einstellungen. A4-Druckregeln entfernen Navigation/Bedienelemente; gezielte Text-/Korrespondenzansichten verwenden nur bereits autorisierte gerenderte Inhalte, keine neuen Datei- oder Exportrechte. Keine eigene Druckertreiberplattform, Warteschlange oder stiller Seriendruck. Angebote und Mahnwesen werden dadurch nicht als neue Fachmodule implementiert.

**Malwaregrenze:** Defender kann lokal Dateien prüfen; ein positiver Einzeltest allein ersetzt keinen sicheren Adapter. Exitcode 0 kann auch erfolgreiche Bereinigung bedeuten. Vor einer produktiven Freigabe sind isolierte Quarantäne, unveränderte geprüfte Bytes, eindeutige Ergebnisse, Fehler-/Timeout-/Race- und Erkennungs-Negativtests unter dem tatsächlichen Laufzeitkonto nachzuweisen. `NichtKonfigurierterScanner` bleibt registriert, Uploads bleiben fail-closed. Keine globale Sicherheitsänderung.

**Revision:** erreichbar eingerichtete Hardware, sichere Companion-Kopplung/Installation, ein nachgewiesener Malwareadapter oder ein konkret benötigter zweiter Standard lösen eine neue Prüfung aus. Geräte- und Betriebssystemunterstützung bleibt ein tatsächlicher Testnachweis, kein allgemeines Kompatibilitätsversprechen.

Primärquellen: [WIA-Architektur](https://learn.microsoft.com/en-us/windows-hardware/drivers/image/wia-architecture-overview), [WIA-Netzwerk-/USB-Treiber](https://learn.microsoft.com/en-us/windows-hardware/drivers/image/auto-configured-scanning), [Windows-Scannereinrichtung](https://support.microsoft.com/en-gb/windows/hardware/printer/install-and-use-a-scanner-in-windows), [TWAIN Working Group](https://twain.org/specification/), [Defender-CLI und Ergebnisgrenzen](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus), [Browser-Druckdialog](https://developer.mozilla.org/en-US/docs/Web/API/Window/print). Am 22.10.2026 eingesehen; keine Treiber oder Werkzeuge heruntergeladen.
