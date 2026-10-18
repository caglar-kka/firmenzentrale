# ADR 0003 – Zentrale Vorgangsakte und gemeinsame Eingangsadapter

Datum: 18.10.2026. Status: im ausdrücklich freigegebenen lokalen Demo-Arbeitsblock umgesetzt; menschliche UX-Abnahme offen. Ergänzt ADR 0002 und ersetzt dessen sessionStorage-Notizprototyp als primäre Notizablage.

## Kontext und Entscheidung

FirmenZentrale benötigt allgemeine Vorgänge statt einer ausschließlich rechnungsbezogenen Ablage. Im bestehenden modularen Monolithen verwahrt `Aktenbestand` Dokumentidentität, unveränderte Originalbytes und Integrität getrennt von Aktenverweisen. Notizablage, Kontaktpflege, Dokumentprüfung/Textauswertung, Suche und Eingangsadapter haben eigene Verantwortlichkeiten. Der Finanzkern und der vorhandene MVP-02-Leser bleiben die einzigen fachlichen Implementierungen ihrer Geschäftsregeln.

Manuelle Dateiauswahl, gezieltes Drag & Drop und der optionale Scan-Ordner-Adapter rufen dieselbe Dokumentprüfung und Ablage auf. SHA-256-Dubletten verwenden dasselbe Original; Zuordnung ergänzt Verweise. Ein Eingang ist zunächst zur Prüfung und keine Rechnung. Zuordnung zu vorhandener oder neuer Akte erfolgt ausdrücklich mit Kategorie und bei neuer Akte mit Firma. XLSX als Aktenanhang erzeugt keinen strukturierten Datenimport. Korrespondenz hält Dokumentverweise; kein zweites Anhangsarchiv und kein Mailversand.

Notizen besitzen Betreff, Inhalt, Autor, UTC-Zeiten, Version und erhaltenen Änderungsverlauf. Änderungen verlangen die gelesene Version; Entfernen ist reversibler Soft Delete. Frühere Tab-Notizen bleiben unverändert lesbar. Dokumente/Notizen/Korrespondenz erscheinen als ein aktiver Bereich mit Zählern und begrenzten Scrolllisten. Drucken verwendet Browser/System beziehungsweise den vorhandenen PDF-Betrachter und ändert keinen Fachstatus.

Beschriftete Kontaktwerte aus tatsächlich gelesenem Text ergeben prüfbare Vorschläge für die Firma der Akte. Übernehmen, zusätzlich speichern und ignorieren halten Quelle, alte/neue Werte, Demo-Benutzer, Zeitpunkt und Version fest. Ein einzelner Brief ersetzt keinen Ansprechpartner automatisch. TXT und DOCX-Absatztext sind lesbar; PDF-Text ist auf einfache unkomprimierte Textoperationen begrenzt. `IDocumentTextExtractor` erlaubt spätere geprüfte Extraktoren. Für Scanbilder ist keine OCR vorhanden.

Eine zentrale Suche liefert tatsächliche Rechnungen, Firmen, Personen, Vorgänge, Dokumente und Eingangsdokumente. Das Frontend nutzt mindestens zwei Zeichen, 250-ms-Debounce, Abbruch veralteter Anfragen und zugängliche Listbox-/Combobox-Bedienung. `ILesezugriff` filtert Ressourcen serverseitig bereits vor Ausgabe; die aktive lokale Demo erlaubt alle synthetischen Ressourcen. Das ist keine produktive Authentifizierung. Arbeitsmodul-Aktivierung wird an API-Grenzen und Suche berücksichtigt; sie ersetzt weder Berechtigungen noch eine spätere Lizenzentscheidung. Die aktuellen Verantwortlichkeiten sind vorläufige technische Grenzen, keine endgültige Produktmodulliste.

## Scannerentscheidung

Direkte Steuerung bleibt hinter `IScannerAdapter` vorbereitet. Ein späterer signierter lokaler Windows-Companion muss Gerätewahl, Abbruch, Zeitlimit und authentisierte Übergabe an denselben Eingang anbieten. Keine frei erreichbare Scanner-HTTP-Schnittstelle und keine Fake-Scanschaltfläche.

Microsoft beschreibt WIA als COM-Schnittstelle über den WIA-Dienst und `Windows.Devices.Scanners` als darauf aufbauende Scan-API für UWP/Desktop. Daraus ergibt sich als Entwurfsoption ein Windows-spezifischer Adapter außerhalb des plattformneutralen Fachkerns. Hardwarekompatibilität, WIA gegenüber TWAIN/Hersteller-API, Verpackung/Signierung, IPC und Sitzungsrechte sind vor Umsetzung mit konkreter Hardware zu prüfen. Keine Treiber oder Pakete installiert. Quellen: [WIA-Architektur](https://learn.microsoft.com/en-us/windows-hardware/drivers/image/wia-architecture-overview), [Scanner-Namespace](https://learn.microsoft.com/en-us/uwp/api/windows.devices.scanners), [Scannen aus einer App](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/scan-from-your-app) (am 18.10.2026 geprüft).

Als tatsächlicher Fallback läuft ein optionaler lokaler Polling-Adapter im Host: expliziter Startschalter `--scan-demo`, ausschließlich eigener `src/backend/private/scan-eingang`, zwei stabile Beobachtungen, Größen-/Strukturprüfung, kein rekursiver Zugriff und keine Reparse-Points, gemeinsamer Hash-Dublettenschutz. Quelldateien bleiben erhalten. Neue Dateien werden ohne manuellen Upload eingelesen; die Oberfläche zeigt sie nach Aktualisieren. Das ist ein getesteter Ordneradapter, kein Nachweis eines angeschlossenen Hardware-Scanners. Nach Prozessneustart wird erneut eingelesen, weil auch die zentrale Demoablage flüchtig ist.

## Dateigrenzen und Alternativen

Erlaubt: PDF, PNG, JPEG, UTF-8-TXT, begrenztes DOCX und XLSX. Höchstens 2 MiB je Datei, 512 Dokumente/64 MiB im Prozess. ZIP-/XML-Grenzen, keine DTD-/External-Entity-Auflösung, keine Makros/Einbettungen/externen Office-Verweise. Bilder haben Dimensions- und Strukturgrenzen. Keine Office- oder Formel-Ausführung. EML/MSG, OCR und beliebige Office-Pakete bleiben offen. Ein Formatfilter ersetzt keinen Virenscanner oder vollständigen Dateiparser.

Die zwei eigenen bekannten PDFs dürfen direkt in den vorhandenen Viewer. Fremde zulässige PDFs bleiben kontrollierte Downloads; aktive/verschlüsselte oder nicht ausreichend prüfbare Strukturen werden abgewiesen. DOCX zeigt lediglich lesbaren Textextrakt, XLSX bleibt Download-Anhang. Diese konservative Einschränkung ist einem zusätzlichen PDF-/OCR-/Office-Paket ohne Freigabe vorzuziehen.

Getrennte Ablagen je Eingangskanal würden Originale, Dublettenschutz und Audit mehrfach implementieren. Ein vollständiger Mailclient, neue Suchmaschine, Scannerpakete oder produktive Datenbank wären für diesen Ausschnitt zu groß und überschritten die Freigabe. Eine bloße Schnittstelle ohne echten Eingang wäre ebenfalls unzureichend; Upload/Zuordnung und Ordneradapter sind deshalb als reale lokale Vertikalstrecken umgesetzt.

## Folgen und erneute Entscheidung

Alle Aktenänderungen, Entscheidungen und Importe sind In-Memory-Demo und können nach Neustart verloren gehen; Scan-Quelldateien bleiben lokal. Kein dauerhaft revisionssicheres Archiv. Feste Grenzen schützen den Demoprozess, ersetzen aber keine produktiven Quoten. Loopback-/Origin-/Header-Schutz, Ressourcenlesepolitik und Modulprüfung sind vorbereitende Grenzen, keine Rollenverwaltung.

Vor realen Daten oder Produktbetrieb: Ressourcen- und Schreibrechte, Mandanten, Malware-/Dateiprüfung, Persistenz/Transaktionen, Aufbewahrung und Löschung, Restore, Audit, Überwachung, Crash-Wiederanlauf und Scanner-Vertrauensgrenze gesondert entscheiden. Vor weiteren Extraktoren/Dependencies Lizenz-/Security-Gate. Kein Push, Release oder automatische Designabnahme aus diesem ADR.
