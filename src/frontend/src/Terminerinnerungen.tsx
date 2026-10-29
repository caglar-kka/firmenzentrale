import{useEffect,useRef,useState}from'react';
import{Dialog,Seiten}from'./Buero.js';
import{anfrage}from'./akte-api.js';
import{useUmgebung}from'./Arbeitskontext.js';
type Hinweis={id:string;titel:string;beginn:string;erinnerungskennung:string;adresse?:string;ort?:string};
type Liste={anzahl:number;eintraege:Hinweis[]};
const datum=(s:string)=>/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(s)?s.slice(8,10)+'.'+s.slice(5,7)+'.'+s.slice(0,4)+' · '+s.slice(11)+' Uhr (Europe/Berlin)':s;
export default function Terminerinnerungen(){
 const u=useUmgebung(),[offen,O]=useState(false),[daten,D]=useState<Liste>({anzahl:0,eintraege:[]}),[seite,P]=useState(0),[fehler,F]=useState(''),[busy,B]=useState(false),[automatisch,A]=useState(false);
 const seitenstand=useRef(seite);seitenstand.current=seite;
 async function laden(s=seitenstand.current,gueltig=()=>true){try{const v=await anfrage<Liste>('termine/erinnerungen?seite='+s);if(gueltig()){D(v);F('');}}catch{if(gueltig()){D({anzahl:0,eintraege:[]});F('Erinnerungen nicht erreichbar. Bitte Anmeldung und Verbindung prüfen.');}}}
 useEffect(()=>{
  if(!u.authentifiziert)return;let aktiv=true,freigeben:(()=>void)|undefined;D({anzahl:0,eintraege:[]});
  // All tabs refresh their read-only list; only one tab shows the automatic callout.
  void laden(0,()=>aktiv);const poll=setInterval(()=>void laden(seitenstand.current,()=>aktiv),15000);
  const locks=window.navigator.locks;
  const versuchen=()=>{if(!locks){A(true);return;}void locks.request('mbop-persoenliche-termine',{ifAvailable:true},async lock=>{if(!lock||!aktiv)return;A(true);try{await new Promise<void>(r=>{freigeben=r;if(!aktiv)r();});}finally{if(aktiv)A(false);}}).catch(()=>{if(aktiv)A(true);});};
  versuchen();const retry=setInterval(versuchen,30000);return()=>{aktiv=false;clearInterval(poll);clearInterval(retry);freigeben?.();};
 },[u.authentifiziert,u.benutzer]);
 useEffect(()=>{if(offen)void laden();},[offen,seite]);
 if(!u.authentifiziert)return null;
 function oeffnen(t:Hinweis){window.location.hash='#kalender?termin='+encodeURIComponent(t.id);O(false);}
 async function quittieren(t:Hinweis){if(busy)return;B(true);try{await anfrage('termine/'+encodeURIComponent(t.id)+'/quittieren',{kennung:t.erinnerungskennung});await laden();}catch(e){F((e as Error).message);}finally{B(false);}}
 const erster=daten.eintraege[0];
 function eintrag(t:Hinweis){return <><strong>{t.titel}</strong><time dateTime={t.beginn}>{datum(t.beginn)}</time>{(t.adresse||t.ort)&&<span>{t.adresse||t.ort}</span>}<div className="p-aktionen"><button onClick={()=>oeffnen(t)}>Termin öffnen</button><button disabled={busy} onClick={()=>void quittieren(t)}>Als gelesen markieren</button></div></>;}
 return <div className="p-erinnerungen"><button className="p-button-leise" onClick={()=>{P(0);O(true);}} aria-label="Persönliche Terminerinnerungen öffnen"><span role="status" aria-live="polite">{daten.anzahl?daten.anzahl+' Terminerinnerung'+(daten.anzahl===1?'':'en')+' fällig':'Erinnerungen'}</span></button>
 {automatisch&&!offen&&erster&&<aside className="p-erinnerung-hinweis" aria-label="Fällige Terminerinnerung"><p role="status">Termin fällig{daten.anzahl>1?' · '+daten.anzahl+' offene Hinweise':''}</p>{eintrag(erster)}{daten.anzahl>1&&<button className="p-button-leise" onClick={()=>{P(0);O(true);}}>Alle Erinnerungen ansehen</button>}{fehler&&<p role="alert">{fehler}</p>}</aside>}
 {offen&&<Dialog titel="Persönliche Terminerinnerungen" busy={busy} schliessen={()=>O(false)}><p>Verpasste Hinweise bleiben ohne Verfallsfrist bis zur Quittierung verfügbar. Nur bei geöffneter angemeldeter Anwendung; kein E-Mail- oder System-Push. Andere geöffnete Tabs gleichen innerhalb von 15 Sekunden ab.</p>{fehler&&<p role="alert">{fehler}</p>}<ul className="p-quellenliste">{daten.eintraege.map(t=><li key={t.id}>{eintrag(t)}</li>)}</ul>{!daten.anzahl&&!fehler&&<p>Keine fälligen Erinnerungen.</p>}<Seiten seite={seite} anzahl={daten.anzahl} waehlen={P}/></Dialog>}</div>;
}
