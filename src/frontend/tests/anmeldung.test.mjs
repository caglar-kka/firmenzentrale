import{test}from'node:test';import assert from'node:assert/strict';import{JSDOM}from'jsdom';import{act,createElement as h}from'react';import{createRoot}from'react-dom/client';import{Anmeldung,Abmelden}from'../node_modules/.cache/betriebsplattform-tests/Anmeldung.js';import{apiFetch}from'../node_modules/.cache/betriebsplattform-tests/auth.js';
async function mount(fetcher,check){const dom=new JSDOM('<div id="root"></div>',{url:'https://127.0.0.1:5180/'});const old={fetch:globalThis.fetch,FormData:globalThis.FormData};globalThis.window=dom.window;globalThis.document=dom.window.document;globalThis.FormData=dom.window.FormData;globalThis.IS_REACT_ACT_ENVIRONMENT=true;dom.window.HTMLElement.prototype.attachEvent=()=>{};dom.window.HTMLElement.prototype.detachEvent=()=>{};globalThis.fetch=fetcher;const root=createRoot(document.getElementById('root'));try{await act(async()=>root.render(h(Anmeldung,null,h('div',null,'Geschützter Arbeitsplatz',h(Abmelden)))));await check();}finally{await act(async()=>root.unmount());globalThis.fetch=old.fetch;globalThis.FormData=old.FormData;delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;dom.window.close();}}
const ok=data=>({ok:true,status:200,json:async()=>data});
test('Anmeldung: Demo bleibt explizit ohne Anmeldemaske',async()=>mount(async()=>ok({aktiv:false}),async()=>assert.match(document.body.textContent,/Geschützter Arbeitsplatz/)));
test('Anmeldung: geschützter Bereich erst nach Login, CSRF im Header, Logout entfernt Inhalt',async()=>{let signed=false;await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:true,angemeldet:signed,csrf:signed?'csrf-auth':'csrf-anonym'});assert.equal(options.headers.get('X-CSRF-TOKEN'),signed?'csrf-auth':'csrf-anonym');assert.equal(options.credentials,'same-origin');if(path==='/api/auth/login')signed=true;if(path==='/api/auth/logout')signed=false;return ok({});},async()=>{assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);assert.equal(document.querySelectorAll('[required][aria-required="true"]').length,2);document.querySelector('[name=login]').value='muster';document.querySelector('[name=passwort]').value='a'.repeat(30);await act(async()=>document.querySelector('form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.body.textContent,/Geschützter Arbeitsplatz/);await act(async()=>apiFetch('/api/akten/a/bearbeitungsstatus',{method:'POST',body:'{}'}));assert.equal(window.localStorage.length,0);await assert.rejects(()=>apiFetch('https://example.invalid/api/x',{method:'POST'}),/lokale/);await act(async()=>document.querySelector('button').click());assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);});});
test('Anmeldung: Verbindungsfehler öffnet niemals den Arbeitsplatz',async()=>mount(async()=>({ok:false,status:503}),async()=>{assert.match(document.body.textContent,/nicht erreichbar/);assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);}));


test('Mehrbenutzer: sichtbare Liste aktualisiert sich, Entwurf bleibt, Offline und Rueckkehr sind kontrolliert',async()=>{
 const {useDatenbankliste}=await import('../node_modules/.cache/betriebsplattform-tests/Buero.js');
 const {useState}=await import('react');let ticks=[];let names=['Firma A'];let requests=0;let denied=false;
 await mount(async(path)=>{if(path==='/api/auth/session')return ok({aktiv:false});requests++;if(denied)return {ok:false,status:403,json:async()=>({meldung:'Keine Berechtigung.'})};return ok({eintraege:names.map((firma,i)=>({id:String(i),firma,werte:[],historie:[]})),anzahl:names.length});},async()=>{
  function Liste(){const v=useDatenbankliste(true,'kontakte?seite=0');const[d,D]=useState('Eigener Entwurf');return h('section',null,h('input',{value:d,onChange:e=>D(e.target.value)}),h('output',null,v.daten?.eintraege.map(k=>k.firma).join(', ')),v.fehler&&h('p',{role:'alert'},v.fehler));}
  const originalSet=window.setInterval,originalClear=window.clearInterval;window.setInterval=f=>{ticks.push(f);return ticks.length;};window.clearInterval=()=>{};
  const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
  try{await act(async()=>root.render(h(Liste)));await act(async()=>new Promise(r=>setTimeout(r,240)));assert.match(el.textContent,/Firma A/);
   names.push('Firma B');await act(async()=>ticks[0]());await act(async()=>new Promise(r=>setTimeout(r,240)));assert.match(el.textContent,/Firma B/);assert.equal(el.querySelector('input').value,'Eigener Entwurf');
   Object.defineProperty(window.navigator,'onLine',{configurable:true,value:false});const before=requests;await act(async()=>ticks[0]());assert.equal(requests,before);
   Object.defineProperty(window.navigator,'onLine',{configurable:true,value:true});names.push('Firma C');await act(async()=>window.dispatchEvent(new window.Event('online')));await act(async()=>new Promise(r=>setTimeout(r,240)));assert.match(el.textContent,/Firma C/);
   denied=true;await act(async()=>window.dispatchEvent(new window.Event('focus')));await act(async()=>new Promise(r=>setTimeout(r,240)));assert.doesNotMatch(el.querySelector('output').textContent,/Firma/);assert.match(el.textContent,/Keine Berechtigung/);assert.equal(el.querySelector('input').value,'Eigener Entwurf');
  }finally{await act(async()=>root.unmount());window.setInterval=originalSet;window.clearInterval=originalClear;el.remove();}
 });
});

test('Benutzerverwaltung: Administrator legt Mitarbeiter ueber das Formular mit Startpasswort an',async()=>{
 const {default:Verwaltung}=await import('../node_modules/.cache/betriebsplattform-tests/Benutzerverwaltung.js');let angelegt=null;
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:false});if(options.method==='POST'){angelegt=JSON.parse(options.body);return ok({});}return ok([]);},async()=>{
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);try{await act(async()=>root.render(h(Verwaltung)));const form=el.querySelector('form');form.elements.login.value='musterteam';form.elements.anzeigename.value='Musterperson';form.elements.passwort.value='Nur-im-Test-!23456789';await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(angelegt.rolle,'Mitarbeiter');assert.equal(angelegt.login,'musterteam');assert.equal(form.elements.passwort.value,'');assert.doesNotMatch(el.textContent,/Nur-im-Test/);}finally{await act(async()=>root.unmount());el.remove();}
 });
});


test('Mehrbenutzer: neue fremde Notiz sichtbar waehrend eigener Notizentwurf offen bleibt',async()=>{
 const {default:Aktenbereich}=await import('../node_modules/.cache/betriebsplattform-tests/Aktenbereich.js');
 const {Arbeitskontext}=await import('../node_modules/.cache/betriebsplattform-tests/Arbeitskontext.js');let ticks=[],notizen=[];
 await mount(async(path)=>{if(path==='/api/auth/session')return ok({aktiv:false});if(path==='/api/arbeitsumgebung')return ok({fluechtigeDaten:false,beispielkennzeichnung:true,benutzer:'Muster',kontakteVerwalten:true,kategorienVerwalten:true});if(path==='/api/akten/a')return ok({vorgang:{id:'a',titel:'Beispielakte',kategorie:'Vertrag',firmaId:null,rechnungsnummer:null},dokumente:[],notizen,korrespondenz:[],kontaktvorschlaege:[]});return ok([]);},async()=>{
 const old=window.setInterval;window.setInterval=f=>{ticks.push(f);return ticks.length;};const clear=window.clearInterval;window.clearInterval=()=>{};
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
 try{await act(async()=>root.render(h(Arbeitskontext,null,h(Aktenbereich,{id:'a'}))));await act(async()=>[...el.querySelectorAll('[role=tab]')].find(b=>b.textContent.startsWith('Notizen')).click());await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Notiz erfassen').click());el.querySelector('[name=betreff]').value='Eigener ungespeicherter Betreff';el.querySelector('[name=inhalt]').value='Eigener Entwurf';
 notizen=[{id:'n',betreff:'Rueckruf von Benutzer B',inhalt:'Neue Information',autor:'Muster B',erstelltAm:'2026-10-19T10:00:00Z',geaendertAm:'2026-10-19T10:00:00Z',version:1,entfernt:false,historie:[]}];await act(async()=>{for(const tick of ticks)await tick();});
 assert.match(el.textContent,/Rueckruf von Benutzer B/);assert.equal(el.querySelector('[name=betreff]').value,'Eigener ungespeicherter Betreff');assert.equal(el.querySelector('[name=inhalt]').value,'Eigener Entwurf');assert.ok(el.querySelector('dialog[open]'));
 }finally{await act(async()=>root.unmount());window.setInterval=old;window.clearInterval=clear;el.remove();}
 });
});
