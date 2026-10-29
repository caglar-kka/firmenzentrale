import {terminzeiten} from './Terminzeit.js';
import{posteingangVorschau,eingangsUrsprung}from'./PosteingangVorschau.js';
import{useState}from'react';
// Only enabled by the explicit loopback preview host. Never stores passwords or browser data.
let aktiv=false,admin=true,mfa=false;
type Konto={id:string;login:string;anzeigename:string;rolle:string;aktiv:boolean;version:number;eingerichtet:boolean;einladungBis:string|null;wiederherstellungBis:string|null};
const users:Konto[]=[{id:'beispiel-admin',login:'beispiel.admin',anzeigename:'Mara Hoffmann',rolle:'Administrator',aktiv:true,version:1,eingerichtet:true,einladungBis:null,wiederherstellungBis:null},{id:'beispiel-team',login:'beispiel.team',anzeigename:'Jonas Weber',rolle:'Mitarbeiter',aktiv:true,version:1,eingerichtet:true,einladungBis:null,wiederherstellungBis:null}];
type Task={id:string;betreff:string;beschreibung:string;zustaendigId:string;erstellerId:string;status:string;prioritaet:string;faelligAm:string|null;faelligUm:string|null;archiviert:boolean;version:number;firmaId:string|null;rechnungId:string|null;akteId:string|null;kontaktId:string|null;dokumentId:string|null};
const today=()=>new Date().toLocaleDateString('sv-SE');
const tasks:Task[]=[{id:'vorschau-aufgabe',betreff:'Rückruf vorbereiten',beschreibung:'Rückruf mit dem Büro abstimmen.',zustaendigId:'beispiel-admin',erstellerId:'beispiel-admin',status:'Offen',prioritaet:'Normal',faelligAm:today(),faelligUm:null,archiviert:false,version:1,firmaId:null,rechnungId:null,akteId:null,kontaktId:null,dokumentId:null}];
type PreviewTermin={id:string;eigentuemerId:string;titel:string;beginn:string;ende:string;ganztaegig:boolean;beschreibung:string;ort:string;version:number;anlageHash:string;entfernt:boolean};
const termine:PreviewTermin[]=[];
const history=new Map<string,{benutzerId:string;autor:string;zeitpunkt:string;vorher:string;nachher:string}[]>();
let regel={administrator:false,mitarbeiter:false,version:0};
const actor=()=>users[admin?0:1]!;
export function vorschauAktivieren(value:boolean){aktiv=value&&typeof window!=='undefined'&&['127.0.0.1','localhost','[::1]'].includes(window.location.hostname);}
export function VorschauHinweis(){return <a className="p-vorschaubadge" href="#vorschau" title="Nur Beispieldaten, kein Echtbetrieb. Details in den Einstellungen.">Lokale Vorschau · Beispieldaten</a>;}
export function VorschauEinstellungen(){const[a,A]=useState(admin);return <section className="p-vorschaueinstellungen"><p>Diese lokale Vorschau verwendet ausschließlich frei erfundene Beispieldaten. Aufgaben, Benutzerkonten und Sicherheitsabläufe werden im Arbeitsspeicher nachgebildet; Neuladen setzt sie zurück. Es besteht keine persönliche Anmeldung und keine Verbindung zur Geschäftsdatenbank.</p><p>Keine echten Passwörter oder Kundendaten eingeben. Sicherheitsaktionen und QR-Code dienen hier nur der Ansicht. Zum Beenden das Browserfenster schließen.</p><details><summary>Andere Benutzeransicht ausprobieren</summary><label>Beispielkonto<select value={a?'admin':'team'} onChange={e=>{admin=e.target.value==='admin';A(admin);window.dispatchEvent(new window.Event('vorschau-rolle'));}}><option value="admin">Mara Hoffmann · Administrator</option><option value="team">Jonas Weber · Mitarbeiter</option></select></label><p>Der Ansichtswechsel verändert keine echten Benutzerrechte.</p></details></section>;}
export function vorschauAntwort(url:URL,init?:RequestInit):Response|null|Promise<Response>{
 if(!aktiv)return null;
 const p=url.pathname,q=url.searchParams,post=init?.method==='POST',u=actor();
 if(p.startsWith('/api/bildverwaltung'))return new Response(JSON.stringify({meldung:'Bildannahme gesperrt. Die Vorschau liest und speichert keine Bilder.'}),{status:503,headers:{'Content-Type':'application/json'}});
 if(p.startsWith('/api/posteingang')&&!(p.startsWith('/api/posteingang/beispiele')&&init?.method!=='POST'))return posteingangVorschau(url,init,(path,body)=>vorschauAntwort(new URL(path,url),body),u);
 const ok=(v:unknown,status=200)=>new Response(JSON.stringify(v),{status,headers:{'Content-Type':'application/json'}});
 const error=(text:string,status=409)=>ok({meldung:text,text},status);
 const visible=tasks.filter(t=>admin||t.erstellerId===u.id||t.zustaendigId===u.id);
 const row=(t:Task)=>({...t,zustaendig:users.find(u=>u.id===t.zustaendigId)?.anzeigename,ueberfaellig:!t.archiviert&&!['Erledigt','Abgebrochen'].includes(t.status)&&!!t.faelligAm&&t.faelligAm<today(),heuteFaellig:t.faelligAm===today()});
 if(p.startsWith('/api/termine')){
  const seg=p.split('/').filter(Boolean),id=seg[2],t=termine.find(t=>t.id===id&&t.eigentuemerId===u.id&&!t.entfernt);
  if(!post){if(id)return t?ok(t):error('Termin nicht verfügbar.',404);const von=q.get('von')??'',bis=q.get('bis')??'';const a=termine.filter(t=>t.eigentuemerId===u.id&&!t.entfernt&&t.beginn<bis+'T00:00'&&t.ende>von+'T00:00');return ok({eintraege:a.slice(0,500),anzahl:a.length,zeitzone:'Europe/Berlin'});}
  const b=JSON.parse(String(init?.body));
  if(id&&!t)return error('Termin nicht verfügbar.',404);
  if(t&&t.version!==b.version)return error('Der Termin wurde inzwischen geändert. Bitte neu laden.',409);
  if(seg[3]==='loeschen'){t!.entfernt=true;t!.version++;return ok({entfernt:true});}
  if(typeof b.titel!=='string'||!b.titel.trim()||b.titel.trim().length>160||typeof b.beschreibung!=='string'||b.beschreibung.length>4000||typeof b.ort!=='string'||b.ort.length>200||typeof b.ganztaegig!=='boolean')return error('Titel, Beschreibung und Ort prüfen.',400);
  let zeiten;try{zeiten=terminzeiten(b.beginn,b.ende,b.ganztaegig);}catch(e){return error((e as Error).message,400);}
  const hash=JSON.stringify([b.titel,b.beginn,b.ende,b.ganztaegig,b.beschreibung,b.ort]);
  if(!t){if(b.version!==0||!/^[a-f0-9]{32}$/.test(b.anlageId??''))return error('Anlagekennung prüfen.',400);const alt=termine.find(t=>t.id===b.anlageId);if(alt)return alt.eigentuemerId===u.id&&!alt.entfernt&&alt.version===1&&alt.anlageHash===hash?ok(alt):error('Anlagekennung bereits verwendet.',409);}
  const neu:PreviewTermin={...b,...zeiten,id:t?.id??b.anlageId,eigentuemerId:u.id,titel:b.titel.trim(),beschreibung:b.beschreibung.trim(),ort:b.ort.trim(),version:(t?.version??0)+1,anlageHash:t?.anlageHash??hash,entfernt:false};if(t)termine.splice(termine.indexOf(t),1,neu);else termine.push(neu);return ok(neu);
 }
 if(p==='/api/kalender'){const von=q.get('von')??'',bis=q.get('bis')??'';const rows=visible.filter(t=>t.zustaendigId===u.id&&!t.archiviert&&t.status!=='Abgebrochen'&&!!t.faelligAm&&t.faelligAm>=von&&t.faelligAm<bis);return ok({eintraege:rows.slice(0,200).map(row),anzahl:rows.length,zeitzone:'Europe/Berlin',art:'Aufgabenfristen'});}
 if(p==='/api/arbeitsumgebung')return ok({vorschau:true,authentifiziert:false,administrator:admin,benutzer:u.anzeigename,fluechtigeDaten:true,beispielkennzeichnung:true,kategorienVerwalten:admin,kontakteVerwalten:admin,aktenVerwalten:admin});
 if(p.startsWith('/api/admin')&&!admin)return error('In der Mitarbeitersimulation nicht verfügbar.',403);
 if(p==='/api/auth/session')return ok({aktiv:false,vorschau:true,benutzerId:u.id});
 if(p==='/api/auth/mfa/status')return ok({aktiv:mfa,verbleibend:mfa?8:0,pflicht:false});
 if(p==='/api/auth/mfa/vorbereiten')return ok({schluessel:'GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ',gueltigBis:new Date(Date.now()+600000).toISOString()});
 if(p==='/api/auth/mfa/aktivieren'||p==='/api/auth/mfa/codes'){mfa=true;return ok({codes:Array.from({length:8},(_,i)=>'SIMULATION-KEIN-ECHTER-CODE-'+i)});}
 if(p==='/api/auth/mfa/deaktivieren'){mfa=false;return ok(null);}
 if(p==='/api/auth/passwort')return ok(null); // Deliberately discard all entered values.
 if(p==='/api/auth/logout')return error('Vorschau beenden: Browserfenster schließen. Es besteht keine echte Sitzung.');
 if(p==='/api/admin/mfa/richtlinie'){if(post){const b=JSON.parse(String(init?.body));regel={administrator:!!b.administrator,mitarbeiter:!!b.mitarbeiter,version:regel.version+1};}return ok(regel);}
 if(p==='/api/admin/mfa/notfaelle/'&&!post)return ok([]);
 if(p.startsWith('/api/admin/mfa/notfaelle'))return error('Notfallverfahren hier nur zur Ansicht. Echte Identitätsprüfung, Freigabe und Sitzungswiderruf benötigen den authentifizierten Betrieb.');
 if(p==='/api/admin/benutzer'&&!post)return ok(users);
 if(p==='/api/admin/benutzer'&&post){const b=JSON.parse(String(init?.body));const id=crypto.randomUUID(),bis=new Date(Date.now()+3600000).toISOString();users.push({id,login:String(b.login),anzeigename:String(b.anzeigename),rolle:b.rolle,aktiv:true,eingerichtet:false,version:1,einladungBis:bis,wiederherstellungBis:null});return ok({id,code:'SIMULATION-KEIN-ZUGANG',gueltigBis:bis});}
 if(p.startsWith('/api/aufgaben')){
  if(p==='/api/aufgaben/mitarbeiter'||p==='/api/aufgaben/zustaendige')return ok(users.map(u=>({id:u.id,anzeigename:u.anzeigename})));
  if(p==='/api/aufgaben/meine-uebersicht'){const a=visible.filter(t=>t.zustaendigId===u.id&&!t.archiviert&&!['Erledigt','Abgebrochen'].includes(t.status));return ok({offen:a.length,heute:a.filter(t=>t.faelligAm===today()).length,ueberfaellig:a.filter(t=>row(t).ueberfaellig).length});}
  const id=p.slice('/api/aufgaben/'.length),old=visible.find(t=>t.id===id);
  if(post){const b=JSON.parse(String(init?.body));if(id&&!old)return error('Aufgabe nicht verfügbar.',404);if(old&&old.version!==b.version)return error('Die Aufgabe wurde inzwischen geändert.',409);if(!b.betreff||!b.zustaendigId)return error('Betreff und Zuständigkeit prüfen.');const t:Task={...b,archiviert:!!b.archiviert,id:old?.id??crypto.randomUUID(),erstellerId:old?.erstellerId??u.id,version:(old?.version??0)+1,rechnungId:null,akteId:null,kontaktId:null,dokumentId:null};if(b.bezugArt&&b.bezugId){const key=({Rechnung:'rechnungId',Akte:'akteId',Kontakt:'kontaktId',Dokument:'dokumentId'} as const)[b.bezugArt as 'Rechnung'];if(key)t[key]=b.bezugId;}history.set(t.id,[{benutzerId:u.id,autor:u.anzeigename,zeitpunkt:new Date().toISOString(),vorher:old?JSON.stringify({Betreff:old.betreff,Status:old.status}):'',nachher:JSON.stringify({Betreff:t.betreff,Status:t.status,Beschreibung:t.beschreibung})},...(history.get(t.id)??[])]);if(old)tasks.splice(tasks.indexOf(old),1,t);else tasks.push(t);return ok(row(t));}
  if(id)return old?ok({...eingangsUrsprung(old.id),aufgabe:row(old),ersteller:users.find(u=>u.id===old.erstellerId)?.anzeigename,zustaendig:row(old).zustaendig,historie:history.get(old.id)??[],zeitzone:'Europe/Berlin',ueberfaellig:row(old).ueberfaellig}):error('Aufgabe nicht verfügbar.',404);
  const view=q.get('ansicht'),open=(t:Task)=>!['Erledigt','Abgebrochen'].includes(t.status);let a=visible.filter(t=>t.archiviert===(view==='archiv'));if(q.get('bezugArt')==='Akte')a=a.filter(t=>t.akteId===q.get('bezugId')||eingangsUrsprung(t.id).eingangsakteId===q.get('bezugId'));
  if(view==='meine')a=a.filter(t=>t.zustaendigId===u.id);if(view==='erledigt')a=a.filter(t=>t.status==='Erledigt');if(view==='ueberfaellig')a=a.filter(t=>row(t).ueberfaellig);if(view==='heute')a=a.filter(t=>open(t)&&t.faelligAm===today());if(view==='zukuenftig')a=a.filter(t=>open(t)&&!!t.faelligAm&&t.faelligAm>today());if(view==='morgen'){const d=new Date();d.setDate(d.getDate()+1);a=a.filter(t=>open(t)&&t.faelligAm===d.toLocaleDateString('sv-SE'));}
  for(const [param,key] of [['status','status'],['prioritaet','prioritaet'],['zustaendig','zustaendigId']] as const)if(q.get(param))a=a.filter(t=>t[key]===q.get(param));if(q.get('text'))a=a.filter(t=>(t.betreff+' '+t.beschreibung).toLocaleLowerCase().includes(q.get('text')!.toLocaleLowerCase()));a.sort((a,b)=>(a.faelligAm??'9999').localeCompare(b.faelligAm??'9999'));const page=Math.max(0,Number(q.get('seite'))||0);return ok({eintraege:a.slice(page*20,page*20+20).map(row),anzahl:a.length,zeitzone:'Europe/Berlin'});
 }
 // Unknown account/security requests must never fall through to a real host endpoint.
 if(p.startsWith('/api/auth')||p.startsWith('/api/admin'))return error('Diese Funktion ist in der Vorschau nicht simuliert. Persönlichen Betrieb separat einrichten.');
 return null;
}
