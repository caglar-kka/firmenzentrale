import { test } from 'node:test';
import assert from 'node:assert/strict';
import { JSDOM } from 'jsdom';
import { readFileSync } from 'node:fs';
import { act, createElement } from 'react';
import { createRoot } from 'react-dom/client';
import App from '../node_modules/.cache/betriebsplattform-tests/App.js';
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

test('Oberfläche zeigt Serversalden, Navigation, Quellen und escaped Originalwerte', async () => {
  const dom = new JSDOM('<!doctype html><div id="root"></div>', { url: 'http://127.0.0.1:5180/' });
  const originalFetch = globalThis.fetch;
  globalThis.window = dom.window;
  globalThis.document = dom.window.document;
  globalThis.IS_REACT_ACT_ENVIRONMENT = true;
  globalThis.fetch = async url => antwort(String(url).includes('importvorschau') ? vorschau : uebersicht);
  const root = createRoot(document.getElementById('root'));
  try {
    await act(async () => { root.render(createElement(App)); });
    assert.match(document.body.textContent, /SERVER-SALDO/);
    assert.match(document.body.textContent, /SERVER-TEILBETRAG/);
    assert.match(document.body.textContent, /Zur Prüfung/);
    assert.equal(document.querySelector('.detailzeile').hidden, true);
    await act(async () => document.querySelector('.textknopf').click());
    assert.equal(document.querySelector('.textknopf').getAttribute('aria-expanded'), 'true');
    assert.equal(document.querySelector('.detailzeile').hidden, false);
    assert.match(document.querySelector('.detailzeile').textContent, /SYNTH-ZAHLUNG/);
    assert.match(document.body.textContent, /SERVER-ZUORDNUNG/);
    const region = document.querySelector('.listen-scroll');
    await act(async () => region.dispatchEvent(new window.KeyboardEvent('keydown', {key:'ArrowRight',bubbles:true})));
    assert.equal(region.scrollLeft,300);
    await act(async () => region.dispatchEvent(new window.KeyboardEvent('keydown', {key:'ArrowLeft',bubbles:true})));
    assert.equal(region.scrollLeft,0);
    await act(async () => {
      window.location.hash = '#import';
      window.dispatchEvent(new window.HashChangeEvent('hashchange'));
    });
    assert.match(document.body.textContent, /Excel-Importvorschau/);
    assert.equal(document.querySelector('nav a[href="#import"]').textContent,'Importvorschau');
    assert.match(document.querySelector('.schritte').textContent,/Datensätze prüfen/);
    assert.match(document.body.textContent, /Betrag prüfen/);
    await act(async () => document.querySelector('.importtabelle .textknopf').click());
    assert.equal(document.querySelector('.importtabelle .detailzeile').hidden, false);
    const quellRegion = document.querySelector('.tabellenbereich');
    await act(async () => quellRegion.dispatchEvent(new window.KeyboardEvent('keydown', {key:'ArrowRight',bubbles:true})));
    assert.equal(quellRegion.scrollLeft,300);
    assert.equal(document.querySelector('.listen-scroll').scrollLeft,0);
    assert.match(document.body.textContent, /ISD-Testdaten!G2/);
    assert.match(document.body.textContent, /<img src=x onerror=alert\(1\)>/);
    assert.equal(document.querySelector('img'), null);
    assert.equal(document.querySelector('nav a[aria-current="page"]').getAttribute('href'), '#import');
    await act(async () => {
      window.location.hash = '#inhalt';
      window.dispatchEvent(new window.HashChangeEvent('hashchange'));
    });
    assert.match(document.querySelector('main').textContent, /Excel-Importvorschau/);
    assert.equal(document.querySelector('nav a[aria-current="page"]').getAttribute('href'), '#import');
  } finally {
    await act(async () => { root.unmount(); });
    globalThis.fetch = originalFetch;
    delete globalThis.window; delete globalThis.document; delete globalThis.IS_REACT_ACT_ENVIRONMENT;
    dom.window.close();
  }
});

test('Oberfläche bietet nach Backend-Ausfall einen funktionierenden erneuten Versuch', async () => {
  const dom = new JSDOM('<!doctype html><div id="root"></div>', { url: 'http://127.0.0.1:5180/' });
  const originalFetch = globalThis.fetch;
  globalThis.window = dom.window; globalThis.document = dom.window.document; globalThis.IS_REACT_ACT_ENVIRONMENT = true;
  globalThis.fetch = async () => { throw new Error('SYNTH-Ausfall'); };
  const root = createRoot(document.getElementById('root'));
  try {
    await act(async () => { root.render(createElement(App)); });
    assert.match(document.querySelector('[role="alert"]').textContent, /lokale Verbindung/);
    globalThis.fetch = async () => antwort(uebersicht);
    await act(async () => { document.querySelector('[role="alert"] button').click(); });
    assert.match(document.body.textContent, /SERVER-SALDO/);
    assert.equal(document.querySelector('[role="alert"]'), null);
  } finally {
    await act(async () => { root.unmount(); }); globalThis.fetch = originalFetch;
    delete globalThis.window; delete globalThis.document; delete globalThis.IS_REACT_ACT_ENVIRONMENT;
    dom.window.close();
  }
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


async function mitAnsicht(daten, pruefen) {
  const dom = new JSDOM('<!doctype html><div id="root"></div>', { url: 'http://127.0.0.1:5180/' });
  const originalFetch = globalThis.fetch;
  globalThis.window = dom.window; globalThis.document = dom.window.document; globalThis.IS_REACT_ACT_ENVIRONMENT = true;
  globalThis.fetch = async url => antwort(String(url).includes('importvorschau') ? vorschau : daten);
  const root = createRoot(document.getElementById('root'));
  try {
    await act(async () => root.render(createElement(App)));
    await pruefen();
  } finally {
    await act(async () => root.unmount()); globalThis.fetch = originalFetch;
    delete globalThis.window; delete globalThis.document; delete globalThis.IS_REACT_ACT_ENVIRONMENT; dom.window.close();
  }
}

test('Rechnungsfilter, Zurücksetzen und Sortierauswahl verändern tatsächliche Zeilen', async () => {
  const a = uebersicht.rechnungen[0];
  const daten = { ...uebersicht, rechnungen: [a, {...a, nummer:'AAA', status:'Offen', empfaenger:'Firma B'}] };
  await mitAnsicht(daten, async () => {
    const select = document.getElementById('zahlungsstatus');
    await act(async () => { select.value='Bezahlt'; select.dispatchEvent(new window.Event('change',{bubbles:true})); });
    assert.equal(document.querySelector('.rechnungstabelle'),null);
    assert.match(document.body.textContent,/Keine passenden Rechnungen/);
    await act(async () => document.querySelector('button.sekundaer').click());
    assert.equal(document.querySelectorAll('.rechnungstabelle .textknopf').length,2);
    const sort = document.getElementById('sortierung');
    await act(async () => { sort.value='empfaenger'; sort.dispatchEvent(new window.Event('change',{bubbles:true})); });
    assert.match(document.querySelector('.rechnungstabelle .textknopf').textContent,/AAA/);
    assert.match(document.querySelector('.kennzahlen').textContent,/SERVER-SALDO/);
  });
});

test('Importfilter unterscheidet Fehler, Warnungen und leere Filterergebnisse', async () => {
  await mitAnsicht(uebersicht, async () => {
    await act(async () => { window.location.hash='#import'; window.dispatchEvent(new window.HashChangeEvent('hashchange')); });
    const select = document.getElementById('prueffilter');
    await act(async () => { select.value='Warnung'; select.dispatchEvent(new window.Event('change',{bubbles:true})); });
    assert.match(document.body.textContent,/Keine Datensätze für diesen Filter/);
    await act(async () => { select.value='Fehler'; select.dispatchEvent(new window.Event('change',{bubbles:true})); });
    assert.equal(document.querySelectorAll('.importtabelle .textknopf').length,1);
    assert.match(document.querySelector('.importtabelle .status.fehler').textContent,/Fehler/);
  });
});

test('Leerer Rechnungsbestand zeigt eigene Hilfestellung ohne nutzlosen Zurücksetzen-Button', async () => {
  await mitAnsicht({...uebersicht,rechnungen:[]}, async () => {
    assert.match(document.body.textContent,/Noch keine Rechnungen/);
    assert.equal(document.querySelector('button.sekundaer'),null);
  });
});

test('Ladezustand bleibt sichtbar bis zur Antwort', async () => {
  await mitAnsicht(uebersicht, async () => {
    globalThis.fetch = () => new Promise(() => {});
    await act(async () => { window.location.hash='#import'; window.dispatchEvent(new window.HashChangeEvent('hashchange')); });
    assert.match(document.querySelector('[role="status"]').textContent,/wird geprüft/);
    assert.equal(document.querySelector('.beispielauswahl button').disabled,true);
  });
});

test('Semantische CSS-Tokenpaare erfüllen die festgelegten Kontrastgrenzen', () => {
  const css = readFileSync(new URL('../src/style.css', import.meta.url), 'utf8');
  const tokens = Object.fromEntries([...css.matchAll(/--([a-z-]+):\s*(#[a-f0-9]{6})/g)].map(m => [m[1],m[2]]));
  const luminanz = hex => {
    const rgb = [1,3,5].map(i => parseInt(hex.slice(i,i+2),16)/255).map(c => c <= .04045 ? c/12.92 : ((c+.055)/1.055)**2.4);
    return rgb[0]*.2126+rgb[1]*.7152+rgb[2]*.0722;
  };
  const kontrast = (a,b) => { const x=luminanz(a),y=luminanz(b); return (Math.max(x,y)+.05)/(Math.min(x,y)+.05); };
  const paare = [['kopf-text','kopf-bg'],['kopf-leise','kopf-bg'],['kopf-leise','kopf-badge'],['tabellentext','tabellenkopf'],['text','detail'],['leise','detail'],['text','zeile-alternativ'],['text','flaeche'],['leise','flaeche'],['leise','grund'],['leise','weich'],['akzent','akzent-hell'],
    ...['neutral','erfolg','arbeit','warnung','fehler'].map(s => [s+'-text',s+'-bg'])];
  for (const [text,hintergrund] of paare) assert.ok(kontrast(tokens[text],tokens[hintergrund]) >= 4.5, text+'/'+hintergrund);
  for (const akzent of ['akzent','akzent-dunkel']) assert.ok(kontrast('#ffffff',tokens[akzent]) >= 4.5);
  for (const grenze of ['eingaberand','fokus']) assert.ok(kontrast(tokens[grenze],tokens.flaeche) >= 3);
});
