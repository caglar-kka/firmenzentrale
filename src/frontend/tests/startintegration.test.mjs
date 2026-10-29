import{test}from'node:test';import assert from'node:assert/strict';import{JSDOM}from'jsdom';import{act,createElement as h}from'react';import{createRoot}from'react-dom/client';
import{Anwendung}from'../node_modules/.cache/betriebsplattform-tests/App.js';
const ok=data=>({ok:true,status:200,json:async()=>data});
async function start(admin,check,{signed=true,ready=true,demo=false}={}){
 const dom=new JSDOM('<div id="root"></div>',{url:'https://127.0.0.1:5180/#sicherheit'});const old={fetch:globalThis.fetch,FormData:globalThis.FormData};
 Object.assign(globalThis,{window:dom.window,document:dom.window.document,FormData:dom.window.FormData,IS_REACT_ACT_ENVIRONMENT:true});window.scrollTo=()=>{};window.HTMLElement.prototype.attachEvent=()=>{};window.HTMLElement.prototype.detachEvent=()=>{};
 const calls=[];globalThis.fetch=async(path,options)=>{calls.push(path);if(path==='/api/auth/session')return ok({aktiv:!demo,angemeldet:signed,bereit:ready,csrf:'synthetisch',meldung:'Anmeldung noch nicht eingerichtet.'});if(path==='/api/auth/logout'){signed=false;return ok({});}if(path==='/api/arbeitsumgebung')return ok({authentifiziert:!demo,administrator:admin,benutzer:demo?'Beispielperson':'Echte Testidentität',kategorienVerwalten:admin,beispielkennzeichnung:demo});if(path==='/api/admin/benutzer'){assert.ok(admin);return ok([]);}if(path==='/api/auth/mfa/status')return ok({aktiv:false,verbleibend:0});return ok([]);};
 const root=createRoot(document.getElementById('root'));
 try{await act(async()=>root.render(h(Anwendung)));await check(calls);}finally{await act(async()=>root.unmount());Object.assign(globalThis,old);delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;dom.window.close();}
}
const button=(text,scope=document)=>Array.from(scope.querySelectorAll('button')).find(b=>b.textContent===text);
test('Reguläre Anwendung: ohne Anmeldung kein Arbeitsplatz, auch bei direkter Sicherheitsroute',async()=>start(false,async calls=>{assert.match(document.body.textContent,/Anmelden/);assert.equal(document.querySelector('.studio'),null);assert.deepEqual(calls,['/api/auth/session']);},{signed:false}));
test('Reguläre Anwendung: fehlende Voraussetzungen geben weder Demo noch Loginformular frei',async()=>start(false,async calls=>{assert.match(document.body.textContent,/noch nicht eingerichtet/);assert.equal(document.querySelector('form'),null);assert.deepEqual(calls,['/api/auth/session']);},{signed:false,ready:false}));
test('Reguläre Anwendung: Profilmenü, Sicherheitsdialoge, Administratornavigation und Logout sind verbunden',async()=>start(true,async calls=>{
 const summary=document.querySelector('summary[aria-label="Benutzerprofil"]');assert.ok(summary);assert.match(summary.textContent,/Echte Testidentität/);assert.equal(summary.parentElement.open,false);
 await act(async()=>summary.click());assert.equal(summary.parentElement.open,true);
 await act(async()=>summary.dispatchEvent(new window.KeyboardEvent('keydown',{key:'Escape',bubbles:true})));assert.equal(summary.parentElement.open,false);assert.equal(document.activeElement,summary);
 await act(async()=>summary.click());await act(async()=>button('Mein Profil').click());assert.match(document.querySelector('main').textContent,/Mein Profil/);
 await act(async()=>button('Sicherheit',document.querySelector('main')).click());assert.ok(document.querySelector('dialog'));assert.ok(calls.includes('/api/auth/mfa/status'));
 await act(async()=>Array.from(document.querySelectorAll('dialog button')).find(b=>b.getAttribute('aria-label')==='Schließen'||b.textContent==='Schließen').click());
 await act(async()=>button('Passwort ändern',document.querySelector('main')).click());assert.ok(document.querySelector('input[autocomplete=current-password]'));
 await act(async()=>Array.from(document.querySelectorAll('dialog button')).find(b=>b.getAttribute('aria-label')==='Schließen'||b.textContent==='Schließen').click());
 await act(async()=>document.querySelector('a[href="#einstellungen"]').click());await act(async()=>button('Benutzer & Berechtigungen').click());assert.match(document.querySelector('main').textContent,/Benutzer und Zugriffe/);assert.ok(calls.includes('/api/admin/benutzer'));
 await act(async()=>summary.click());assert.equal(summary.parentElement.open,true);await act(async()=>button('Abmelden').click());assert.equal(document.querySelector('.studio'),null);assert.match(document.body.textContent,/Anmelden/);
}));
test('Reguläre Anwendung: Mitarbeiter können Verwaltungsroute nicht öffnen',async()=>start(false,async calls=>{
 assert.equal(document.querySelector('a[href="#einstellungen"]'),null);
 await act(async()=>{window.location.hash='benutzer';window.dispatchEvent(new window.Event('hashchange'));});assert.match(document.querySelector('main').textContent,/Administratorberechtigung/);assert.ok(!calls.includes('/api/admin/benutzer'));
}));
test('Explizite Demo: Beispielprofil ist bedienbar und eindeutig gekennzeichnet',async()=>start(false,async()=>{const summary=document.querySelector('summary[aria-label="Demo-Beispielprofil"]');assert.ok(summary);await act(async()=>summary.click());assert.equal(summary.parentElement.open,true);assert.match(summary.parentElement.textContent,/Beispielprofil/);assert.equal(button('Abmelden'),undefined);},{demo:true}));

test('Einstellungsübersicht: fünf kompakte Funktionen, gesperrtes Logo und unveränderte Navigation',async()=>start(true,async calls=>{
 await act(async()=>document.querySelector('a[href="#einstellungen"]').click());
 const page=document.querySelector('.p-einstellungsuebersicht');assert.ok(page);const tiles=page.querySelector('.p-einstellungskacheln');assert.ok(tiles);assert.equal(tiles.children.length,5);
 assert.deepEqual([...tiles.querySelectorAll('h2')].map(h=>h.textContent),['Benutzer & Berechtigungen','Sicherheit','Kategorien','Drucker einrichten','Firmenlogo']);
 assert.ok(document.querySelector('.p-kontextkopf .p-suche'));assert.ok(document.querySelector('button[aria-label="Persönliche Terminerinnerungen öffnen"]'));
 const logo=tiles.querySelector('[aria-labelledby="bild-firmenlogo"]');assert.ok(logo.querySelector('.p-bildhinweise'));assert.equal(logo.querySelector('.p-bildhinweise').open,false);assert.equal(logo.querySelector('input[type=file]'),null);assert.ok([...logo.querySelectorAll('button')].every(b=>b.disabled));
 const printer=tiles.querySelector('.p-druckhilfe');assert.equal(printer.open,false);await act(async()=>printer.querySelector('summary').click());assert.equal(printer.open,true);assert.match(printer.textContent,/keine Treiber/);assert.equal(printer.querySelector('a').getAttribute('rel'),'noopener noreferrer');
 await act(async()=>button('Sicherheit',tiles).click());assert.match(document.querySelector('main').textContent,/Zwei-Faktor-Authentifizierung vorschreiben/);assert.equal(document.querySelector('.p-einstellungsuebersicht'),null);
 await act(async()=>document.querySelector('a[href="#einstellungen"]').click());await act(async()=>button('Kategorien',document.querySelector('main')).click());assert.equal(document.querySelector('main').dataset.bereich,'kategorien');
 await act(async()=>document.querySelector('a[href="#einstellungen"]').click());await act(async()=>button('Benutzer & Berechtigungen',document.querySelector('main')).click());assert.equal(document.querySelector('main').dataset.bereich,'benutzer');assert.ok(calls.includes('/api/admin/benutzer'));
}));
