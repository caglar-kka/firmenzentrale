import{useEffect,useRef,useState,useId,type ReactNode,type FormEvent,type FormHTMLAttributes}from'react';
import{anfrage,type Kontakt}from'./akte-api.js';
import{useUmgebung,useAktualisierung}from'./Arbeitskontext.js';
export function useDatenbankliste<T>(aktiv:boolean,pfad:string){
 const[daten,D]=useState<T|null>(null),[fehler,F]=useState(''),[stand,S]=useState(0),abgleich=useRef<()=>Promise<void>>(async()=>{});
 useEffect(()=>{if(!aktiv)return;const c=new AbortController();let request:Promise<void>|undefined;
 const laden=()=>{clearTimeout(timer);if(request)return request;F('');request=anfrage<T>(pfad,undefined,c.signal).then(v=>{if(!c.signal.aborted)D(v);}).catch(e=>{if(!c.signal.aborted){D(null);F(e.message);}}).finally(()=>{request=undefined;});return request;};
 const timer=setTimeout(()=>void laden(),200);abgleich.current=laden;
 return()=>{clearTimeout(timer);c.abort();abgleich.current=async()=>{};};},[aktiv,pfad,stand]);
 useAktualisierung(()=>abgleich.current(),aktiv);return{daten,fehler,neuLaden:()=>S(v=>v+1)};
}

export function Dialog({titel,schliessen,children,busy=false}:{titel:string;schliessen:()=>void;children:ReactNode;busy?:boolean}){const d=useRef<HTMLDialogElement>(null),vorher=useRef<HTMLElement|null>(null);useEffect(()=>{vorher.current=document.activeElement as HTMLElement;if(d.current?.showModal)d.current.showModal();else d.current?.setAttribute('open','');(d.current?.querySelector<HTMLInputElement>('input:not([disabled]),textarea')??d.current?.querySelector<HTMLButtonElement>('button'))?.focus();return()=>{const e=vorher.current;queueMicrotask(()=>{if(e?.isConnected)e.focus();});};},[]);return<dialog ref={d} className="p-dialog" aria-label={titel} onCancel={e=>{e.preventDefault();if(!busy)schliessen();}}><div className="p-vorschaukopf"><h2>{titel}</h2><button type="button" disabled={busy} onClick={schliessen}>Schließen</button></div>{children}</dialog>;}
export function Seiten({seite,anzahl,waehlen}:{seite:number;anzahl:number;waehlen:(n:number)=>void}){const letzte=Math.max(0,Math.ceil(anzahl/20)-1);return anzahl>20?<nav className="p-listenseiten" aria-label="Ergebnisseiten"><button type="button" disabled={!seite} onClick={()=>waehlen(seite-1)}>Vorherige</button><span>Seite {seite+1} von {letzte+1}</span><button type="button" disabled={seite>=letzte} onClick={()=>waehlen(seite+1)}>Nächste</button></nav>:null;}

// Shared contact selector: persistent mode queries a bounded page instead of loading all contacts.
export function Kontaktfeld({name,titel,werte,wert='',nurFirmen=false,pflicht=false}:{name:string;titel:string;werte:Kontakt[];wert?:string;nurFirmen?:boolean;pflicht?:boolean}){
 const pg=useUmgebung().fluechtigeDaten===false,[text,T]=useState(''),[seite,P]=useState(0),[wahl,W]=useState(wert),[aktuell,A]=useState<Kontakt|null>(null),[lokal,L]=useState<Kontakt[]>([]);
 const remote=useDatenbankliste<{eintraege:Kontakt[];anzahl:number}>(pg,'kontakte?'+new URLSearchParams({seite:String(seite),text,art:nurFirmen?'Firma':''}));
 useEffect(()=>{if(pg||werte.length)return;const c=new AbortController();void anfrage<Kontakt[]>('kontakte',undefined,c.signal).then(L).catch(()=>{});return()=>c.abort();},[pg,werte.length]);
 useEffect(()=>{if(!pg||!wahl)return;const c=new AbortController();void anfrage<Kontakt>('kontakte/'+encodeURIComponent(wahl),undefined,c.signal).then(v=>{if(!c.signal.aborted)A(v);}).catch(()=>{});return()=>c.abort();},[pg,wahl]);
 const alle=(werte.length?werte:lokal).filter(k=>!k.archiviert&&(!nurFirmen||k.art!=='Person'));
 const gefiltert=alle.filter(k=>k.firma.toLocaleLowerCase().includes(text.toLocaleLowerCase())).sort((a,b)=>a.firma.localeCompare(b.firma,'de'));
 const liste=pg?remote.daten?.eintraege??[]:gefiltert.slice(seite*20,seite*20+20),ausgewaehlt=pg?aktuell:alle.find(k=>k.id===wahl);
 return<div className="p-kontaktauswahl"><label>{titel} suchen<input type="search" value={text} maxLength={120} onChange={e=>{T(e.target.value);P(0);}}/></label><label>{titel}{pflicht?' *':''}<select name={name} required={pflicht} aria-required={pflicht||undefined} value={wahl} onChange={e=>W(e.target.value)}><option value="">{titel} wählen</option>{ausgewaehlt&&!liste.some(k=>k.id===ausgewaehlt.id)&&<option value={ausgewaehlt.id}>{ausgewaehlt.firma}</option>}{liste.map(k=><option key={k.id} value={k.id}>{k.firma}</option>)}</select></label><Seiten seite={seite} anzahl={pg?remote.daten?.anzahl??0:gefiltert.length} waehlen={P}/>{remote.fehler&&<p role="alert">{remote.fehler}</p>}</div>;
}
export function Pflichtinfo(){return<p className="p-pflichtinfo">* Pflichtfeld</p>;}
export function Dublettenvergleich({hinweise,neu,bereich,oeffnen}:{oeffnen?:()=>void;hinweise:import('./akte-api.js').Dublette[];neu:string;bereich:'kontakte'|'vorgaenge'}){return hinweise.length>0?<section className="p-hinweise" aria-label="Möglicher vorhandener Eintrag"><h3>Möglicher vorhandener Eintrag gefunden</h3><p>Neuer Eintrag: <strong>{neu}</strong></p>{hinweise.map(h=><article key={h.id}><h4>{h.name} · {h.art}</h4><p>{h.anschrift}</p><ul>{h.angaben.map((a,i)=><li key={i}>{a}</li>)}</ul><p>{h.gruende.join(' · ')}</p><a onClick={oeffnen} href={'#'+bereich+'?id='+encodeURIComponent(h.id)}>Bestehenden Eintrag öffnen</a></article>)}<p>Ähnlichkeit bedeutet nicht, dass es derselbe Eintrag ist. Es wird nichts automatisch zusammengeführt.</p></section>:null;}

export function Kategoriefilter({kategorien,wert,waehlen}:{kategorien:import('./akte-api.js').Kategorie[];wert:string;waehlen:(id:string)=>void}){
 const[q,Q]=useState('');const d=useRef<HTMLDetailsElement>(null);
 function auswahl(id:string){waehlen(id);Q('');if(d.current){d.current.open=false;d.current.querySelector('summary')?.focus();}}
 const wahl=kategorien.find(k=>k.id===wert);
 return<div className="p-kategoriefilter"><span>Kategorie</span><details ref={d}><summary>{wahl?.name??'Alle Kategorien'}</summary><label>Kategorien suchen<input type="search" value={q} onChange={e=>Q(e.target.value)}/></label><div className="p-kategorieoptionen" role="group" aria-label="Kategorie auswählen"><button type="button" aria-pressed={!wert} onClick={()=>auswahl('')}>Alle Kategorien</button>{kategorien.filter(k=>!k.archiviert&&(!q||[k.name,kategorien.find(e=>e.id===k.elternId)?.name].join(' ').toLocaleLowerCase().includes(q.toLocaleLowerCase()))).map(k=><button type="button" className={k.elternId?'p-kategorie-kind':undefined} aria-pressed={wert===k.id} key={k.id} onClick={()=>auswahl(k.id)}>{k.name}{k.elternId&&<small>{kategorien.find(e=>e.id===k.elternId)?.name}</small>}</button>)}</div></details></div>;
}

export function Sortierkopf({feld,aktiv,ab,sortieren,children}:{feld:string;aktiv:string;ab:boolean;sortieren:(feld:string)=>void;children:ReactNode}){return<th scope="col" aria-sort={aktiv===feld?(ab?'descending':'ascending'):'none'}><button className="p-sortierkopf" onClick={()=>sortieren(feld)} title={aktiv===feld?(ab?'Aufsteigend sortieren':'Absteigend sortieren'):'Aufsteigend sortieren'}>{children}<span aria-hidden="true">{aktiv===feld?(ab?' ↓':' ↑'):' ↕'}</span></button></th>;}

// Gemeinsamer Zielablauf für erste Ablage und spätere Änderung.
export function Zuordnungsziel({neu,N,kategorien,kontakte,akten,kategorieId,firmaId,vorschlag,ausgeschlossen=[]}:{neu:boolean;N:(v:boolean)=>void;kategorien:import('./akte-api.js').Kategorie[];kontakte:import('./akte-api.js').Kontakt[];akten:import('./akte-api.js').Vorgang[];kategorieId?:string;firmaId?:string;vorschlag?:string|null;ausgeschlossen?:string[]}){
 const pg=useUmgebung().fluechtigeDaten===false,[text,T]=useState(''),[seite,P]=useState(0);
 const remote=useDatenbankliste<{eintraege:import('./akte-api.js').Vorgang[];anzahl:number}>(pg&&!neu,'akten?'+new URLSearchParams({seite:String(seite),text,sort:'titel'}));
 const auswahl=pg?remote.daten?.eintraege??[]:akten;
 return<div className="p-zuordnungsziel"><label>Kategorie *<select name="kategorie" required aria-required="true" defaultValue={kategorieId??'ungeklaert'}>{kategorien.filter(k=>!k.archiviert).map(k=><option key={k.id} value={k.id}>{k.name}</option>)}</select></label><label><input type="checkbox" checked={neu} onChange={e=>N(e.target.checked)}/> Neue Akte anlegen</label>{neu?<><label>Aktenbetreff *<input name="titel" required aria-required="true" maxLength={160}/></label><Kontaktfeld name="firma" titel="Firma" werte={kontakte} wert={firmaId} nurFirmen pflicht/></>:<>{pg&&<label>Akten suchen<input type="search" maxLength={120} value={text} onChange={e=>{T(e.target.value);P(0);}}/></label>}<label>Bestehende Akte *<select name="akte" required aria-required="true" defaultValue={vorschlag??''}><option value="">Akte wählen</option>{auswahl.filter(a=>!ausgeschlossen.includes(a.id)&&a.status!=='Archiviert').map(a=><option key={a.id} value={a.id}>{a.titel} · {a.kategorie}</option>)}</select></label>{pg&&<Seiten seite={seite} anzahl={remote.daten?.anzahl??0} waehlen={P}/ >}{remote.fehler&&<p role="alert">{remote.fehler}</p>}</>}</div>;
}

// Only already-authorized, rendered content is selected. Browser/OS chooses the printer.
export function druckeBereich(ziel:HTMLElement|null){
 if(!ziel)return;const pfad:HTMLElement[]=[];let p=ziel.parentElement;
 while(p){pfad.push(p);p=p.parentElement;}
 ziel.classList.add('p-druckziel');pfad.forEach(e=>e.classList.add('p-druckpfad'));
 try{window.print();}finally{ziel.classList.remove('p-druckziel');pfad.forEach(e=>e.classList.remove('p-druckpfad'));}
}
export function Druckhilfe(){return<details className="p-druckhilfe"><summary>Drucker einrichten und drucken</summary><p>Eine Druckaktion öffnet den normalen Browser-Druckdialog. Wählen Sie dort Ihren eingerichteten USB-, WLAN- oder Netzwerkdrucker oder eine PDF-Ausgabe aus. MBOP startet keinen stillen Druckauftrag.</p><p>Fehlt der Drucker, prüfen Sie auf diesem Arbeitsplatz Windows „Einstellungen → Bluetooth und Geräte → Drucker und Scanner“. Verwenden Sie nur Windows oder offizielle Herstellertreiber; MBOP installiert keine Treiber.</p><a href="https://support.microsoft.com/en-US/Windows/hardware/printer/fix-printer-not-found-and-printer-not-recognized-errors-in-windows" target="_blank" rel="noopener noreferrer">Offizielle Windows-Druckerhilfe</a><p>Original-PDFs öffnen Sie über „Original zum Drucken öffnen“ und drucken in der PDF-Anzeige. Eine MBOP-Druckansicht ist eine Übersicht; sie verändert keinen Originalbeleg.</p></details>;}


/** Shared accessible validation. Server validation remains authoritative. */
export function Formular({children,onSubmit,pruefung,...props}:Omit<FormHTMLAttributes<HTMLFormElement>,'onSubmit'>&{children:ReactNode;onSubmit:(e:FormEvent<HTMLFormElement>)=>void;pruefung?:(form:HTMLFormElement)=>Record<string,string|undefined>}){
 const id=useId(),[fehler,F]=useState<{id:string;text:string}[]>([]),formular=useRef<HTMLFormElement>(null);
 function leeren(){formular.current?.querySelectorAll('[data-formfehler]').forEach(el=>{el.removeAttribute('aria-invalid');const alt=el.getAttribute('data-formbeschreibung');if(alt)el.setAttribute('aria-describedby',alt);else el.removeAttribute('aria-describedby');el.removeAttribute('data-formbeschreibung');el.removeAttribute('data-formfehler');});F([]);}
 return <form {...props} noValidate ref={formular} onChangeCapture={leeren} onSubmit={e=>{
  e.preventDefault();leeren();const form=e.currentTarget,extra=pruefung?.(form)??{},errors:{id:string;text:string}[]=[];let erster:HTMLElement|undefined;
  form.querySelectorAll<HTMLInputElement|HTMLSelectElement|HTMLTextAreaElement>('input,select,textarea').forEach((feld,i)=>{
   if(!feld.willValidate)return;const label=feld.labels?.[0]?.textContent?.replace(/\s*\*.*/,'').trim()||'dieses Feld';
   let text=extra[feld.name]??'';
   if(!text&&feld.validity.valueMissing)text='Bitte '+(feld.tagName==='SELECT'?'wählen Sie ':'geben Sie ')+label+(feld.tagName==='SELECT'?' aus.':' ein.');
   else if(!text&&feld.validity.typeMismatch)text=feld.type==='email'?'Bitte geben Sie eine gültige E-Mail-Adresse ein.':'Bitte geben Sie eine gültige Internetadresse ein.';
   else if(!text&&feld.validity.tooLong)text='Die Eingabe ist zu lang.';
   else if(!text&&!feld.validity.valid)text='Bitte prüfen Sie '+label+'.';
   if(!text&&feld.type==='tel'&&feld.value.trim()&&!/^\+?[0-9 ()/.-]+$/.test(feld.value.trim()))text='Die Telefonnummer enthält unzulässige Zeichen.';
   if(!text&&feld.type==='tel'&&feld.value.trim()&&((feld.value.match(/[0-9]/g)?.length??0)<3||(feld.value.match(/[0-9]/g)?.length??0)>32))text='Bitte geben Sie eine gültige Telefonnummer ein.';
   if(text){const key=id+'-fehler-'+i;feld.setAttribute('aria-invalid','true');const alt=feld.getAttribute('aria-describedby')??'';feld.setAttribute('data-formbeschreibung',alt);feld.setAttribute('aria-describedby',[alt,key].filter(Boolean).join(' '));feld.setAttribute('data-formfehler','');errors.push({id:key,text});erster??=feld;}
  });
  if(errors.length){F(errors);erster?.focus();return;}onSubmit(e);
 }}>{fehler.length>0&&<div className="p-formularfehler" role="alert"><strong>Bitte prüfen Sie Ihre Angaben.</strong><ul>{fehler.map(f=><li id={f.id} key={f.id}>{f.text}</li>)}</ul></div>}{children}</form>;
}
export function adresspruefung(form:HTMLFormElement):Record<string,string>{
 const d=new FormData(form),land=String(d.get('land')??'').trim().toUpperCase(),plz=String(d.get('plz')??'').trim();
 const ok=!plz||(['DE','DEUTSCHLAND','GERMANY'].includes(land)?/^[0-9]{5}$/.test(plz):['AT','ÖSTERREICH','AUSTRIA','CH','SCHWEIZ','SWITZERLAND'].includes(land)?/^[0-9]{4}$/.test(plz):/^[A-Za-z0-9][A-Za-z0-9 -]{0,19}$/.test(plz));
 return ok?{}:{plz:'Bitte geben Sie eine gültige Postleitzahl für das ausgewählte Land ein.'};
}
