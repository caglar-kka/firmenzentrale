import type { Rechnung } from './api.js';

// Regionale Darstellung vorbereiten; keine Steuerregeln, Umrechnung oder CHF-Fachfreigabe.
export const regionen = {
  DE: { locale: 'de-DE', waehrung: 'EUR' },
  AT: { locale: 'de-AT', waehrung: 'EUR' },
  CH: { locale: 'de-CH', waehrung: 'CHF' },
} as const;
export type Region = keyof typeof regionen;
export const ui = { suchen: 'Rechnungen suchen', alle: 'Alle Zahlungsstatus', leer: 'Keine passenden Rechnungen' } as const;

export function datumAnzeigen(iso: string, region: Region = 'DE'): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(iso)) return iso;
  const datum = new Date(`${iso}T12:00:00Z`);
  if (Number.isNaN(datum.valueOf()) || datum.toISOString().slice(0, 10) !== iso) return iso;
  return new Intl.DateTimeFormat(regionen[region].locale, { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' }).format(datum);
}
export type Statusdarstellung = { text: string; ton: 'neutral' | 'erfolg' | 'arbeit' | 'warnung' | 'fehler'; symbol: string };
export function zahlungsstatus(text: string): Statusdarstellung {
  if (text === 'Bezahlt' || text === 'Bestätigt' || text === 'Bestätigt (synthetisch)') return { text: text.startsWith('Bestätigt')?'Zahlung bestätigt':text, ton: 'erfolg', symbol: '✓' };
  if (text === 'Teilzahlung') return { text: 'Teilweise bezahlt', ton: 'arbeit', symbol: '◐' };
  if (text === 'Zur Prüfung') return { text, ton: 'warnung', symbol: '△' };
  return { text, ton: 'neutral', symbol: '○' };
}
export function importstatus(fehler: boolean, warnung: boolean): Statusdarstellung {
  return fehler ? { text: 'Fehler', ton: 'fehler', symbol: '×' }
    : warnung ? { text: 'Zur Prüfung', ton: 'warnung', symbol: '△' }
    : { text: 'Keine Zeilenfehler', ton: 'neutral', symbol: '○' };
}
export type Sortierung = 'nummer' | 'datum' | 'aussteller' | 'empfaenger';
export function rechnungenFiltern(rechnungen: Rechnung[], suche: string, status: string, sortierung: Sortierung): Rechnung[] {
  const begriff = suche.trim().toLocaleLowerCase('de-DE');
  return rechnungen.filter(r => (status === 'Alle' || r.status === status)
    && [r.nummer, r.aussteller, r.empfaenger, r.beleg, r.quellenort].some(s => s.toLocaleLowerCase('de-DE').includes(begriff)))
    .sort((a, b) => sortierung === 'datum' ? b.datum.localeCompare(a.datum) || a.nummer.localeCompare(b.nummer, 'de')
      : a[sortierung].localeCompare(b[sortierung], 'de', { numeric: true }));
}


// Ausschließlich Anzeige einer expliziten, konsistenten Demo-Perspektive; keine Geldberechnung.
export function eigeneRolle(r: Rechnung): 'Aussteller' | 'Empfänger' | null {
  const p = r.perspektive;
  return p && ((p.rolle === 'Aussteller' && p.unternehmen === r.aussteller) || (p.rolle === 'Empfänger' && p.unternehmen === r.empfaenger)) ? p.rolle : null;
}
export function zahlungsrichtung(r: Rechnung, z: Rechnung['zahlungen'][number]): string | null {
  if (!eigeneRolle(r) || !r.perspektive) return null;
  const eigen = r.perspektive.unternehmen;
  if (z.zahler === eigen && z.empfaenger !== eigen) return 'Eigene Zahlung';
  if (z.empfaenger === eigen && z.zahler !== eigen) return (z.status === 'Bestätigt'||z.status === 'Bestätigt (synthetisch)') ? 'Zahlung erhalten' : 'Zahlungseingang zur Prüfung';
  return null;
}

export function rechnungsart(r:Rechnung):string{return eigeneRolle(r)==='Aussteller'?'Ausgangsrechnung':eigeneRolle(r)==='Empfänger'?'Eingangsrechnung':'Rechnung';}
export function zahlungsanzeige(r:Rechnung,z:Rechnung['zahlungen'][number]):Statusdarstellung{
 const basis=zahlungsstatus(z.status);if(!['Bestätigt','Bestätigt (synthetisch)'].includes(z.status))return basis;
 const richtung=zahlungsrichtung(r,z);return{...basis,text:richtung==='Eigene Zahlung'?'Eigene Zahlung bestätigt':richtung==='Zahlung erhalten'?'Zahlungseingang bestätigt':'Zahlung bestätigt · Richtung ungeklärt'};
}
export const eingangsstatus=(s?:string)=>s==='Zugeordnet'?'Akte zugeordnet':s??'Zur Prüfung';
