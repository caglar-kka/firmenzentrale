import {test} from 'node:test';import assert from 'node:assert/strict';import {JSDOM} from 'jsdom';import {act,createElement as h} from 'react';import {createRoot} from 'react-dom/client';import App from '../node_modules/.cache/betriebsplattform-tests/App.js';

const ok=v=>({ok:true,status:200,json:async()=>structuredClone(v)});

async function start(hash,check,{aktiv=true,entwurf=false}={}){const dom=new JSDOM('<div id="root"></div>',{url:'https://127.0.0.1:5180/'+hash}),old={fetch:globalThis.fetch,FormData:globalThis.FormData};Object.assign(globalThis,{window:dom.window,document:dom.window.document,FormData:dom.window.FormData,IS_REACT_ACT_ENVIRONMENT:true});window.scrollTo=()=>{};window.HTMLElement.prototype.attachEvent=()=>{};window.HTMLElement.prototype.detachEvent=()=>{};const calls=[];

 const stand={kunde:'Fiktive Elbequartier Objektservice',objekt:'Fiktives Kontorhaus',anschrift:'Prüfweg7',plan:'Unterhaltsreinigung',planversion:1,soll:[{leistung:'Boden wischen',bereich:'Flur',turnus:'Bei jedem Einsatz',pflicht:true,hinweis:''}],ist:[{nummer:0,status:'Teilweise erledigt',bemerkung:'Fiktiver Raum gesperrt'}],bemerkung:'Test',erfasstDurch:'Fiktives Büro',erfassungsart:'Erfasst durch Büro',rueckmeldung:'',nacharbeitId:null};

 const einsatz={id:'e',objektId:'o',arbeitstag:'2026-10-25',status:entwurf?'Entwurf':'In Büroprüfung',version:4,daten:JSON.stringify(stand),vorgaengerId:null,aenderungsgrund:''};let denied=false;

 globalThis.fetch=async(path,init)=>{if(path==='/api/arbeitsumgebung')return ok({leistungsnachweiseAktiv:aktiv,benutzer:'Fiktives Büro',beispielkennzeichnung:true,diagnose:false,kategorienVerwalten:false,kontakteVerwalten:true,authentifiziert:true,administrator:true,fluechtigeDaten:false});if(path==='/api/module')return ok({module:['Akten','Dokumente','Finanzen']});if(path==='/api/organisation')return ok({name:'Fiktiver Testbetrieb'});

 if(path.startsWith('/api/leistungen')){if(denied)return{ok:false,status:403,json:async()=>({meldung:'Kein Zugriff.'})};if(init?.method==='POST'){calls.push({path,body:JSON.parse(init.body)});return ok({id:'p',version:5});}if(path.includes('/katalog'))return ok({anzahl:1,eintraege:[{id:'k',version:1,name:'Fenster reinigen',beschreibung:'Rahmen abwischen',kategorie:'Glas',reihenfolge:1,aktiv:true}]});if(path.includes('/einsaetze/'))return ok({einsatz,stand,buero:true,nachweis:null});if(path.includes('/objekte/'))return ok({objekt:{id:'o',name:'Fiktives Kontorhaus',kundeId:'kunde',version:2,anschrift:'Prüfweg7'},buero:true,plaene:[],einsaetze:[]});return ok({eintraege:[]});}

 if(path.startsWith('/api/kontakte'))return ok({eintraege:[],anzahl:0});return ok([]);};

 const root=createRoot(document.querySelector('#root'));const click=async(text)=>{await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent===text).click());};

 try{await act(async()=>root.render(h(App)));await act(async()=>new Promise(r=>setTimeout(r,280)));await check({calls,click,deny:()=>{denied=true;}});}finally{await act(async()=>root.unmount());Object.assign(globalThis,old);delete globalThis.document;delete globalThis.window;delete globalThis.IS_REACT_ACT_ENVIRONMENT;dom.window.close();}}



test('Leistungsnachweise sind ohne ausdrückliche Hostaktivierung keine verfügbare Previewfunktion',()=>start('#leistungen',async({calls})=>{assert.equal(document.querySelector('nav a[href="#leistungen"]'),null);assert.match(document.querySelector('main').textContent,/authentifizierten Host/);assert.equal(calls.length,0);},{aktiv:false}));

test('Neue Planversion: Pflichtfehler fokussieren, Abbruch ohne Schreiben, genau ein gültiger POST',()=>start('#leistungen?id=o',async({calls,click})=>{await click('Neue Planversion');const vorher=window.location.hash;await act(async()=>document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(document.activeElement.name,'name');assert.equal(calls.length,0);await click('Abbrechen');assert.equal(window.location.hash,vorher);await click('Neue Planversion');document.querySelector('dialog [name=name]').value='Fiktiver Plan';document.querySelector('dialog [name=leistung0]').value='Sanitär reinigen';await act(async()=>{for(let i=0;i<2;i++)document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true}));});assert.equal(calls.length,1);assert.equal(calls[0].body.objektversion,2);assert.equal(calls[0].body.positionen[0].leistung,'Sanitär reinigen');assert.match(document.querySelector('main').textContent,/Gespeichert/);}));

test('Bürofreigabe benötigt ausdrückliche Dialogbestätigung; Quellenansicht verschwindet bei Rechteentzug',()=>start('#leistungen?id=o&einsatz=e',async({calls,click,deny})=>{assert.match(document.querySelector('main').textContent,/Teilweise erledigt/);assert.match(document.querySelector('main').textContent,/Erfasst durch Büro/);await click('Intern freigeben');assert.match(document.querySelector('dialog').textContent,/keine Kundenbestätigung/);await click('Abbrechen');assert.equal(calls.length,0);await click('Intern freigeben');await click('Ausdrücklich intern freigeben');assert.equal(calls.length,1);assert.equal(calls[0].body.art,'freigeben');assert.equal(calls[0].body.version,4);deny();await act(async()=>{window.dispatchEvent(new window.Event('focus'));await new Promise(r=>setTimeout(r,40));});assert.doesNotMatch(document.querySelector('main').textContent,/Fiktive Elbequartier|Fiktiver Raum gesperrt/);}));



test('Objektpflege zeigt Stammdaten, bewahrt Version und bricht ohne Schreiben ab',()=>start('#leistungen?id=o',async({calls,click})=>{

 await click('Objekt bearbeiten');assert.equal(document.querySelector('dialog [name=name]').value,'Fiktives Kontorhaus');assert.equal(document.querySelector('dialog [name=aktiv]').checked,true);await click('Abbrechen');assert.equal(calls.length,0);

 await click('Objekt bearbeiten');document.querySelector('dialog [name=name]').value='Kontorhaus Süd';document.querySelector('dialog [name=aktiv]').checked=false;

 await act(async()=>document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));

 assert.equal(calls.length,1);assert.equal(calls[0].path,'/api/leistungen/objekte/o');assert.equal(calls[0].body.version,2);assert.equal(calls[0].body.aktiv,false);assert.equal(calls[0].body.name,'Kontorhaus Süd');assert.match(document.querySelector('main').textContent,/Gespeichert/);

}));



test('Leistungskatalog: Pflichtfeld, Abbruch und einmaliges Speichern',()=>start('#leistungen?katalog=1',async({calls,click})=>{

 await click('Neue Leistung');await act(async()=>document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(document.activeElement.name,'name');assert.equal(calls.length,0);await click('Abbrechen');await click('Neue Leistung');document.querySelector('dialog [name=name]').value='Fenster reinigen';

 await act(async()=>{const f=document.querySelector('dialog form');f.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true}));f.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true}));});assert.equal(calls.length,1);assert.equal(calls[0].path,'/api/leistungen/katalog');assert.equal(calls[0].body.version,0);assert.match(document.querySelector('main').textContent,/Leistung gespeichert/);

}));


test('Katalogauswahl übernimmt eine bearbeitbare Kopie in die neue Planversion',()=>start('#leistungen?id=o',async({calls,click})=>{
 await click('Neue Planversion');for(let n=0;n<100&&!document.querySelector('dialog option[value="k"]');n++)await act(async()=>new Promise(r=>setTimeout(r,10)));const form=document.querySelector('dialog form');const auswahl=[...form.querySelectorAll('select')].find(s=>[...s.options].some(o=>o.value==='k'));assert.ok(auswahl);
 await act(async()=>{auswahl.value='k';auswahl.dispatchEvent(new window.Event('change',{bubbles:true}));});assert.equal(form.querySelector('[name=leistung0]').value,'Fenster reinigen');assert.equal(form.querySelector('[name=hinweis0]').value,'Rahmen abwischen');form.querySelector('[name=name]').value='Glasreinigung';form.querySelector('[name=leistung0]').value='Fenster innen reinigen';
 await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(calls.length,1);assert.equal(calls[0].body.positionen[0].leistung,'Fenster innen reinigen');assert.equal(calls[0].body.positionen[0].hinweis,'Rahmen abwischen');
}));


test('Planpositionen behalten freie Texte beim Sortieren und senden ihren Aktivstatus',()=>start('#leistungen?id=o',async({calls,click})=>{
 await click('Neue Planversion');document.querySelector('[name=name]').value='Objektplan';document.querySelector('[name=leistung0]').value='Boden säubern';document.querySelector('[name=hinweis0]').value='Längerer Hinweis mit Umlauten: gründlich wischen.';await click('Weitere Leistung');document.querySelector('[name=leistung1]').value='Spiegel reinigen';document.querySelector('[name=aktiv1]').checked=false;const move=[...document.querySelectorAll('button')].find(b=>b.textContent==='Nach oben'&&!b.disabled);await act(async()=>move.click());assert.equal(document.querySelector('[name=leistung0]').value,'Spiegel reinigen');assert.equal(document.querySelector('[name=hinweis1]').value,'Längerer Hinweis mit Umlauten: gründlich wischen.');
 await act(async()=>document.querySelector('dialog form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(calls.length,1);assert.equal(calls[0].body.positionen[0].aktiv,false);assert.equal(calls[0].body.positionen[1].leistung,'Boden säubern');
}));

test('Sonderposition und freier Einsatztext werden nur ausdrücklich im aktuellen Entwurf gespeichert',()=>start('#leistungen?id=o&einsatz=e',async({calls,click})=>{
 await click('Sonderposition ergänzen');document.querySelector('[name=sonderleistung0]').value='Türgriffe zusätzlich desinfizieren';document.querySelector('[name=status1]').value='Erledigt';document.querySelector('[name=bemerkung1]').value='Nach Absprache mit Objektkontakt.';document.querySelector('[name=bemerkung]').value='Übergabe mit Rückfrage: Schlüssel fehlt.';await click('Checkliste speichern');assert.equal(calls.length,1);assert.equal(calls[0].body.sonderpositionen[0].leistung,'Türgriffe zusätzlich desinfizieren');assert.equal(calls[0].body.positionen[1].bemerkung,'Nach Absprache mit Objektkontakt.');assert.equal(calls[0].body.bemerkung,'Übergabe mit Rückfrage: Schlüssel fehlt.');assert.equal(calls[0].path,'/api/leistungen/einsaetze/e');
},{entwurf:true}));
