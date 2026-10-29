import type {Umgebung} from './Arbeitskontext.js';
import type {Iconname} from './Icon.js';
export type Modul={id:string;titel:string;icon:Iconname;kategorie:'kern'|'mehr';bedarf:string[];persoenlich?:boolean;prioritaet:number;hub?:boolean};
export const modulregister:readonly Modul[]=[
 {id:'arbeitsplatz',titel:'Dashboard',icon:'dashboard',kategorie:'kern',bedarf:[],prioritaet:0},
 {id:'vorgaenge',titel:'Akten',icon:'akte',kategorie:'kern',bedarf:['Akten'],prioritaet:1,hub:true},
 {id:'kontakte',titel:'Kontakte',icon:'personen',kategorie:'kern',bedarf:[],prioritaet:2,hub:true},
 {id:'rechnungen',titel:'Rechnungen',icon:'rechnung',kategorie:'kern',bedarf:['Finanzen'],prioritaet:3,hub:true},
 {id:'posteingang',titel:'Posteingang',icon:'email',kategorie:'kern',bedarf:['Akten','Dokumente'],prioritaet:4},
 {id:'aufgaben',titel:'Aufgaben',icon:'aufgaben',kategorie:'kern',bedarf:[],persoenlich:true,prioritaet:5,hub:true},
 {id:'kalender',titel:'Kalender',icon:'kalender',kategorie:'kern',bedarf:[],persoenlich:true,prioritaet:6,hub:true},
 {id:'dokumente',titel:'Dokumente',icon:'beleg',kategorie:'mehr',bedarf:['Akten','Dokumente'],prioritaet:7,hub:true},
 {id:'prozesse',titel:'Arbeitsabläufe',icon:'prozess',kategorie:'mehr',bedarf:['Akten','Dokumente'],persoenlich:true,prioritaet:8,hub:true},
 {id:'berichte',titel:'Auswertungen',icon:'bericht',kategorie:'mehr',bedarf:['Finanzen'],prioritaet:9},
 {id:'leistungen',titel:'Leistungsnachweise',icon:'aufgaben',kategorie:'mehr',bedarf:[],persoenlich:true,prioritaet:10},
 {id:'import',titel:'Importe',icon:'import',kategorie:'mehr',bedarf:['Import'],prioritaet:10}];
export function sichtbareModule(u:Umgebung){return modulregister.filter(m=>(m.id!=='leistungen'||u.authentifiziert&&u.leistungsnachweiseAktiv)&&(!m.persoenlich||u.authentifiziert||u.vorschau)&&m.bedarf.every(b=>(u.module??[]).includes(b))).sort((a,b)=>a.prioritaet-b.prioritaet);}
