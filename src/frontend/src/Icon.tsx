import type { ReactNode } from 'react';

// Eigene einfache Geometrien; keine externen Assets, Fonts oder Iconpakete.
const formen = {
  kontakt: <><circle cx="12" cy="12" r="9" /><path d="M12 11v6m0-10v.1" /></>,
  email: <><rect x="3" y="5" width="18" height="14" rx="2" /><path d="m4 7 8 6 8-6" /></>,
  telefon: <path d="m8 3 3 5-3 3a16 16 0 0 0 5 5l3-3 5 3v3a2 2 0 0 1-2 2C10 21 3 14 3 5a2 2 0 0 1 2-2h3Z" />,
  marke: <><rect x="3" y="3" width="7" height="7" rx="2" /><rect x="14" y="3" width="7" height="7" rx="2" /><rect x="3" y="14" width="7" height="7" rx="2" /><path d="M14 17h7m-3.5-3.5v7" /></>,
  plattform: <><path d="M5 21V3h7a4.5 4.5 0 0 1 0 9H5m7 0h2a4.5 4.5 0 0 1 0 9H5" /><path d="m9 7 4 5-4 5" /></>,
  sidebar: <><rect x="3" y="4" width="18" height="16" rx="3" /><path d="M9 4v16m7-11-3 3 3 3" /></>,
  filter: <><path d="M4 6h16M4 12h16M4 18h16" /><circle cx="8" cy="6" r="2" fill="white" /><circle cx="16" cy="12" r="2" fill="white" /><circle cx="10" cy="18" r="2" fill="white" /></>,
  beleg: <><path d="M7 3h10v18l-2-1.5-3 1.5-3-1.5L7 21V3Z" /><path d="M10 7h4m-4 4h4m-4 4h3" /></>,
  zahlung: <><rect x="3" y="6" width="18" height="13" rx="2" /><circle cx="12" cy="12.5" r="3" /><path d="M6 10v5m12-5v5" /></>,
  rechnung: <><path d="M7 3h7l4 4v14H6V3h1Zm7 0v5h4M9 12h6m-6 4h6" /></>,
  import: <><path d="M5 3h14v18H5zM5 9h14M11 9v12M8 6h8" /><path d="m14 14 2 2 2-2m-2-3v5" /></>,
  betrag: <><path d="M4 5h12v14H4zM8 2h12v14" /><path d="M7 9h6m-6 4h6m-6 3h3" /></>,
  saldo: <><path d="M4 6h12m-3-3 3 3-3 3M20 18H8m3-3-3 3 3 3" /><path d="M4 12h16" /></>,
  suche: <><circle cx="10" cy="10" r="6" /><path d="m15 15 5 5" /></>,
  pfeil: <path d="m7 10 5 5 5-5" />,
  bezahlt: <><circle cx="12" cy="12" r="9" /><path d="m8 12 3 3 5-6" /></>,
  teilweise: <><circle cx="12" cy="12" r="9" /><path d="M12 3a9 9 0 0 1 0 18Z" fill="currentColor" fillOpacity=".22" stroke="none" /><path d="M12 3v18" /></>,
  offen: <><path d="M20 11a8 8 0 1 1-7-7" /><circle cx="19" cy="5" r="2" /></>,
  pruefen: <><path d="m12 3 10 18H2L12 3Z" /><path d="M12 9v5m0 3v.1" /></>,
  fehler: <><circle cx="12" cy="12" r="9" /><path d="m9 9 6 6m0-6-6 6" /></>,
  quelle: <><path d="M9 4H4v16h16v-5M13 4h7v7m0-7L10 14" /><path d="M8 17h4" /></>,
  erneut: <><path d="M20 10a8 8 0 1 0-1 7M20 4v6h-6" /></>,
} satisfies Record<string, ReactNode>;

export type Iconname = keyof typeof formen;
export function Icon({ name }: { name: Iconname }) {
  return <svg className={`icon icon-${name}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">{formen[name]}</svg>;
}
