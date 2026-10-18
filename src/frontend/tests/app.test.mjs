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
  rechnungen: [{ nummer: 'SYNTH-TEST', datum: '2026-10-17', aussteller: 'Firma A', empfaenger: 'ISD', betrag: '1.000,00 €',
    offenerBetrag: 'SERVER-TEILBETRAG', bestaetigtZugeordnet: 'SERVER-ZUORDNUNG', status: 'Teilzahlung', beleg: 'SYNTH-BELEG', quellenort: 'synthetisch/Test',
    zahlungen: [{ datum: '2026-10-17', zahler: 'ISD', empfaenger: 'Firma A', betrag: '300,00 €', zugeordnet: '300,00 €', status: 'Zur Prüfung', beleg: 'SYNTH-ZAHLUNG', quellenort: 'synthetisch/Zahlung' }] }],
};
const vorschau = {
  synthetisch: true, dateiHash: 'A'.repeat(64), hatFehler: true, zeilenanzahl: 1, fehlerzeilen: 1, bericht: 'Kein Import. Zur Prüfung.',
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
  assert.deepEqual([a,b].map(r => r.nummer), ['SYNTH-TEST','AAA']);
});

test('Status trennt offene Rechnungen, Bestätigung und Importprüfung', async () => {
  const { zahlungsstatus, importstatus, datumAnzeigen, regionen } = await import('../node_modules/.cache/betriebsplattform-tests/darstellung.js');
  assert.equal(zahlungsstatus('Offen').ton, 'neutral');
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
  globalThis.fetch = async url => antwort(String(url).includes('importvorschau') ? vorschau : daten);
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
    assert.equal(document.querySelectorAll('nav a').length,2);
    assert.match(document.querySelector('.p-bilanz').textContent,/SERVER-SALDO/);
    await act(async () => [...document.querySelectorAll('.p-filter button')].find(b=>b.textContent==='Offen').click());
    assert.match(document.querySelector('.p-leer').textContent,/Keine passenden/);
    await act(async () => [...document.querySelectorAll('.p-filter button')].find(b=>b.textContent==='Teilweise bezahlt').click());
    const oeffnen=document.querySelector('.p-rowlink');
    await act(async () => oeffnen.click());
    assert.equal(document.querySelector('.p-detail h1').textContent,'SYNTH-TEST');
    assert.match(document.querySelector('.p-detailbetraege').textContent,/SERVER-ZUORDNUNG/);
    assert.match(document.querySelector('.p-detail').textContent,/Zur Prüfung/);
    assert.match(document.querySelector('.p-detail').textContent,/synthetisch\/Zahlung/);
    await act(async () => document.querySelector('.p-zurueck').click());
    assert.equal(document.activeElement,oeffnen);
    assert.equal(document.querySelector('.p-filter [aria-pressed="true"]').textContent,'Teilweise bezahlt');
  },'http://127.0.0.1:5180/?vorschau=arbeitsraum',Designprototyp);
});

test('Integrierter Import erhält Quellen, escaped Originalwerte, Prüfbericht und echte Filter', async () => {
  await mitAnsicht(uebersicht, async () => {
    assert.match(document.querySelector('h1').textContent,/Excel-Importvorschau/);
    await act(async () => document.querySelector('.p-rowlink').click());
    assert.match(document.querySelector('.p-zellinspektor').textContent,/ISD-Testdaten!G2/);
    assert.match(document.querySelector('.p-zellinspektor').textContent,/<img src=x/);
    assert.equal(document.querySelector('.p-zellinspektor img'),null);
    assert.equal(document.querySelector('.p-hash').textContent,'A'.repeat(64));
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
    globalThis.fetch=async()=>antwort(vorschau);
    await act(async () => document.querySelector('[role="alert"] button').click());
    assert.ok(document.querySelector('.p-pruefraster'));
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
    assert.match(document.title,/Lokaler Arbeitsraum/);
    assert.equal(document.querySelectorAll('nav a').length,2);
    assert.match(document.querySelector('.p-tabelle thead').textContent,/Rechnung von \/ an/);
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
    assert.equal(document.querySelector('.p-breadcrumb [aria-current="page"]').textContent,'SYNTH-TEST');
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

test('Dokumentenbereich ist ausdrücklich vorbereitet und enthält keine Schein-Anhänge', async () => {
  await mitAnsicht(uebersicht,async()=>{
    await act(async()=>document.querySelector('.p-rowlink').click());
    const akte=document.querySelector('[aria-label="Dokumentenakte"]');
    assert.match(akte.textContent,/keine Originaldateien/);
    assert.match(akte.textContent,/noch nicht verfügbar/);
    assert.equal(akte.querySelector('a,button,input,iframe,object'),null);
    assert.match(document.querySelector('.p-detailbetraege').textContent,/SERVER-TEILBETRAG/);
    assert.match(document.querySelector('.p-detail .status.warnung').textContent,/Zur Prüfung/);
  });
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
    assert.match(document.querySelector('[role="status"]').textContent,/werden geladen/);
    assert.equal(document.querySelector('.p-dateiauswahl button').disabled,true);
    await act(async()=>{window.location.hash='#p-inhalt';window.dispatchEvent(new window.HashChangeEvent('hashchange'));});
    assert.match(document.querySelector('h1').textContent,/Excel-Importvorschau/);
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
    const aus=document.querySelector('[aria-label="Kontakt Rechnungsaussteller"]');
    const emp=document.querySelector('[aria-label="Kontakt Rechnungsempfänger"]');
    assert.match(aus.textContent,/Firma A.*Alex Beispiel/s);
    assert.match(emp.textContent,/ISD/);
    assert.match(emp.textContent,/Keine Telefonnummer hinterlegt/);
    assert.equal(aus.querySelector('a[href^="mailto:"]').getAttribute('href'),'mailto:buero%2Bnord@example.invalid?subject=Rechnung%20SYNTH-TEST');
    assert.equal(aus.querySelector('a[href^="tel:"]').getAttribute('href'),'tel:+493023125001');
    assert.equal(aus.querySelectorAll('a').length,2);
    assert.ok(aus.querySelector('.icon-email')); assert.ok(aus.querySelector('.icon-telefon'));
    assert.match(document.querySelector('.p-kontakthinweis').textContent,/keine Nachricht versendet/);
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
      const kontaktbereich=document.querySelector('.p-kontakte');
      assert.ok(kontaktbereich);
      assert.equal(kontaktbereich.querySelector('a,button,img'),null);
      assert.match(kontaktbereich.textContent,/E-Mail-Adresse/);
      assert.match(kontaktbereich.textContent,/Telefonnummer/);
      if(kontakt) assert.match(kontaktbereich.textContent,/nicht nutzbar/);
    });
  }
});

test('Rechnungsgrundlage und einzelner Zahlungsnachweis bleiben eindeutig und ohne doppelten Leerhinweis',async()=>{
  await mitAnsicht(uebersicht,async()=>{
    await act(async()=>document.querySelector('.p-rowlink').click());
    const detail=document.querySelector('.p-detail');
    assert.match(document.querySelector('.p-rechnungsgrundlage').textContent,/SYNTH-BELEG.*synthetisch\/Test/s);
    assert.match(document.querySelector('.p-zahlungsnachweis').textContent,/SYNTH-ZAHLUNG.*synthetisch\/Zahlung/s);
    assert.equal(detail.textContent.split('synthetisch/Test').length-1,1);
    assert.equal(detail.textContent.split('synthetisch/Zahlung').length-1,1);
    assert.equal(detail.textContent.split('keine Originaldateien').length-1,1);
    assert.doesNotMatch(detail.textContent,/Es sind keine Dokumentdateien hinterlegt/);
  });
});

test('DOK-01 zeigt kompakte Kontakte und zugeordnete echte Dokumentlinks ohne Finanzänderung',async()=>{
  const r={...uebersicht.rechnungen[0],vorgangId:'demo-nord-rechnung-002',perspektive:{unternehmen:'ISD',rolle:'Empfänger'},zahlungen:[{...uebersicht.rechnungen[0].zahlungen[0],id:'demo-zahlung-002',status:'Bestätigt (synthetisch)'}],ausstellerKontakt:{ansprechpartner:'Alex',email:'a@example.invalid',telefon:'+49 30 23125 001',quelle:'synthetisch/Kontakt'}};
  const basis={unternehmenId:'DEMO-NORD',vorgangId:r.vorgangId,datum:'2026-10-17',version:1,bytezahl:999,dateiHash:'A'.repeat(64),quelle:'synthetisch/DOK-01'};
  const dokumente={synthetisch:true,vorgangId:r.vorgangId,dokumente:[{...basis,id:'original-002-v1',art:'Originalrechnung',dateiname:'Original.pdf',zahlungId:null},{...basis,id:'zahlung-002-v1',art:'Zahlungsbeleg',dateiname:'Zahlung.pdf',zahlungId:'demo-zahlung-002'}]};
  await mitAnsicht({...uebersicht,rechnungen:[r]},async()=>{
    globalThis.fetch=async()=>antwort(dokumente);
    await act(async()=>document.querySelector('.p-rowlink').click());
    assert.match(document.querySelector('.p-detailbetraege').textContent,/Bestätigt bezahlt.*SERVER-ZUORDNUNG.*Noch zu bezahlen.*SERVER-TEILBETRAG/s);
    assert.match(document.querySelector('.p-zahlungsfolge').textContent,/Eigene Zahlung/);
    const original=document.querySelector('.p-dokumente a[href]');
    const zahlung=document.querySelector('.p-zahlungsfolge .p-pdf a[href]');
    assert.match(original.getAttribute('href'),/\/demo-nord-rechnung-002\/dokumente\/original-002-v1$/);
    assert.match(zahlung.getAttribute('href'),/\/demo-nord-rechnung-002\/dokumente\/zahlung-002-v1$/);
    assert.equal(original.target,'_blank');assert.match(original.rel,/noopener/);
    assert.equal(document.querySelectorAll('a[href$="original-002-v1"]').length,1);
    assert.equal(document.querySelectorAll('a[href$="zahlung-002-v1"]').length,1);
    const mail=document.querySelector('.p-kontaktaktionen a[href^="mailto:"]');
    assert.match(mail.getAttribute('aria-label'),/Firma A/);
    assert.ok(mail.querySelector('[role="tooltip"]'));
    assert.ok(document.querySelector('.p-kontaktkarte details summary'));
    assert.equal(document.querySelector('iframe,object'),null);
    await act(async()=>document.querySelector('.p-zurueck').click());
    assert.equal(document.activeElement,document.querySelector('.p-rowlink'));
  });
});

test('Dokumentenfehler sind verständlich und wiederholbar, fremde Referenzen werden abgewiesen',async()=>{
  const {istDokumentenliste}=await import('../node_modules/.cache/betriebsplattform-tests/api.js');
  assert.equal(istDokumentenliste({synthetisch:false,vorgangId:'x',dokumente:[]}),false);
  assert.equal(istDokumentenliste({synthetisch:true,vorgangId:'x',dokumente:[{id:'../../x'}]}),false);
  const r={...uebersicht.rechnungen[0],vorgangId:'demo-nord-rechnung-002'};
  await mitAnsicht({...uebersicht,rechnungen:[r]},async()=>{
    globalThis.fetch=async()=>({ok:false});
    await act(async()=>document.querySelector('.p-rowlink').click());
    assert.match(document.querySelector('.p-dokumente [role="alert"]').textContent,/Dokumente/);
    assert.equal(document.querySelector('.p-dokumente a'),null);
    globalThis.fetch=async()=>antwort({synthetisch:true,vorgangId:'fremder-vorgang',dokumente:[]});
    await act(async()=>document.querySelector('.p-dokumente button').click());
    assert.ok(document.querySelector('.p-dokumente [role="alert"]'));
    globalThis.fetch=async()=>antwort({synthetisch:true,vorgangId:r.vorgangId,dokumente:[]});
    await act(async()=>document.querySelector('.p-dokumente button').click());
    assert.match(document.querySelector('.p-dokumente').textContent,/keine Originaldateien/);
  });
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
