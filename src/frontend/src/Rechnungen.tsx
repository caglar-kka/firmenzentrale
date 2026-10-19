import { useCallback, useEffect, useRef, useState } from 'react';
import{useNavigation,useUmgebung}from'./Arbeitskontext.js';
import { Icon } from './Icon.js';
import { useDaten, Status, Titel, Zustand, Kennzahlen, tabellenTasten } from './ui.js';
import{Sortierkopf,Seiten}from'./Buero.js';
import Rechnungsdetails from './Rechnungsdetails.js';
import { ladeUebersicht, type Rechnung } from './api.js';
import { datumAnzeigen, rechnungenFiltern, zahlungsstatus, type Sortierung } from './darstellung.js';

export default function Rechnungen({ rueckkehr = 0, suchziel }: { rueckkehr?: number; suchziel?: string }) {
  const nav=useNavigation();
  const pg=useUmgebung().fluechtigeDaten===false;const[seite,P]=useState(0);
  const [suche, setSuche] = useState('');
  const [filter, setFilter] = useState('Alle');
  const [sortierung, setSortierung] = useState<Sortierung>('nummer');
  const [ab,Ab]=useState(false),[richtung,Richtung]=useState('Alle');
  function sortieren(feld:string){if(sortierung===feld)Ab(!ab);else{setSortierung(feld as Sortierung);Ab(false);}}
  const [detail, setDetail] = useState<Rechnung | null>(null);
  const query=pg?'?'+new URLSearchParams({seite:String(seite),text:suche,status:filter==='Alle'?'':filter,richtung:richtung==='Alle'?'':richtung,sort:sortierung,ab:ab?'1':'0'}):'';
  const loader=useCallback((signal:AbortSignal)=>ladeUebersicht(signal,query),[query]);
  const {daten,laedt,fehler,erneut}=useDaten(loader);
  useEffect(()=>{if(!pg||!suchziel)return;const c=new AbortController();void ladeUebersicht(c.signal,'?id='+encodeURIComponent(suchziel)).then(a=>{if(!c.signal.aborted&&a.rechnungen[0]){setDetail(a.rechnungen[0]);}}).catch(()=>{});return()=>c.abort();},[pg,suchziel]);
  useEffect(()=>P(0),[suche,filter,richtung,sortierung,ab]);
  const ausloeser = useRef<HTMLButtonElement | null>(null);
  useEffect(() => { if (!detail) ausloeser.current?.focus(); }, [detail]);
  useEffect(()=>{if(rueckkehr>0)setDetail(null);},[rueckkehr]);
  useEffect(()=>{if(suchziel && daten){const r=daten.rechnungen.find(r=>r.vorgangId===suchziel||r.nummer===suchziel);if(r)setDetail(r);}},[suchziel,daten]);
  useEffect(()=>{const aktualisieren=()=>erneut();window.addEventListener('akten:kontakt-geaendert',aktualisieren);return()=>window.removeEventListener('akten:kontakt-geaendert',aktualisieren);},[]);
  useEffect(()=>{if(daten)setDetail(alt=>alt?daten.rechnungen.find(r=>(r.vorgangId&&r.vorgangId===alt.vorgangId)||r.nummer===alt.nummer)||alt:null);},[daten]);
  const lokal = rechnungenFiltern(daten?.rechnungen ?? [], suche, filter, sortierung,ab,richtung);const zeilen=pg?daten?.rechnungen??[]:lokal;
  return <><div hidden={!!detail}>
    <Titel oben="Finanzen / Rechnungsbestand" titel="Rechnungen" text="Zahlungsstände verstehen. Jeden Beleg nachvollziehen." />
    <Zustand laedt={laedt} fehler={fehler} erneut={erneut} />
    {daten && <><Kennzahlen offen={daten.offenerBetrag} betrag={daten.gesamtbetrag} anzahl={(daten.bestandsanzahl??daten.rechnungen.length)} forderungen={daten.offeneForderungen} verbindlichkeiten={daten.offeneVerbindlichkeiten} />
      <section className="p-register" aria-label="Rechnungsbestand"><div className="p-registerkopf"><div><h2>Rechnungsbestand <span className="p-anzahl">{(daten.bestandsanzahl??daten.rechnungen.length)}</span></h2><p>Summen beziehen sich auf den gesamten Bestand.</p></div><div className="p-suche"><label className="nur-vorlesen" htmlFor="p-suche">Rechnungen suchen</label><Icon name="suche" /><input id="p-suche" type="search" placeholder="Nummer, Partner oder Beleg suchen" value={suche} onChange={e => setSuche(e.target.value)} /></div></div>
      <div className="p-filterleiste"><div className="p-filter" role="group" aria-label="Zahlungsstatus filtern">{([['Alle','Alle'],['Offen','Offen'],['Teilzahlung','Teilweise bezahlt'],['Bezahlt','Bezahlt']] as const).map(([wert,text]) => <button key={wert} aria-pressed={filter === wert} onClick={() => setFilter(wert)}>{text}</button>)}</div><div className="p-sortierung"><label htmlFor="p-sortierung">Sortieren</label><select id="p-sortierung" value={sortierung} onChange={e => {setSortierung(e.target.value as Sortierung);Ab(e.target.value==='datum');}}><option value="nummer">Rechnungsnummer</option><option value="datum">Rechnungsdatum</option><option value="aussteller">Aussteller A–Z</option><option value="empfaenger">Empfänger A–Z</option><option value="betrag">Betrag</option><option value="status">Zahlungsstatus</option></select></div><label>Offene Beträge<select aria-label="Forderungen und Verbindlichkeiten" value={richtung} onChange={e=>Richtung(e.target.value)}><option value="Alle">Alle Rechnungsrichtungen</option><option value="Forderungen">Offene Forderungen · noch zu erhalten</option><option value="Verbindlichkeiten">Offene Verbindlichkeiten · noch zu zahlen</option></select></label></div>
      <p className="p-ergebnis" role="status">{daten.anzahl??zeilen.length} von {(daten.bestandsanzahl??daten.rechnungen.length)} Rechnungen</p>
      {zeilen.length === 0 ? <div className="p-leer"><Icon name="suche" /><h3>{(daten.bestandsanzahl??daten.rechnungen.length) ? 'Keine passenden Rechnungen' : 'Noch keine Rechnungen'}</h3><p>Hier erscheinen die Rechnungen.</p>{(daten.bestandsanzahl??daten.rechnungen.length) > 0 && <button onClick={() => { setSuche(''); setFilter('Alle');Richtung('Alle'); }}>Filter zurücksetzen</button>}</div> : <div className="p-tabellenraum" onKeyDown={tabellenTasten} tabIndex={0} role="region" aria-label="Rechnungstabelle"><table className="p-tabelle"><thead><tr><Sortierkopf feld="nummer" aktiv={sortierung} ab={ab} sortieren={sortieren}>Rechnung</Sortierkopf><th scope="col" aria-sort={["aussteller","empfaenger"].includes(sortierung)?(ab?"descending":"ascending"):"none"}><button className="p-sortierkopf" onClick={()=>sortieren("aussteller")}>Aussteller{sortierung==="aussteller"?(ab?" ↓":" ↑"):" ↕"}</button> / <button className="p-sortierkopf" onClick={()=>sortieren("empfaenger")}>Empfänger{sortierung==="empfaenger"?(ab?" ↓":" ↑"):" ↕"}</button></th><Sortierkopf feld="datum" aktiv={sortierung} ab={ab} sortieren={sortieren}>Rechnungsdatum</Sortierkopf><Sortierkopf feld="betrag" aktiv={sortierung} ab={ab} sortieren={sortieren}>Betrag</Sortierkopf><th scope="col" className="geld">Bestätigt bezahlt</th><th scope="col" className="geld">Offen</th><Sortierkopf feld="status" aktiv={sortierung} ab={ab} sortieren={sortieren}>Zahlungsstatus</Sortierkopf></tr></thead><tbody>{zeilen.map(r => <tr key={`${r.aussteller}/${r.nummer}`}><th scope="row"><button className="p-rowlink" onClick={e => { ausloeser.current=e.currentTarget; setDetail(r);nav('rechnungen',r.vorgangId??r.nummer); }}>{r.nummer}<Icon name="pfeil" /></button></th><td data-label="Rechnung von / an"><span className="p-partner"><small>Rechnung von</small>{r.aussteller}</span><span className="p-partner"><small>Rechnung an</small>{r.empfaenger}</span></td><td data-label="Rechnungsdatum">{datumAnzeigen(r.datum)}</td><td data-label="Rechnungsbetrag" className="geld">{r.betrag}</td><td data-label="Bestätigt bezahlt" className="geld">{r.bestaetigtZugeordnet}</td><td data-label="Offener Betrag" className="geld p-saldo">{r.offenerBetrag}</td><td data-label="Zahlungsstatus"><Status wert={zahlungsstatus(r.status)} /></td></tr>)}</tbody></table></div>}
      {pg&&<Seiten seite={seite} anzahl={daten.anzahl??0} waehlen={P}/>}</section><p className="p-fussnote"><Icon name="quelle" />Bestätigte Zahlungen sind berücksichtigt. Fälligkeitsdaten sind nicht erfasst.</p></>}
    </div>{detail && <Rechnungsdetails key={detail.vorgangId??JSON.stringify([detail.aussteller,detail.nummer])} rechnung={detail} zurueck={() => {setDetail(null);nav('rechnungen');}} />}</>;
}
