import {test} from 'node:test';
import assert from 'node:assert/strict';
import {JSDOM} from 'jsdom';
import {act,createElement as h} from 'react';
import {createRoot} from 'react-dom/client';
import Assistent,{gueltigeQuellen} from '../node_modules/.cache/betriebsplattform-tests/Assistent.js';
import {Arbeitskontext} from '../node_modules/.cache/betriebsplattform-tests/Arbeitskontext.js';
test('Quellen erlauben ausschließlich bestehende MBOP-Ziele, keine Modell-URLs',()=>{
 const q={id:'a',titel:'Beleg',text:'Daten',ziel:'vorgaenge',objektId:'a'};assert.ok(gueltigeQuellen([q]));for(const ziel of ['javascript:alert(1)','https://extern','shell'])assert.equal(gueltigeQuellen([{...q,ziel}]),false);assert.equal(gueltigeQuellen(Array(21).fill(q)),false);
});
test('Assistenz lädt Quellen, behandelt Modellantwort nur als editierbaren Entwurf und speichert erst nach Freigabe',async()=>{
 const dom=new JSDOM('<div id="root"></div>',{url:'https://127.0.0.1/'}),old=globalThis.fetch;
 Object.assign(globalThis,{window:dom.window,document:dom.window.document,IS_REACT_ACT_ENVIRONMENT:true});window.HTMLElement.prototype.attachEvent=()=>{};window.HTMLElement.prototype.detachEvent=()=>{};const calls=[];let notizen=0;
 globalThis.fetch=async(path,init)=>{calls.push(path);let d;if(path==='/api/arbeitsumgebung')d={beispielkennzeichnung:false,authentifiziert:true,benutzer:'Test',aktenVerwalten:true};else if(path.startsWith('/api/assistenz/'))d={text:'Synthetischer Vertragsentwurf',quellen:[{id:'akte:a',titel:'Testakte',text:'Belegte Angabe',ziel:'vorgaenge',objektId:'a'}]};else if(path==='/api/akten/a/notizen'){notizen++;d={id:'note'};}else d={};return {ok:true,status:200,json:async()=>d};};
 const root=createRoot(document.getElementById('root'));
 try{await act(async()=>root.render(h(Arbeitskontext,null,h(Assistent,{bereich:'vorgaenge',id:'a'}))));assert.equal(document.querySelector('button').getAttribute('aria-label'),'Mit KI unterstützen');await act(async()=>document.querySelector('button').click());
 await act(async()=>document.querySelector('.p-assistent-inhalt form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.querySelector('.p-quellenliste').textContent,/Testakte/);assert.equal(notizen,0);
 await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent==='Lokal beantworten').click());assert.match(document.querySelector('textarea[rows="8"]').value,/Vertragsentwurf/);let speichern=[...document.querySelectorAll('button')].find(b=>b.textContent==='Notiz ausdrücklich speichern');assert.equal(speichern.disabled,true);assert.equal(notizen,0);
 await act(async()=>document.querySelector('input[type="checkbox"]').click());assert.equal(speichern.disabled,false);await act(async()=>speichern.closest('form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(notizen,1);assert.ok([...document.querySelectorAll('[role="status"]')].some(e=>/Notiz gespeichert/.test(e.textContent)));
 }finally{await act(async()=>root.unmount());globalThis.fetch=old;delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;dom.window.close();}
});
