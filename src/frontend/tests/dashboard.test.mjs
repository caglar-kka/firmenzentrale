import {test} from 'node:test';
import assert from 'node:assert/strict';
import {JSDOM} from 'jsdom';
import {act,createElement as h} from 'react';
import {createRoot} from 'react-dom/client';
import App from '../node_modules/.cache/betriebsplattform-tests/App.js';
import {sichtbareModule} from '../node_modules/.cache/betriebsplattform-tests/Module.js';
import {kalenderbereich} from '../node_modules/.cache/betriebsplattform-tests/Kalender.js';
import * as dashboard from '../node_modules/.cache/betriebsplattform-tests/Dashboard.js';
import {begruessung} from '../node_modules/.cache/betriebsplattform-tests/Dashboard.js';
const ok=v=>({ok:true,status:200,json:async()=>structuredClone(v)});
async function start(check,{personal=true,fehler=false}={}){
 const dom=new JSDOM('<div id="root"></div>',{url:'https://127.0.0.1:5180/'}),old=globalThis.fetch;
 Object.assign(globalThis,{window:dom.window,document:dom.window.document,IS_REACT_ACT_ENVIRONMENT:true});window.scrollTo=()=>{};
 window.HTMLElement.prototype.attachEvent=()=>{};window.HTMLElement.prototype.detachEvent=()=>{};
 let denied=false;const calls=[];
 globalThis.fetch=async path=>{calls.push(path);if(path==='/api/module')return ok({module:['Akten','Dokumente','Finanzen','Import','Kommunikation']});if(path==='/api/organisation')return ok({name:'Testbetrieb',logoStatus:'Nicht eingerichtet'});if(path==='/api/arbeitsumgebung')return ok({benutzer:'Prüfperson',authentifiziert:personal,fluechtigeDaten:false,beispielkennzeichnung:false,kategorienVerwalten:false});
 if(denied||fehler)return {ok:false,status:403,json:async()=>({meldung:'Kein Zugriff.'})};
 if(path==='/api/uebersicht')return ok({synthetisch:true,gesamtbetrag:'SERVER-SUMME',offenerBetrag:'SERVER-OFFEN',bestandsanzahl:42,offeneForderungen:'SERVER-FORDERUNG',offeneVerbindlichkeiten:'SERVER-VERBINDLICHKEIT',rechnungen:[]});
 if(path.startsWith('/api/termine/?'))return ok({anzahl:1,zeitzone:'Europe/Berlin',eintraege:[{id:'termin',version:1,beschreibung:'Fiktive Besprechung',ort:'Besprechungsraum',titel:'Fiktive Bürobesprechung',beginn:new Date().toLocaleDateString('sv-SE',{timeZone:'Europe/Berlin'})+'T23:00',ende:new Date().toLocaleDateString('sv-SE',{timeZone:'Europe/Berlin'})+'T23:59',ganztaegig:false}]});
 if(path.startsWith('/api/kontakte'))return ok({eintraege:[],anzahl:7});
 if(path==='/api/aufgaben/meine-uebersicht')return ok({offen:6,heute:2,ueberfaellig:1});
 if(path==='/api/posteingang/uebersicht')return ok({neu:9,pruefen:4,meine:2});
 if(path.startsWith('/api/dokumentenliste'))return ok({anzahl:1,eintraege:[{id:'doc',titel:'Berechtigtes Dokument',dateiname:'beleg.pdf',art:'Anhang',status:'Zur Prüfung',erstelltAm:'2026-10-22',akteId:'akte'}]});
 if(path.startsWith('/api/posteingang?'))return ok({anzahl:1,eintraege:[{id:'ein',dokumentId:'doc',betreff:'Echter Workflow',status:'Zur Prüfung',eingegangenAm:'2026-10-22',art:'Sonstiges',akteId:'akte',aufgabeId:'task'}]});
 if(path.startsWith('/api/kalender?'))return ok({anzahl:1,eintraege:[{id:'task',betreff:'Nur erlaubte Aufgabe',faelligAm:'2026-10-22',faelligUm:'10:30:00',status:'Offen'}],zeitzone:'Europe/Berlin'});
 if(path.startsWith('/api/aufgaben/?'))return ok({anzahl:25,eintraege:[{id:'task',betreff:'Nur erlaubte Aufgabe',status:'Offen',prioritaet:'Hoch',faelligAm:null,archiviert:false}]});
 return ok([]);};
 const root=createRoot(document.getElementById('root'));
 try{await act(async()=>root.render(h(App)));await act(async()=>new Promise(r=>setTimeout(r,260)));await check(calls,()=>{denied=true;});}
 finally{await act(async()=>root.unmount());globalThis.fetch=old;delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;dom.window.close();}
}
test('Dashboard ist tatsächliche Rootseite, zeigt Servergesamtzahlen und nur reale Modulziele',()=>start(async calls=>{
 assert.ok(document.querySelector('[aria-label="Dashboard"]'));assert.equal(document.querySelector('[aria-current="page"]').textContent,'Dashboard');
 assert.match(document.querySelector('h1').textContent,/Prüfperson/);assert.match(document.querySelector('.p-railprofil').textContent,/Prüfperson/);
 assert.deepEqual([...document.querySelectorAll('.p-dashboard-kpi strong')].map(e=>e.textContent),['4','6','SERVER-FORDERUNG','SERVER-VERBINDLICHKEIT']);
 assert.equal(document.querySelector('.p-hub-kern span').textContent,'Testbetrieb');
 assert.deepEqual([...document.querySelectorAll('.p-hub-modul')].map(e=>e.getAttribute('href')),['#vorgaenge','#kontakte','#rechnungen','#aufgaben','#kalender','#dokumente']);
 assert.equal(document.querySelectorAll('.p-ki-orb').length,1);assert.equal(document.querySelector('header .p-ki-orb'),null);
 assert.equal(document.querySelector('.p-kommando img').getAttribute('src'),'/mbop-kommandobruecke.png');assert.match(document.querySelector('.p-kommando img').alt,/Originalmotiv/);assert.equal(document.querySelector('.p-kommando svg'),null);
 await act(async()=>document.querySelector('.p-prioritaetenliste a').click());await act(async()=>new Promise(r=>setTimeout(r,20)));assert.equal(window.location.hash,'#aufgaben?id=task');
 assert.ok(calls.includes('/api/aufgaben/meine-uebersicht'));
}));
test('Dashboard entfernt frühere Kennzahlen und Aufgaben bei entzogenen Leserechten',()=>start(async(calls,deny)=>{
 assert.match(document.querySelector('main').textContent,/Nur erlaubte Aufgabe/);deny();
 await act(async()=>window.dispatchEvent(new window.Event('focus')));await act(async()=>new Promise(r=>setTimeout(r,260)));
 assert.doesNotMatch(document.querySelector('main').textContent,/SERVER-OFFEN|Nur erlaubte Aufgabe/);
 assert.ok([...document.querySelectorAll('.p-dashboard-kpi strong')].every(e=>e.textContent==='Nicht verfügbar'));
}));
test('Dashboard ruft im unpersönlichen Modus keine persönlichen Aufgaben- oder Eingangsübersichten ab',()=>start(async calls=>{
 assert.ok(!calls.some(p=>p.startsWith('/api/aufgaben')||p==='/api/posteingang/uebersicht'));
 assert.equal(document.querySelector('.p-dashboard-kpi[href="#aufgaben"]'),null);assert.equal(document.querySelector('.p-hub-modul[href="#aufgaben"]'),null);
 assert.match(document.querySelector('main').textContent,/Persönliche Anmeldung erforderlich/);
},{personal:false}));
test('Begrüßung berücksichtigt die Tageszeit an den Übergängen',()=>{for(const [hour,text] of [[0,'Guten Morgen'],[10,'Guten Morgen'],[11,'Guten Tag'],[17,'Guten Tag'],[18,'Guten Abend'],[23,'Guten Abend']])assert.equal(begruessung(hour),text);});

test('Referenznavigation hat die vorgegebene Reihenfolge, offene Zeiterfassung ohne Link und Profil nur unten links',()=>start(async()=>{
 const nav=document.querySelector('nav[aria-label="Hauptnavigation"]');
 assert.deepEqual([...nav.children].filter(e=>e.matches('a')).map(e=>e.textContent),['Dashboard','Akten','Kontakte','Rechnungen','Posteingang','Aufgaben','Kalender']);assert.equal(nav.querySelectorAll('.p-nav-geplant').length,0);assert.ok(nav.querySelector('details'));
 const profile=document.querySelector('.p-railprofil');assert.ok(profile.closest('aside'));assert.ok(profile.closest('.p-railfuss'));assert.equal(document.querySelector('header .p-benutzer'),null);
 assert.ok(document.querySelector('.p-prioritaetenliste .icon-flagge'));
}));

test('Neue Navigation führt zu Agenda, Originalbestand, bestehendem Prozess und belegtem Bericht',()=>start(async()=>{
 async function route(id){await act(async()=>document.querySelector('nav a[href="#'+id+'"]').click());await act(async()=>new Promise(r=>setTimeout(r,260)));}
 await route('kalender');assert.match(document.querySelector('main h1').textContent,/Kalender/);assert.match(document.querySelector('.p-kalendermonat').textContent,/Nur erlaubte Aufgabe/);assert.equal(document.querySelector('.p-kalendermonat a').getAttribute('href'),'#aufgaben?id=task');
 await act(async()=>[...document.querySelectorAll('main button')].find(b=>b.textContent==='Neue Aufgabe').click());assert.ok(document.querySelector('dialog'));await act(async()=>[...document.querySelectorAll('dialog button')].find(b=>b.textContent==='Abbrechen').click());assert.equal(window.location.hash,'#kalender');
 await route('dokumente');assert.match(document.querySelector('main').textContent,/Berechtigtes Dokument/);assert.equal(document.querySelector('main .p-quellenliste a').getAttribute('href'),'#vorgaenge?id=akte&dokument=doc');
 await route('prozesse');assert.match(document.querySelector('main').textContent,/Echter Workflow/);assert.ok(document.querySelector('main a[href="#aufgaben?id=task"]'));
 await route('berichte');assert.match(document.querySelector('[aria-label=Finanzbestand]').textContent,/SERVER-FORDERUNG/);assert.match(document.querySelector('[aria-label=Finanzbestand]').textContent,/SERVER-VERBINDLICHKEIT/);assert.match(document.querySelector('[aria-label=Aufgabenbestand]').textContent,/6/);assert.equal(document.querySelector('.p-berichtskarten'),null);
}));

test('Modulregister blendet deaktivierte Bereiche aus und begrenzt persönliche Ziele',()=>{const u={module:[],authentifiziert:false};assert.deepEqual(sichtbareModule(u).map(m=>m.id),['arbeitsplatz','kontakte']);assert.ok(!sichtbareModule({...u,module:['Finanzen']}).some(m=>m.id==='dokumente'));});
test('Kalender berechnet Montag-Raster, Schaltjahr und kurze Tages-/Wochenbereiche',()=>{assert.equal(kalenderbereich('2026-10-22','Monat').tage.length,42);assert.equal(kalenderbereich('2026-10-22','Monat').von,'2026-09-28');assert.equal(kalenderbereich('2024-02-29','Tag').bis,'2024-03-01');assert.equal(kalenderbereich('2026-10-25','Woche').tage.length,7);});


test('Dashboard untere Arbeitskarten: berechtigte Termine und Eingänge, keine doppelte KI oder Aufgabenliste',()=>start(async()=>{
 const bereich=document.querySelector('[aria-label="Nächste Arbeitsschritte"]');assert.ok(bereich);
 assert.match(bereich.textContent,/Fiktive Bürobesprechung/);assert.match(bereich.textContent,/Echter Workflow/);
 assert.equal(bereich.querySelector('a[href="#posteingang?id=ein"]').textContent,'Echter Workflow');
 assert.equal(document.querySelectorAll('.p-ki-orb').length,1);assert.equal(document.querySelectorAll('.p-prioritaetenliste').length,1);
 assert.equal(document.querySelector('h2[contenteditable]'),null);
}));
test('Dashboard untere Arbeitskarten zeigen Datenfehler statt erfundener Einträge',()=>start(async()=>{
 const bereich=document.querySelector('[aria-label="Nächste Arbeitsschritte"]');assert.ok(bereich);assert.match(bereich.textContent,/nicht verfügbar/);assert.doesNotMatch(bereich.textContent,/Fiktive Bürobesprechung|Echter Workflow/);
},{fehler:true}));
test('Kommende Termine unterscheiden verstrichene, laufende und exklusive Ganztag-Enden in Berlin',()=>{
 const jetzt=new Date('2026-10-25T09:00:00Z');const t=(id,beginn,ende,ganztaegig=false)=>({id,titel:id,beginn,ende,ganztaegig});
 const liste=[t('vorbei','2026-10-25T08:00','2026-10-25T09:00'),t('laeuft','2026-10-25T09:30','2026-10-25T10:30'),t('morgen','2026-10-26T11:00','2026-10-26T12:00'),t('ganzvorbei','2026-10-24T00:00','2026-10-25T00:00',true),t('heute','2026-10-25T00:00','2026-10-26T00:00',true)];
 assert.deepEqual(dashboard.kommendeTermine(liste,jetzt).map(t=>t.id),['heute','laeuft']);
 assert.deepEqual(dashboard.kommendeTermine([liste[0]],jetzt),[]);
});
