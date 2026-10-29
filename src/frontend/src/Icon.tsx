import type { ReactNode } from 'react';

// Eigene einfache Geometrien; keine externen Assets, Fonts oder Iconpakete.
const formen = {
  flagge: <><path d="M5 22V3m0 1c5-4 8 4 15 0v10c-7 4-10-4-15 0"/><path d="M6 5c4-2 8 4 13 1v6c-5 3-9-3-13-1Z" fill="currentColor" stroke="none"/></>,
  kalender: <><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M7 2v6m10-6v6M3 11h18M7 15h2m3 0h2m3 0h1"/></>,
  uhr: <><circle cx="12" cy="12" r="9"/><path d="M12 6v6l4 2"/></>,
  prozess: <><circle cx="12" cy="4" r="2"/><circle cx="4" cy="19" r="2"/><circle cx="20" cy="19" r="2"/><circle cx="12" cy="12" r="2"/><path d="M12 6v4m-2 3-5 4m9-4 5 4"/></>,
  bericht: <><path d="M5 3h11l4 4v14H5Z"/><path d="M8 17v-4m4 4V9m4 8v-6"/></>,
  dashboard: <><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="11" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><path d="M14 18h7m-7 3h7"/></>,
  personen: <><circle cx="9" cy="8" r="3"/><path d="M3 21v-3a6 6 0 0 1 12 0v3M16 5a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 5v2"/></>,
  aufgaben: <><rect x="4" y="4" width="16" height="17" rx="2"/><path d="M9 3h6v3H9zm-2 10 3 3 7-7"/></>,
  akte: <path d="M3 7V4h6l3 3h9v13H3V7Z"/>,
  einstellungen: <><path d="m9 3-1 3-3 1v4l-2 1 2 2v4l3 1 1 2h5l1-2 3-1v-4l2-2-2-1V7l-3-1-1-3Z"/><circle cx="11.5" cy="12" r="3"/></>,
  ki: <><path d="m12 2 2.5 7.5L22 12l-7.5 2.5L12 22l-2.5-7.5L2 12l7.5-2.5Z"/></>,
  profil: <><circle cx="12" cy="8" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></>,
  firma: <><path d="M4 21V3h12v18M16 10h4v11H2M8 7h4M8 11h4M8 15h4M9 21v-3h2v3"/></>,
  notiz: <><path d="M5 3h14v18H5zM8 8h8M8 12h8M8 16h5" /></>,
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
