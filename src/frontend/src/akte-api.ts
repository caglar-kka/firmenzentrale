import{apiFetch as fetch}from'./auth.js';
export interface Vorgang{id:string;titel:string;kategorie:string;firmaId:string|null;rechnungsnummer:string|null;kategorieId?:string;firma?:string;status?:string;letztesEreignis?:string;dokumentanzahl?:number;kurzinfo?:string;version?:number}
export interface Dokument{id:string;titel:string;dateiname:string;art:string;kategorie:string;erfasstAm:string;version:number;vorschau:string;vorgaenge:string[];kategorieId?:string;status?:string;kanal?:string;absenderId?:string;absender?:string;vorgangsvorschlag?:string|null;pruefbedarf?:boolean;letzterEingangAm?:string;geaendertAm?:string;kurzfassung?:string;handlungsbedarf?:string;auswertungsquelle?:string;aktivitaeten?:{text:string;zeitpunkt:string;benutzer:string;vorgangId?:string;kanal?:string;vorherigerVorgangId?:string}[]}
export interface Notiz{id:string;betreff:string;inhalt:string;autor:string;erstelltAm:string;geaendertAm:string;version:number;entfernt:boolean;historie:{version:number;betreff:string;inhalt:string;autor:string;zeitpunkt:string;aktion:string}[]}
export interface Vorschlag{firma?:string;id:string;firmaId:string;dokumentId:string;art:string;bisher:string;neu:string;quelle:string;status:string;kontaktversion:number}
export interface Post{id:string;betreff:string;inhalt:string;art:string;autor:string;erfasstAm:string;dokumentId:string|null;dokumentIds?:string[];richtung?:string;kontaktId?:string;kommuniziertAm?:string}
export interface Akte{vorgang:Vorgang;dokumente:Dokument[];notizen:Notiz[];korrespondenz:Post[];kontaktvorschlaege:Vorschlag[]}
export interface Kategorie{id:string;name:string;beschreibung:string;elternId:string|null;reihenfolge:number;version:number;archiviert:boolean}
export interface Kontakt{firmaName?:string|null;geaendertAm?:string;vorname?:string;nachname?:string;hausnummer?:string;strasse?:string;plz?:string;ort?:string;land?:string;art?:string;firmaId?:string|null;anschrift?:string;abteilung?:string;rolle?:string;archiviert?:boolean;id:string;firma:string;version:number;werte:{art:string;wert:string;aktiv:boolean;primaer?:boolean;typ?:string}[];historie:{aktion:string;vorher:string;nachher:string;benutzer:string;zeitpunkt:string;dokumentId:string}[]}
export interface Treffer{id:string;art:string;titel:string;zusatz:string;ziel:string;vorgangId:string|null}
export const zeit=(s:string,zeitzone?:string)=>Number.isNaN(Date.parse(s))?'Zeitpunkt nicht verfügbar':new Date(s).toLocaleString('de-DE',{dateStyle:'short',timeStyle:'short',...(zeitzone?{timeZone:zeitzone}:{})});
export interface Dublette{id:string;name:string;art:string;anschrift:string;angaben:string[];gruende:string[]}
export class Arbeitsfehler extends Error{constructor(text:string,public hinweise:Dublette[]=[]){super(text);}}
export async function anfrage<T>(pfad:string,body?:unknown,signal?:AbortSignal):Promise<T>{
 const r=await fetch('/api/'+pfad,{method:body===undefined?'GET':'POST',headers:body===undefined?{Accept:'application/json'}:{'Content-Type':'application/json','X-Arbeitsaktion':'1'},body:body===undefined?undefined:JSON.stringify(body),signal:AbortSignal.any([...(signal?[signal]:[]),AbortSignal.timeout(8000)]),cache:'no-store'}).catch(()=>{throw new Error('Lokale Verbindung nicht erreichbar. Bitte erneut versuchen.');});
 if(!r.ok){let hinweise:Dublette[]=[];let text='Aktion fehlgeschlagen. Bitte neu laden.';try{const e:unknown=await r.json();if(e&&typeof e==='object'){if('hinweise'in e&&Array.isArray(e.hinweise))hinweise=e.hinweise.filter(h=>obj(h)&&strs(h,'id','name','art','anschrift')&&arr(h.angaben,x=>typeof x==='string')&&arr(h.gruende,x=>typeof x==='string')) as Dublette[];if('meldung'in e&&typeof e.meldung==='string')text=e.meldung;else if('text'in e&&typeof e.text==='string')text=e.text;}}catch{}throw new Arbeitsfehler(text,hinweise);}let daten:unknown;try{daten=await r.json();}catch{throw new Error('Die Antwort konnte nicht gelesen werden. Bitte erneut versuchen.');}if(!antwortGueltig(pfad,daten,body!==undefined))throw new Error('Die Antwort hat ein unerwartetes Format. Bitte neu laden.');return daten as T;
}
export const adresse=(d:Dokument)=>d.vorschau==='pdf'&&d.vorgaenge[0]?`/api/vorgaenge/${encodeURIComponent(d.vorgaenge[0])}/dokumente/${encodeURIComponent(d.id)}`:`/api/dokumente/${encodeURIComponent(d.id)}/datei`;
export async function ablegen(file:File,akte?:string,kanal='Dateiauswahl'){
 const r=await fetch('/api/dokumenteingang'+(akte?'?akte='+encodeURIComponent(akte):''),{method:'POST',body:file,headers:{'Content-Type':'application/octet-stream','X-Arbeitsaktion':'1','X-Dateiname':encodeURIComponent(file.name),'X-Dateityp':file.type,'X-Eingangsweg':kanal},signal:AbortSignal.timeout(15000)});if(!r.ok){let hinweise:Dublette[]=[];let text='Datei konnte nicht abgelegt werden.';try{const e=await r.json();if(typeof e.meldung==='string')text=e.meldung;else if(typeof e.text==='string')text=e.text;}catch{}throw new Error(text);}return r.json() as Promise<Dokument>;
}

const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const strs=(v:Record<string,unknown>,...keys:string[])=>keys.every(k=>typeof v[k]==='string');
const arr=(v:unknown,check:(x:unknown)=>boolean)=>Array.isArray(v)&&v.length<=1500&&v.every(check);
const dok=(v:unknown)=>obj(v)&&strs(v,'id','titel','dateiname','art','erfasstAm','vorschau')&&arr(v.vorgaenge,x=>typeof x==='string');
const vorgang=(v:unknown)=>obj(v)&&strs(v,'id','titel','kategorie');
const istTermin=(v:unknown)=>obj(v)&&strs(v,'id','titel','beginn','ende','beschreibung','ort')&&typeof v.ganztaegig==='boolean'&&Number.isSafeInteger(v.version)&&Number(v.version)>0&&['beginn','ende'].every(k=>/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(String(v[k])));
export function antwortGueltig(pfad:string,v:unknown,schreiben=false):boolean{
 if(pfad==='bildverwaltung/status')return obj(v)&&v.uploadAktiv===false&&v.status==='Gesperrt'&&v.bildVorhanden===false&&strs(v,'grund')&&typeof v.profilbildVerwalten==='boolean'&&typeof v.firmenlogoVerwalten==='boolean'&&arr(v.vorgeseheneFormate,x=>x==='PNG'||x==='JPEG')&&v.maxBytes===2000000&&v.maxDimension===2048&&v.maxPixel===4000000;
 if(/^termine\/[^/]+\/quittieren$/.test(pfad))return obj(v)&&v.quittiert===true;
 if(pfad==='termine/erinnerungen'||pfad.startsWith('termine/erinnerungen?'))return obj(v)&&Number.isSafeInteger(v.anzahl)&&Number(v.anzahl)>=0&&Array.isArray(v.eintraege)&&v.eintraege.length<=20&&v.eintraege.length<=Number(v.anzahl)&&arr(v.eintraege,x=>obj(x)&&strs(x,'id','titel','beginn','erinnerungskennung')&&String(x.erinnerungskennung).length>0&&String(x.erinnerungskennung).length<=64);
 if(/^termine\/[^/]+\/loeschen$/.test(pfad))return obj(v)&&v.entfernt===true;
 if(pfad.startsWith('termine/?'))return obj(v)&&Number.isSafeInteger(v.anzahl)&&Number(v.anzahl)>=0&&v.zeitzone==='Europe/Berlin'&&arr(v.eintraege,istTermin);
 if(pfad.startsWith('termine/'))return istTermin(v);
 if(schreiben&&pfad==='akten')return vorgang(v);
 if(pfad==='akten-status')return arr(v,x=>typeof x==='string');
 if(schreiben&&pfad==='kategorien')return obj(v)&&strs(v,'id','name','beschreibung')&&typeof v.archiviert==='boolean';
 if(schreiben&&pfad==='kontakte')return obj(v)&&strs(v,'id','firma')&&Array.isArray(v.werte)&&Array.isArray(v.historie);

 if(/^akten\/[^/]+$/.test(pfad))return obj(v)&&vorgang(v.vorgang)&&arr(v.dokumente,dok)&&arr(v.notizen,n=>obj(n)&&strs(n,'id','betreff','inhalt','autor','erstelltAm','geaendertAm')&&typeof n.entfernt==='boolean'&&arr(n.historie,h=>obj(h)&&strs(h,'betreff','inhalt','aktion','autor','zeitpunkt')))&&arr(v.korrespondenz,k=>obj(k)&&strs(k,'id','betreff','inhalt','art','autor','erfasstAm'))&&arr(v.kontaktvorschlaege,k=>obj(k)&&strs(k,'id','firmaId','dokumentId','art','bisher','neu','quelle','status'));
 if((pfad.startsWith('akten?')||pfad.startsWith('kontakte?'))&&pfad.includes('seite='))return obj(v)&&Number.isInteger(v.anzahl)&&arr(v.eintraege,pfad.startsWith('akten?')?vorgang:k=>obj(k)&&strs(k,'id','firma')&&Array.isArray(k.werte)&&Array.isArray(k.historie));
 if(pfad==='akten'||pfad.startsWith('akten?'))return arr(v,vorgang);
 if(pfad==='posteingang/uebersicht')return obj(v)&&[v.neu,v.pruefen,v.meine].every(n=>typeof n==='number'&&Number.isInteger(n)&&n>=0);
 if(/^posteingang\/[^/?]+$/.test(pfad))return dok(v)||(obj(v)&&obj(v.eingang)&&strs(v.eingang,'id','dokumentId','betreff','status')&&Array.isArray(v.historie));
 if(pfad==='posteingang'||pfad.startsWith('posteingang?'))return obj(v)&&(('dokumente'in v&&arr(v.dokumente,dok)&&obj(v.scanner)&&(v.anzahl===undefined||Number.isInteger(v.anzahl)))||('eintraege'in v&&Number.isInteger(v.anzahl)&&arr(v.eintraege,e=>obj(e)&&strs(e,'id','dokumentId','betreff','status','eingegangenAm'))));
 if(pfad==='kategorien')return arr(v,k=>obj(k)&&strs(k,'id','name','beschreibung')&&typeof k.archiviert==='boolean');
 if(pfad==='kontakte')return arr(v,k=>obj(k)&&strs(k,'id','firma')&&arr(k.werte,w=>obj(w)&&strs(w,'art','wert')&&typeof w.aktiv==='boolean')&&Array.isArray(k.historie));
 if(pfad.startsWith('suche?'))return arr(v,t=>obj(t)&&strs(t,'id','art','titel','zusatz','ziel')&&['rechnung','vorgang','dokument','eingang','kontakt'].includes(String(t.ziel)));
 if(pfad.endsWith('/text'))return obj(v)&&typeof v.text==='string';
 return obj(v);
}
