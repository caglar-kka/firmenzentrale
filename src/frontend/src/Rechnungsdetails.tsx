import { useEffect, useRef } from 'react';
import { Icon } from './Icon.js';
import { Status } from './ui.js';
import Kontaktkarte from './Kontaktkarte.js';
import { Dokumentbereich, Dokumentkarte, useDokumente } from './Dokumente.js';
import type { Rechnung } from './api.js';
import { datumAnzeigen, zahlungsstatus, eigeneRolle, zahlungsrichtung } from './darstellung.js';

export default function Rechnungsdetails({ rechnung: r, zurueck }: { rechnung: Rechnung; zurueck: () => void }) {
  const titel = useRef<HTMLHeadingElement>(null);
  const dokumente = useDokumente(r.vorgangId);
  const rolle = eigeneRolle(r);
  useEffect(() => { window.scrollTo(0,0); titel.current?.focus({ preventScroll: true }); }, []);
  return <section className="p-detail" aria-label="Rechnungs- und Zahlungsdetails">
    <div className="p-detailnavigation"><nav className="p-breadcrumb" aria-label="Brotkrumennavigation"><ol><li><button onClick={zurueck}>Rechnungen</button></li><li aria-current="page">{r.nummer}</li></ol></nav><button className="p-zurueck" onClick={zurueck}><Icon name="pfeil" />Zurück zur Rechnungsübersicht</button></div>
    <header className="p-detailkopf"><div><p className="p-eyebrow">Rechnung · {datumAnzeigen(r.datum)}{rolle && <> · Ansicht für {r.perspektive?.unternehmen}</>}</p><h1 ref={titel} tabIndex={-1}>{r.nummer}</h1></div><Status wert={zahlungsstatus(r.status)} /></header>
    <dl className="p-detailbetraege"><div><dt>Rechnungsbetrag</dt><dd>{r.betrag}</dd></div><div><dt>Bestätigt bezahlt</dt><dd>{r.bestaetigtZugeordnet}</dd></div><div><dt>{rolle === 'Empfänger' ? 'Noch zu bezahlen' : rolle === 'Aussteller' ? 'Noch zu erhalten' : 'Noch offen'}</dt><dd>{r.offenerBetrag}</dd></div></dl>
    <section className="p-firmenbereich" aria-label="Firma und Kontakt"><h2>Firma &amp; Kontakt</h2>
      <div className="p-kontakte"><Kontaktkarte organisation={r.aussteller} rolle="Rechnungsaussteller" kontakt={r.ausstellerKontakt} referenz={r.nummer} /><Kontaktkarte organisation={r.empfaenger} rolle="Rechnungsempfänger" kontakt={r.empfaengerKontakt} referenz={r.nummer} /></div>
      <details className="p-demohinweis"><summary>Synthetische Testkontakte</summary><p className="p-kontakthinweis">Testkontakte sind nicht erreichbar. Links benötigen eine passende E-Mail- oder Telefonie-Anwendung; es wird keine Nachricht versendet und kein Gespräch bestätigt.</p></details>
    </section>
    <div className="p-detailraster"><section className="p-abschnitt"><header><Icon name="zahlung" /><h2>Zahlungen</h2><span className="p-anzahl">{r.zahlungen.length}</span></header>
      {r.zahlungen.length === 0 ? <p className="p-notiz">Noch keine Zahlungen vorhanden.</p> : <ol className="p-zahlungsfolge">{r.zahlungen.map((z, i) => {
        const richtung = zahlungsrichtung(r,z);
        const dateien = z.id ? dokumente.daten?.dokumente.filter(d => d.zahlungId === z.id) ?? [] : [];
        return <li key={z.id ?? i}><span className="p-nummer" aria-hidden="true">{i+1}</span><article>
          <div className="p-zahlungskopf"><div><strong>{z.betrag}</strong><p>{richtung ?? `${z.zahler} → ${z.empfaenger}`}</p></div><Status wert={zahlungsstatus(z.status)} /></div>
          <dl className="p-zahlungsdaten"><div><dt>Zahlungsdatum</dt><dd>{datumAnzeigen(z.datum)}</dd></div><div><dt>Für diese Rechnung</dt><dd>{z.zugeordnet}</dd></div></dl>
          {dateien.map(d => <Dokumentkarte key={d.id} dokument={d} />)}
          <details className="p-zahlungsnachweis"><summary>Zahler, Empfänger und Quelle</summary><dl>
            {richtung && <><div><dt>Zahler</dt><dd>{z.zahler}</dd></div><div><dt>Zahlungsempfänger</dt><dd>{z.empfaenger}</dd></div></>}
            <div><dt>Belegreferenz</dt><dd>{z.beleg}</dd></div><div><dt>Quelle</dt><dd><code>{z.quellenort}</code></dd></div>
          </dl></details>
        </article></li>;
      })}</ol>}
      <p className="p-notiz">Unbestätigte Zahlungen reduzieren den offenen Betrag nicht. Keine Aussage zur Fälligkeit. Bearbeitungsvermerke werden noch nicht erfasst.</p>
    </section><aside className="p-nachweise">
      <Dokumentbereich zustand={dokumente} />
      <details className="p-rechnungsgrundlage"><summary>Angaben zur Originalrechnung</summary><dl><div><dt>Belegreferenz</dt><dd>{r.beleg}</dd></div><div><dt>Quelle der Rechnung</dt><dd><code>{r.quellenort}</code></dd></div></dl></details>
    </aside></div>
  </section>;
}
