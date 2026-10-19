import { test } from 'node:test';
import assert from 'node:assert/strict';
import { JSDOM } from 'jsdom';
import { readFileSync } from 'node:fs';
import { act, createElement } from 'react';
import { createRoot } from 'react-dom/client';
import App from '../node_modules/.cache/betriebsplattform-tests/App.js';
import Designprototyp from '../node_modules/.cache/betriebsplattform-tests/Designprototyp.js';
import { istUebersicht, istImportansicht, ladeUebersicht } from '../node_modules/.cache/betriebsplattform-tests/api.js';

const quelle = { dateiHash: 'A'.repeat(64), blatt: 'ISD-Testdaten', zeile: 2, zelle: 'G2' };
const uebersicht = {
  synthetisch: true, gesamtbetrag: '1.000,00 €', offenerBetrag: 'SERVER-SALDO',
  rechnungen: [{ nummer: 'RE-2026-1101', datum: '2026-10-17', aussteller: 'Firma A', empfaenger: 'ISD', betrag: '1.000,00 €',
    offenerBetrag: 'SERVER-TEILBETRAG', bestaetigtZugeordnet: 'SERVER-ZUORDNUNG', status: 'Teilzahlung', beleg: 'SYNTH-BELEG', quellenort: 'synthetisch/Test',
    zahlungen: [{ datum: '2026-10-17', zahler: 'ISD', empfaenger: 'Firma A', betrag: '300,00 €', zugeordnet: '300,00 €', status: 'Zur Prüfung', beleg: 'SYNTH-ZAHLUNG', quellenort: 'synthetisch/Zahlung' }] }],
};
const vorschau = {
  synthetisch: true, dateiHash: 'A'.repeat(64), hatFehler: true, zeilenanzahl: 1, fehlerzeilen: 1, bericht: `SHA-256: ${'A'.repeat(64)}\nKein Import. Zur Prüfung.`,
  meldungen: [{ stufe: 'Warnung', code: 'NUR_VORSCHAU', text: 'Keine Buchung.', quelle: null }],
  zeilen: [{ zeile: 2, hatFehler: true, art: null, betrag: null, datum: null, von: null, an: null,
    zellen: [{ quelle, datentyp: 'inlineStr', originalwert: '<img src=x onerror=alert(1)>', text: '<img src=x onerror=alert(1)>', formel: null, formatindex: null }],
    meldungen: [{ stufe: 'Fehler', code: 'BETRAG', text: 'Betrag prüfen.', quelle }] }],
};
const antwort = daten => ({ ok: true, json: async () => structuredClone(daten) });

test('API-Vertrag verlangt vollständige synthetische Ansichten', () => {
  assert.equal(istUebersicht(uebersicht), true);
  assert.equal(istUebersicht({ ...uebersicht, synthetisch: false }), false);
  assert.equal(istUebersicht({ ...uebersicht, rechnungen: [{}] }), false);
  assert.equal(istImportansicht(vorschau), true);
  assert.equal(istImportansicht({ ...vorschau, zeilen: [{}] }), false);
});

test('Netzwerk-, HTTP- und Datenformatfehler bleiben verständlich', async () => {
  const original = globalThis.fetch;
  try {
    globalThis.fetch = async () => { throw new Error('INTERNE-DETAILS'); };
    await assert.rejects(ladeUebersicht(new AbortController().signal), /lokale Verbindung/);
    globalThis.fetch = async () => ({ ok: false });
    await assert.rejects(ladeUebersicht(new AbortController().signal), /erneut versuchen/);
    globalThis.fetch = async () => antwort({ synthetisch: false });
    await assert.rejects(ladeUebersicht(new AbortController().signal), /Datenformat/);
  } finally { globalThis.fetch = original; }
});

test('Rechnungssuche, Statusfilter und Sortierung arbeiten ohne Geldberechnung', async () => {
  const { rechnungenFiltern } = await import('../node_modules/.cache/betriebsplattform-tests/darstellung.js');
  const a = uebersicht.rechnungen[0];
  const b = { ...a, nummer: 'AAA', status: 'Offen', datum: '2026-10-16', empfaenger: 'Musterbetrieb' };
  assert.deepEqual(rechnungenFiltern([a,b], ' isd ', 'Alle', 'nummer'), [a]);
  assert.deepEqual(rechnungenFiltern([a,b], '', 'Offen', 'nummer'), [b]);
  assert.deepEqual(rechnungenFiltern([a,b], '', 'Alle', 'datum'), [a,b]);
  assert.deepEqual(rechnungenFiltern([a,b], 'nicht vorhanden', 'Alle', 'nummer'), []);
  assert.deepEqual([a,b].map(r => r.nummer), ['RE-2026-1101','AAA']);
});

test('Status trennt offene Rechnungen, Bestätigung und Importprüfung', async () => {
  const { zahlungsstatus, importstatus, datumAnzeigen, regionen } = await import('../node_modules/.cache/betriebsplattform-tests/darstellung.js');
  assert.equal(zahlungsstatus('Offen').ton, 'warnung');
  assert.equal(zahlungsstatus('Teilzahlung').text, 'Teilweise bezahlt');
  assert.equal(zahlungsstatus('Zur Prüfung').ton, 'warnung');
  assert.equal(zahlungsstatus('Bestätigt (synthetisch)').ton, 'erfolg');
  assert.equal(zahlungsstatus('Unbekannt').ton, 'neutral');
  assert.equal(importstatus(true, true).ton, 'fehler');
  assert.equal(importstatus(false, true).ton, 'warnung');
  assert.equal(importstatus(false, false).ton, 'neutral');
  assert.equal(datumAnzeigen('2026-10-17'), '17.10.2026');
  assert.equal(datumAnzeigen('2026-02-30'), '2026-02-30');
  assert.equal(datumAnzeigen('2026-10-17', 'CH'), '17.10.2026');
  assert.equal(regionen.CH.waehrung, 'CHF');
});


async function mitAnsicht(daten, pruefen, url = 'http://127.0.0.1:5180/', Komponente = App) {
  const dom = new JSDOM('<!doctype html><div id="root"></div>', { url });
  const originalFetch = globalThis.fetch;
  globalThis.window = dom.window; globalThis.document = dom.window.document; globalThis.IS_REACT_ACT_ENVIRONMENT = true;
  dom.window.scrollTo = () => {};
  // React is imported before jsdom; its legacy focus-event branch needs these test-only no-ops.
  dom.window.HTMLElement.prototype.attachEvent = () => {}; dom.window.HTMLElement.prototype.detachEvent = () => {};
  globalThis.fetch = async url => antwort(String(url).endsWith('arbeitsumgebung')?{beispielkennzeichnung:true,diagnose:true,benutzer:'Lina Fennbrück',kategorienVerwalten:false,kontakteVerwalten:false}:String(url).endsWith('/kontakte')?[{id:'DEMO-NORD',firma:'Fjordfalter Objektpflege GmbH',version:1,werte:[],historie:[]},{id:'DEMO-ISD',firma:'Elbpfad Büroausstattung GmbH',version:1,werte:[],historie:[]}]:String(url).endsWith('/importe')?[]:String(url).includes('importvorschau')?vorschau:daten);
  const root = createRoot(document.getElementById('root'));
  try {
    await act(async () => root.render(createElement(Komponente)));
    await pruefen();
  } finally {
    await act(async () => root.unmount()); globalThis.fetch = originalFetch;
    delete globalThis.window; delete globalThis.document; delete globalThis.IS_REACT_ACT_ENVIRONMENT; dom.window.close();
  }
}

test('Semantische CSS-Tokenpaare erfüllen die festgelegten Kontrastgrenzen', () => {
  const css = readFileSync(new URL('../src/design-tokens.css', import.meta.url), 'utf8');
  const paletten = [...css.matchAll(/^(\.studio)\s*\{([^}]+)\}/gm)];
  assert.equal(paletten.length,1);
  const luminanz = hex => {
    const rgb = [1,3,5].map(i => parseInt(hex.slice(i,i+2),16)/255).map(c => c <= .04045 ? c/12.92 : ((c+.055)/1.055)**2.4);
    return rgb[0]*.2126+rgb[1]*.7152+rgb[2]*.0722;
  };
  const kontrast = (a,b) => { const x=luminanz(a),y=luminanz(b); return (Math.max(x,y)+.05)/(Math.min(x,y)+.05); };
  const paare = [['zweite-text','zweite-hell'],['kopf-text','kopf-bg'],['kopf-leise','kopf-bg'],['kopf-leise','kopf-badge'],['tabellentext','tabellenkopf'],['text','detail'],['leise','detail'],['text','zeile-alternativ'],['text','flaeche'],['leise','flaeche'],['leise','grund'],['leise','weich'],['akzent','akzent-hell'],
    ...['neutral','erfolg','arbeit','warnung','fehler'].map(s => [s+'-text',s+'-bg'])];
  for (const palette of paletten) {
    const werte = Object.fromEntries([...palette[2].matchAll(/--([a-z-]+):\s*(#[a-f0-9]{6}(?![a-f0-9])|var\(--[a-z-]+\))/g)].map(m => [m[1],m[2]]));
    const aufloesen = (name, besucht = []) => {
      assert.ok(!besucht.includes(name), 'Zyklischer Farbtoken: '+name);
      const wert = werte[name];
      assert.ok(wert, 'Fehlender Farbtoken: '+name);
      return wert.startsWith('var(') ? aufloesen(wert.slice(6,-1),[...besucht,name]) : wert;
    };
    const tokens = Object.fromEntries(Object.keys(werte).map(name => [name,aufloesen(name)]));
    for (const [text,hintergrund] of paare) assert.ok(kontrast(tokens[text],tokens[hintergrund]) >= 4.5, palette[1]+': '+text+'/'+hintergrund);
    if (palette[1] === '.studio') {
      for (const text of ['rail-text','rail-leise']) assert.ok(kontrast(tokens[text],tokens.rail) >= 4.5);
      for (const bg of ['rail-active','rail-hover']) assert.ok(kontrast(tokens['rail-text'],tokens[bg]) >= 4.5);
      assert.ok(kontrast(tokens['rail-fokus'],tokens.rail) >= 3);
      for (const text of ['text','leise']) assert.ok(kontrast(tokens[text],tokens['nachweis-bg']) >= 4.5);
    }
    if (palette[1] === 'c') {
      assert.ok(kontrast(tokens['offen-text'],tokens['offen-bg']) >= 4.5);
      assert.ok(kontrast(tokens['info-text'],tokens['info-bg']) >= 4.5);
      assert.ok(kontrast(tokens['auf-akzent'],tokens.akzent) >= 4.5);
    }
    for (const akzent of ['akzent','akzent-dunkel','zweite-farbe']) assert.ok(kontrast('#ffffff',tokens[akzent]) >= 4.5);
    for (const grenze of ['eingaberand','fokus']) assert.ok(kontrast(tokens[grenze],tokens.flaeche) >= 3);
  }
});


test('Integrierter Arbeitsraum nutzt Serverwerte, Filter und eigenständige Details mit Rückkehr', async () => {
  await mitAnsicht(uebersicht, async () => {
    assert.ok(document.querySelector('.studio'));
    assert.equal(document.querySelector('[data-design]'),null);
    assert.equal(document.querySelectorAll('nav a').length,5);
    assert.match(document.querySelector('.p-bilanz').textContent,/SERVER-SALDO/);
    await act(async () => [...document.querySelectorAll('.p-filter button')].find(b=>b.textContent==='Offen').click());
    assert.match(document.querySelector('.p-leer').textContent,/Keine passenden/);
    await act(async () => [...document.querySelectorAll('.p-filter button')].find(b=>b.textContent==='Teilweise bezahlt').click());
    const oeffnen=document.querySelector('.p-rowlink');
    await act(async () => oeffnen.click());
    assert.equal(document.querySelector('.p-detail h1').textContent,'RE-2026-1101');
    assert.match(document.querySelector('.p-detailbetraege').textContent,/SERVER-ZUORDNUNG/);
    assert.match(document.querySelector('.p-detail').textContent,/Zur Prüfung/);
    assert.doesNotMatch(document.querySelector('.p-detail').textContent,/synthetisch\/Zahlung/);
    await act(async () => document.querySelector('.p-zurueck').click());
    assert.equal(document.activeElement,oeffnen);
    assert.equal(document.querySelector('.p-filter [aria-pressed="true"]').textContent,'Teilweise bezahlt');
  },'http://127.0.0.1:5180/?vorschau=arbeitsraum',Designprototyp);
});

test('Integrierter Import erhält Quellen, escaped Originalwerte, Prüfbericht und echte Filter', async () => {
  await mitAnsicht(uebersicht, async () => {
    assert.match(document.querySelector('h1').textContent,/Excel-Datei importieren/);
    await act(async () => document.querySelector('.p-rowlink').click());
    assert.match(document.querySelector('.p-zellinspektor').textContent,/ISD-Testdaten!G2/);
    assert.match(document.querySelector('.p-zellinspektor').textContent,/<img src=x/);
    assert.equal(document.querySelector('.p-zellinspektor img'),null);
    assert.ok(document.querySelector('.p-dateiquelle pre').textContent.includes('A'.repeat(64)));
    assert.match(document.querySelector('.p-dateiquelle').textContent,/Kein Import/);
    await act(async () => { const f=document.querySelector('#p-prueffilter');f.value='Warnung';f.dispatchEvent(new window.Event('change',{bubbles:true})); });
    assert.match(document.querySelector('.p-leer').textContent,/Keine Datensätze/);
  },'http://127.0.0.1:5180/?vorschau=arbeitsraum#import',Designprototyp);
});

test('Integrierte Anwendung zeigt Verbindungsfehler und erlaubt erneuten Versuch', async () => {
  await mitAnsicht(uebersicht, async () => {
    globalThis.fetch=async()=>{throw new Error('NICHT-ANZEIGEN');};
    await act(async () => { window.location.hash='#import';window.dispatchEvent(new window.HashChangeEvent('hashchange')); });
    assert.match(document.querySelector('[role="alert"]').textContent,/Verbindung/);
    assert.doesNotMatch(document.body.textContent,/NICHT-ANZEIGEN/);
    globalThis.fetch=async url=>antwort(String(url).endsWith('/importe')?[]:vorschau);
    await act(async () => [...document.querySelectorAll('[role="alert"] button')].forEach(b=>b.click()));
    assert.equal(!!document.querySelector('[role=alert]'),false);
    await act(async () => document.querySelector('.p-collapse').click());
    assert.equal(document.querySelector('.studio').dataset.compact,'true');
    assert.equal(document.querySelector('nav a').getAttribute('aria-label'),'Rechnungen');
  },'http://127.0.0.1:5180/?vorschau=arbeitsraum',Designprototyp);
});


test('Regulärer Einstieg und historischer Importpfad verwenden dieselbe Anwendung', async () => {
  assert.equal(Designprototyp,App);
  await mitAnsicht(uebersicht, async () => {
    assert.ok(document.querySelector('.studio'));
    assert.equal(document.querySelector('.designschalter'),null);
    assert.match(document.querySelector('.p-register h2').textContent,/Rechnungsbestand/);
    assert.doesNotMatch(document.body.textContent,/Alle Vorgänge|Mit Variante C vergleichen|Nicht freigegeben/);
    assert.match(document.title,/Arbeitsplatz/);
    assert.equal(document.querySelectorAll('nav a').length,5);
    assert.match(document.querySelector('.p-tabelle thead').textContent,/Aussteller.*\/ Empfänger/);
  });
});

test('Zurück-Button und Breadcrumb erhalten Zahlungsfilter, Sortierung und Fokus', async () => {
  await mitAnsicht(uebersicht, async () => {
    await act(async () => [...document.querySelectorAll('.p-filter button')].find(b=>b.textContent==='Teilweise bezahlt').click());
    const sort=document.querySelector('#p-sortierung');
    await act(async () => { sort.value='empfaenger';sort.dispatchEvent(new window.Event('change',{bubbles:true})); });
    const ausloeser=document.querySelector('.p-rowlink');
    await act(async () => ausloeser.click());
    assert.equal(document.activeElement,document.querySelector('.p-detail h1'));
    const zurueck=document.querySelector('.p-zurueck');
    assert.equal(zurueck.textContent,'Zurück zur Rechnungsübersicht');
    assert.equal(document.querySelector('.p-breadcrumb [aria-current="page"]').textContent,'RE-2026-1101');
    await act(async () => zurueck.click());
    assert.equal(document.activeElement,ausloeser);
    assert.equal(document.querySelector('#p-sortierung').value,'empfaenger');
    assert.equal(document.querySelector('.p-filter [aria-pressed="true"]').textContent,'Teilweise bezahlt');
    await act(async () => ausloeser.click());
    await act(async () => document.querySelector('.p-breadcrumb button').click());
    assert.equal(document.activeElement,ausloeser);
    assert.equal(document.querySelector('#p-sortierung').value,'empfaenger');
  });
});

test('Rechnungsfilter, Sortieren und Zurücksetzen verändern echte Zeilen ohne Summenänderung', async () => {
  const a=uebersicht.rechnungen[0];
  const daten={...uebersicht,rechnungen:[a,{...a,nummer:'AAA',status:'Offen',empfaenger:'Firma B'}]};
  await mitAnsicht(daten,async()=>{
    await act(async()=>[...document.querySelectorAll('.p-filter button')].find(b=>b.textContent==='Bezahlt').click());
    assert.equal(document.querySelector('.p-tabelle'),null);
    assert.match(document.querySelector('.p-leer').textContent,/Keine passenden/);
    await act(async()=>document.querySelector('.p-leer button').click());
    assert.equal(document.querySelectorAll('.p-rowlink').length,2);
    const sort=document.querySelector('#p-sortierung');
    await act(async()=>{sort.value='empfaenger';sort.dispatchEvent(new window.Event('change',{bubbles:true}));});
    assert.match(document.querySelector('.p-rowlink').textContent,/AAA/);
    assert.match(document.querySelector('.p-bilanz').textContent,/SERVER-SALDO/);
  });
});

test('Rechnung ohne Vorgangskennung zeigt keine erfundene Dokumentenakte',async()=>{
 await mitAnsicht(uebersicht,async()=>{await act(async()=>document.querySelector('.p-rowlink').click());assert.match(document.querySelector('.p-nachweise').textContent,/keine digitale Akte/);assert.equal(document.querySelector('.p-nachweise iframe'),null);assert.match(document.querySelector('.p-detailbetraege').textContent,/SERVER-TEILBETRAG/);});
});


test('Leerer Rechnungsbestand bietet keinen nutzlosen Zurücksetzen-Button', async () => {
  await mitAnsicht({...uebersicht,rechnungen:[]},async()=>{
    assert.match(document.querySelector('.p-leer').textContent,/Noch keine Rechnungen/);
    assert.equal(document.querySelector('.p-leer button'),null);
  });
});

test('Importfilter und Tastatur bewegen nur den ausgewählten Tabellenbereich', async () => {
  await mitAnsicht(uebersicht,async()=>{
    await act(async()=>document.querySelector('.p-rowlink').click());
    await act(async()=>document.querySelector('.p-zurueck').click());
    const bereich=document.querySelector('.p-tabellenraum');
    await act(async()=>bereich.dispatchEvent(new window.KeyboardEvent('keydown',{key:'ArrowRight',bubbles:true})));
    assert.equal(bereich.scrollLeft,300);
    await act(async()=>{window.location.hash='#import';window.dispatchEvent(new window.HashChangeEvent('hashchange'));});
    const f=document.querySelector('#p-prueffilter');
    await act(async()=>{f.value='Warnung';f.dispatchEvent(new window.Event('change',{bubbles:true}));});
    assert.match(document.querySelector('.p-leer').textContent,/Keine Datensätze/);
    await act(async()=>{f.value='Fehler';f.dispatchEvent(new window.Event('change',{bubbles:true}));});
    await act(async()=>document.querySelector('.p-rowlink').click());
    const quell=document.querySelector('.p-quellscroll');
    await act(async()=>quell.dispatchEvent(new window.KeyboardEvent('keydown',{key:'ArrowRight',bubbles:true})));
    assert.equal(quell.scrollLeft,300);
    assert.equal(document.querySelector('.p-tabellenraum').scrollLeft,0);
    assert.match(document.querySelector('.p-importtabelle .status.fehler').textContent,/Fehler/);
  });
});

test('Laden sperrt erneute Prüfung bis zur Antwort; Sprunganker bewahrt Importansicht', async () => {
  await mitAnsicht(uebersicht,async()=>{
    globalThis.fetch=()=>new Promise(()=>{});
    await act(async()=>{window.location.hash='#import';window.dispatchEvent(new window.HashChangeEvent('hashchange'));});
    assert.equal(document.querySelector('button').disabled,false);
    assert.equal(document.querySelector('.p-dateieingang button').disabled,true);
    await act(async()=>{window.location.hash='#p-inhalt';window.dispatchEvent(new window.HashChangeEvent('hashchange'));});
    assert.match(document.querySelector('h1').textContent,/Excel-Datei importieren/);
    assert.equal(document.querySelector('nav a[aria-current="page"]').getAttribute('href'),'#import');
  });
});

test('HTTP-Ausfall der Rechnungen kann ohne Neuladen der Anwendung wiederholt werden', async () => {
  await mitAnsicht(uebersicht,async()=>{
    await act(async()=>{window.location.hash='#import';window.dispatchEvent(new window.HashChangeEvent('hashchange'));});
    globalThis.fetch=async()=>{throw new Error('INTERN');};
    await act(async()=>{window.location.hash='#rechnungen';window.dispatchEvent(new window.HashChangeEvent('hashchange'));});
    assert.match(document.querySelector('[role="alert"]').textContent,/lokale Verbindung/);
    globalThis.fetch=async()=>antwort(uebersicht);
    await act(async()=>document.querySelector('[role="alert"] button').click());
    assert.equal(document.querySelector('[role="alert"]'),null);
    assert.match(document.querySelector('.p-bilanz').textContent,/SERVER-SALDO/);
  });
});

test('Kontaktlinks kodieren Einzeladresse und Betreff, erlauben nur globale Telefonnummern', async () => {
  const { emailLink, telefonLink } = await import('../node_modules/.cache/betriebsplattform-tests/kontaktaktionen.js');
  assert.equal(emailLink('team+buero@example.invalid','SYNTH / 02 & Rückfrage'), 'mailto:team%2Bbuero@example.invalid?subject=Rechnung%20SYNTH%20%2F%2002%20%26%20R%C3%BCckfrage');
  assert.equal(telefonLink('+49 30 23125 001'), 'tel:+493023125001');
  for (const wert of [null,undefined,'','javascript:alert(1)','a@b.invalid?bcc=x@b.invalid','a@b.invalid\r\nBcc:x@b.invalid','a%0d%0a@b.invalid','a@b.invalid,c@d.invalid','a@b.invalid\n','a..b@b.invalid','a@-b.invalid','a@b..invalid','a'.repeat(65)+'@b.invalid']) assert.equal(emailLink(wert,'SYNTH'),null, String(wert));
  for (const wert of [null,undefined,'','030 23125001','+49 30\n23125001','+493023125001;ext=1','+493023125001?x=1','+1234567890123456','+0001234567','+49--3023125001','+493023125001\n']) assert.equal(telefonLink(wert),null,String(wert));
  for (const ref of ['SYNTH\r\nBcc:x','SYNTH\u0000','x'.repeat(129),'\ud800']) assert.equal(emailLink('a@example.invalid',ref),'mailto:a@example.invalid');
});

test('Optionale Kontakte erhalten alten Vertrag und weisen fehlerhafte Kontaktstrukturen ab', () => {
  const r=uebersicht.rechnungen[0];
  const mitKontakt=k=>({...uebersicht,rechnungen:[{...r,ausstellerKontakt:k,empfaengerKontakt:null}]});
  assert.equal(istUebersicht(uebersicht),true);
  assert.equal(istUebersicht(mitKontakt({ansprechpartner:null,email:'team@example.invalid',telefon:null,quelle:'synthetisch/Kontakt'})),true);
  for (const k of [[], 'falsch', {}, {ansprechpartner:null,email:42,telefon:null,quelle:'SYNTH'}]) assert.equal(istUebersicht(mitKontakt(k)),false);
});

test('Sofortaktionen zeigen Kontakt und Vorgangsrolle ohne Senden, Anruf oder Saldenänderung', async () => {
  const r={...uebersicht.rechnungen[0],ausstellerKontakt:{ansprechpartner:'Alex Beispiel (synthetisch)',email:'buero+nord@example.invalid',telefon:'+49 30 23125 001',quelle:'synthetisch/Kontakt-Nord'},empfaengerKontakt:{ansprechpartner:null,email:'isd@example.invalid',telefon:null,quelle:'synthetisch/Kontakt-ISD'}};
  await mitAnsicht({...uebersicht,rechnungen:[r]},async()=>{
    await act(async()=>document.querySelector('.p-rowlink').click());
    const aus=document.querySelector('.p-kontakte');
    const emp=document.querySelector('.p-aktionszeile');
    assert.match(aus.textContent,/Firma A.*Alex Beispiel/s);
    assert.match(emp.textContent,/ISD/);
    assert.equal(document.querySelector('select[aria-label="Kontakt für Aktionen"]').options.length,2);
    assert.equal(emp.querySelector('a[href^="mailto:"]').getAttribute('href'),'mailto:buero%2Bnord@example.invalid?subject=Rechnung%20RE-2026-1101');
    assert.equal(emp.querySelector('a[href^="tel:"]').getAttribute('href'),'tel:+493023125001');
    assert.equal(emp.querySelectorAll('a').length,2);
    assert.ok(emp.querySelector('.icon-email')); assert.ok(emp.querySelector('.icon-telefon'));
    assert.match(document.querySelector('.p-arbeitskontext').textContent,/Büroarbeitsplatz/);
    assert.match(document.querySelector('.p-detailbetraege').textContent,/SERVER-ZUORDNUNG/);
    assert.match(document.querySelector('.p-detail .status.warnung').textContent,/Zur Prüfung/);
    await act(async()=>document.querySelector('.p-zurueck').click());
    assert.equal(document.activeElement,document.querySelector('.p-rowlink'));
  });
});

test('Fehlende oder ungültige Kontaktdaten erzeugen keine Aktionslinks oder HTML',async()=>{
  for (const kontakt of [null, {ansprechpartner:'<img src=x onerror=alert(1)>',email:'x@example.invalid?bcc=evil',telefon:'javascript:alert(1)',quelle:'synthetisch/Test'}]) {
    await mitAnsicht({...uebersicht,rechnungen:[{...uebersicht.rechnungen[0],ausstellerKontakt:kontakt}]},async()=>{
      await act(async()=>document.querySelector('.p-rowlink').click());
      const kontaktbereich=document.querySelector('.p-aktionszeile');
      assert.ok(kontaktbereich);
      assert.equal(kontaktbereich.querySelector('a,img'),null);
      assert.equal(kontaktbereich.querySelectorAll('button:disabled').length,3);
      assert.ok(kontaktbereich.querySelector('[aria-label="Keine nutzbare E-Mail-Adresse"]'));
      assert.ok(kontaktbereich.querySelector('[aria-label="Keine nutzbare Telefonnummer"]'));
      if(kontakt) assert.equal(kontaktbereich.querySelector('img'),null);
    });
  }
});

test('Rechnungsgrundlage und einzelner Zahlungsnachweis bleiben eindeutig und ohne doppelten Leerhinweis',async()=>{
  await mitAnsicht(uebersicht,async()=>{
    await act(async()=>document.querySelector('.p-rowlink').click());
    const detail=document.querySelector('.p-detail');
    assert.equal(document.querySelector('.p-rechnungsgrundlage'),null);
    assert.equal(document.querySelector('.p-zahlungsnachweis'),null);
    assert.equal(detail.textContent.split('synthetisch/Test').length-1,0);
    assert.equal(detail.textContent.split('synthetisch/Zahlung').length-1,0);
    assert.equal(detail.textContent.split('keine digitale Akte').length-1,1);
    assert.doesNotMatch(detail.textContent,/Es sind keine Dokumentdateien hinterlegt/);
  });
});

test('Zentrale Akte zeigt Original und Zahlungsbeleg getrennt und erhält Salden und Rückweg',async()=>{
 const r={...uebersicht.rechnungen[0],vorgangId:'demo-nord-rechnung-002',perspektive:{unternehmen:'ISD',rolle:'Empfänger'},zahlungen:[{...uebersicht.rechnungen[0].zahlungen[0],id:'demo-zahlung-002',status:'Bestätigt (synthetisch)'}]};
 const meta={unternehmenId:'DEMO-NORD',vorgangId:r.vorgangId,datum:'2026-10-17',version:1,bytezahl:999,dateiHash:'A'.repeat(64),quelle:'synthetisch/Test'};
 const katalog={synthetisch:true,vorgangId:r.vorgangId,dokumente:[{...meta,id:'original-002-v1',art:'Originalrechnung',dateiname:'Original.pdf',zahlungId:null},{...meta,id:'zahlung-002-v1',art:'Zahlungsbeleg',dateiname:'Zahlung.pdf',zahlungId:'demo-zahlung-002'}]};
 const a={vorgang:{id:r.vorgangId,titel:r.nummer,kategorie:'Rechnung'},dokumente:katalog.dokumente.map(d=>({...d,titel:d.art,erfasstAm:'2026-10-17T12:00:00Z',vorschau:'pdf',vorgaenge:[r.vorgangId]})),notizen:[],korrespondenz:[],kontaktvorschlaege:[]};
 await mitAnsicht({...uebersicht,rechnungen:[r]},async()=>{
  globalThis.fetch=async url=>String(url).includes('/api/akten/')?antwort(a):String(url).endsWith('/dokumente')?antwort(katalog):{ok:true,headers:new Headers({'Content-Type':'application/pdf'}),arrayBuffer:async()=>new ArrayBuffer(3)};
  await act(async()=>document.querySelector('.p-rowlink').click());
  assert.match(document.querySelector('.p-detailbetraege').textContent,/SERVER-ZUORDNUNG.*SERVER-TEILBETRAG/s);assert.match(document.querySelector('.p-zahlungsfolge').textContent,/Eigene Zahlung/);
  assert.deepEqual([...document.querySelectorAll('.p-akteneintrag strong')].map(b=>b.textContent),['Originalrechnung','Zahlungsbeleg']);
  await act(async()=>document.querySelector('.p-textaktion').click());assert.match(document.querySelector('iframe').src,/zahlung-002-v1#view=FitH$/);
  await act(async()=>document.querySelector('.p-vorschaukopf button').click());assert.ok(document.querySelector('dialog[open] iframe'));
  await act(async()=>document.querySelector('dialog button').click());assert.equal(document.querySelector('dialog'),null);assert.equal(document.activeElement.textContent,'Vergrößern');
  await act(async()=>[...document.querySelectorAll('.p-vorschaukopf button')].find(b=>b.textContent==='Schließen').click());assert.equal(document.querySelector('iframe'),null);
  await act(async()=>document.querySelector('.p-zurueck').click());assert.equal(document.activeElement,document.querySelector('.p-rowlink'));
 });
});


test('Aktenfehler und fremde Antworten bleiben verständlich und wiederholbar',async()=>{
 const r={...uebersicht.rechnungen[0],vorgangId:'demo-nord-rechnung-002'};
 await mitAnsicht({...uebersicht,rechnungen:[r]},async()=>{globalThis.fetch=async()=>({ok:false});await act(async()=>document.querySelector('.p-rowlink').click());assert.ok(document.querySelector('.p-aktenbereich [role="alert"]'));
 globalThis.fetch=async()=>antwort({vorgang:{id:'fremd',titel:'Fremd',kategorie:'Rechnung'},dokumente:[],notizen:[],korrespondenz:[],kontaktvorschlaege:[]});await act(async()=>document.querySelector('.p-aktenbereich [role="alert"] button').click());assert.match(document.querySelector('.p-aktenbereich [role="alert"]').textContent,/gehört nicht/);
 globalThis.fetch=async()=>antwort({vorgang:{id:r.vorgangId,titel:r.nummer,kategorie:'Rechnung'},dokumente:[],notizen:[],korrespondenz:[],kontaktvorschlaege:[]});await act(async()=>document.querySelector('.p-aktenbereich [role="alert"] button').click());assert.match(document.querySelector('.p-aktenbereich').textContent,/Noch keine Dokumente/);});
});


test('Fehlende oder widersprüchliche Perspektive erfindet keinen Zahlungseingang oder -ausgang',async()=>{
  const {eigeneRolle,zahlungsrichtung}=await import('../node_modules/.cache/betriebsplattform-tests/darstellung.js');
  const r=uebersicht.rechnungen[0],z=r.zahlungen[0];
  assert.equal(eigeneRolle(r),null);
  assert.equal(eigeneRolle({...r,perspektive:{unternehmen:'fremd',rolle:'Empfänger'}}),null);
  assert.equal(zahlungsrichtung(r,z),null);
  const aus={...r,perspektive:{unternehmen:r.aussteller,rolle:'Aussteller'}};
  assert.equal(zahlungsrichtung(aus,z),'Zahlungseingang zur Prüfung');
  assert.equal(zahlungsrichtung(aus,{...z,status:'Bestätigt (synthetisch)'}),'Zahlung erhalten');
});

test('Dokumentkatalog weist doppelte, fremde und unvollständige Metadaten zurück',async()=>{
  const {istDokumentenliste}=await import('../node_modules/.cache/betriebsplattform-tests/api.js');
  const d={id:'original-002-v1',unternehmenId:'DEMO-NORD',vorgangId:'demo-nord-rechnung-002',art:'Originalrechnung',dateiname:'Original.pdf',datum:'2026-10-17',version:1,bytezahl:1000,dateiHash:'A'.repeat(64),quelle:'synthetisch/Test',zahlungId:null};
  const liste=(docs)=>({synthetisch:true,vorgangId:d.vorgangId,dokumente:docs});
  assert.equal(istDokumentenliste(liste([d])),true);
  for(const falsch of [{id:'original-002-v1\n'},{unternehmenId:'FREMD'},{vorgangId:'fremd'},{bytezahl:0},{dateiHash:'kein-hash'},{dateiHash:'A'.repeat(64)+'\n'},{zahlungId:'unpassend'},{art:'Zahlungsbeleg',zahlungId:null}]) assert.equal(istDokumentenliste(liste([{...d,...falsch}])),false);
  assert.equal(istDokumentenliste(liste([d,d])),false);
});


test('Eine Aktionsgruppe wählt mehrere Empfänger ohne automatischen Versand',async()=>{
 const r={...uebersicht.rechnungen[0],ausstellerKontakt:{ansprechpartner:'Alex',email:'a@example.invalid',telefon:null,emails:['a@example.invalid','b@example.invalid'],quelle:'synthetisch/Kontakt'},empfaengerKontakt:{ansprechpartner:null,email:'isd@example.invalid',telefon:null,quelle:'synthetisch/Kontakt'}};
 await mitAnsicht({...uebersicht,rechnungen:[r]},async()=>{await act(async()=>document.querySelector('.p-rowlink').click());assert.equal(document.querySelectorAll('.p-kontaktaktionen').length,1);const select=document.querySelector('[aria-label="E-Mail-Empfänger"]');await act(async()=>{select.value='b@example.invalid';select.dispatchEvent(new window.Event('change',{bubbles:true}));});assert.match(document.querySelector('a[href^="mailto:"]').href,/b@example.invalid/);const firma=document.querySelector('[aria-label="Kontakt für Aktionen"]');await act(async()=>{firma.value='an';firma.dispatchEvent(new window.Event('change',{bubbles:true}));});assert.match(document.querySelector('a[href^="mailto:"]').href,/isd@example.invalid/);assert.equal(document.querySelector('[aria-label="Notiz erstellen"]').disabled,true);});
});


test('Echte Dateiauswahl führt über Mapping und ausdrückliche Zeilenauswahl zum Demoimportbericht', async()=>{
  const zeile={...vorschau.zeilen[0],hatFehler:false,art:'Rechnung',betrag:'100,25 €',datum:'2026-10-17',von:'Beispielbetrieb Nord',an:'ISD',meldungen:[]};
  const v={id:'a'.repeat(32),synthetisch:true,blatt:'ISD-Testdaten',spalten:['DatensatzId','Art','Nummer','Von','An','Datum','Betrag','Waehrung','Beleg'],firmen:['Firma A','Firma B'],mapping:{},mappingBestaetigt:false,vorschau:{...vorschau,hatFehler:false,fehlerzeilen:0,zeilen:[zeile]},ergebnis:null,entschiedenAm:null,auswahl:[]};
  await mitAnsicht(uebersicht,async()=>{
    const aufrufe=[];
    globalThis.fetch=async(url,options)=>{
      if(options?.method==='POST')aufrufe.push([url,options]);
      if(String(url).endsWith('/importe'))return antwort([]);
      if(String(url).endsWith('/mapping')) return antwort({...v,mapping:{'Firma A':'DEMO-NORD','Firma B':'DEMO-ISD'},mappingBestaetigt:true});
      if(String(url).endsWith('/uebernehmen')) return antwort({...v,mappingBestaetigt:true,auswahl:[2],entschiedenAm:'2026-10-18T12:00:00Z',ergebnis:{uebernommen:1,uebersprungen:0,fehlerhaft:0,zurPruefung:0}});
      return antwort(v);
    };
    const upload=[...document.querySelectorAll('button')].find(b=>b.textContent.includes('Excel-Datei auswählen'));
    assert.equal(upload.disabled,true);
    await act(async()=>document.querySelector('.p-upload input[type="checkbox"]').click());
    assert.equal(upload.disabled,false);
    const file=new window.File(['synthetische Testbytes'],'SYNTH-test.xlsx');
    const feld=document.querySelector('input[type="file"]');
    Object.defineProperty(feld,'files',{configurable:true,value:[file]});
    await act(async()=>feld.dispatchEvent(new window.Event('change',{bubbles:true})));
    await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent==='Datei prüfen').click());
    assert.equal(aufrufe[0][0],'/api/importe/vorschau');
    assert.equal(aufrufe[0][1].body,file);
    assert.equal(new Headers(aufrufe[0][1].headers).get('X-Dateiname'),'SYNTH-test.xlsx');
    for(const [index,wert] of [[0,'DEMO-NORD'],[1,'DEMO-ISD']]) await act(async()=>{
      const select=document.getElementById(`firma-${index}`);select.value=wert;select.dispatchEvent(new window.Event('change',{bubbles:true}));
    });
    await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent==='Blatt und Firmenzuordnung bestätigen').click());
    assert.deepEqual(JSON.parse(aufrufe[1][1].body),{blatt:'ISD-Testdaten',unternehmen:{'Firma A':'DEMO-NORD','Firma B':'DEMO-ISD'},bestaetigt:true});
    const auswahl=document.querySelector('[aria-label="Rechnung aus Zeile 2 auswählen"]');
    await act(async()=>auswahl.click());
    const uebernehmen=document.querySelector('.p-uebernahme button');
    assert.equal(uebernehmen.disabled,true);
    await act(async()=>document.querySelector('.p-uebernahme input').click());
    await act(async()=>uebernehmen.click());
    assert.deepEqual(JSON.parse(aufrufe[2][1].body),{zeilen:[2],bestaetigt:true});
    assert.match(document.querySelector('.p-importbericht').textContent,/Übernommen1/);
    assert.ok(document.querySelector('a[href="#rechnungen"]'));
    assert.equal(document.querySelector('.p-uebernahme'),null);
  },'http://127.0.0.1:5180/#import');
});

test('Dateigrenzen und defekte Importantworten erzeugen keinen Erfolgsbericht',async()=>{
  const {dateiPruefen,istDemoimport}=await import('../node_modules/.cache/betriebsplattform-tests/api.js');
  const original=globalThis.fetch;let anzahl=0;
  try{
    globalThis.fetch=async()=>{anzahl++;return antwort({synthetisch:true});};
    for(const datei of [{name:'test.xlsm',size:4},{name:'test.xlsx',size:0},{name:'test.xlsx',size:2*1024*1024+1}]) await assert.rejects(dateiPruefen(datei),/XLSX/);
    assert.equal(anzahl,0);
    await assert.rejects(dateiPruefen({name:'test.xlsx',size:4}),/Demoformat/);
    assert.equal(istDemoimport({synthetisch:true,id:'../'}),false);
  }finally{globalThis.fetch=original;}
});

test('PDF-Fehler erlaubt erneutes Laden ohne Navigation oder Originaländerung',async()=>{
 const {PdfVorschau}=await import('../node_modules/.cache/betriebsplattform-tests/Dokumente.js');
 const Komponente=()=>createElement(PdfVorschau,{url:'/api/vorgaenge/demo/dokumente/test',titel:'Originalrechnung · Test.pdf'});
 await mitAnsicht(uebersicht,async()=>{assert.match(document.querySelector('[role="alert"]').textContent,/PDF nicht verfügbar/);assert.equal(document.querySelector('iframe'),null);globalThis.fetch=async()=>({ok:true,headers:new Headers({'Content-Type':'application/pdf'}),arrayBuffer:async()=>new ArrayBuffer(3)});await act(async()=>document.querySelector('[role="alert"] button').click());assert.ok(document.querySelector('iframe'));assert.equal(window.location.hash,'');},'http://127.0.0.1:5180/',Komponente);
});


test('Sidebar Rechnungen öffnet aus der Detailansicht die erhaltene Übersicht',async()=>{
  await mitAnsicht(uebersicht,async()=>{
    await act(async()=>document.querySelector('.p-rowlink').click());
    assert.ok(document.querySelector('.p-detail'));
    await act(async()=>document.querySelector('nav[aria-label="Hauptnavigation"] a[href="#rechnungen"]').click());
    assert.equal(!!document.querySelector('.p-detail'),false);
    assert.equal(document.querySelector('#p-suche').value,'');
  });
});

test('Browserdruck ändert keine Rechnungswerte und ungezielter Datei-Drop wird nicht verarbeitet',async()=>{
 await mitAnsicht(uebersicht,async()=>{let druck=0;window.print=()=>druck++;await act(async()=>document.querySelector('.p-rowlink').click());const vorher=document.querySelector('.p-detailbetraege').textContent;await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent==='Akte drucken').click());assert.equal(druck,1);assert.equal(document.querySelector('.p-detailbetraege').textContent,vorher);const drop=new window.Event('drop',{bubbles:true,cancelable:true});Object.defineProperty(drop,'dataTransfer',{value:{types:['Files'],files:[new window.File(['Text'],'Brief.txt')]}});await act(async()=>document.querySelector('h1').dispatchEvent(drop));assert.equal(drop.defaultPrevented,true);assert.match(document.querySelector('main>[role=status]').textContent,/Datei nicht abgelegt/);assert.equal(document.querySelector('.p-detailbetraege').textContent,vorher);});
});

test('UX-Delta Kennzahlen zeigen getrennte Serverwerte und natürliche Rechnungsanzahl',async()=>{await mitAnsicht({...uebersicht,offeneForderungen:'450,00 €',offeneVerbindlichkeiten:'460,00 €'},async()=>{const text=document.querySelector('.p-bilanz').textContent;assert.match(text,/Offene Forderungen450,00/);assert.match(text,/Offene Verbindlichkeiten460,00/);assert.match(text,/1 Rechnung/);assert.doesNotMatch(text,/Offene Positionen|SERVER-SALDO/);});});

test('Datenqualität Zahlungsbestätigung verständlich',async()=>{const {zahlungsstatus}=await import('../node_modules/.cache/betriebsplattform-tests/darstellung.js');assert.equal(zahlungsstatus('Bestätigt').text,'Zahlung bestätigt');assert.equal(zahlungsstatus('Bestätigt (synthetisch)').text,'Zahlung bestätigt');assert.equal(zahlungsstatus('Bezahlt').text,'Bezahlt');});
