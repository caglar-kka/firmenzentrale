import { Fragment, useCallback, useEffect, useState, type KeyboardEvent } from 'react';
import { ladeImport, ladeUebersicht, type Beispiel, type Importansicht, type Meldung, type Rechnung, type Uebersicht } from './api.js';
import { datumAnzeigen, importstatus, rechnungenFiltern, ui, zahlungsstatus, type Sortierung, type Statusdarstellung } from './darstellung.js';

function useDaten<T>(laden: (signal: AbortSignal) => Promise<T>) {
  const [daten, setDaten] = useState<T | null>(null);
  const [fehler, setFehler] = useState<string | null>(null);
  const [laedt, setLaedt] = useState(true);
  const [versuch, setVersuch] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setLaedt(true); setFehler(null); setDaten(null);
    laden(controller.signal).then(ergebnis => {
      if (!controller.signal.aborted) { setDaten(ergebnis); setLaedt(false); }
    }).catch(error => {
      if (!controller.signal.aborted) {
        setFehler(error instanceof Error ? error.message : 'Die Ansicht konnte nicht geladen werden.'); setLaedt(false);
      }
    });
    return () => controller.abort();
  }, [laden, versuch]);
  return { daten, fehler, laedt, erneut: () => setVersuch(v => v + 1) };
}
function tabellenTasten(e: KeyboardEvent<HTMLDivElement>) {
  if (e.target !== e.currentTarget || (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft')) return;
  e.preventDefault();
  e.currentTarget.scrollLeft += e.key === 'ArrowRight' ? 300 : -300;
}
function Fehler({ text, erneut }: { text: string; erneut: () => void }) {
  return <div className="fehlerkasten" role="alert"><h2>Ansicht nicht erreichbar</h2><p>{text}</p><button onClick={erneut}>Erneut versuchen</button></div>;
}
function Status({ wert }: { wert: Statusdarstellung }) {
  return <span className={`status ${wert.ton}`}><span aria-hidden="true">{wert.symbol}</span> {wert.text}</span>;
}
function Rechnungdetails({ rechnung: r }: { rechnung: Rechnung }) {
  return <div className="detailinhalt"><h3>Rechnung {r.nummer} · Zahlungen und Belege</h3>
    <dl className="metadaten"><div><dt>Aussteller</dt><dd>{r.aussteller}</dd></div><div><dt>Empfänger</dt><dd>{r.empfaenger}</dd></div><div><dt>Rechnungsbeleg</dt><dd>{r.beleg}</dd></div><div><dt>Quelle</dt><dd>{r.quellenort}</dd></div></dl>
    <p className="klein">Belegreferenzen sind synthetische Nachweise; es sind keine Dokumentdateien hinterlegt. Fälligkeit wurde nicht erfasst.</p>
    {r.zahlungen.length === 0 ? <p>Keine Zahlungen vorhanden.</p> : <ul className="zahlungen">{r.zahlungen.map((z, i) => <li key={i}>
      <div className="kartenkopf"><strong>{z.betrag}</strong><Status wert={zahlungsstatus(z.status)} /></div>
      <dl className="metadaten"><div><dt>Zahlungsdatum</dt><dd>{datumAnzeigen(z.datum)}</dd></div><div><dt>Dieser Rechnung zugeordnet</dt><dd>{z.zugeordnet}</dd></div><div><dt>Zahler</dt><dd>{z.zahler}</dd></div><div><dt>Zahlungsempfänger</dt><dd>{z.empfaenger}</dd></div><div><dt>Beleg</dt><dd>{z.beleg}</dd></div><div><dt>Quelle</dt><dd>{z.quellenort}</dd></div></dl>
    </li>)}</ul>}
  </div>;
}
function Rechnungsuebersicht() {
  const { daten, fehler, laedt, erneut } = useDaten<Uebersicht>(ladeUebersicht);
  const [suche, setSuche] = useState('');
  const [filter, setFilter] = useState('Alle');
  const [sortierung, setSortierung] = useState<Sortierung>('nummer');
  const [detail, setDetail] = useState<Rechnung | null>(null);
  const zeilen = rechnungenFiltern(daten?.rechnungen ?? [], suche, filter, sortierung);
  return <><div className="seitenkopf"><p className="oberzeile">Finanzen / Übersicht</p><h1>Rechnungen</h1><p>Offene Beträge im Blick. Jede Zahlung bleibt nachvollziehbar.</p></div>
    {laedt && <p className="zustand" role="status">Rechnungen werden geladen …</p>}{fehler && <Fehler text={fehler} erneut={erneut} />}
    {daten && <><dl className="kennzahlen"><div><dt>Rechnungen insgesamt</dt><dd>{daten.rechnungen.length}</dd></div><div><dt>Rechnungsbeträge gesamt</dt><dd>{daten.gesamtbetrag}</dd></div><div className="akzent"><dt>Offener Betrag gesamt</dt><dd>{daten.offenerBetrag}</dd></div></dl>
      <section className="karte listenkarte" aria-label="Rechnungsübersicht"><h2 className="nur-vorlesen">Rechnungsbestand</h2><div className="werkzeuge">
        <div className="suchfeld"><label htmlFor="suche">{ui.suchen}</label><input id="suche" type="search" placeholder="Nummer, Geschäftspartner oder Beleg" value={suche} onChange={e => setSuche(e.target.value)} /></div>
        <div><label htmlFor="zahlungsstatus">Zahlungsstatus</label><select id="zahlungsstatus" value={filter} onChange={e => setFilter(e.target.value)}><option value="Alle">{ui.alle}</option><option>Offen</option><option value="Teilzahlung">Teilweise bezahlt</option><option>Bezahlt</option></select></div>
        <div><label htmlFor="sortierung">Sortieren nach</label><select id="sortierung" value={sortierung} onChange={e => setSortierung(e.target.value as Sortierung)}><option value="nummer">Rechnungsnummer ↑</option><option value="datum">Datum · neueste zuerst</option><option value="aussteller">Aussteller A–Z</option><option value="empfaenger">Empfänger A–Z</option></select></div>
      </div><p className="ergebniszahl" role="status">{zeilen.length} von {daten.rechnungen.length} Rechnungen · Summen oben beziehen sich auf alle Rechnungen.</p>
      {zeilen.length === 0 ? <div className="zustand"><h2>{daten.rechnungen.length === 0 ? 'Noch keine Rechnungen' : ui.leer}</h2><p>{daten.rechnungen.length === 0 ? 'Es sind noch keine Beispielrechnungen vorhanden.' : 'Ändern Sie den Suchbegriff oder den Zahlungsstatus.'}</p>{daten.rechnungen.length > 0 && <button className="sekundaer" onClick={() => { setSuche(''); setFilter('Alle'); }}>Filter zurücksetzen</button>}</div> :
        <div className="listen-scroll" onKeyDown={tabellenTasten} role="region" aria-label="Rechnungstabelle, auf Tablet seitlich scrollbar" tabIndex={0}><table className="datentabelle rechnungstabelle"><caption className="nur-vorlesen">Rechnungen mit serverseitig berechneten Beträgen. Rechnungsnummer öffnet Details.</caption><thead><tr><th scope="col">Rechnung / Datum</th><th scope="col">Geschäftspartner</th><th scope="col" className="geld">Rechnungsbetrag</th><th scope="col" className="geld">Bestätigt zugeordnet</th><th scope="col" className="geld">Offener Betrag</th><th scope="col">Zahlungsstatus</th></tr></thead><tbody>{zeilen.map((r, i) => <Fragment key={`${r.aussteller}/${r.nummer}`}><tr>
          <th scope="row"><button className="textknopf" aria-expanded={detail === r} aria-controls={`rechnung-${i}`} onClick={() => setDetail(detail === r ? null : r)}>{r.nummer} <span aria-hidden="true">{detail === r ? '−' : '+'}</span></button><span className="unterzeile">{datumAnzeigen(r.datum)}</span></th>
          <td data-label="Geschäftspartner"><span className="parteizeile"><span className="klein">Aussteller</span>{r.aussteller}</span><span className="parteizeile"><span className="klein">Empfänger</span>{r.empfaenger}</span></td><td data-label="Rechnungsbetrag" className="geld">{r.betrag}</td><td data-label="Bestätigt zugeordnet" className="geld">{r.bestaetigtZugeordnet}</td><td data-label="Offener Betrag" className="geld saldo">{r.offenerBetrag}</td><td data-label="Zahlungsstatus"><Status wert={zahlungsstatus(r.status)} /></td>
        </tr><tr className="detailzeile" hidden={detail !== r} id={`rechnung-${i}`}><td colSpan={6}><Rechnungdetails rechnung={r} /></td></tr></Fragment>)}</tbody></table></div>}
      </section><p className="hinweis">Offen und Zahlungsstatus sind unabhängig von Fälligkeit. Zahlungen „Zur Prüfung“ reduzieren keinen offenen Betrag. Alle Bestätigungen und Belege hier sind erfunden.</p>
    </>}
  </>;
}
function Meldungsliste({ meldungen }: { meldungen: Meldung[] }) {
  if (meldungen.length === 0) return <p className="klein">Keine zusätzlichen Prüfmeldungen.</p>;
  return <ul className="meldungen">{meldungen.map((m, i) => <li key={i} className={m.stufe === 'Fehler' ? 'meldung-fehler' : 'meldung-warnung'}><strong><span aria-hidden="true">{m.stufe === 'Fehler' ? '×' : '△'}</span> {m.stufe}</strong><p>{m.text}</p>{m.quelle && <p className="klein">Quelle: <code>{m.quelle.blatt}!{m.quelle.zelle}</code> · Zeile {m.quelle.zeile}</p>}</li>)}</ul>;
}
function Importergebnis({ daten }: { daten: Importansicht }) {
  const [filter, setFilter] = useState('Alle');
  const [detail, setDetail] = useState<number | null>(null);
  const zeilen = daten.zeilen.filter(z => filter === 'Alle' || (filter === 'Fehler' ? z.hatFehler : z.meldungen.some(m => m.stufe === 'Warnung')));
  return <>
    <section className="pruefsumme" aria-label="Prüfergebnis"><div><p className="oberzeile">2 · Prüfergebnis</p><h2>{daten.hatFehler ? 'Fehler vor einer Übernahme klären' : 'Vorschau geprüft · Freigabe bleibt offen'}</h2><p>{daten.zeilenanzahl} Datenzeilen · {daten.fehlerzeilen} Zeilen mit Fehlern. Keine Importfreigabe und keine Buchung.</p></div><Status wert={daten.hatFehler ? { text: 'Fehler vorhanden', ton: 'fehler', symbol: '×' } : importstatus(false, true)} /></section>
    <details className="karte dateimeldungen" open={daten.zeilen.length === 0 && daten.hatFehler}><summary>Meldungen zur Datei ({daten.meldungen.length})</summary><Meldungsliste meldungen={daten.meldungen} /></details>
    <section className="karte listenkarte" aria-label="Geprüfte Datensätze"><div className="werkzeuge"><div><h2>Datensätze prüfen</h2><p className="klein">Zeile öffnen für Meldungen, Originalwerte und Quellen.</p></div><div><label htmlFor="prueffilter">Prüfmeldungen filtern</label><select id="prueffilter" value={filter} onChange={e => setFilter(e.target.value)}><option>Alle</option><option value="Fehler">Mit Fehlern</option><option value="Warnung">Mit Warnungen</option></select></div></div>
      <p className="ergebniszahl" role="status">{zeilen.length} von {daten.zeilen.length} Datensätzen angezeigt</p>
      {zeilen.length === 0 ? <p className="zustand">{daten.zeilen.length === 0 ? 'Es liegen keine Vorschauzeilen vor.' : 'Keine Datensätze für diesen Filter. Wählen Sie „Alle“.'}</p> : <div className="listen-scroll" onKeyDown={tabellenTasten} role="region" aria-label="Importtabelle, auf Tablet seitlich scrollbar" tabIndex={0}><table className="datentabelle importtabelle"><caption className="nur-vorlesen">Geprüfte Daten aus der synthetischen XLSX-Datei. Zeilennummer öffnet Quellen.</caption><thead><tr><th scope="col">Zeile / Vorgang</th><th scope="col">Beteiligte</th><th scope="col" className="geld">Betrag</th><th scope="col">Prüfstatus</th><th scope="col">Prüfhinweis</th></tr></thead><tbody>{zeilen.map(z => <Fragment key={z.zeile}><tr>
        <th scope="row"><button className="textknopf" aria-expanded={detail === z.zeile} aria-controls={`import-${z.zeile}`} onClick={() => setDetail(detail === z.zeile ? null : z.zeile)}>Zeile {z.zeile} <span aria-hidden="true">{detail === z.zeile ? '−' : '+'}</span></button><span className="unterzeile">{z.art ?? 'Nicht übernommen'}{z.datum && ` · ${datumAnzeigen(z.datum)}`}</span></th>
        <td data-label="Beteiligte">{z.von && z.an ? <><span className="parteizeile"><span className="klein">{z.art === 'Rechnung' ? 'Aussteller' : 'Zahler'}</span>{z.von}</span><span className="parteizeile"><span className="klein">{z.art === 'Rechnung' ? 'Empfänger' : 'Zahlungsempfänger'}</span>{z.an}</span></> : 'Keine geprüfte Zuordnung'}</td><td className="geld" data-label="Betrag">{z.betrag ?? 'Nicht übernommen'}</td><td data-label="Prüfstatus"><Status wert={importstatus(z.hatFehler, z.meldungen.some(m => m.stufe === 'Warnung'))} /></td><td data-label="Prüfhinweis">{z.meldungen.find(m => m.stufe === 'Fehler')?.text ?? z.meldungen[0]?.text ?? 'Keine Zeilenfehler. Keine Importfreigabe.'}</td>
      </tr><tr className="detailzeile" hidden={detail !== z.zeile} id={`import-${z.zeile}`}><td colSpan={5}><div className="detailinhalt"><h3>Zeile {z.zeile} · Originalwerte und Quellen</h3><Meldungsliste meldungen={z.meldungen} />
        <div className="tabellenbereich" onKeyDown={tabellenTasten} role="region" aria-label={`Originalwerte Zeile ${z.zeile}`} tabIndex={0}><table className="quelltabelle"><caption>Unveränderte Zellwerte · horizontal scrollbar</caption><thead><tr><th scope="col">Blatt / Zelle / Zeile</th><th scope="col">Datentyp</th><th scope="col">Originalwert</th><th scope="col">Aufgelöster Text</th></tr></thead><tbody>{z.zellen.map(c => <tr key={c.quelle.zelle}><th scope="row">{c.quelle.blatt}!{c.quelle.zelle}<span className="unterzeile">Zeile {c.quelle.zeile}</span></th><td>{c.datentyp}{c.formatindex !== null && ` · Format ${c.formatindex}`}</td><td>{c.originalwert ?? '—'}{c.formel !== null && <p>Formel (nicht ausgeführt): {c.formel}</p>}</td><td>{c.text}</td></tr>)}</tbody></table></div>
      </div></td></tr></Fragment>)}</tbody></table></div>}
    </section><details className="karte technische-details"><summary>Dateiherkunft und vollständiger Prüfbericht</summary><p className="klein">Datei-Hash (SHA-256) · identifiziert Dateibytes, bestätigt keine Echtheit.</p><code className="hash">{daten.dateiHash ?? 'Kein vollständiger Datei-Hash verfügbar.'}</code><pre className="bericht">{daten.bericht}</pre></details>
  </>;
}
function Importvorschau() {
  const [beispiel, setBeispiel] = useState<Beispiel>('fehler');
  const laden = useCallback((signal: AbortSignal) => ladeImport(beispiel, signal), [beispiel]);
  const { daten, fehler, laedt, erneut } = useDaten<Importansicht>(laden);
  return <><div className="seitenkopf"><p className="oberzeile">Daten / Importvorschau</p><h1>Excel-Importvorschau</h1><p>Erst prüfen, dann entscheiden. Originalwerte und Herkunft bleiben erhalten.</p></div>
    <ol className="schritte" aria-label="Orientierung für die Vorschau, keine Buchung"><li>1 · Beispiel auswählen</li><li>2 · Datensätze prüfen</li><li>3 · Prüfbericht ansehen</li></ol>
    <section className="karte beispielauswahl" aria-label="Künstliche Beispieldatei auswählen"><div><label htmlFor="beispiel">Synthetisches XLSX-Beispiel</label><select id="beispiel" value={beispiel} onChange={e => setBeispiel(e.target.value as Beispiel)}><option value="fehler">Rechnung, Zahlung und fehlerhafter Betrag</option><option value="gueltig">Rechnung und unbestätigte Zahlung</option><option value="dublette">Widersprüchliche Zahlungsdublette</option></select></div><button disabled={laedt} onClick={erneut}>Vorschau erneut prüfen</button><p className="klein">Lokal erzeugte Testdateien. Kein Upload eigener Dateien, keine Makroausführung und keine Buchung.</p></section>
    {laedt && <p className="zustand" role="status">Beispieldatei wird geprüft …</p>}{fehler && <Fehler text={fehler} erneut={erneut} />}{daten && <Importergebnis daten={daten} />}
  </>;
}
export default function App() {
  const [ansicht, setAnsicht] = useState(() => window.location.hash === '#import' ? 'import' : 'rechnungen');
  useEffect(() => {
    const wechseln = () => {
      if (window.location.hash === '#import') setAnsicht('import');
      else if (window.location.hash === '#rechnungen' || window.location.hash === '') setAnsicht('rechnungen');
      // #inhalt ändert nur den Fokus, nicht die Ansicht.
    };
    window.addEventListener('hashchange', wechseln);
    return () => window.removeEventListener('hashchange', wechseln);
  }, []);
  return <><a className="sprunglink" href="#inhalt">Zum Inhalt</a><header className="appkopf"><div className="marke"><span className="markenzeichen" aria-hidden="true">B</span><div><strong>Betriebsplattform</strong><span className="klein">Klarheit im Büroalltag</span></div></div><span className="demohinweis">Lokaler Prototyp · Synthetische Daten</span></header>
    <nav aria-label="Hauptnavigation"><a href="#rechnungen" aria-current={ansicht === 'rechnungen' ? 'page' : undefined}>Rechnungen</a><a href="#import" aria-current={ansicht === 'import' ? 'page' : undefined}>Importvorschau</a></nav><main id="inhalt" tabIndex={-1}>{ansicht === 'import' ? <Importvorschau /> : <Rechnungsuebersicht />}</main><footer>ISD wird vorläufig als Geschäftspartner behandelt. Vollständiger Firmenname und Rolle sind je Vorgang zu prüfen. Keine echte Buchhaltung.</footer></>;
}