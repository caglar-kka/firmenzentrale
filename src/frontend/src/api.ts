export interface Zahlung {
  datum: string; zahler: string; empfaenger: string; betrag: string; zugeordnet: string;
  status: string; beleg: string; quellenort: string;
}
export interface Rechnung {
  nummer: string; datum: string; aussteller: string; empfaenger: string; betrag: string;
  offenerBetrag: string; bestaetigtZugeordnet: string; status: string; beleg: string; quellenort: string; zahlungen: Zahlung[];
}
export interface Uebersicht {
  synthetisch: true; gesamtbetrag: string; offenerBetrag: string; rechnungen: Rechnung[];
}
export interface Quelle { dateiHash: string; blatt: string; zeile: number; zelle: string }
export interface Meldung { stufe: 'Warnung' | 'Fehler'; code: string; text: string; quelle: Quelle | null }
export interface Zelle {
  quelle: Quelle; datentyp: string; originalwert: string | null; text: string; formel: string | null; formatindex: string | null;
}
export interface Importzeile {
  zeile: number; hatFehler: boolean; art: string | null; betrag: string | null; datum: string | null;
  von: string | null; an: string | null; zellen: Zelle[]; meldungen: Meldung[];
}
export interface Importansicht {
  synthetisch: true; dateiHash: string | null; hatFehler: boolean; zeilenanzahl: number; fehlerzeilen: number;
  bericht: string; meldungen: Meldung[]; zeilen: Importzeile[];
}
export type Beispiel = 'fehler' | 'gueltig' | 'dublette';

const objekt = (v: unknown): v is Record<string, unknown> => typeof v === 'object' && v !== null && !Array.isArray(v);
const texte = (v: Record<string, unknown>, namen: string[]) => namen.every(n => typeof v[n] === 'string');
const optionalerText = (v: unknown) => v === null || typeof v === 'string';
const quelle = (v: unknown): boolean => objekt(v) && texte(v, ['dateiHash', 'blatt', 'zelle']) && Number.isInteger(v.zeile);
const meldung = (v: unknown): boolean => objekt(v) && (v.stufe === 'Fehler' || v.stufe === 'Warnung')
  && texte(v, ['code', 'text']) && (v.quelle === null || quelle(v.quelle));
const zelle = (v: unknown): boolean => objekt(v) && quelle(v.quelle) && texte(v, ['datentyp', 'text'])
  && ['originalwert', 'formel', 'formatindex'].every(n => optionalerText(v[n]));
export const istUebersicht = (v: unknown): v is Uebersicht => objekt(v) && v.synthetisch === true
  && texte(v, ['gesamtbetrag', 'offenerBetrag']) && Array.isArray(v.rechnungen)
  && v.rechnungen.every(r => objekt(r) && texte(r, ['nummer', 'datum', 'aussteller', 'empfaenger', 'betrag', 'offenerBetrag', 'bestaetigtZugeordnet', 'status', 'beleg', 'quellenort'])
    && Array.isArray(r.zahlungen) && r.zahlungen.every(z => objekt(z) && texte(z, ['datum', 'zahler', 'empfaenger', 'betrag', 'zugeordnet', 'status', 'beleg', 'quellenort'])));
export const istImportansicht = (v: unknown): v is Importansicht => objekt(v) && v.synthetisch === true
  && optionalerText(v.dateiHash) && typeof v.hatFehler === 'boolean' && Number.isInteger(v.zeilenanzahl)
  && Number.isInteger(v.fehlerzeilen) && typeof v.bericht === 'string' && Array.isArray(v.meldungen) && v.meldungen.every(meldung)
  && Array.isArray(v.zeilen) && v.zeilen.every(z => objekt(z) && Number.isInteger(z.zeile) && typeof z.hatFehler === 'boolean'
    && ['art', 'betrag', 'datum', 'von', 'an'].every(n => optionalerText(z[n]))
    && Array.isArray(z.zellen) && z.zellen.every(zelle) && Array.isArray(z.meldungen) && z.meldungen.every(meldung));

async function laden<T>(pfad: string, pruefen: (v: unknown) => v is T, signal: AbortSignal): Promise<T> {
  let antwort: Response;
  try {
    antwort = await fetch(pfad, { signal: AbortSignal.any([signal, AbortSignal.timeout(8000)]), headers: { Accept: 'application/json' }, cache: 'no-store' });
  } catch {
    throw new Error('Die lokale Verbindung ist nicht erreichbar. Bitte prüfen, ob das Backend läuft, und erneut versuchen.');
  }
  if (!antwort.ok) throw new Error('Die Beispieldaten konnten nicht geladen werden. Bitte erneut versuchen.');
  let daten: unknown;
  try { daten = await antwort.json(); } catch { throw new Error('Die Antwort konnte nicht gelesen werden. Bitte erneut versuchen.'); }
  if (!pruefen(daten)) throw new Error('Die Antwort entspricht nicht dem erwarteten synthetischen Datenformat.');
  return daten;
}
export const ladeUebersicht = (signal: AbortSignal) => laden('/api/uebersicht', istUebersicht, signal);
export const ladeImport = (beispiel: Beispiel, signal: AbortSignal) => laden(`/api/importvorschau/${beispiel}`, istImportansicht, signal);
