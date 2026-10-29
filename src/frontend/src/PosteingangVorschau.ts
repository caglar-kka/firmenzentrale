import type{Eingang,Angaben}from'./Posteingang.js';
import type{Dokument,Vorgang}from'./akte-api.js';
type Api=(path:string,init?:RequestInit)=>Response|null|Promise<Response|null>;
const events=new Map<string,Eingang>(),histories=new Map<string,{text:string;benutzer:string;zeitpunkt:string}[]>(),originals=new Map<string,Dokument>();
export function eingangsUrsprung(id:string){const e=[...events.values()].find(e=>e.aufgabeId===id);return{eingangId:e?.id??null,eingangsakteId:e?.akteId??null};}
let sync:Promise<void>|null=null;
async function request(path:string,init?:RequestInit){const r=await fetch(path,init);if(!r.ok)throw Error('Vorschaudaten nicht verfügbar.');return r;}
async function laden(){
 if(sync)return sync;
 sync=(async()=>{const data=await (await request('/api/posteingang')).json() as {dokumente:Dokument[]};for(const d of data.dokumente){originals.set(d.id,d);const times=d.aktivitaeten?.filter(h=>h.kanal)??[];for(const[h,index]of times.map((v,i)=>[v,i] as const)){
  const id=d.id+'-eingang-'+index;if(events.has(id))continue;
  let text='';try{text=(await (await request('/api/dokumente/'+d.id+'/text')).json()).text??'';}catch{/* Keep missing fields unknown. */}
  const value=(key:string)=>new RegExp('^'+key+':\\s*([^\\r\\n]+)$','m').exec(text)?.[1]?.trim()??null;const art=value('Dokumentart');const angebot:Angaben={betreff:d.titel,art:['Eingangsrechnung','Versicherungsschreiben','Behördenbrief','Kundenkorrespondenz'].includes(art??'')?art!:'Sonstiges',dokumentdatum:value('Dokumentdatum'),rechnungsnummer:value('Rechnungsnummer'),betrag:value('Betrag')!==null?Number(value('Betrag')):null,faelligAm:value('Antwort bis'),referenz:value('Referenz')};
  events.set(id,{id,dokumentId:d.id,betreff:d.titel,art:angebot.art,dateiname:d.dateiname,absender:'Noch ungeklärt',absenderId:null,akteId:d.vorgaenge[0]??null,akte:null,zustaendigId:null,zustaendig:null,status:'Neu',kanal:h.kanal??'Upload',eingegangenAm:h.zeitpunkt,version:0,aufgabeId:null,vorschlag:angebot,bestaetigteAngaben:null,quelle:'Regelvorschlag aus Dokumenttext; keine fachliche Bestätigung',rueckfrage:'Zuordnung und Angaben prüfen.',identischErneut:index>0,vorschau:d.vorschau,geprueftAm:null});
 }} })().finally(()=>{sync=null;});return sync;
}
export async function posteingangVorschau(url:URL,init:RequestInit|undefined,api:Api,actor:{id:string;anzeigename:string}):Promise<Response>{
 const ok=(v:unknown,status=200)=>new Response(JSON.stringify(v),{status,headers:{'Content-Type':'application/json'}}),error=(meldung:string,status=409)=>ok({meldung},status);const p=url.pathname,post=init?.method==='POST';
 try{
  if(p.startsWith('/api/posteingang/beispiele/')&&post){const key=p.split('/').pop()!;const f=await request('/api/posteingang/beispiele/'+key);const bytes=await f.arrayBuffer();await request('/api/dokumenteingang',{method:'POST',headers:{'Content-Type':'application/octet-stream','X-Arbeitsaktion':'1','X-Dateiname':key==='wartung-pdf'?'Wartungsrechnung Oktober 2026.pdf':(key==='wiederholung'?'rechnung':key)+'-beispiel.txt','X-Dateityp':key==='wartung-pdf'?'application/pdf':'text/plain'},body:bytes});await laden();return ok({uebernommen:true});}
  await laden();const parts=p.slice('/api/posteingang'.length).split('/').filter(Boolean);const e=events.get(parts[0]??'')??[...events.values()].filter(e=>e.dokumentId===parts[0]).sort((a,b)=>b.eingegangenAm.localeCompare(a.eingegangenAm))[0];
  if(parts[0]==='uebersicht'){const rows=[...events.values()];return ok({neu:rows.filter(e=>e.status==='Neu').length,pruefen:rows.filter(e=>['Neu','Zur Prüfung'].includes(e.status)).length,meine:rows.filter(e=>e.zustaendigId===actor.id&&e.status!=='Erledigt').length});}
  if(!parts.length){const q=url.searchParams,view=q.get('sicht')??'pruefen',text=(q.get('text')??'').toLowerCase();let rows=[...events.values()];if(view==='neu')rows=rows.filter(e=>e.status==='Neu');if(view==='pruefen')rows=rows.filter(e=>['Neu','Zur Prüfung'].includes(e.status));if(view==='meine')rows=rows.filter(e=>e.zustaendigId===actor.id&&e.status!=='Erledigt');if(view==='erledigt')rows=rows.filter(e=>e.status==='Erledigt');if(q.get('akte'))rows=rows.filter(e=>e.akteId===q.get('akte'));if(text)rows=rows.filter(e=>(e.betreff+' '+e.dateiname).toLowerCase().includes(text));rows.sort((a,b)=>b.eingegangenAm.localeCompare(a.eingegangenAm));const start=(Number(q.get('seite'))||0)*20;return ok({eintraege:rows.slice(start,start+20),anzahl:rows.length});}
  if(!e)return error('Eingang nicht verfügbar.',404);
  if(post){const b=JSON.parse(String(init?.body));if(parts[1]==='aufgabe'&&e.aufgabeId)return ok({aufgabeId:e.aufgabeId});if(b.version!==e.version)return error('Eingang wurde geändert. Bitte neu laden.');
   if(parts[1]==='pruefen'){
    if(!b.bestaetigt||!b.grund||!b.angaben?.betreff)return error('Entscheidung und Begründung ausdrücklich bestätigen.');
    if(['Zuordnen','Erledigen'].includes(b.entscheidung)&&!b.akteId)return error('Bitte eine vorhandene Akte wählen.');
    if(['Zuordnen','Erledigen'].includes(b.entscheidung)){
     const doc=originals.get(e.dokumentId)!;if(doc.vorgaenge.length&&!doc.vorgaenge.includes(b.akteId))return error('Das Original gehört bereits zu einer anderen Akte.');
     if(!doc.vorgaenge.length){const akte=await (await request('/api/akten/'+b.akteId)).json() as {vorgang:Vorgang};await request('/api/posteingang/'+doc.id+'/zuordnen',{method:'POST',headers:{'Content-Type':'application/json','X-Arbeitsaktion':'1'},body:JSON.stringify({vorgangId:b.akteId,neuerTitel:null,kategorie:akte.vorgang.kategorieId,firmaId:null,version:doc.version,dublettenBestaetigt:false})});}
    }
    const users=await (await api('/api/aufgaben/mitarbeiter'))!.json() as {id:string;anzeigename:string}[];let sender='Noch ungeklärt',akteName=null;
    if(b.absenderId){const kontakte=await (await request('/api/kontakte')).json() as {id:string;firma:string}[];const kontakt=kontakte.find(k=>k.id===b.absenderId);if(!kontakt)return error('Absender nicht verfügbar.',404);sender=kontakt.firma;}
    if(b.akteId)akteName=(await (await request('/api/akten/'+b.akteId)).json()).vorgang.titel;
    Object.assign(e,{betreff:b.angaben.betreff,art:b.angaben.art,bestaetigteAngaben:b.angaben,absenderId:b.absenderId,absender:sender,akteId:b.akteId,akte:akteName,zustaendigId:b.zustaendigId,zustaendig:users.find(u=>u.id===b.zustaendigId)?.anzeigename??null,rueckfrage:b.rueckfrage,geprueftAm:new Date().toISOString(),status:['Bereits vorhanden','Erledigen'].includes(b.entscheidung)?'Erledigt':b.entscheidung==='Zuordnen'?'Zugeordnet':'Zur Prüfung',version:e.version+1});
    histories.set(e.id,[{text:b.entscheidung+': '+b.grund,benutzer:actor.anzeigename,zeitpunkt:new Date().toISOString()},...(histories.get(e.id)??[])]);
   }else if(parts[1]==='aufgabe'){
    if(!e.geprueftAm||!['Zugeordnet','Erledigt'].includes(e.status)||!b.bestaetigt||b.aufgabe.faelligAm&&!b.fristBestaetigt)return error('Aufgabe und belegte Fälligkeit ausdrücklich bestätigen.');
    const r=await api('/api/aufgaben/',{method:'POST',body:JSON.stringify({...b.aufgabe,bezugArt:'Dokument',bezugId:e.dokumentId})});if(!r?.ok)return error('Aufgabe nicht gespeichert.');e.aufgabeId=(await r.json()).id;e.version++;return ok({aufgabeId:e.aufgabeId});
   }else return error('Vorschauaktion nicht verfügbar.');
  }
  return ok({eingang:e,historie:histories.get(e.id)??[]});
 }catch{return error('Vorschaueingang konnte nicht verarbeitet werden. Bitte neu laden.');}
}
