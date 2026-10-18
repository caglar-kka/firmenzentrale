import type { Kontakt } from './api.js';
import { Icon } from './Icon.js';
import { emailLink, telefonLink } from './kontaktaktionen.js';

interface KontaktkarteProps { organisation: string; rolle: string; kontakt?: Kontakt | null; referenz: string }
export default function Kontaktkarte({ organisation, rolle, kontakt, referenz }: KontaktkarteProps) {
  const email = emailLink(kontakt?.email, referenz);
  const telefon = telefonLink(kontakt?.telefon);
  return <article className="p-kontaktkarte" aria-label={`Kontakt ${rolle}`}>
    <div className="p-kontaktname"><span className="p-kontaktrolle">{rolle === 'Rechnungsaussteller' ? 'Rechnung von' : rolle === 'Rechnungsempfänger' ? 'Rechnung an' : rolle}</span><strong>{organisation}</strong>{kontakt?.ansprechpartner && <small>{kontakt.ansprechpartner}</small>}</div>
    <div className="p-kontaktaktionen">
      {email && <a className="p-aktion" href={email} aria-label={`E-Mail an ${organisation} schreiben`}><Icon name="email" /><span className="p-tooltip" role="tooltip">E-Mail schreiben</span></a>}
      {telefon && <a className="p-aktion" href={telefon} aria-label={`${organisation} anrufen`}><Icon name="telefon" /><span className="p-tooltip" role="tooltip">Anrufen</span></a>}
      <details className="p-kontaktdetails"><summary className="p-aktion" aria-label={`Kontaktdetails für ${organisation}`}><Icon name="kontakt" /><span className="p-tooltip" role="tooltip">Kontaktdetails</span></summary><dl>
        {!kontakt?.ansprechpartner && <div><dt>Ansprechpartner</dt><dd>Kein Ansprechpartner hinterlegt.</dd></div>}
        <div><dt>E-Mail</dt><dd>{email ? kontakt?.email : kontakt?.email ? 'E-Mail-Adresse nicht nutzbar.' : 'Keine E-Mail-Adresse hinterlegt.'}</dd></div>
        <div><dt>Telefon</dt><dd>{telefon ? kontakt?.telefon : kontakt?.telefon ? 'Telefonnummer nicht nutzbar.' : 'Keine Telefonnummer hinterlegt.'}</dd></div>
        {kontakt?.quelle && <div><dt>Kontaktquelle</dt><dd>{kontakt.quelle}</dd></div>}
      </dl></details>
    </div>
  </article>;
}
