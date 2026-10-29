import{useState}from'react';
import{apiFetch}from'./auth.js';

export function ZugangEinrichten({zurAnmeldung,wiederherstellung=false}:{zurAnmeldung:()=>void;wiederherstellung?:boolean}){
 const[busy,B]=useState(false),[fehler,F]=useState(''),[fertig,S]=useState(false);
 return <><h2>{wiederherstellung?'Passwort wiederherstellen':'Zugang einrichten'}</h2>{fertig?<><p role="status">{wiederherstellung?'Ihr Passwort wurde geändert. Alle bisherigen Sitzungen sind abgemeldet.':'Ihr Zugang ist eingerichtet.'} Sie können sich jetzt mit Ihrem Login und Passwort anmelden.</p><button onClick={zurAnmeldung}>Zurück zur Anmeldung</button></>:<form className="p-erstzugang-formular" onSubmit={e=>{
  e.preventDefault();const form=e.currentTarget,d=new FormData(form);F('');
  if(d.get('passwort')!==d.get('bestaetigung')){F('Die Passwörter stimmen nicht überein.');return;}
  B(true);void apiFetch(wiederherstellung?'/api/auth/wiederherstellen':'/api/auth/einrichten',{method:'POST',headers:{'Content-Type':'application/json','X-Arbeitsaktion':'1'},body:JSON.stringify({code:String(d.get('code')).trim(),passwort:d.get('passwort')}),signal:AbortSignal.timeout(15000)})
   .then(r=>{if(!r.ok){F(r.status===429?'Zu viele Versuche. Bitte später erneut versuchen.':r.status===400?(wiederherstellung?'Wiederherstellungscode ungültig oder abgelaufen. Bitte einen neuen Code anfordern.':'Erstzugang ungültig oder abgelaufen. Angaben prüfen oder eine neue Einladung anfordern.'):'Einrichtung nicht möglich. Bitte später erneut versuchen.');return;}form.reset();S(true);})
   .catch(()=>F('Verbindung nicht erreichbar. Bitte erneut versuchen.')).finally(()=>B(false));
 }}><p>{wiederherstellung?'Bitte wenden Sie sich an Ihren Administrator. Nach betrieblicher Identitätsprüfung erhalten Sie persönlich einen einmaligen Wiederherstellungscode. Er gilt 60 Minuten.':'Den einmaligen Code erhalten Sie persönlich von Ihrem Administrator. Er gilt 24 Stunden.'} Verwenden Sie für den Zugang außerhalb dieses Rechners eine vertrauenswürdige HTTPS-Verbindung.</p><p>* Pflichtfeld</p>{fehler&&<p role="alert">{fehler}</p>}
 <label>Einmaliger Code *<input name="code" required aria-required="true" autoComplete="off" spellCheck={false} maxLength={97}/></label>
 <label>Neues Passwort *<input name="passwort" type="password" required aria-required="true" autoComplete="new-password" minLength={15} maxLength={128}/></label>
 <label>Passwort bestätigen *<input name="bestaetigung" type="password" required aria-required="true" autoComplete="new-password" minLength={15} maxLength={128}/></label>
 <p>15 bis 128 Zeichen. Verwenden Sie ein eigenes, langes Passwort.</p><div className="p-benutzer-aktionen"><button disabled={busy}>{wiederherstellung?'Neues Passwort speichern':'Einrichtung abschließen'}</button><button type="button" className="p-button-leise" onClick={zurAnmeldung}>Zurück zur Anmeldung</button></div></form>}</>;
}
