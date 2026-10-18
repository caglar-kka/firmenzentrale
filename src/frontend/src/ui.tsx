import { useEffect, useState, type KeyboardEvent } from 'react';
import { Icon, type Iconname } from './Icon.js';
import type { Meldung } from './api.js';
import type { Statusdarstellung } from './darstellung.js';

export function useDaten<T>(laden: (signal: AbortSignal) => Promise<T>) {
  const [daten, setDaten] = useState<T | null>(null);
  const [fehler, setFehler] = useState<string | null>(null);
  const [laedt, setLaedt] = useState(true);
  const [versuch, setVersuch] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setLaedt(true); setFehler(null); setDaten(null);
    laden(controller.signal).then(ergebnis => {
      if (!controller.signal.aborted) { setDaten(ergebnis); setLaedt(false); }
    }).catch(error => {
      if (!controller.signal.aborted) {
        setFehler(error instanceof Error ? error.message : 'Die Ansicht konnte nicht geladen werden.'); setLaedt(false);
      }
    });
    return () => controller.abort();
  }, [laden, versuch]);
  return { daten, fehler, laedt, erneut: () => setVersuch(v => v + 1) };
}
export function tabellenTasten(e: KeyboardEvent<HTMLDivElement>) {
  if (e.target !== e.currentTarget || (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft')) return;
  e.preventDefault();
  e.currentTarget.scrollLeft += e.key === 'ArrowRight' ? 300 : -300;
}

export function Status({ wert }: { wert: Statusdarstellung }) {
  const icons: Record<Statusdarstellung['ton'], Iconname> = { neutral: 'offen', erfolg: 'bezahlt', arbeit: 'teilweise', warnung: 'pruefen', fehler: 'fehler' };
  return <span className={`status ${wert.ton}${wert.text === 'Offen' ? ' offen' : ''}`}><Icon name={icons[wert.ton]} />{wert.text}</span>;
}

export function Meldungsliste({ meldungen }: { meldungen: Meldung[] }) {
  if (meldungen.length === 0) return <p className="klein">Keine zusätzlichen Prüfmeldungen.</p>;
  return <ul className="meldungen">{meldungen.map((m, i) => <li key={i} className={m.stufe === 'Fehler' ? 'meldung-fehler' : 'meldung-warnung'}><strong><Icon name={m.stufe === 'Fehler' ? 'fehler' : 'pruefen'} />{m.stufe}</strong><p>{m.text}</p>{m.quelle && <p className="klein">Quelle: <code>{m.quelle.blatt}!{m.quelle.zelle}</code> · Zeile {m.quelle.zeile}</p>}</li>)}</ul>;
}

export function Zustand({ laedt, fehler, erneut }: { laedt: boolean; fehler: string | null; erneut: () => void }) {
  return <>{laedt && <p className="p-notiz" role="status">Beispieldaten werden geladen …</p>}{fehler && <section className="p-fehler" role="alert"><Icon name="fehler" /><h2>Verbindung unterbrochen</h2><p>{fehler}</p><button onClick={erneut}>Erneut versuchen</button></section>}</>;
}

export function Titel({ oben, titel, text }: { oben: string; titel: string; text: string }) {
  return <header className="p-titel"><p className="p-eyebrow">{oben}</p><h1>{titel}</h1><p>{text}</p></header>;
}

export function Kennzahlen({ offen, betrag, anzahl,forderungen,verbindlichkeiten }: { offen: string; betrag: string; anzahl: number;forderungen?:string|null;verbindlichkeiten?:string|null }) {
  const getrennt=!!forderungen&&!!verbindlichkeiten;
  return <dl className="p-bilanz">{getrennt?<><div><dt>Offene Forderungen</dt><dd>{forderungen}</dd><span>Von Kunden noch zu erhalten</span></div><div><dt>Offene Verbindlichkeiten</dt><dd>{verbindlichkeiten}</dd><span>Von uns noch zu bezahlen</span></div></>:<><div><dt>Offene Rechnungsbeträge</dt><dd>{offen}</dd><span>Zahlungsrichtungen nicht vollständig zugeordnet · kein Forderungssaldo</span></div><div><dt>Rechnungsbeträge gesamt</dt><dd>{betrag}</dd></div></>}<div><dt>Rechnungsbestand</dt><dd>{anzahl} {anzahl===1?'Rechnung':'Rechnungen'}</dd></div></dl>;
}
