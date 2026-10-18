import { useEffect, useRef, useState } from 'react';
import { Icon } from './Icon.js';
import { useDaten, Status, Titel, Zustand, Kennzahlen, tabellenTasten } from './ui.js';
import Rechnungsdetails from './Rechnungsdetails.js';
import { ladeUebersicht, type Rechnung } from './api.js';
import { datumAnzeigen, rechnungenFiltern, zahlungsstatus, type Sortierung } from './darstellung.js';

export default function Rechnungen() {
  const { daten, laedt, fehler, erneut } = useDaten(ladeUebersicht);
  const [suche, setSuche] = useState('');
  const [filter, setFilter] = useState('Alle');
  const [sortierung, setSortierung] = useState<Sortierung>('nummer');
  const [detail, setDetail] = useState<Rechnung | null>(null);
  const ausloeser = useRef<HTMLButtonElement | null>(null);
  useEffect(() => { if (!detail) ausloeser.current?.focus(); }, [detail]);
  const zeilen = rechnungenFiltern(daten?.rechnungen ?? [], suche, filter, sortierung);
  return <><div hidden={!!detail}>
    <Titel oben="Finanzen / Rechnungsbestand" titel="Rechnungen" text="Zahlungsstände verstehen. Jeden Beleg nachvollziehen." />
    <Zustand laedt={laedt} fehler={fehler} erneut={erneut} />
    {daten && <><Kennzahlen offen={daten.offenerBetrag} betrag={daten.gesamtbetrag} anzahl={daten.rechnungen.length} />
      <section className="p-register" aria-label="Rechnungsbestand"><div className="p-registerkopf"><div><h2>Rechnungsbestand <span className="p-anzahl">{daten.rechnungen.length}</span></h2><p>Summen beziehen sich auf den gesamten Bestand.</p></div><div className="p-suche"><label className="nur-vorlesen" htmlFor="p-suche">Rechnungen suchen</label><Icon name="suche" /><input id="p-suche" type="search" placeholder="Nummer, Partner oder Beleg suchen" value={suche} onChange={e => setSuche(e.target.value)} /></div></div>
      <div className="p-filterleiste"><div className="p-filter" role="group" aria-label="Zahlungsstatus filtern">{([['Alle','Alle'],['Offen','Offen'],['Teilzahlung','Teilweise bezahlt'],['Bezahlt','Bezahlt']] as const).map(([wert,text]) => <button key={wert} aria-pressed={filter === wert} onClick={() => setFilter(wert)}>{text}</button>)}</div><div className="p-sortierung"><label htmlFor="p-sortierung">Sortieren</label><select id="p-sortierung" value={sortierung} onChange={e => setSortierung(e.target.value as Sortierung)}><option value="nummer">Rechnungsnummer</option><option value="datum">Datum · neueste zuerst</option><option value="aussteller">Aussteller A–Z</option><option value="empfaenger">Empfänger A–Z</option></select></div></div>
      <p className="p-ergebnis" role="status">{zeilen.length} von {daten.rechnungen.length} Rechnungen</p>
      {zeilen.length === 0 ? <div className="p-leer"><Icon name="suche" /><h3>{daten.rechnungen.length ? 'Keine passenden Rechnungen' : 'Noch keine Rechnungen'}</h3><p>Hier erscheinen die synthetischen Beispielrechnungen.</p>{daten.rechnungen.length > 0 && <button onClick={() => { setSuche(''); setFilter('Alle'); }}>Filter zurücksetzen</button>}</div> : <div className="p-tabellenraum" onKeyDown={tabellenTasten} tabIndex={0} role="region" aria-label="Rechnungstabelle"><table className="p-tabelle"><thead><tr><th scope="col">Rechnung</th><th scope="col">Rechnung von / an</th><th scope="col" className="geld">Betrag</th><th scope="col" className="geld">Bestätigt bezahlt</th><th scope="col" className="geld">Offen</th><th scope="col">Zahlungsstatus</th></tr></thead><tbody>{zeilen.map(r => <tr key={`${r.aussteller}/${r.nummer}`}><th scope="row"><button className="p-rowlink" onClick={e => { ausloeser.current=e.currentTarget; setDetail(r); }}>{r.nummer}<Icon name="pfeil" /></button><span className="p-sub">{datumAnzeigen(r.datum)}</span></th><td data-label="Rechnung von / an"><span className="p-partner"><small>Von</small>{r.aussteller}</span><span className="p-partner"><small>An</small>{r.empfaenger}</span></td><td data-label="Rechnungsbetrag" className="geld">{r.betrag}</td><td data-label="Bestätigt bezahlt" className="geld">{r.bestaetigtZugeordnet}</td><td data-label="Offener Betrag" className="geld p-saldo">{r.offenerBetrag}</td><td data-label="Zahlungsstatus"><Status wert={zahlungsstatus(r.status)} /></td></tr>)}</tbody></table></div>}
      </section><p className="p-fussnote"><Icon name="quelle" />Beträge und Zahlungsstände kommen aus dem Rechnungskern. Gesamtsummen unterscheiden noch nicht zwischen Forderungen und Verbindlichkeiten. Keine Aussage zur Fälligkeit.</p></>}
    </div>{detail && <Rechnungsdetails rechnung={detail} zurueck={() => setDetail(null)} />}</>;
}
