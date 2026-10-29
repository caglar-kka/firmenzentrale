import{test}from'node:test';import assert from'node:assert/strict';import{JSDOM}from'jsdom';import{act,createElement as h}from'react';import{createRoot}from'react-dom/client';import{Anmeldung,Abmelden,EigenesPasswort}from'../node_modules/.cache/betriebsplattform-tests/Anmeldung.js';import{apiFetch}from'../node_modules/.cache/betriebsplattform-tests/auth.js';
async function mount(fetcher,check,zusatz=null){const dom=new JSDOM('<div id="root"></div>',{url:'https://127.0.0.1:5180/'});const old={fetch:globalThis.fetch,FormData:globalThis.FormData};globalThis.window=dom.window;globalThis.document=dom.window.document;globalThis.FormData=dom.window.FormData;globalThis.IS_REACT_ACT_ENVIRONMENT=true;dom.window.HTMLElement.prototype.attachEvent=()=>{};dom.window.HTMLElement.prototype.detachEvent=()=>{};globalThis.fetch=(path,options)=>String(path).endsWith('/kontaktrechte')&&(!options?.method||options.method==='GET')?Promise.resolve(ok({version:0,modus:'Ohne Freigabe',vorlageId:null,aktionen:[],vorlagen:[]})):fetcher(path,options);const root=createRoot(document.getElementById('root'));try{await act(async()=>root.render(h(Anmeldung,null,h('div',null,'Geschützter Arbeitsplatz',h(Abmelden),zusatz))));await check();}finally{await act(async()=>root.unmount());globalThis.fetch=old.fetch;globalThis.FormData=old.FormData;delete globalThis.window;delete globalThis.document;delete globalThis.IS_REACT_ACT_ENVIRONMENT;dom.window.close();}}
const ok=data=>({ok:true,status:200,json:async()=>data});
async function benutzerSuchen(el,text){const input=el.querySelector('input[type=search]');await act(async()=>{Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set.call(input,text);input.dispatchEvent(new window.Event('input',{bubbles:true}));});}
async function warteAuf(check){for(let i=0;i<80;i++){await act(async()=>new Promise(r=>setTimeout(r,25)));if(check())return;}assert.fail('Erwarteter Navigationszustand wurde nicht erreicht.');}

test('Anmeldung: Demo bleibt explizit ohne Anmeldemaske',async()=>mount(async()=>ok({aktiv:false}),async()=>assert.match(document.body.textContent,/Geschützter Arbeitsplatz/)));
test('Anmeldung: geschützter Bereich erst nach Login, CSRF im Header, Logout entfernt Inhalt',async()=>{let signed=false;await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:true,angemeldet:signed,csrf:signed?'csrf-auth':'csrf-anonym'});assert.equal(options.headers.get('X-CSRF-TOKEN'),signed?'csrf-auth':'csrf-anonym');assert.equal(options.credentials,'same-origin');if(path==='/api/auth/login')signed=true;if(path==='/api/auth/logout')signed=false;return ok({});},async()=>{assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);assert.equal(document.querySelectorAll('[required][aria-required="true"]').length,2);document.querySelector('[name=login]').value='muster';document.querySelector('[name=passwort]').value='a'.repeat(30);await act(async()=>document.querySelector('form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.body.textContent,/Geschützter Arbeitsplatz/);await act(async()=>apiFetch('/api/akten/a/bearbeitungsstatus',{method:'POST',body:'{}'}));assert.equal(window.localStorage.length,0);await assert.rejects(()=>apiFetch('https://example.invalid/api/x',{method:'POST'}),/lokale/);await act(async()=>document.querySelector('button').click());assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);});});
test('Anmeldung: Verbindungsfehler öffnet niemals den Arbeitsplatz',async()=>mount(async()=>({ok:false,status:503}),async()=>{assert.match(document.body.textContent,/nicht erreichbar/);assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);}));


test('Mehrbenutzer: sichtbare Liste aktualisiert sich, Entwurf bleibt, Offline und Rueckkehr sind kontrolliert',async()=>{
 const {useDatenbankliste}=await import('../node_modules/.cache/betriebsplattform-tests/Buero.js');
 const {useState}=await import('react');let ticks=[];let names=['Firma A'];let requests=0;let denied=false;
 await mount(async(path)=>{if(path==='/api/auth/session')return ok({aktiv:false});requests++;if(denied)return {ok:false,status:403,json:async()=>({meldung:'Keine Berechtigung.'})};return ok({eintraege:names.map((firma,i)=>({id:String(i),firma,werte:[],historie:[]})),anzahl:names.length});},async()=>{
  function Liste(){const v=useDatenbankliste(true,'kontakte?seite=0');const[d,D]=useState('Eigener Entwurf');return h('section',null,h('input',{value:d,onChange:e=>D(e.target.value)}),h('output',null,v.daten?.eintraege.map(k=>k.firma).join(', ')),v.fehler&&h('p',{role:'alert'},v.fehler));}
  const originalSet=window.setInterval,originalClear=window.clearInterval;window.setInterval=(f,ms)=>{assert.equal(ms,5000);ticks.push(f);return ticks.length;};window.clearInterval=()=>{};
  const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
  try{await act(async()=>root.render(h(Liste)));await act(async()=>new Promise(r=>setTimeout(r,240)));assert.match(el.textContent,/Firma A/);
   names.push('Firma B');await act(async()=>ticks[0]());await act(async()=>new Promise(r=>setTimeout(r,240)));assert.match(el.textContent,/Firma B/);assert.equal(el.querySelector('input').value,'Eigener Entwurf');
   Object.defineProperty(window.navigator,'onLine',{configurable:true,value:false});const before=requests;await act(async()=>ticks[0]());assert.equal(requests,before);
   Object.defineProperty(window.navigator,'onLine',{configurable:true,value:true});names.push('Firma C');await act(async()=>window.dispatchEvent(new window.Event('online')));await act(async()=>new Promise(r=>setTimeout(r,240)));assert.match(el.textContent,/Firma C/);
   denied=true;await act(async()=>window.dispatchEvent(new window.Event('focus')));await act(async()=>new Promise(r=>setTimeout(r,240)));assert.doesNotMatch(el.querySelector('output').textContent,/Firma/);assert.match(el.textContent,/Keine Berechtigung/);assert.equal(el.querySelector('input').value,'Eigener Entwurf');
  }finally{await act(async()=>root.unmount());window.setInterval=originalSet;window.clearInterval=originalClear;el.remove();}
 });
});

test('Benutzerverwaltung: Erstzugang ohne Administratorpasswort, Status, Kopieren, Neu und Widerruf',async()=>{
 const {default:Verwaltung}=await import('../node_modules/.cache/betriebsplattform-tests/Benutzerverwaltung.js');let angelegt=null,users=[],version=2,code='synthetischer-code',copied='';
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:false});if(options.method==='POST'){
  const body=JSON.parse(options.body);if(path==='/api/admin/benutzer'){angelegt=body;users=[{id:'u',login:body.login,anzeigename:body.anzeigename,rolle:body.rolle,aktiv:true,eingerichtet:false,einladungBis:'2099-01-01T00:00:00Z',version}];}
  else{assert.equal(body.version,version);version++;users[0].version=version;users[0].einladungBis=body.widerrufen?null:'2099-01-01T00:00:00Z';code='neuer-synthetischer-code';if(body.widerrufen)return ok(null);}
  return ok({id:'u',code,gueltigBis:'2099-01-01T00:00:00Z'});
 }return ok(users);},async()=>{
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);Object.defineProperty(window.navigator,'clipboard',{value:{writeText:async value=>{copied=value;}},configurable:true});
 try{await act(async()=>root.render(h(Verwaltung)));const form=el.querySelector('form');assert.equal(form.querySelector('[type=password]'),null);form.elements.login.value='musterteam';form.elements.anzeigename.value='Musterperson';await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(angelegt.rolle,'Mitarbeiter');assert.equal(angelegt.login,'musterteam');assert.equal('passwort' in angelegt,false);assert.match(el.textContent,/1 Benutzer geladen/);assert.equal(el.querySelector('[readonly]').value,code);
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Code kopieren').click());assert.equal(copied,code);await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Anzeige schließen').click());assert.equal(el.querySelector('[readonly]'),null);
 await benutzerSuchen(el,'musterteam');assert.match(el.textContent,/Einladung ausstehend/);await act(async()=>el.querySelector('[aria-label="Benutzerliste"] button').click());
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Erstzugang neu ausstellen').click());assert.equal(el.querySelector('[readonly]').value,'neuer-synthetischer-code');
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Einladung widerrufen').click());assert.match(el.textContent,/Einladung widerrufen/);assert.equal(el.querySelector('[readonly]'),null);assert.equal(window.localStorage.length,0);
 }finally{await act(async()=>root.unmount());el.remove();}
 });
});


test('Benutzerverwaltung: Benutzerliste und erneuerter Erstzugang bleiben am betroffenen Benutzer',async()=>{
 const {default:Verwaltung}=await import('../node_modules/.cache/betriebsplattform-tests/Benutzerverwaltung.js');
 const code='a'.repeat(32)+'.'+'B'.repeat(64);let fail=false,copied='',version=2;
 const users=()=>['anna','bert'].map(id=>({id,login:id,anzeigename:id==='anna'?'Anna Muster':'Bert Beispiel',rolle:'Mitarbeiter',aktiv:true,eingerichtet:false,einladungBis:'2099-01-01T00:00:00Z',version}));
 await mount(async(path,options)=>{
  if(path==='/api/auth/session')return ok({aktiv:false});
  if(options.method==='POST'){assert.equal(path,'/api/admin/benutzer/bert/einladung');assert.deepEqual(JSON.parse(options.body),{version,widerrufen:false});if(fail)return {ok:false,status:409};version++;return ok({id:'bert',code,gueltigBis:'2099-01-01T00:00:00Z'});}
  assert.equal(path,'/api/admin/benutzer');return ok(users());
 },async()=>{
  const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
  Object.defineProperty(window.navigator,'clipboard',{value:{writeText:async value=>{copied=value;}},configurable:true});
  try{
   await act(async()=>root.render(h(Verwaltung)));
   const list=el.querySelector('[aria-label="Benutzerliste"]');assert.ok(list);assert.match(el.textContent,/Benutzersuche.*2 Benutzer/s);assert.equal(list.children.length,0);assert.equal(list.classList.contains('p-aktenliste'),false);
   await benutzerSuchen(el,'bert');await act(async()=>list.children[0].querySelector('button').click());
   const row=el.querySelector('[aria-label="Benutzer bearbeiten"] li'),renew=()=>[...row.querySelectorAll('button')].find(b=>b.textContent==='Erstzugang neu ausstellen');
   for(const select of el.querySelectorAll('select[name=rolle]'))assert.deepEqual([...select.options].map(o=>o.value),['Mitarbeiter','Administrator']);
   await act(async()=>renew().click());assert.equal(el.querySelectorAll('aside').length,1);const panel=row.querySelector('aside');assert.ok(panel);assert.equal(document.activeElement,panel);assert.match(panel.textContent,/vorherige Code ist.*ungültig/);assert.match(panel.textContent,/Gültig bis/);assert.equal(panel.querySelector('input[readonly]').value,code);
   assert.doesNotMatch(panel.querySelector('[role=status]').textContent,new RegExp(code));
   await act(async()=>[...panel.querySelectorAll('button')].find(b=>b.textContent==='Code kopieren').click());assert.equal(copied,code);
   await act(async()=>[...panel.querySelectorAll('button')].find(b=>b.textContent==='Anzeige schließen').click());assert.equal(el.querySelector('input[readonly]'),null);
   fail=true;await act(async()=>renew().click());assert.match(row.querySelector('[role=alert]').textContent,/Aktion nicht bestätigt/);assert.equal(el.querySelectorAll('[role=alert]').length,1);assert.equal(el.querySelector('input[readonly]'),null);
   assert.equal(window.localStorage.length,0);assert.equal(window.sessionStorage.length,0);assert.doesNotMatch(window.location.href,new RegExp(code));
  }finally{await act(async()=>root.unmount());el.remove();}
 });
});

test('Benutzersuche: Name und Login live filtern, einzeln bearbeiten und alle wieder anzeigen ohne Schreibanfrage',async()=>{
 const {default:Verwaltung}=await import('../node_modules/.cache/betriebsplattform-tests/Benutzerverwaltung.js');let reads=0;
 const users=[{id:'anna',anzeigename:'Anna Muster',login:'buero.nord',rolle:'Mitarbeiter',aktiv:true,eingerichtet:true,version:3},{id:'bert',anzeigename:'Bert Beispiel',login:'team.sued',rolle:'Mitarbeiter',aktiv:false,eingerichtet:false,version:5}];
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:false});assert.notEqual(options.method,'POST');assert.equal(path,'/api/admin/benutzer');reads++;return ok(users);},async()=>{
  const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
  try{await act(async()=>root.render(h(Verwaltung)));const search=el.querySelector('input[type=search]');assert.ok(search);assert.equal(search.placeholder,'Benutzer suchen …');
   const results=()=>el.querySelector('[aria-label="Benutzerliste"]');
   async function suchen(value){await act(async()=>{Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set.call(search,value);search.dispatchEvent(new window.Event('input',{bubbles:true}));});}
   assert.equal(results().children.length,0);assert.equal(el.querySelector('[aria-label="Benutzer bearbeiten"]'),null);
   await suchen('  ANNA  ');assert.equal(results().children.length,1);assert.match(results().textContent,/Anna Muster/);
   await suchen('TEAM.SUED');assert.equal(results().children.length,1);assert.match(results().textContent,/Bert Beispiel/);
   await suchen('unbekannt');assert.match(el.textContent,/Keine Benutzer gefunden/);
   await suchen('');assert.equal(results().children.length,0);await suchen('anna');
   const before=reads;await act(async()=>results().children[0].querySelector('button').click());const editor=el.querySelector('[aria-label="Benutzer bearbeiten"]');assert.ok(editor);assert.match(editor.textContent,/Anna Muster/);assert.doesNotMatch(editor.textContent,/Bert Beispiel/);assert.equal(document.activeElement,editor);assert.equal(results(),null);
   assert.ok(editor.querySelector('select[name=rolle]'));assert.ok(editor.querySelector('[name=aktiv]'));assert.match(editor.textContent,/Passwortwiederherstellung/);assert.match(editor.textContent,/Ressource freigeben oder entziehen/);
   await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Zurück zur Benutzersuche').click());assert.equal(el.querySelector('[aria-label="Benutzer bearbeiten"]'),null);assert.equal(results().children.length,0);assert.equal(search.value,'');assert.equal(document.activeElement,search);assert.equal(reads,before);
   assert.equal(window.localStorage.length,0);assert.equal(window.sessionStorage.length,0);
  }finally{await act(async()=>root.unmount());el.remove();}
 });
});

test('Benutzersuche: kein Gesamtbestand beim Öffnen, höchstens zehn Treffer und Suche im gesamten geladenen Bestand',async()=>{
 const {default:Verwaltung}=await import('../node_modules/.cache/betriebsplattform-tests/Benutzerverwaltung.js');let reads=0;
 const users=Array.from({length:100},(_,i)=>({id:String(i),anzeigename:'Mitarbeiter '+String(i).padStart(3,'0'),login:'team.'+String(i).padStart(3,'0'),rolle:'Mitarbeiter',aktiv:true,eingerichtet:true,version:1}));
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:false});assert.notEqual(options.method,'POST');assert.equal(path,'/api/admin/benutzer');reads++;return ok(users);},async()=>{
  const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
  try{await act(async()=>root.render(h(Verwaltung)));const list=()=>el.querySelector('[aria-label="Benutzerliste"]');assert.equal(list().children.length,0);assert.match(el.textContent,/100 Benutzer geladen/);const creation=el.querySelector('.p-benutzeranlage');assert.ok(creation);assert.equal(creation.open,false);const search=el.querySelector('input[type=search]');assert.ok(search.compareDocumentPosition(creation)&window.Node.DOCUMENT_POSITION_FOLLOWING);await act(async()=>creation.querySelector('summary').click());assert.ok(creation.open);assert.equal(search.closest('details'),null);assert.ok(search.compareDocumentPosition(creation)&window.Node.DOCUMENT_POSITION_FOLLOWING);
   await benutzerSuchen(el,'team');assert.equal(list().children.length,10);assert.match(el.textContent,/10 von 100 passenden/);assert.match(el.textContent,/Suche eingrenzen/);
   await benutzerSuchen(el,'team.099');assert.equal(list().children.length,1);assert.match(list().textContent,/Mitarbeiter 099/);
   await act(async()=>list().querySelector('button').click());assert.match(el.querySelector('[aria-label="Benutzer bearbeiten"]').textContent,/Mitarbeiter 099/);
   await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Zurück zur Benutzersuche').click());assert.equal(list().children.length,0);
   await benutzerSuchen(el,'   ');assert.equal(list().children.length,0);assert.equal(reads,1);
  }finally{await act(async()=>root.unmount());el.remove();}
 });
});

test('Einzelbearbeitung: Rolle, Kontostatus und Ressourcenfreigabe senden nur den gewählten Benutzer',async()=>{
 const {default:Verwaltung}=await import('../node_modules/.cache/betriebsplattform-tests/Benutzerverwaltung.js');const posts=[];
 const users=[{id:'anna',anzeigename:'Anna Muster',login:'anna',rolle:'Mitarbeiter',aktiv:true,eingerichtet:true,version:2},{id:'bert',anzeigename:'Bert Beispiel',login:'bert',rolle:'Mitarbeiter',aktiv:false,eingerichtet:false,version:4}];
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:false});if(options.method==='POST'){const body=JSON.parse(options.body);posts.push({path,body});if(path==='/api/admin/benutzer/bert'){Object.assign(users[1],body,{version:5});}return ok(null);}assert.equal(path,'/api/admin/benutzer');return ok(users);},async()=>{
  const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
  try{await act(async()=>root.render(h(Verwaltung)));await benutzerSuchen(el,'bert');await act(async()=>el.querySelector('[aria-label="Benutzerliste"] button').click());
   const editor=el.querySelector('[aria-label="Benutzer bearbeiten"]');assert.equal(editor.querySelector('[name=aktiv]').checked,false);
   let form=editor.querySelector('form');form.elements.rolle.value='Administrator';form.elements.aktiv.checked=true;
   await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));
   assert.deepEqual(posts[0],{path:'/api/admin/benutzer/bert',body:{rolle:'Administrator',aktiv:true,version:4}});assert.equal(users[0].rolle,'Mitarbeiter');assert.match(editor.textContent,/Erstzugang neu ausstellen/);
   form=[...editor.querySelectorAll('form')].find(f=>f.elements.namedItem('art'));form.elements.art.value='Akte';form.elements.id.value='synthetische-akte';form.elements.erlaubt.checked=true;
   await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));
   assert.deepEqual(posts[1],{path:'/api/admin/benutzer/bert/freigaben',body:{art:'Akte',id:'synthetische-akte',erlaubt:true}});
   form.elements.erlaubt.checked=false;await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));
   assert.deepEqual(posts[2],{path:'/api/admin/benutzer/bert/freigaben',body:{art:'Akte',id:'synthetische-akte',erlaubt:false}});assert.equal(posts.length,3);
  }finally{await act(async()=>root.unmount());el.remove();}
 });
});

test('Mehrbenutzer: neue fremde Notiz sichtbar waehrend eigener Notizentwurf offen bleibt',async()=>{
 const {default:Aktenbereich}=await import('../node_modules/.cache/betriebsplattform-tests/Aktenbereich.js');
 const {Arbeitskontext}=await import('../node_modules/.cache/betriebsplattform-tests/Arbeitskontext.js');let ticks=[],notizen=[];
 await mount(async(path)=>{if(path==='/api/auth/session')return ok({aktiv:false});if(path==='/api/arbeitsumgebung')return ok({fluechtigeDaten:false,beispielkennzeichnung:true,benutzer:'Muster',kontakteVerwalten:true,kategorienVerwalten:true});if(path==='/api/akten/a')return ok({vorgang:{id:'a',titel:'Beispielakte',kategorie:'Vertrag',firmaId:null,rechnungsnummer:null},dokumente:[],notizen,korrespondenz:[],kontaktvorschlaege:[]});return ok([]);},async()=>{
 const old=window.setInterval;window.setInterval=f=>{ticks.push(f);return ticks.length;};const clear=window.clearInterval;window.clearInterval=()=>{};
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
 try{await act(async()=>root.render(h(Arbeitskontext,null,h(Aktenbereich,{id:'a'}))));await act(async()=>[...el.querySelectorAll('[role=tab]')].find(b=>b.textContent.startsWith('Notizen')).click());await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Notiz erfassen').click());el.querySelector('[name=betreff]').value='Eigener ungespeicherter Betreff';el.querySelector('[name=inhalt]').value='Eigener Entwurf';
 notizen=[{id:'n',betreff:'Rueckruf von Benutzer B',inhalt:'Neue Information',autor:'Muster B',erstelltAm:'2026-10-19T10:00:00Z',geaendertAm:'2026-10-19T10:00:00Z',version:1,entfernt:false,historie:[]}];await act(async()=>{for(const tick of ticks)await tick();});
 assert.match(el.textContent,/Rueckruf von Benutzer B/);assert.equal(el.querySelector('[name=betreff]').value,'Eigener ungespeicherter Betreff');assert.equal(el.querySelector('[name=inhalt]').value,'Eigener Entwurf');assert.ok(el.querySelector('dialog[open]'));
 }finally{await act(async()=>root.unmount());window.setInterval=old;window.clearInterval=clear;el.remove();}
 });
});


test('Passwortwechsel im Profil: falsch bisherig, Wiederholung und Erfolg ohne Klartextausgabe',async()=>{
 const {EigenesPasswort}=await import('../node_modules/.cache/betriebsplattform-tests/Anmeldung.js');let versuche=0;
 await mount(async(path,o)=>{if(path==='/api/auth/session')return ok({aktiv:true,angemeldet:true,csrf:'schutz'});assert.equal(path,'/api/auth/passwort');assert.equal(o.headers.get('X-CSRF-TOKEN'),'schutz');assert.deepEqual(Object.keys(JSON.parse(o.body)).sort(),['bisherigesPasswort','neuesPasswort']);versuche++;return versuche===1?{ok:false,status:400,json:async()=>({meldung:'Bisheriges Passwort ist nicht korrekt.'})}:ok({});},async()=>{
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);try{await act(async()=>root.render(h(EigenesPasswort)));await act(async()=>el.querySelector('button').click());const form=el.querySelector('form');const pass='Muster-'+crypto.randomUUID();form.elements.bisher.value='Muster-'+crypto.randomUUID();form.elements.neu.value=pass;form.elements.wiederholen.value=pass+'x';await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(versuche,0);assert.match(el.textContent,/nicht.*berein/);
 form.elements.wiederholen.value=pass;await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(el.textContent,/nicht korrekt/);assert.equal(form.elements.neu.value,pass);
 await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(el.textContent,/Andere Sitzungen/);assert.equal(form.elements.neu.value,'');assert.equal(form.elements.bisher.value,'');assert.doesNotMatch(el.textContent,new RegExp(pass));assert.equal(window.localStorage.length,0);
 }finally{await act(async()=>root.unmount());el.remove();}
 });
});

test('5-Sekunden-Abgleich startet keine zweite Anfrage solange die Liste noch laedt',async()=>{
 const {useDatenbankliste}=await import('../node_modules/.cache/betriebsplattform-tests/Buero.js');let ticks=[],requests=0,freigeben;
 await mount(async path=>{if(path==='/api/auth/session')return ok({aktiv:false});requests++;return await new Promise(resolve=>{freigeben=()=>resolve(ok({eintraege:[],anzahl:0}));});},async()=>{
 const old=window.setInterval,clear=window.clearInterval;window.setInterval=(f,ms)=>{assert.equal(ms,5000);ticks.push(f);return ticks.length;};window.clearInterval=()=>{};
 function Liste(){const d=useDatenbankliste(true,'kontakte?seite=0');return h('span',null,d.daten?'geladen':'wartet');}const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
 try{await act(async()=>root.render(h(Liste)));await act(async()=>new Promise(r=>setTimeout(r,240)));assert.equal(requests,1);void ticks[0]();window.dispatchEvent(new window.Event('focus'));assert.equal(requests,1);await act(async()=>{freigeben();await new Promise(r=>setTimeout(r,0));});assert.match(el.textContent,/geladen/);
 }finally{await act(async()=>root.unmount());window.setInterval=old;window.clearInterval=clear;el.remove();}
 });
});

test('Erstzugang: Einrichtung prueft Bestaetigung, Fehler, Erfolg und Rueckkehr ohne Geheimnis im URL',async()=>{
 let attempts=0;const pass='Synthetisch-'+crypto.randomUUID();
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:true,angemeldet:false,csrf:'test-csrf'});assert.equal(path,'/api/auth/einrichten');assert.equal(options.headers.get('X-CSRF-TOKEN'),'test-csrf');assert.deepEqual(JSON.parse(options.body),{code:'synthetischer-code',passwort:pass});attempts++;return attempts===1?{ok:false,status:400}:ok({});},async()=>{
 await act(async()=>[...document.querySelectorAll('a')].find(b=>b.textContent==='Zugang einrichten').click());await warteAuf(()=>document.querySelector('[name=code]'));const form=document.querySelector('form');form.elements.code.value='synthetischer-code';form.elements.passwort.value=pass;form.elements.bestaetigung.value=pass+'x';
 await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(attempts,0);assert.match(document.body.textContent,/stimmen nicht/);
 form.elements.bestaetigung.value=pass;await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(attempts,1);assert.match(document.body.textContent,/abgelaufen/);
 await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.body.textContent,/Zugang ist eingerichtet/);assert.equal(document.querySelector('[type=password]'),null);assert.equal(window.location.href,'https://127.0.0.1:5180/#zugang-einrichten');assert.equal(window.localStorage.length,0);assert.doesNotMatch(document.body.textContent,new RegExp(pass));
 await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent==='Zurück zur Anmeldung').click());await warteAuf(()=>document.querySelector('[name=login]'));assert.ok(document.querySelector('[name=login]'));assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);
 });
});

test('Wiederherstellung: Passwort vergessen startet keinen Reset; Bestaetigung, Fehler und Erfolg',async()=>{
 let attempts=0;const pass='Synthetisch-'+crypto.randomUUID();
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:true,angemeldet:false,csrf:'test-csrf'});assert.equal(path,'/api/auth/wiederherstellen');assert.equal(options.headers.get('X-CSRF-TOKEN'),'test-csrf');assert.deepEqual(JSON.parse(options.body),{code:'synthetischer-reset',passwort:pass});attempts++;return attempts===1?{ok:false,status:400}:ok({});},async()=>{
 await act(async()=>[...document.querySelectorAll('a')].find(b=>b.textContent==='Passwort vergessen?').click());await warteAuf(()=>document.querySelector('[name=code]'));assert.equal(attempts,0);assert.match(document.body.textContent,/Identit/);assert.match(document.body.textContent,/60 Minuten/);assert.equal(document.querySelector('[name=login]'),null);
 const form=document.querySelector('form');form.elements.code.value='synthetischer-reset';form.elements.passwort.value=pass;form.elements.bestaetigung.value=pass+'x';await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(attempts,0);
 form.elements.bestaetigung.value=pass;await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.body.textContent,/Wiederherstellungscode.*abgelaufen/);
 await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.body.textContent,/bisherigen Sitzungen/);assert.equal(document.querySelector('[type=password]'),null);assert.equal(window.location.href,'https://127.0.0.1:5180/#passwort-vergessen');assert.equal(window.localStorage.length,0);
 await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent==='Zurück zur Anmeldung').click());await warteAuf(()=>document.querySelector('[name=login]'));assert.ok(document.querySelector('[name=login]'));
 });
});

test('Benutzerverwaltung: Wiederherstellung nur nach Bestaetigung, einmalige Anzeige, Neu und Widerruf',async()=>{
 const {default:Verwaltung}=await import('../node_modules/.cache/betriebsplattform-tests/Benutzerverwaltung.js');let attempts=0,version=1,until=null;
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:false});if(options.method==='POST'){
  assert.equal(path,'/api/admin/benutzer/u/wiederherstellung');const body=JSON.parse(options.body);assert.equal(body.version,version);version++;attempts++;
  if(body.widerrufen){until=null;return ok(null);}assert.equal(body.identitaetGeprueft,true);until='2099-01-01T00:00:00Z';return ok({id:'u',code:'synthetischer-reset-'+version,gueltigBis:until});
 }return ok([{id:'u',login:'team',anzeigename:'Team',rolle:'Mitarbeiter',aktiv:true,eingerichtet:true,version,wiederherstellungBis:until}]);},async()=>{
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
 try{await act(async()=>root.render(h(Verwaltung)));await benutzerSuchen(el,'team');await act(async()=>el.querySelector('[aria-label="Benutzerliste"] button').click());const panel=[...el.querySelectorAll('details')].find(d=>d.querySelector('summary')?.textContent==='Passwortwiederherstellung'),form=panel.querySelector('form');
 assert.match(panel.textContent,/Kein offener/);await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(attempts,0);
 form.elements.identitaet.checked=true;await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(attempts,1);assert.match(panel.textContent,/ausgestellt/);assert.match(el.querySelector('aside').textContent,/Passwort vergessen/);assert.equal(el.querySelector('[readonly]').value,'synthetischer-reset-2');assert.equal(form.elements.identitaet.checked,false);
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Anzeige schließen').click());assert.equal(el.querySelector('[readonly]'),null);form.elements.identitaet.checked=true;await act(async()=>form.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.equal(el.querySelector('[readonly]').value,'synthetischer-reset-3');
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Wiederherstellung widerrufen').click());assert.equal(el.querySelector('[readonly]'),null);assert.match(panel.textContent,/Kein offener/);assert.equal(window.localStorage.length,0);
 }finally{await act(async()=>root.unmount());el.remove();}
 });
});

test('MFA Anmeldung: Arbeitsplatz bleibt bis zum zweiten Faktor gesperrt, Recovery-Auswahl und Fehler',async()=>{
 let signed=false,attempts=0;const pass='Synthetisch-'+crypto.randomUUID();
 await mount(async(path,options)=>{
  if(path==='/api/auth/session')return ok({aktiv:true,angemeldet:signed,csrf:'csrf-test'});
  assert.equal(options.headers.get('X-CSRF-TOKEN'),'csrf-test');
  if(path==='/api/auth/login'){assert.equal(JSON.parse(options.body).passwort,pass);return ok({mfaErforderlich:true});}
  assert.equal(path,'/api/auth/mfa-anmelden');const body=JSON.parse(options.body);assert.equal(body.passwort,null);assert.equal(body.code,'synthetischer-recovery');assert.equal(body.wiederherstellung,true);attempts++;if(attempts===1)return {ok:false,status:400};signed=true;return ok({});
 },async()=>{
 document.querySelector('[name=login]').value='team';document.querySelector('[name=passwort]').value=pass;await act(async()=>document.querySelector('form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));
 assert.match(document.body.textContent,/Authenticator-Code eingeben/);assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);assert.equal(document.querySelector('[type=password]'),null);
 document.querySelector('[name=code]').value='synthetischer-recovery';document.querySelector('[name=recovery]').checked=true;await act(async()=>document.querySelector('form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.body.textContent,/Anmeldung nicht möglich/);assert.doesNotMatch(document.body.textContent,/Geschützter Arbeitsplatz/);
 await act(async()=>document.querySelector('form').dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(document.body.textContent,/Geschützter Arbeitsplatz/);assert.equal(window.localStorage.length,0);assert.equal(window.location.search,'');assert.equal(window.location.hash,'');
 });
});

test('MFA Sicherheit: manuelle Einrichtung, einmalige Codes, Neuausstellung und Deaktivierung',async()=>{
 const {default:Sicherheit}=await import('../node_modules/.cache/betriebsplattform-tests/MfaSicherheit.js');let active=false,remaining=0,seq=0;
 await mount(async(path,options)=>{
  if(path==='/api/auth/session')return ok({aktiv:false});if(path==='/api/auth/mfa/status')return ok({aktiv:active,verbleibend:remaining});
  const body=JSON.parse(options.body);assert.equal(body.passwort,'SynthetischesPasswort123');
  if(path.endsWith('/vorbereiten'))return ok({schluessel:'GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ'});
  assert.ok(body.code);if(path.endsWith('/deaktivieren')){assert.equal(body.wiederherstellung,true);active=false;remaining=0;return ok(null);}
  assert.ok(path.endsWith('/aktivieren')||path.endsWith('/codes'));active=true;remaining=8;seq++;return ok({codes:['synthetischer-code-'+seq]});
 },async()=>{
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
 async function submit(code=false){const f=el.querySelector('form');f.elements.passwort.value='SynthetischesPasswort123';if(code){f.elements.code.value='synthetischer-code';if(f.elements.recovery)f.elements.recovery.checked=true;}await act(async()=>f.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));}
 try{await act(async()=>root.render(h(Sicherheit)));await act(async()=>el.querySelector('button').click());assert.match(el.textContent,/Nicht aktiviert/);await submit();assert.equal(el.querySelector('[readonly]').value,'GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ');assert.match(el.textContent,/zehn Minuten/);
 await submit(true);assert.equal(el.querySelector('[readonly]'),null);assert.match(el.textContent,/synthetischer-code-1/);assert.match(el.textContent,/8 Wiederherstellungscodes/);
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent.startsWith('Codes sicher aufbewahrt')).click());assert.doesNotMatch(el.textContent,/synthetischer-code-1/);
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Neue Wiederherstellungscodes erzeugen').click());await submit(true);assert.match(el.textContent,/synthetischer-code-2/);
 await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Zwei-Faktor-Authentifizierung deaktivieren').click());await submit(true);assert.match(el.textContent,/Nicht aktiviert/);assert.doesNotMatch(el.textContent,/synthetischer-code-2/);assert.equal(window.localStorage.length,0);
 }finally{await act(async()=>root.unmount());el.remove();}
 });
});

test('MFA letzter Recovery-Code: frische Einmalbestaetigung erlaubt neue Codes mit erneutem Passwort',async()=>{
 const {default:Sicherheit}=await import('../node_modules/.cache/betriebsplattform-tests/MfaSicherheit.js');let fresh=true;
 await mount(async(path,options)=>{if(path==='/api/auth/session')return ok({aktiv:false});if(path==='/api/auth/mfa/status')return ok({aktiv:true,verbleibend:fresh?0:8,wiederherstellungGeprueft:fresh});assert.equal(path,'/api/auth/mfa/codes');const body=JSON.parse(options.body);assert.equal(body.passwort,'SynthetischesPasswort123');assert.equal(body.code,'');fresh=false;return ok({codes:['synthetischer-neuer-code']});},async()=>{
 const el=document.createElement('div');document.body.append(el);const root=createRoot(el);
 try{await act(async()=>root.render(h(Sicherheit)));await act(async()=>el.querySelector('button').click());assert.match(el.textContent,/0 Wiederherstellungscodes/);await act(async()=>[...el.querySelectorAll('button')].find(b=>b.textContent==='Neue Wiederherstellungscodes erzeugen').click());const f=el.querySelector('form');assert.equal(f.elements.code.required,false);assert.equal(f.elements.passwort.required,true);assert.match(el.textContent,/fünf Minuten/);f.elements.passwort.value='SynthetischesPasswort123';await act(async()=>f.dispatchEvent(new window.Event('submit',{bubbles:true,cancelable:true})));assert.match(el.textContent,/synthetischer-neuer-code/);assert.match(el.textContent,/8 Wiederherstellungscodes/);assert.equal(window.localStorage.length,0);
 }finally{await act(async()=>root.unmount());el.remove();}
 });
});

for(const minimum of [8,15])test('Passwortformular übernimmt serverseitige Mindestlänge '+minimum,async()=>mount(async()=>ok({aktiv:false,passwortMinimum:minimum}),async()=>{
 await act(async()=>[...document.querySelectorAll('button')].find(b=>b.textContent==='Passwort ändern').click());await act(async()=>new Promise(r=>setTimeout(r,10)));assert.equal(document.querySelector('[name=neu]').minLength,minimum);assert.equal(document.querySelector('[name=wiederholen]').minLength,minimum);assert.match(document.body.textContent,new RegExp(minimum+' bis 128'));assert.doesNotMatch(document.body.textContent,/14 bis 128/);
},h(EigenesPasswort)));

test('Login: echte Fragment-History zurück/vorwärts, direkter Wiederherstellungsaufruf und Abbruch ohne Geheimnis',async()=>mount(async()=>ok({aktiv:true,angemeldet:false,csrf:'c'}),async()=>{
 const tick=selector=>warteAuf(()=>document.querySelector(selector));
 await act(async()=>document.querySelector('a[href="#passwort-vergessen"]').click());await tick('[name=code]');assert.ok(document.querySelector('[name=code]'));
 document.querySelector('[name=code]').value='nur-im-formular';await act(async()=>window.history.back());await tick('[name=login]');assert.ok(document.querySelector('[name=login]'));
 await act(async()=>window.history.forward());await tick('[name=code]');assert.equal(document.querySelector('[name=code]').value,'');
 assert.doesNotMatch(window.location.href,/nur-im-formular/);assert.equal(window.localStorage.length,0);
 await act(async()=>[...document.querySelectorAll('button')].find(x=>x.textContent==='Zurück zur Anmeldung').click());await tick('[name=login]');assert.ok(document.querySelector('[name=login]'));
 await act(async()=>{window.location.hash='zugang-einrichten';});await tick('[name=code]');assert.ok(document.querySelector('[name=code]'));
 assert.match(document.body.textContent,/Zurück zur Anmeldung/);
}));
