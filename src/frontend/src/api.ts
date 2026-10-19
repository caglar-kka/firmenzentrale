export interface Zahlung {
  datum: string; zahler: string; empfaenger: string; betrag: string; zugeordnet: string;
  status: string; beleg: string; quellenort: string; id?: string | null;
}
export interface Kontakt {
  emails?:string[]; telefone?:string[]; ansprechpartnerListe?:string[]; ansprechpartner: string | null; email: string | null; telefon: string | null; quelle: string;
}
export interface Rechnung {
  nummer: string; datum: string; aussteller: string; empfaenger: string; betrag: string;
  offenerBetrag: string; bestaetigtZugeordnet: string; status: string; beleg: string; quellenort: string; zahlungen: Zahlung[];
  ausstellerKontakt?: Kontakt | null; empfaengerKontakt?: Kontakt | null;
  vorgangId?: string | null; perspektive?: {unternehmen: string; rolle: 'Aussteller' | 'Empfänger'} | null;
}
export interface Uebersicht {
  synthetisch: true; anzahl?:number;bestandsanzahl?:number;offeneForderungen?:string|null; offeneVerbindlichkeiten?:string|null; gesamtbetrag: string; offenerBetrag: string; rechnungen: Rechnung[];
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
const optionaleId = (v: unknown) => v === undefined || v === null || (typeof v === 'string' && /^[a-z0-9][a-z0-9-]{0,63}$/.test(v) && !/\s/.test(v));
const kontakt = (v: unknown): boolean => v === undefined || v === null || (objekt(v)
  && ['emails','telefone','ansprechpartnerListe'].every(n=>v[n]===undefined||(Array.isArray(v[n])&&v[n].length<=250&&v[n].every(x=>typeof x==='string'))) && typeof v.quelle === 'string' && ['ansprechpartner', 'email', 'telefon'].every(n => optionalerText(v[n])));
const quelle = (v: unknown): boolean => objekt(v) && texte(v, ['dateiHash', 'blatt', 'zelle']) && Number.isInteger(v.zeile);
const meldung = (v: unknown): boolean => objekt(v) && (v.stufe === 'Fehler' || v.stufe === 'Warnung')
  && texte(v, ['code', 'text']) && (v.quelle === null || quelle(v.quelle));
const zelle = (v: unknown): boolean => objekt(v) && quelle(v.quelle) && texte(v, ['datentyp', 'text'])
  && ['originalwert', 'formel', 'formatindex'].every(n => optionalerText(v[n]));
export const istUebersicht = (v: unknown): v is Uebersicht => objekt(v) && v.synthetisch === true
  && texte(v, ['gesamtbetrag', 'offenerBetrag']) && Array.isArray(v.rechnungen)
  && v.rechnungen.every(r => objekt(r) && texte(r, ['nummer', 'datum', 'aussteller', 'empfaenger', 'betrag', 'offenerBetrag', 'bestaetigtZugeordnet', 'status', 'beleg', 'quellenort'])
    && kontakt(r.ausstellerKontakt) && kontakt(r.empfaengerKontakt) && optionaleId(r.vorgangId)
    && (r.perspektive === undefined || r.perspektive === null || (objekt(r.perspektive) && typeof r.perspektive.unternehmen === 'string' && ['Aussteller','Empfänger'].includes(String(r.perspektive.rolle))))
    && Array.isArray(r.zahlungen) && r.zahlungen.every(z => objekt(z) && optionaleId(z.id) && texte(z, ['datum', 'zahler', 'empfaenger', 'betrag', 'zugeordnet', 'status', 'beleg', 'quellenort'])));
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
  if (!pruefen(daten)) throw new Error('Die Antwort entspricht nicht dem erwarteten Datenformat.');
  return daten;
}
export const ladeUebersicht = (signal: AbortSignal,query='') => laden('/api/uebersicht'+query, istUebersicht, signal);
export const ladeImport = (beispiel: Beispiel, signal: AbortSignal) => laden(`/api/importvorschau/${beispiel}`, istImportansicht, signal);


export interface Dokument {
  id: string; unternehmenId: string; vorgangId: string; art: string;
  dateiname: string; datum: string; version: number; bytezahl: number; dateiHash: string; quelle: string; zahlungId: string | null;
}
export interface Dokumentenliste { synthetisch: true; vorgangId: string; dokumente: Dokument[] }
const id = (v: unknown): v is string => typeof v === 'string' && /^[a-z0-9][a-z0-9-]{0,63}$/.test(v) && !/\s/.test(v);
export const istDokumentenliste = (v: unknown): v is Dokumentenliste => objekt(v) && v.synthetisch === true && id(v.vorgangId)
  && Array.isArray(v.dokumente) && v.dokumente.length <= 512 && v.dokumente.every(d => objekt(d) && id(d.id)
    && d.unternehmenId === 'DEMO-NORD' && d.vorgangId === v.vorgangId && texte(d,['dateiname','datum','dateiHash','quelle'])
    && typeof d.dateiHash === 'string' && d.dateiHash.length === 64 && /^[A-F0-9]{64}$/.test(d.dateiHash) && Number.isInteger(d.version) && Number(d.version) > 0
    && Number.isInteger(d.bytezahl) && Number(d.bytezahl) > 0 && Number(d.bytezahl) <= 2097152
    && ((d.art === 'Anhang' && d.zahlungId === null) || (d.art === 'Originalrechnung' && d.zahlungId === null) || (d.art === 'Zahlungsbeleg' && id(d.zahlungId))))
  && new Set(v.dokumente.map(d => d.id)).size === v.dokumente.length;
export async function ladeDokumente(vorgangId: string, signal: AbortSignal): Promise<Dokumentenliste> {
  try {
    if (!id(vorgangId)) throw new Error();
    const daten = await laden(`/api/vorgaenge/${encodeURIComponent(vorgangId)}/dokumente`, istDokumentenliste, signal);
    if (daten.vorgangId !== vorgangId) throw new Error();
    return daten;
  } catch { throw new Error('Dokumente konnten nicht geladen werden. Bitte erneut versuchen.'); }
}
export function dokumentAdresse(d: Dokument): string {
  return `/api/vorgaenge/${encodeURIComponent(d.vorgangId)}/dokumente/${encodeURIComponent(d.id)}`;
}


export interface Demoimport {
  id: string; synthetisch: true; blatt: string; spalten: string[]; firmen: string[]; firmenhinweise?: Record<string,import("./akte-api.js").Dublette[]>;
  mapping: Record<string,string>; mappingBestaetigt: boolean; vorschau: Importansicht;
  ergebnis: {uebernommen: number; uebersprungen: number; fehlerhaft: number; zurPruefung: number} | null;
  entschiedenAm: string | null; auswahl: number[];
}
export const istDemoimport = (v: unknown): v is Demoimport => objekt(v) && id(v.id) && v.synthetisch === true
  && v.blatt === 'ISD-Testdaten' && Array.isArray(v.spalten) && v.spalten.length === 9 && v.spalten.every(s=>typeof s==='string')
  && Array.isArray(v.firmen) && v.firmen.every(s=>typeof s==='string') && objekt(v.mapping)
  && Object.values(v.mapping).every(s=>typeof s==='string') && typeof v.mappingBestaetigt === 'boolean' && istImportansicht(v.vorschau)
  && Array.isArray(v.auswahl) && v.auswahl.every(Number.isInteger) && optionalerText(v.entschiedenAm)
  && (v.ergebnis === null || (objekt(v.ergebnis) && ['uebernommen','uebersprungen','fehlerhaft','zurPruefung'].every(k=>Number.isInteger((v.ergebnis as Record<string,unknown>)[k]) && Number((v.ergebnis as Record<string,unknown>)[k])>=0)));

async function importAntwort(pfad: string, options: RequestInit): Promise<Demoimport> {
  let response: Response;
  try { response=await fetch(pfad,{...options,signal:AbortSignal.timeout(15000),cache:'no-store'}); }
  catch { throw new Error('Die lokale Verbindung ist nicht erreichbar. Bitte erneut versuchen. Der Übernahmestand kann über die bisherigen Importe geprüft werden.'); }
  let daten: unknown;
  try { daten=await response.json(); } catch { throw new Error('Die Antwort konnte nicht gelesen werden. Bitte erneut versuchen.'); }
  if (!response.ok) throw new Error(objekt(daten) && typeof daten.meldung === 'string' && daten.meldung.length < 1000 ? daten.meldung : 'Die Datei konnte nicht verarbeitet werden. Bitte prüfen.');
  if (!istDemoimport(daten)) throw new Error('Die Importantwort entspricht nicht dem erwarteten Demoformat.');
  return daten;
}
export async function dateiPruefen(datei: File): Promise<Demoimport> {
  if (!/\.xlsx$/i.test(datei.name) || datei.size === 0 || datei.size > 2*1024*1024) throw new Error('Bitte eine XLSX-Datei bis 2,1 MB auswählen.');
  return importAntwort('/api/importe/vorschau',{method:'POST',headers:{'Content-Type':'application/octet-stream','X-Demo-Import':'1','X-Dateiname':encodeURIComponent(datei.name)},body:datei});
}
export const importZuordnen = (v: Demoimport, mapping: Record<string,string>) => importAntwort(`/api/importe/${encodeURIComponent(v.id)}/mapping`,{
  method:'POST',headers:{'Content-Type':'application/json','X-Demo-Import':'1'},body:JSON.stringify({blatt:v.blatt,unternehmen:mapping,bestaetigt:true})});
export const importUebernehmen = (v: Demoimport, zeilen: number[]) => importAntwort(`/api/importe/${encodeURIComponent(v.id)}/uebernehmen`,{
  method:'POST',headers:{'Content-Type':'application/json','X-Demo-Import':'1'},body:JSON.stringify({zeilen,bestaetigt:true})});
export const importLesen = (id: string) => importAntwort(`/api/importe/${encodeURIComponent(id)}`,{method:'GET'});
export async function importeLaden(): Promise<Demoimport[]> {
  const r=await fetch('/api/importe',{signal:AbortSignal.timeout(8000),cache:'no-store'}).catch(()=>{throw new Error('Die lokale Verbindung ist nicht erreichbar. Bitte erneut versuchen.');});
  const daten: unknown=await r.json();
  if (!r.ok || !Array.isArray(daten) || !daten.every(istDemoimport)) throw new Error('Bisherige Importe konnten nicht geladen werden.');
  return daten;
}
