import{vorschauAntwort}from'./Vorschau.js';
let csrf='';
export function token(value:string){csrf=value;}
export async function apiFetch(input:RequestInfo|URL,init?:RequestInit):Promise<Response>{
 const base=typeof window==='undefined'?'http://127.0.0.1/':window.location.href;const url=new URL(typeof input==='string'?input:input instanceof URL?input.href:input.url,base);if(url.origin!==new URL(base).origin||!url.pathname.startsWith('/api/'))throw new Error('Nur lokale Anwendungsanfragen sind zugelassen.');
 const simulated=await vorschauAntwort(url,init);if(simulated)return simulated;
 const headers=new Headers(init?.headers);if(init?.method&& !['GET','HEAD'].includes(init.method.toUpperCase())&&csrf)headers.set('X-CSRF-TOKEN',csrf);
 const r=await fetch(input,{...init,headers,credentials:'same-origin'});if(r.status===401&&typeof window!=='undefined')window.dispatchEvent(new window.Event('fz-anmeldung'));if(r.status===403&&typeof window!=='undefined'){try{if((await r.clone().json()).mfaEinrichten)window.dispatchEvent(new window.Event('fz-anmeldung'));}catch{}}return r;
}
