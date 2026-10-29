import{useEffect,useState}from'react';
import{apiFetch}from'./auth.js';
import{StarkePruefung,pruefung}from'./MfaRichtlinie.js';
type Aktion={id:string;titel:string;basis:boolean;abweichung:boolean|null;erlaubt:boolean};
type Vorlage={id:string;name:string;version:number;rechte:Record<string,boolean>};
type Stand={version:number;modus:string;vorlageId:string|null;aktionen:Aktion[];vorlagen:Vorlage[]};
export default function Kontaktrechte({benutzerId}:{benutzerId:string}){
 const[daten,D]=useState<Stand|null>(null),[vorlage,V]=useState(''),[abweichungen,A]=useState<Record<string,boolean>>({}),[fehler,F]=useState(''),[meldung,M]=useState(''),[busy,B]=useState(false),[neu,N]=useState(0),[dirty,U]=useState(false);
 const path='/api/admin/benutzer/'+encodeURIComponent(benutzerId)+'/kontaktrechte';
 function uebernehmen(s:Stand){D(s);V(s.vorlageId??'');A(Object.fromEntries(s.aktionen.filter(x=>x.abweichung!==null).map(x=>[x.id,x.abweichung!])));U(false);}
 useEffect(()=>{const c=new AbortController();D(null);F('');M('');void apiFetch(path,{signal:AbortSignal.any([c.signal,AbortSignal.timeout(15000)])}).then(async r=>{if(!r.ok)throw Error('Kontaktberechtigungen konnten nicht geladen werden.');const s=await r.json() as Stand;if(!Array.isArray(s.aktionen)||!Array.isArray(s.vorlagen))throw Error('Kontaktberechtigungen sind nicht verfügbar.');if(!c.signal.aborted)uebernehmen(s);}).catch(e=>{if(!c.signal.aborted)F((e as Error).message);});return()=>c.abort();},[path,neu]);
 const basis=daten?.vorlagen.find(v=>v.id===vorlage)?.rechte??{};
 const erlaubt=(id:string)=>Object.hasOwn(abweichungen,id)?abweichungen[id]===true:basis[id]===true;
 return <section className="p-kontaktrechte" aria-label="Kontaktberechtigungen"><h3>Kontaktberechtigungen</h3>
 <p>Bereichsrechte bestimmen die erlaubten Aktionen. Bestehende Firmen-, Akten- und Dokumentfreigaben bleiben zusätzlich erforderlich.</p>
 {fehler&&<p role="alert">{fehler} {!daten&&<button type="button" onClick={()=>N(x=>x+1)}>Neu laden</button>}</p>}{meldung&&<p role="status">{meldung}</p>}
 {!daten&&!fehler&&<p role="status">Berechtigungen werden geladen …</p>}
 {daten&&<form onSubmit={e=>{e.preventDefault();const form=e.currentTarget,d=new FormData(form);B(true);F('');M('');void apiFetch(path,{method:'POST',headers:{'Content-Type':'application/json','X-Arbeitsaktion':'1'},body:JSON.stringify({version:daten.version,vorlageId:vorlage||null,abweichungen,bestandErsetzen:d.get('bestand')==='on',pruefung:pruefung(d)}),signal:AbortSignal.timeout(15000)}).then(async r=>{if(!r.ok){const v=await r.json().catch(()=>({}));throw Error(v.meldung??'Berechtigungen nicht gespeichert. Bitte aktuellen Stand und Sicherheitsbestätigung prüfen.');}const s=await r.json() as Stand;uebernehmen(s);for(const n of ['passwort','code'])(form.elements.namedItem(n) as HTMLInputElement).value='';(form.elements.namedItem('recovery') as HTMLInputElement).checked=false;M('Kontaktberechtigungen gespeichert. Sie gelten ab der nächsten Anfrage.');}).catch(e=>F((e as Error).message)).finally(()=>B(false));}}><fieldset disabled={busy}>
 <label>Berechtigungsvorlage<select value={vorlage} onChange={e=>{V(e.target.value);A({});U(true);M('');}}><option value="">Keine Vorlage – nur ausdrücklich gesetzte Rechte</option>{daten.vorlagen.map(v=><option key={v.id} value={v.id}>{v.name} · Version {v.version}</option>)}</select></label>
 {daten.modus==='Bestand'&&<div className="p-rechte-bestand"><p>Bisherige Kontaktrechte sind unverändert übernommen. Eine neue Vorlage ersetzt diese erst nach Bestätigung und Speichern.</p><label><input type="checkbox" name="bestand" required/> Bisherige Kontaktrechte bewusst durch diese Auswahl ersetzen</label></div>}
 <div className="p-rechtematrix"><table><caption>Kontakte · {dirty?'Vorschau, noch nicht gespeichert':'gespeicherter Stand'}</caption><thead><tr><th>Aktion</th><th>Erlaubt</th><th>Herkunft</th></tr></thead><tbody>{daten.aktionen.map(a=><tr key={a.id}><th scope="row">{a.titel}</th><td><input type="checkbox" aria-label={'Kontakte: '+a.titel} checked={erlaubt(a.id)} onChange={e=>{A(x=>({...x,[a.id]:e.target.checked}));U(true);M('');}}/></td><td>{Object.hasOwn(abweichungen,a.id)?<>Individuell <button type="button" className="p-button-leise" onClick={()=>{A(x=>{const copy={...x};delete copy[a.id];return copy;});U(true);M('');}}>Vorlage verwenden: {a.titel}</button></>:vorlage?'Vorlage':'Keine Freigabe'}</td></tr>)}</tbody></table></div>
 <p>Erstellen, Bearbeiten und Archivieren benötigen zusätzlich Lesen. Endgültiges Löschen wird nicht unterstützt. Vorlagen vergeben keine Administratorrolle.</p>
 <details><summary>Weitere Bereiche</summary><p>Akten, Dokumente, Aufgaben, Rechnungen und Leistungen folgen später. Diese Matrix verändert dort keine Rechte.</p></details>
 <p>Speichern erfordert Ihren eingerichteten Authenticator sowie eine frische starke Bestätigung.</p><StarkePruefung/>
 <div className="p-benutzer-aktionen"><button disabled={busy}>Kontaktberechtigungen speichern</button><button type="button" className="p-button-sekundaer" onClick={()=>N(x=>x+1)}>Gespeicherten Stand neu laden</button></div>
 </fieldset></form>}
 </section>;
}
