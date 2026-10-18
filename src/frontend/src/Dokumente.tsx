import { useCallback } from 'react';
import { ladeDokumente, dokumentAdresse, type Dokument, type Dokumentenliste } from './api.js';
import { useDaten } from './ui.js';
import { Icon } from './Icon.js';
import { datumAnzeigen } from './darstellung.js';

export function useDokumente(vorgangId?: string | null) {
  const laden = useCallback((signal: AbortSignal) => vorgangId ? ladeDokumente(vorgangId, signal)
    : Promise.resolve<Dokumentenliste>({ synthetisch: true, vorgangId: '', dokumente: [] }), [vorgangId]);
  return useDaten(laden);
}

// Ein kanonischer Abrufweg für Originalrechnung und den Beleg einer einzelnen Zahlung.
export function Dokumentkarte({ dokument: d }: { dokument: Dokument }) {
  return <div className="p-pdf">
    <a href={dokumentAdresse(d)} target="_blank" rel="noopener noreferrer"><Icon name="beleg" /><span><strong>{d.art} öffnen</strong><small>{d.dateiname} · PDF · neuer Tab</small></span><Icon name="quelle" /></a>
    <details><summary>Dateiangaben</summary><dl>
      <div><dt>Datum / Version</dt><dd>{datumAnzeigen(d.datum)} · {d.version}</dd></div>
      <div><dt>Dateigröße</dt><dd>{d.bytezahl} Bytes</dd></div>
      <div><dt>Herkunft</dt><dd>{d.quelle}</dd></div>
      <div><dt>SHA-256</dt><dd><code>{d.dateiHash}</code></dd></div>
    </dl><p>Hash identifiziert die Dateibytes, bestätigt keine Echtheit oder Zahlung.</p></details>
  </div>;
}

export function Dokumentbereich({ zustand }: { zustand: ReturnType<typeof useDokumente> }) {
  const { daten, laedt, fehler, erneut } = zustand;
  return <section className="p-dokumente" aria-label="Dokumentenakte"><h2><Icon name="beleg" />Dokumente</h2>
    {laedt && <p role="status">Dokumente werden geladen …</p>}
    {fehler && <div role="alert"><p>{fehler}</p><button onClick={erneut}>Erneut versuchen</button></div>}
    {daten && (daten.dokumente.length === 0 ? <p>In diesem Beispiel sind keine Originaldateien hinterlegt.</p>
      : <>{daten.dokumente.filter(d => !d.zahlungId).map(d => <Dokumentkarte key={d.id} dokument={d} />)}<p className="p-notiz">Zahlungsbelege stehen bei der jeweiligen Zahlung.</p><p className="p-notiz">PDF im neuen Tab lesen; zum Schließen den Tab schließen. Diese Rechnung bleibt geöffnet.</p></>)}
    <small>Synthetische Testbelege. Upload, Notizen und Stempel sind noch nicht verfügbar.</small>
  </section>;
}
