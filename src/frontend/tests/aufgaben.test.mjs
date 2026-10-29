import{test}from'node:test';import assert from'node:assert/strict';import{JSDOM}from'jsdom';import{act,createElement as h}from'react';import{createRoot}from'react-dom/client';import{Anwendung}from'../node_modules/.cache/betriebsplattform-tests/App.js';
const ok=data=>({ok:true,status:200,json:async()=>data}),button=text=>Array.from(document.querySelectorAll('button')).find(b=>b.textContent===text);
test('Echte Root-Anwendung: Aufgabenroute, Anlegen, Wiedervorlage, Konfliktentwurf und Statuswechsel',async()=>{
 const dom=new JSDOM('<div id="root"></div>',{url:'https://127.0.0.1:5180/#aufgaben'});const old={fetch:globalThis.fetch,FormData:globalThis.FormData};Object.assign(globalThis,{window:dom.window,document:dom.window.document,FormData:dom.window.FormData,IS_REACT_ACT_ENVIRONMENT:true});window.scrollTo=()=>{};window.HTMLElement.prototype.attachEvent=()=>{};window.HTMLElement.prototype.detachEvent=()=>{};let task=null,conflict=false;const calls=[];
 globalThis.fetch=async(path,options={})=>{calls.push(path);if(path==='/api/auth/session')return ok({aktiv:true,angemeldet:true,csrf:'synthetisch'});if(path==='/api/arbeitsumgebung')return ok({authentifiziert:true,administrator:false,benutzer:'Testperson',fluechtigeDaten:false,beispielkennzeichnung:false});if(path.startsWith('/api/aufgaben/mitarbeiter')||path==='/api/aufgaben/zustaendige')return ok([{id:'team',anzeigename:'Testperson'}]);if(path.startsWith('/api/kontakte?'))return ok({eintraege:[],anzahl:0});if(path.startsWith('/api/aufgaben/?'))return ok({eintraege:task?[task]:[],anzahl:task?1:0,heute:'2026-10-19',zeitzone:'Europe/Berlin'});if(options.method==='POST'&&path.startsWith('/api/aufgaben/')){if(conflict){task.version++;conflict=false;return{ok:false,status:409,json:async()=>({text:'Die Aufgabe wurde inzwischen geändert.'})};}task={...JSON.parse(options.body),id:'task-1',version:(task?.version??0)+1};return ok(task);}if(path.startsWith('/api/aufgaben/task-1'))return ok({aufgabe:task,historie:[],historieAnzahl:103,ueberfaellig:false,zeitzone:'Europe/Berlin'});return ok({});};
 const root=createRoot(document.getElementById('root'));try{
 await act(async()=>{root.render(h(Anwendung));await new Promise(r=>setTimeout(r,250));});await act(async()=>new Promise(r=>setTimeout(r,250)));assert.ok(document.querySelector('a[href="#aufgaben"]'));assert.match(document.body.textContent,/Keine Aufgaben/);assert.equal(document.querySelector('.p-aufgabenfilter').open,false);await act(async()=>button('Heute fällig').click());assert.equal(button('Heute fällig').getAttribute('aria-pressed'),'true');await act(async()=>button('Meine Aufgaben').click());
 await act(async()=>button('Neue Aufgabe').click());assert.ok(document.querySelector('dialog'));assert.equal(document.querySelector('.p-aufgabenbezug').open,false);assert.equal(document.querySelector('.p-personensuche').open,false);assert.ok(document.querySelector('dialog button[type=button]'));assert.deepEqual([...document.querySelectorAll('dialog h3')].map(e=>e.textContent),['Grunddaten','Zuständigkeit','Planung']);document.querySelector('[name=betreff]').value='Rückruf planen';document.querySelector('[name=beschreibung]').value='Synthetischer Rückruf';document.querySelector('[name=zustaendig]').value='team';document.querySelector('[name=datum]').value='2026-10-23';
 await act(async()=>document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(task.faelligAm,'2026-10-23');assert.equal(task.bezeichnung,undefined);assert.match(document.querySelector('main').textContent,/Aufgabe gespeichert/);assert.equal(document.querySelector('dialog'),null);assert.ok(button('Aufgabe bearbeiten / Status ändern').closest('.p-aufgaben-detailaktionen'));
 await act(async()=>button('Aufgabe bearbeiten / Status ändern').click());document.querySelector('[name=beschreibung]').value='Erhaltener Entwurf';document.querySelector('[name=status]').value='Erledigt';
 await act(async()=>window.dispatchEvent(new window.Event('focus')));assert.equal(document.querySelector('[name=beschreibung]').value,'Erhaltener Entwurf');
 conflict=true;await act(async()=>document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(document.querySelector('[name=beschreibung]').value,'Erhaltener Entwurf');assert.match(document.querySelector('dialog').textContent,/inzwischen geändert/);
 await act(async()=>button('Aktuellen Stand vergleichen').click());await act(async()=>button('Diesen Stand als Grundlage übernehmen').click());await act(async()=>document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(task.status,'Erledigt');assert.equal(task.beschreibung,'Erhaltener Entwurf');assert.ok(calls.includes('/api/aufgaben/task-1'));await act(async()=>button('Nächste').click());assert.ok(calls.includes('/api/aufgaben/task-1?verlaufSeite=1'));assert.equal(window.localStorage.length,0);
 }finally{await act(async()=>root.unmount());Object.assign(globalThis,old);delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;dom.window.close();}
});


test('Aufgabe aus Rechnung, Akte und Kontakt abbrechen erhält Ursprung und Fokus',async()=>{
 const {AufgabeAktion}=await import('../node_modules/.cache/betriebsplattform-tests/Aufgaben.js');
 const {Arbeitskontext,Navigationsanbieter}=await import('../node_modules/.cache/betriebsplattform-tests/Arbeitskontext.js');
 for(const art of ['Rechnung','Akte','Kontakt']){
  const dom=new JSDOM('<div id="root"></div>',{url:'https://localhost/#kontakte?id=ursprung'}),old=globalThis.fetch;
  Object.assign(globalThis,{window:dom.window,document:dom.window.document,IS_REACT_ACT_ENVIRONMENT:true});
  window.HTMLElement.prototype.attachEvent=()=>{};window.HTMLElement.prototype.detachEvent=()=>{};
  const navigation=[];globalThis.fetch=async path=>ok(path.includes('arbeitsumgebung')?{authentifiziert:true,fluechtigeDaten:false,beispielkennzeichnung:false}:[]);
  const root=createRoot(document.getElementById('root'));
  try{
   await act(async()=>{root.render(h(Arbeitskontext,null,h(Navigationsanbieter,{value:(...args)=>navigation.push(args)},h('section',null,h('input',{defaultValue:'Erhaltener Filter'}),h(AufgabeAktion,{art,id:'ursprung'})))));});
   const start=button('Aufgabe erstellen');start.focus();
   await act(async()=>start.click());assert.ok(document.querySelector('dialog'),art+' öffnet über dem Ursprung');assert.equal(navigation.length,0);
   await act(async()=>button('Abbrechen').click());assert.equal(document.querySelector('dialog'),null);assert.equal(document.activeElement,start);assert.equal(document.querySelector('input').value,'Erhaltener Filter');assert.equal(window.location.hash,'#kontakte?id=ursprung');
  }finally{await act(async()=>root.unmount());globalThis.fetch=old;dom.window.close();delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;}
 }
});


test('Firmenauswahl lädt Vorschaukontakte und begrenzt 55 Einträge ohne Auswahlverlust',async()=>{
 const {Kontaktfeld}=await import('../node_modules/.cache/betriebsplattform-tests/Buero.js');const {Arbeitskontext}=await import('../node_modules/.cache/betriebsplattform-tests/Arbeitskontext.js');
 const dom=new JSDOM('<div id="root"></div>',{url:'https://localhost/'}),old=globalThis.fetch;
 Object.assign(globalThis,{window:dom.window,document:dom.window.document,IS_REACT_ACT_ENVIRONMENT:true});window.HTMLElement.prototype.attachEvent=()=>{};window.HTMLElement.prototype.detachEvent=()=>{};
 globalThis.fetch=async path=>ok(path.includes('arbeitsumgebung')?{vorschau:true,fluechtigeDaten:true,beispielkennzeichnung:true}:Array.from({length:55},(_,n)=>({id:'firma-'+n,art:'Firma',version:1,werte:[],historie:[],firma:'Nordufer Service '+String(n).padStart(2,'0')})));
 let uebernahmen=0;const root=createRoot(document.getElementById('root'));
 try{await act(async()=>root.render(h(Arbeitskontext,null,h('form',{onSubmit:e=>{e.preventDefault();uebernahmen++;}},h(Kontaktfeld,{name:'firma',titel:'Firma',werte:[],wert:'firma-54',nurFirmen:true})))));assert.equal(document.querySelector('select').value,'firma-54');assert.equal(document.querySelectorAll('option').length,22);await act(async()=>button('Nächste').click());assert.equal(document.querySelector('select').value,'firma-54');assert.equal(document.querySelectorAll('option').length,22);await act(async()=>button('Nächste').click());assert.equal(document.querySelectorAll('option').length,16);assert.equal(document.querySelector('select').value,'firma-54');assert.equal(uebernahmen,0,'Blättern ist keine Speicherentscheidung');}
 finally{await act(async()=>root.unmount());globalThis.fetch=old;dom.window.close();delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;}
});


test('Gezielter Druck wählt nur vorhandenen Inhalt und räumt Druckzustand auch bei Fehler auf',async()=>{
 const {druckeBereich}=await import('../node_modules/.cache/betriebsplattform-tests/Buero.js');const dom=new JSDOM('<main><article><h3>Brief Oktober</h3><p>Erhaltener Inhalt</p></article><aside>Andere Akte</aside></main>');Object.assign(globalThis,{window:dom.window,document:dom.window.document});const target=document.querySelector('article');let printed=0;
 try{window.print=()=>{printed++;assert.ok(target.classList.contains('p-druckziel'));assert.ok(document.querySelector('main').classList.contains('p-druckpfad'));assert.equal(document.querySelector('aside').classList.contains('p-druckziel'),false);};druckeBereich(target);assert.equal(printed,1);assert.equal(document.querySelector('[class*=p-druck]'),null);window.print=()=>{throw Error('Druckdialog nicht verfügbar');};assert.throws(()=>druckeBereich(target));assert.equal(document.querySelector('[class*=p-druck]'),null);assert.match(target.textContent,/Erhaltener Inhalt/);druckeBereich(null);}
 finally{dom.window.close();delete globalThis.window;delete globalThis.document;}
});
