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
  if(text==='Offen')return{text,ton:'warnung',symbol:'○'};
  if(text==='Überfällig')return{text,ton:'fehler',symbol:'!'};
  if (text === 'Teilzahlung') return { text: 'Teilweise bezahlt', ton: 'arbeit', symbol: '◐' };
  if (text === 'Zur Prüfung') return { text, ton: 'warnung', symbol: '△' };
  return { text, ton: 'neutral', symbol: '○' };
}
export function importstatus(fehler: boolean, warnung: boolean): Statusdarstellung {
  return fehler ? { text: 'Fehler', ton: 'fehler', symbol: '×' }
    : warnung ? { text: 'Zur Prüfung', ton: 'warnung', symbol: '△' }
    : { text: 'Keine Zeilenfehler', ton: 'neutral', symbol: '○' };
}
// Nur Anzeige-Sortierung formatierter Serverbeträge, keine finanzielle Neuberechnung.
export type Sortierung = 'nummer' | 'datum' | 'aussteller' | 'empfaenger' | 'betrag' | 'status';
export const suchwoerter=(s:string)=>s.toLocaleLowerCase('de').replace(/[ä]/g,'a').replace(/[ö]/g,'o').replace(/[ü]/g,'u').replace(/ß/g,'ss').replace(/ae/g,'a').replace(/oe/g,'o').replace(/ue/g,'u').normalize('NFD').replace(/\p{M}/gu,'').match(/[\p{L}\p{N}]+/gu)??[];
function einFehler(a:string,b:string):boolean{if(Math.abs(a.length-b.length)>1)return false;let i=0,j=0,f=0;while(i<a.length&&j<b.length){if(a[i]===b[j]){i++;j++;continue;}if(++f>1)return false;if(a.length===b.length){if(i+1<a.length&&a[i]===b[j+1]&&a[i+1]===b[j]){i+=2;j+=2;}else{i++;j++;}}else if(a.length>b.length)i++;else j++;}return f+a.length-i+b.length-j<=1;}
export function suchePasst(q:string,text:string):boolean{if(q.length>120)return false;const a=suchwoerter(q),w=suchwoerter(text);return a.length?a.every(t=>w.some(s=>s.includes(t)||(t.length>=4&&s.length>=4&&einFehler(t,s)))):!q.trim();}
export function geldSortierwert(s:string):number|null{const t=s.replace(/\s/g,'').replace(/€$/,'');if(!/^-?(?:\d{1,3}(?:\.\d{3})*|\d+),\d{2}$/.test(t))return null;const n=Number(t.replace(/\./g,'').replace(',','.'));return Number.isFinite(n)?n:null;}
export function rechnungenFiltern(rechnungen:Rechnung[],suche:string,status:string,sortierung:Sortierung,ab=sortierung==='datum',richtung='Alle'):Rechnung[]{
 return rechnungen.filter(r=>(status==='Alle'||r.status===status)&&suchePasst(suche,[r.nummer,r.aussteller,r.empfaenger,r.beleg,r.quellenort].join(' '))&&(richtung==='Alle'||r.status!=='Bezahlt'&&eigeneRolle(r)===(richtung==='Forderungen'?'Aussteller':'Empfänger'))).sort((a,b)=>{
  let n:number;if(sortierung==='betrag'){const x=geldSortierwert(a.betrag),y=geldSortierwert(b.betrag);if(x===null||y===null)return x===y?0:x===null?1:-1;n=x-y;}
  else if(sortierung==='status')n=['Offen','Teilzahlung','Bezahlt'].indexOf(a.status)-['Offen','Teilzahlung','Bezahlt'].indexOf(b.status);
  else n=a[sortierung].localeCompare(b[sortierung],'de',{numeric:true});return (ab?-n:n)||a.nummer.localeCompare(b.nummer,'de');
 });
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

export function aktenstatus(s:string):Statusdarstellung{return{text:s,ton:s==='Offen'?'warnung':s==='Erledigt'?'erfolg':s==='In Bearbeitung'?'arbeit':s==='Wartet auf Rückmeldung'?'warnung':'neutral',symbol:'○'};}
