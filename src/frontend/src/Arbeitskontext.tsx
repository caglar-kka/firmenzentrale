import{createContext,useContext,useEffect,useState,type ReactNode}from'react';
import{anfrage,type Kategorie}from'./akte-api.js';
export interface Umgebung{beispielkennzeichnung:boolean;diagnose:boolean;benutzer:string;kategorienVerwalten:boolean;kontakteVerwalten:boolean;aktenVerwalten?:boolean}
const standard:Umgebung={beispielkennzeichnung:false,diagnose:false,benutzer:'',kategorienVerwalten:false,kontakteVerwalten:false};
const Kontext=createContext(standard);
export const useUmgebung=()=>useContext(Kontext);
export function Arbeitskontext({children}:{children:ReactNode}){const[u,U]=useState(standard);useEffect(()=>{const c=new AbortController();void anfrage<Umgebung>('arbeitsumgebung',undefined,c.signal).then(u=>{if(typeof u.beispielkennzeichnung==='boolean'&&!c.signal.aborted)U(u);}).catch(()=>{});return()=>c.abort();},[]);return<Kontext.Provider value={u}>{children}</Kontext.Provider>;}
const Navigation=createContext<(bereich:string,id?:string|null,dokument?:string|null,eingang?:string|null)=>void>(()=>{});
export const Navigationsanbieter=Navigation.Provider;export const useNavigation=()=>useContext(Navigation);
export function useKategorien(){const[k,K]=useState<Kategorie[]>([]);useEffect(()=>{const c=new AbortController();void anfrage<Kategorie[]>('kategorien',undefined,c.signal).then(v=>{if(!c.signal.aborted)K(v);}).catch(()=>{});return()=>c.abort();},[]);return k;}
export function Breadcrumb({bereich,titel}:{bereich:string;titel:string}){const nav=useNavigation();return<nav className="p-breadcrumb" aria-label="Brotkrumennavigation"><ol><li><button onClick={()=>nav(bereich.toLowerCase()==='importe'?'import':['vorgänge','akten'].includes(bereich.toLowerCase())?'vorgaenge':bereich.toLowerCase())}>{bereich}</button></li><li aria-current="page">{titel}</li></ol></nav>;}
