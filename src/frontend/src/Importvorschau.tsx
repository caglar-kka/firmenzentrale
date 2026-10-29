import{useNavigation,useUmgebung}from'./Arbeitskontext.js';
import{anfrage,type Kontakt}from'./akte-api.js';
import Dateieingang from './Dateieingang.js';
import { Fragment, useCallback, useEffect, useRef, useState } from 'react';
import { Icon } from './Icon.js';
import { useDaten, Status, Titel, Zustand, Meldungsliste, tabellenTasten } from './ui.js';
import { ladeImport, type Beispiel, type Importansicht, type Demoimport, dateiPruefen, importZuordnen, importUebernehmen, importeLaden } from './api.js';
import { datumAnzeigen, importstatus } from './darstellung.js';

function Importergebnis({ daten, auswahl, waehlen, audit, prefix = '' }: { audit?: Demoimport; prefix?: string; daten: Importansicht; auswahl?: number[]; waehlen?: (zeile: number) => void }) {
  const umgebung=useUmgebung();
  const [filter,setFilter] = useState('Alle');
  const [zeile,setZeile] = useState<number | null>(null);
  const zeilen = daten.zeilen.filter(z => filter === 'Alle' || (filter === 'Fehler' ? z.hatFehler : z.meldungen.some(m => m.stufe === 'Warnung')));
  return <div className="p-pruefraster"><aside className="p-pruefakte"><p className="p-eyebrow">Prüfergebnis</p><strong className="p-fehlerzahl">{daten.fehlerzeilen.toString().padStart(2,'0')}</strong><h2>{daten.fehlerzeilen===1?'Zeile mit Fehlern':'Zeilen mit Fehlern'}</h2><p>von {daten.zeilenanzahl} Datenzeilen</p><Status wert={daten.hatFehler ? importstatus(true,false) : importstatus(false,true)} /><hr /><p>Nur geprüfte Rechnungen können nach Ihrer Bestätigung in den Arbeitsbestand übernommen werden. Zahlungen bleiben zur Prüfung.</p><details open={daten.zeilen.length === 0}><summary>Meldungen zur Datei ({daten.meldungen.length})</summary><Meldungsliste meldungen={daten.meldungen} /></details></aside>
    <div className="p-pruefdaten"><section className="p-register" aria-label="Geprüfte Datensätze"><div className="p-registerkopf"><div><h2>Datensätze prüfen</h2><p>Originalwerte und Hinweise lassen sich je Zeile öffnen.</p></div><div><label htmlFor={`${prefix}p-prueffilter`}>Prüfmeldungen</label><select id={`${prefix}p-prueffilter`} value={filter} onChange={e=>{setFilter(e.target.value);setZeile(null);}}><option>Alle</option><option value="Fehler">Mit Fehlern</option><option value="Warnung">Mit Warnungen</option></select></div></div><p className="p-ergebnis" role="status">{zeilen.length} von {daten.zeilen.length} Datensätzen</p>
    {zeilen.length === 0 ? <p className="p-leer">Keine Datensätze für diese Auswahl.</p> : <div className="p-tabellenraum" onKeyDown={tabellenTasten} tabIndex={0} role="region" aria-label="Importtabelle"><table className="p-tabelle p-importtabelle"><thead><tr><th scope="col">Zeile / Vorgang</th><th scope="col">Beteiligte</th><th scope="col" className="geld">Betrag</th><th scope="col">Prüfstatus</th></tr></thead><tbody>{zeilen.map(z=><Fragment key={z.zeile}><tr><th scope="row">{waehlen && z.art==='Rechnung' && !z.hatFehler && <input type="checkbox" aria-label={`Rechnung aus Zeile ${z.zeile} auswählen`} disabled={z.hatFehler || z.art!=='Rechnung'} checked={auswahl?.includes(z.zeile) ?? false} onChange={()=>waehlen(z.zeile)} />}<button className="p-rowlink" aria-expanded={zeile===z.zeile} aria-controls={`${prefix}p-quelle-${z.zeile}`} onClick={()=>setZeile(zeile===z.zeile?null:z.zeile)}>Zeile {z.zeile}<Icon name="pfeil" /></button><span className="p-sub">{!waehlen && auswahl?.includes(z.zeile) ? 'Übernommen · Rechnung' : z.art??'Nicht übernommen'}{z.datum && ` · ${datumAnzeigen(z.datum)}`}</span></th><td data-label="Beteiligte">{z.von && z.an ? <><span className="p-partner"><small>{z.art==='Rechnung'?'Aussteller':'Zahler'}</small>{z.von}</span><span className="p-partner"><small>{z.art==='Rechnung'?'Empfänger':'Zahlungsempfänger'}</small>{z.an}</span></>:'Keine geprüfte Zuordnung'}</td><td data-label="Betrag" className="geld">{z.betrag??'Nicht übernommen'}</td><td data-label="Prüfstatus"><Status wert={importstatus(z.hatFehler,z.meldungen.some(m=>m.stufe==='Warnung'))} />{z.meldungen[0] && <p className="p-pruefhinweis">{z.meldungen.some(m=>m.code==='BETRAG') ? 'Bitte einen positiven Betrag mit höchstens zwei Nachkommastellen angeben.' : z.meldungen.find(m=>m.stufe==='Fehler')?.text??z.meldungen[0].text}</p>}</td></tr><tr className="p-quellzeile" hidden={zeile!==z.zeile} id={`${prefix}p-quelle-${z.zeile}`}><td colSpan={4}><div className="p-zellinspektor"><h3><Icon name="quelle" />Originalwerte · Zeile {z.zeile}</h3><Meldungsliste meldungen={z.meldungen} /><div className="p-quellscroll" onKeyDown={tabellenTasten} tabIndex={0} role="region" aria-label={`Originalwerte Zeile ${z.zeile}`}><table><caption>Unveränderte Zellwerte · horizontal scrollbar</caption><thead><tr><th scope="col">Quelle</th><th scope="col">Datentyp</th><th scope="col">Originalwert</th><th scope="col">Aufgelöster Text</th></tr></thead><tbody>{z.zellen.map(c=><tr key={c.quelle.zelle}><th scope="row">{c.quelle.blatt}!{c.quelle.zelle}<span className="p-sub">Zeile {c.quelle.zeile}</span></th><td>{c.datentyp}{c.formatindex!==null && ` · Format ${c.formatindex}`}</td><td>{c.originalwert??'—'}{c.formel!==null && <p>Formel (nicht ausgeführt): {c.formel}</p>}</td><td>{c.text}</td></tr>)}</tbody></table></div></div></td></tr></Fragment>)}</tbody></table></div>}
    </section>{umgebung.diagnose&&<details className="p-dateiquelle"><summary><Icon name="beleg" />Technische Importdetails und Prüfbericht</summary><p>Dateiherkunft, Originalwerte und Prüfentscheidung bleiben nachvollziehbar. Ein Hash bestätigt keine Echtheit.</p><pre>{daten.bericht}</pre>{audit && <pre>{JSON.stringify({mapping:audit.mapping,mappingBestaetigt:audit.mappingBestaetigt,auswahl:audit.auswahl,entschiedenAm:audit.entschiedenAm},null,2)}</pre>}</details>}</div>
  </div>;
}
function Uebungsfaelle() {
  const [beispiel,setBeispiel] = useState<Beispiel>('fehler');
  const laden = useCallback((signal:AbortSignal)=>ladeImport(beispiel,signal),[beispiel]);
  const {daten,laedt,fehler,erneut} = useDaten(laden);
  return <><section className="p-dateiauswahl" aria-label="Beispiel auswählen"><Icon name="import" /><div><label htmlFor="p-beispiel">Beispiel-Arbeitsmappe</label><select id="p-beispiel" value={beispiel} onChange={e=>setBeispiel(e.target.value as Beispiel)}><option value="fehler">Rechnung, Zahlung und fehlerhafter Betrag</option><option value="gueltig">Rechnung und unbestätigte Zahlung</option><option value="dublette">Widersprüchliche Zahlungsdublette</option></select></div><button disabled={laedt} onClick={erneut}><Icon name="erneut" />Erneut prüfen</button></section><p className="p-fussnote">Lokal erzeugte Testdatei. Kein Upload eigener Dateien, keine Makroausführung.</p><Zustand laedt={laedt} fehler={fehler} erneut={erneut}/>{daten&&<Importergebnis key={beispiel} daten={daten}/>}</>;
}



export default function Importvorschau({rueckkehr=0,dateiId}:{rueckkehr?:number;dateiId?:string|null}) {
  const nav=useNavigation(),umgebung=useUmgebung();const[firmen,Firmen]=useState<string[][]>([]);useEffect(()=>{void anfrage<Kontakt[]>('kontakte').then(ks=>Firmen(ks.filter(k=>k.art!=='Person'&&k.id.startsWith('DEMO-')).map(k=>[k.id,k.firma]))).catch(()=>{});},[]);
  const [vorgang,setVorgang]=useState<Demoimport|null>(null);
  const [mapping,setMapping]=useState<Record<string,string>>({});
  const [auswahl,setAuswahl]=useState<number[]>([]);
  const [bestaetigt,setBestaetigt]=useState(false);
  const [synthetisch,setSynthetisch]=useState(false);
  const [arbeitet,setArbeitet]=useState(false);
  const [fehler,setFehler]=useState('');
  const [liste,setListe]=useState<Demoimport[]|null>(null);

  useEffect(()=>{if(dateiId){void importeLaden().then(l=>{const v=l.find(v=>v.id===dateiId);if(v){setVorgang(v);setMapping(v.mapping);setListe(null);}}).catch(e=>setFehler(e.message));}else{setVorgang(null);setAuswahl([]);setFehler('');void importeLaden().then(setListe).catch(e=>setFehler(e.message));}},[dateiId,rueckkehr]);
  const titel=useRef<HTMLHeadingElement>(null);
  useEffect(()=>{titel.current?.focus();},[vorgang?.id,vorgang?.mappingBestaetigt,vorgang?.ergebnis]);
  function anzeigen(v: Demoimport) {
    nav('import',v.id);setVorgang(v);setListe(null);setMapping(v.mapping);setAuswahl([]);setBestaetigt(false);
    titel.current?.focus();
  }
  async function aktion(aufgabe:()=>Promise<void>) {
    setArbeitet(true);setFehler('');
    try { await aufgabe(); } catch(e) { setFehler(e instanceof Error ? e.message : 'Die Datei konnte nicht verarbeitet werden. Bitte erneut versuchen.'); }
    finally { setArbeitet(false); }
  }
  return <>
    <Titel oben="Daten / Importe" titel="Excel-Datei importieren" text="Datei auswählen, Zuordnung prüfen und Rechnungen bewusst übernehmen." />

    <ol className="p-ablauf" aria-label="Importablauf"><li><span>01</span> Datei auswählen</li><li><span>02</span> Firmen und Daten prüfen</li><li><span>03</span> Übernahme bestätigen</li></ol>
    <details className="p-neuedatei" open={!vorgang}><summary>Excel-Datei auswählen</summary><section className="p-upload" aria-label="Excel-Dateiauswahl">
      <div><h2>Eine Datei, ein klarer Prüfschritt</h2><p>Unterstützt: XLSX bis 2,1 MB, ein Blatt „ISD-Testdaten“ mit neun vorgegebenen Spalten. Andere Arbeitsmappen werden verständlich abgewiesen.</p></div>
      <label className="p-bestaetigung"><input type="checkbox" checked={synthetisch} onChange={e=>setSynthetisch(e.target.checked)} disabled={arbeitet}/> Die Datei enthält ausschließlich freigegebene Beispieldaten.</label>
      <Dateieingang ziel="Excel-Importvorschau" excel disabled={!synthetisch||arbeitet} speichern={async(files)=>{const file=files[0];if(!file||!synthetisch)return;setVorgang(null);setListe(null);anzeigen(await dateiPruefen(file));}}/>
      <div className="p-uploadaktionen"><a href="/api/importvorlage/fehler" download>Beispieldatei herunterladen</a><button className="p-textaktion" disabled={arbeitet} onClick={()=>void aktion(async()=>setListe(await importeLaden()))}>Bisherige Importe anzeigen</button></div>
    </section></details>
    {arbeitet && <p role="status">Datei wird lokal verarbeitet …</p>}
    {fehler && <div className="p-fehler" role="alert"><h2>Bitte prüfen</h2><p>{fehler}</p><button disabled={arbeitet} onClick={()=>void aktion(async()=>setListe(await importeLaden()))}>Erneut versuchen</button><p>Es wurde keine ungeprüfte Übernahme durchgeführt. Bei einer unterbrochenen Verbindung den Stand unter „Bisherige Importe“ prüfen.</p></div>}
    {liste && <section className="p-importliste" aria-label="Bisherige Importe"><h2>Importe dieser Sitzung</h2>{liste.length===0 ? <p>Noch keine Dateien ausgewählt.</p> : liste.map((v,i)=><button key={v.id} disabled={arbeitet} onClick={()=>anzeigen(v)}>Import {i+1} · {v.ergebnis ? `${v.ergebnis.uebernommen} übernommen` : 'Noch nicht übernommen'} · {v.vorschau.zeilenanzahl} Zeilen</button>)}</section>}
    {vorgang && <section className="p-importvorgang" aria-label="Ausgewählte Datei"><h2 ref={titel} tabIndex={-1}>{vorgang.ergebnis ? 'Importbericht' : 'Datei prüfen'}</h2>
      {vorgang.ergebnis ? <><dl className="p-importbericht">{([['Übernommen',vorgang.ergebnis.uebernommen],['Übersprungen',vorgang.ergebnis.uebersprungen],['Fehlerhaft',vorgang.ergebnis.fehlerhaft],['Zur Prüfung',vorgang.ergebnis.zurPruefung]] as const).map(([name,zahl])=><div key={name}><dt>{name}</dt><dd>{zahl}</dd></div>)}</dl><p role="status">Die ausgewählten Rechnungen sind im Rechnungsbestand verfügbar. Zahlungszeilen wurden nicht übernommen.</p><a href="#rechnungen">Übernommene Rechnungen ansehen</a></> : <details className="p-mapping" open={!vorgang.mappingBestaetigt} aria-label="Firmenzuordnung"><summary>Firmenzuordnung · {vorgang.mappingBestaetigt ? 'bestätigt' : 'bitte prüfen'}</summary><h3>Erkanntes Blatt: {vorgang.blatt}</h3><p>Spalten: {vorgang.spalten.join(' · ')}. „Von“ und „An“ bestimmen die Rollen je Vorgang.</p>
        <p className="p-pflichtinfo">* Pflichtfeld</p><div className="p-mappingfelder">{vorgang.firmen.map((name,i)=><div key={name}><label htmlFor={`firma-${i}`}>Firma aus Datei: {name} *</label><select required aria-required="true" id={`firma-${i}`} value={mapping[name]??''} disabled={arbeitet} onChange={e=>{setMapping({...mapping,[name]:e.target.value});setBestaetigt(false);setAuswahl([]);setVorgang({...vorgang,mappingBestaetigt:false});}}><option value="">Bitte Firma zuordnen</option>{firmen.map(([id,text])=><option key={id} value={id}>{text}</option>)}</select>{(vorgang.firmenhinweise?.[name]?.length??0)>0&&<p>Möglicher vorhandener Kontakt: {vorgang.firmenhinweise?.[name]?.map(h=>h.name).join(" · ")}. Bitte Angaben vergleichen und die Firma bewusst auswählen.</p>}</div>)}</div>
        <button disabled={arbeitet || vorgang.firmen.length===0 || vorgang.firmen.some(f=>!mapping[f])} onClick={()=>void aktion(async()=>anzeigen(await importZuordnen(vorgang,mapping)))}>Blatt und Firmenzuordnung bestätigen</button>
      </details>}
      {vorgang.mappingBestaetigt ? <Importergebnis prefix="datei-" audit={vorgang} daten={vorgang.vorschau} auswahl={vorgang.ergebnis ? vorgang.auswahl : auswahl} waehlen={!vorgang.ergebnis && vorgang.mappingBestaetigt ? zeile=>{setAuswahl(a=>a.includes(zeile)?a.filter(z=>z!==zeile):[...a,zeile]);setBestaetigt(false);} : undefined}/> : <p>Bitte zuerst die Firmenzuordnung bestätigen. Danach werden erkannte Werte, Originalwerte und Prüfhinweise angezeigt.</p>}
      {!vorgang.ergebnis && vorgang.mappingBestaetigt && <div className="p-uebernahme"><label className="p-bestaetigung"><input type="checkbox" checked={bestaetigt} disabled={arbeitet} onChange={e=>setBestaetigt(e.target.checked)}/> Ich habe die ausgewählten Rechnungen, Firmenrollen und Hinweise geprüft. Nur in den lokalen Arbeitsbestand übernehmen.</label><button disabled={arbeitet || !bestaetigt || auswahl.length===0} onClick={()=>void aktion(async()=>anzeigen(await importUebernehmen(vorgang,auswahl)))}>{auswahl.length} {auswahl.length===1?'Rechnung':'Rechnungen'} in Arbeitsbestand übernehmen</button></div>}

    </section>}
    {umgebung.diagnose&&<details className="p-uebungen"><summary>Beispiele ohne Dateiübernahme</summary><Uebungsfaelle/></details>}
  </>;
}
