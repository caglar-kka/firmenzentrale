import { useEffect, useRef, useState } from 'react';
import { Icon } from './Icon.js';
import { Status } from './ui.js';
import Kontaktkarte, { Kontaktaktionen } from './Kontaktkarte.js';
import Aktenbereich from './Aktenbereich.js';
import { useDokumente } from './Dokumente.js';
import type { Rechnung, Dokument } from './api.js';
import { datumAnzeigen, zahlungsstatus, eigeneRolle, zahlungsrichtung, zahlungsanzeige, rechnungsart } from './darstellung.js';

export default function Rechnungsdetails({ rechnung: r, zurueck }: { rechnung: Rechnung; zurueck: () => void }) {
  const titel = useRef<HTMLHeadingElement>(null);
  const dokumente = useDokumente(r.vorgangId);
  const [notizSignal,setNotizSignal]=useState(0);
  const [auswahl, setAuswahl] = useState<Dokument | null>(null);
  const rolle = eigeneRolle(r);
  const partner = rolle === 'Aussteller' ? r.empfaenger : r.aussteller;
  useEffect(() => { window.scrollTo(0,0); titel.current?.focus({ preventScroll: true }); }, []);
  return <section className="p-detail" aria-label="Rechnungs- und Zahlungsdetails">
    <div className="p-detailnavigation"><nav className="p-breadcrumb" aria-label="Brotkrumennavigation"><ol><li><button onClick={zurueck}>Rechnungen</button></li><li aria-current="page">{r.nummer}</li></ol></nav><button className="p-zurueck" onClick={zurueck}><Icon name="pfeil" />Zurück zur Rechnungsübersicht</button></div>
    <header className="p-detailkopf"><div><p className="p-eyebrow">{rechnungsart(r)} · {datumAnzeigen(r.datum)}{rolle && <> · Ansicht für {r.perspektive?.unternehmen}</>}</p><h1 ref={titel} tabIndex={-1}>{r.nummer}</h1><p className="p-detailpartner">{partner}</p></div><div><Status wert={zahlungsstatus(r.status)} /><button className="p-button-leise" onClick={()=>window.print()}>Akte drucken</button></div></header>
    <dl className="p-detailbetraege"><div><dt>Rechnungsbetrag</dt><dd>{r.betrag}</dd></div><div><dt>Bereits bestätigt bezahlt</dt><dd>{r.bestaetigtZugeordnet}</dd></div><div><dt>Noch offen</dt><dd>{r.offenerBetrag}</dd></div></dl>
    <div className="p-detailraster"><div className="p-vorgang">
      <dl className="p-kontakte" aria-label="Rechnung von und an"><Kontaktkarte organisation={r.aussteller} rolle="Rechnung von" kontakt={r.ausstellerKontakt} /><Kontaktkarte organisation={r.empfaenger} rolle="Rechnung an" kontakt={r.empfaengerKontakt} /></dl>
      <section className="p-abschnitt"><header><h2>Zahlungen</h2><span className="p-anzahl">{r.zahlungen.length}</span></header>
      {r.zahlungen.length === 0 ? <p className="p-notiz">Noch keine Zahlungen vorhanden.</p> : <ol className="p-zahlungsfolge">{r.zahlungen.map((z, i) => {
        const richtung = zahlungsrichtung(r,z);
        const beleg = dokumente.daten?.dokumente.find(d => d.zahlungId && d.zahlungId === z.id);
        return <li key={z.id ?? i}><article><div className="p-zahlungskopf"><div><time dateTime={z.datum}>{datumAnzeigen(z.datum)}</time><strong>{z.betrag}</strong><p>{richtung ?? `${z.zahler} → ${z.empfaenger}`}</p></div><Status wert={zahlungsanzeige(r,z)} /></div>
          <div className="p-zahlungsabschluss"><span>Für diese Rechnung: {z.zugeordnet}</span>{beleg ? <button className="p-textaktion" onClick={() => setAuswahl({...beleg})}>Zahlungsbeleg öffnen</button> : <span>Kein PDF-Zahlungsbeleg hinterlegt</span>}</div>
        </article></li>;
      })}</ol>}
      </section>

    </div><aside className="p-nachweise"><Kontaktaktionen rechnung={r} onNotiz={r.vorgangId?()=>setNotizSignal(n=>n+1):undefined} />{r.vorgangId?<Aktenbereich id={r.vorgangId} hinweise={r.zahlungen.filter(z=>z.status==='Zur Prüfung').map(z=>'Zahlung zur Prüfung: '+z.betrag+' vom '+datumAnzeigen(z.datum))} notizSignal={notizSignal} dokumentId={auswahl?.id} onDokumentSchliessen={()=>setAuswahl(null)} legacyKey={`betriebsplattform-demo-notiz:${JSON.stringify([r.aussteller,r.empfaenger,r.nummer])}`}/>:<p>Für diese Rechnung ist keine digitale Akte verfügbar.</p>}</aside></div>
  </section>;
}
