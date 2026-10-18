import { useCallback, useEffect, useState } from 'react';
import { ladeDokumente, type Dokumentenliste } from './api.js';
import { useDaten } from './ui.js';

export function useDokumente(vorgangId?: string | null) {
  const laden = useCallback((signal: AbortSignal) => vorgangId ? ladeDokumente(vorgangId, signal)
    : Promise.resolve<Dokumentenliste>({ synthetisch: true, vorgangId: '', dokumente: [] }), [vorgangId]);
  return useDaten(laden);
}
export function PdfVorschau({url,titel}:{url:string;titel:string}) {
 const[status,S]=useState('laden'),[versuch,V]=useState(0);
 useEffect(()=>{const c=new AbortController();S('laden');void fetch(url,{signal:AbortSignal.any([c.signal,AbortSignal.timeout(8000)])}).then(async r=>{if(!r.ok||!r.headers?.get('Content-Type')?.startsWith('application/pdf'))throw new Error();await r.arrayBuffer();if(!c.signal.aborted)S('bereit');}).catch(()=>{if(!c.signal.aborted)S('fehler');});return()=>c.abort();},[url,versuch]);
 if(status==='laden')return <p role="status">PDF wird geladen …</p>;
 if(status==='fehler')return <div role="alert"><p>PDF nicht verfügbar. Bitte erneut versuchen.</p><button onClick={()=>V(n=>n+1)}>Erneut laden</button></div>;
 return <iframe className="p-pdf-vorschau" src={url+(url.includes('#')?'&':'#')+'view=FitH'} title={titel} onError={()=>S('fehler')}/>;
}
