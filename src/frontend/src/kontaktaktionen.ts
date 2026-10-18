// Bewusst begrenztes Aktionsformat, keine vollständige RFC-/Erreichbarkeitsprüfung.
// Keine Mehrfachempfänger, Header aus Adresswerten oder lokalen Telefonkontexte.
export function emailLink(wert: string | null | undefined, referenz: string): string | null {
  if (!wert || wert.length > 254 || /[^\x21-\x7e]/.test(wert)) return null;
  const teile = wert.split('@');
  if (teile.length !== 2) return null;
  const [lokal, domain] = teile;
  if (!lokal || !domain) return null;
  if (lokal.length > 64 || !/^[A-Za-z0-9_+-]+(?:\.[A-Za-z0-9_+-]+)*$/.test(lokal)) return null;
  const labels = domain.split('.');
  if (labels.length < 2 || labels.some(label => !/^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/.test(label))) return null;
  const adresse = `mailto:${encodeURIComponent(lokal)}@${domain}`;
  // Eine ungeeignete Referenz unterdrückt nur den optionalen Betreff.
  if (referenz.length === 0 || referenz.length > 128 || /[\u0000-\u001f\u007f-\u009f]/.test(referenz)) return adresse;
  try { return `${adresse}?subject=${encodeURIComponent(`Rechnung ${referenz}`)}`; }
  catch { return adresse; } // Ein beschädigtes Unicode-Zeichen darf die Detailansicht nicht abbrechen.
}

export function telefonLink(wert: string | null | undefined): string | null {
  if (!wert || wert.length > 40 || /[^\x20-\x7e]/.test(wert) || !/^\+[1-9]\d*(?:[ -]\d+)*$/.test(wert)) return null;
  const nummer = wert.replace(/[ -]/g, '');
  return /^\+[1-9]\d{6,14}$/.test(nummer) ? `tel:${nummer}` : null;
}
