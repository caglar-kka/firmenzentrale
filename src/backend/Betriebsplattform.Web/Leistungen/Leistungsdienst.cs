using System.Security.Claims;

using System.Security.Cryptography;

using System.Text;

using System.Text.Encodings.Web;

using System.Text.Json;

using Betriebsplattform.Web.Akten;

using Betriebsplattform.Web.Aufgaben;

using Betriebsplattform.Web.Persistenz;

using Betriebsplattform.Web.Sicherheit;

using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Leistungen;

public sealed class Leistungsdienst(FirmenDbContext db,AufgabenDienst rechte,TimeProvider clock,IConfiguration? konfiguration=null,IDokumentdateien? dateien=null){

 readonly List<string> neueDateien=[];

 async Task Kompensieren(){if(dateien!=null)foreach(var key in neueDateien)await dateien.Entfernen(key,CancellationToken.None);neueDateien.Clear();}

 static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);

 public static string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b));

 static string Text(string? s,string label,int max)=>string.IsNullOrWhiteSpace(s)||s.Length>max?throw new Arbeitsfehler(label+" prüfen."):s.Trim();

 static void Version(int aktuell,int erwartet){if(aktuell!=erwartet)throw new Arbeitsfehler("Der Vorgang wurde geändert. Bitte neu laden; Entwurf bleibt erhalten.",409);}

 async Task<Benutzerkonto> Konto(string actor)=>await db.Benutzer.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor&&x.Aktiv&&x.Eingerichtet)??throw new Arbeitsfehler("Bitte erneut anmelden.",401);

 async Task<bool> Kontext(string actor,Leistungsobjekt o)=>await db.Kontakte.AnyAsync(k=>k.Id==o.KundeId&&!k.Archiviert)&&await rechte.RessourceErlaubt(actor,"Kontakt",o.KundeId)&&(o.AkteId==null||await rechte.RessourceErlaubt(actor,"Akte",o.AkteId)&&await db.Akten.AnyAsync(a=>a.Id==o.AkteId&&a.Status!="Archiviert"));

 public async Task<Leistungsobjekt> Objekt(string actor,string id){var u=await Konto(actor);var o=await db.Leistungsobjekte.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(o==null||!await Kontext(actor,o)||(u.Rolle!="Administrator"&&o.BueroId!=actor&&!await db.Leistungseinsaetze.AnyAsync(x=>x.ObjektId==id&&x.MitarbeiterId==actor)))throw new Arbeitsfehler("Objekt nicht verfügbar.",404);return o;}

 async Task Buero(string actor,Leistungsobjekt o){if((await Konto(actor)).Rolle!="Administrator"&&o.BueroId!=actor)throw new Arbeitsfehler("Bürofreigabe für dieses Objekt erforderlich.",403);}

 async Task<object> Ausfuehren(string actor,string nonce,string? objekt,string art,object anfrage,Func<Task<object>> tun){

  if(!Guid.TryParseExact(nonce,"N",out _))throw new Arbeitsfehler("Anlagekennung prüfen.");

  var id=Hash(Encoding.UTF8.GetBytes(actor+"/"+nonce))[..32];var hash=Hash(JsonSerializer.SerializeToUtf8Bytes(new{objekt,art,anfrage},Json));

  async Task<Leistungsaktion?> Vorhanden()=>await db.Leistungsaktionen.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);

  object Replay(Leistungsaktion x){if(x.AnfrageHash!=hash||x.ObjektId!=objekt)throw new Arbeitsfehler("Kennung wurde für andere Eingaben verwendet.",409);return JsonSerializer.Deserialize<JsonElement>(x.Ergebnis);}

  var vorhanden=await Vorhanden();if(vorhanden!=null)return Replay(vorhanden);neueDateien.Clear();

  await using var tx=await db.Database.BeginTransactionAsync();try{

   var result=JsonSerializer.SerializeToElement(await tun(),Json);

   db.Leistungsaktionen.Add(new(){Id=id,ObjektId=objekt,BenutzerId=actor,Art=art,AnfrageHash=hash,Ergebnis=result.GetRawText(),Zeitpunkt=clock.GetUtcNow()});

   db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Ressource=objekt??"Leistungskatalog",Aktion="Leistungsnachweis: "+art,Zeitpunkt=clock.GetUtcNow()});

   await db.SaveChangesAsync();await tx.CommitAsync();neueDateien.Clear();return result;

  }catch(DbUpdateException){await tx.RollbackAsync();db.ChangeTracker.Clear();await Kompensieren();var alt=await Vorhanden();if(alt!=null)return Replay(alt);throw new Arbeitsfehler("Der Vorgang wurde gleichzeitig geändert. Bitte neu laden.",409);}catch{await tx.RollbackAsync();db.ChangeTracker.Clear();await Kompensieren();throw;}

 }

 async Task Katalogrecht(string actor,bool schreiben){

  var u=await Konto(actor);if(u.Rolle=="Administrator")return;

  if(!schreiben){foreach(var o in await db.Leistungsobjekte.AsNoTracking().Where(o=>o.BueroId==actor).ToArrayAsync())if(await Kontext(actor,o))return;}

  throw new Arbeitsfehler("Leistungskatalog: keine Berechtigung.",403);

 }

 public async Task<object> Katalog(string actor,int seite=0,bool nurAktiv=false){await Katalogrecht(actor,false);var q=db.Leistungskatalog.AsNoTracking().Where(x=>!nurAktiv||x.Aktiv);return new{anzahl=await q.CountAsync(),eintraege=await q.OrderBy(x=>x.Reihenfolge).ThenBy(x=>x.Name).ThenBy(x=>x.Id).Skip(FirmenDaten.Seite(seite)*20).Take(20).ToArrayAsync()};}

 public async Task<object> KatalogSpeichern(string actor,string? id,Katalogeingabe e){

  await Katalogrecht(actor,true);var name=Text(e.Name,"Leistung",160);

  if(e.Beschreibung==null||e.Beschreibung.Length>1000||e.Kategorie==null||e.Kategorie.Length>100||e.Reihenfolge is <0 or >9999)throw new Arbeitsfehler("Beschreibung, Kategorie und Reihenfolge prüfen.");

  return await Ausfuehren(actor,e.AnlageId,null,"Katalogleistung speichern",new{id,e},async()=>{

   Katalogleistung k;if(id==null){if(e.Version!=0)throw new Arbeitsfehler("Neue Leistung ohne Vorgängerversion anlegen.",409);k=new();db.Leistungskatalog.Add(k);}else{k=await db.Leistungskatalog.SingleOrDefaultAsync(x=>x.Id==id)??throw new Arbeitsfehler("Leistung nicht verfügbar.",404);Version(k.Version,e.Version);db.Entry(k).Property(x=>x.Version).IsModified=true;}

   k.Name=name;k.Beschreibung=e.Beschreibung.Trim();k.Kategorie=e.Kategorie.Trim();k.Reihenfolge=e.Reihenfolge;k.Aktiv=e.Aktiv;return new{id=k.Id,version=id==null?1:k.Version+1};

  });

 }

 public async Task<object> Objekte(string actor,int seite=0,string suche="",string status="alle"){var u=await Konto(actor);var kunden=await rechte.KontaktIds(actor);var akten=await rechte.AktenIds(actor);var q=db.Leistungsobjekte.AsNoTracking().Where(o=>(u.Rolle=="Administrator"||o.BueroId==actor||db.Leistungseinsaetze.Any(e=>e.ObjektId==o.Id&&e.MitarbeiterId==actor))&&kunden.Contains(o.KundeId)&&db.Kontakte.Any(k=>k.Id==o.KundeId&&!k.Archiviert)&&(o.AkteId==null||akten.Contains(o.AkteId)&&db.Akten.Any(a=>a.Id==o.AkteId&&a.Status!="Archiviert")));if(suche.Length>160||status is not("alle" or "aktiv" or "inaktiv"))throw new Arbeitsfehler("Objektfilter prüfen.");var suchtext=suche.Trim().ToLower();if(suchtext.Length>0)q=q.Where(o=>o.Name.ToLower().Contains(suchtext)||o.Anschrift.ToLower().Contains(suchtext));if(status!="alle")q=q.Where(o=>o.Aktiv==(status=="aktiv"));return new{anzahl=await q.CountAsync(),eintraege=await q.OrderBy(x=>x.Name).ThenBy(x=>x.Id).Skip(FirmenDaten.Seite(seite)*20).Take(20).ToArrayAsync()};}



 public async Task<object> ObjektAnlegen(string actor,Objekteingabe e){

  if((await Konto(actor)).Rolle!="Administrator")throw new Arbeitsfehler("Objekte legt die Administration an.",403);

  var o=new Leistungsobjekt{Id=Hash(Encoding.UTF8.GetBytes(actor+"/objekt/"+e.AnlageId))[..32],KundeId=e.KundeId,Name=Text(e.Name,"Objektbezeichnung",160),Anschrift=Text(e.Anschrift,"Objektanschrift",500),BueroId=e.BueroId,AkteId=e.AkteId};

  if(!await db.Kontakte.AnyAsync(k=>k.Id==e.KundeId&&k.Art=="Firma"&&!k.Archiviert)||!await Kontext(actor,o)||o.AkteId!=null&&!await db.Akten.AnyAsync(a=>a.Id==o.AkteId&&a.FirmaId==o.KundeId))throw new Arbeitsfehler("Kunde oder Aktenbezug nicht verfügbar.",404);

  await Konto(e.BueroId);if(!await Kontext(e.BueroId,o))throw new Arbeitsfehler("Büroperson benötigt die Kontakt-/Aktenfreigabe.",403);

  return await Ausfuehren(actor,e.AnlageId,o.Id,"Objekt anlegen",e,()=>{db.Leistungsobjekte.Add(o);return Task.FromResult<object>(o);});

 }

 public async Task<object> ObjektAendern(string actor,string id,Objektaenderung e){

  var o=await Objekt(actor,id);await Buero(actor,o);

  var name=Text(e.Name,"Objektbezeichnung",160);var anschrift=Text(e.Anschrift,"Objektanschrift",500);

  if(e.Bemerkung==null||e.Bemerkung.Length>2000)throw new Arbeitsfehler("Interne Bemerkung auf höchstens 2000 Zeichen begrenzen.");

  return await Ausfuehren(actor,e.AnlageId,id,"Objekt bearbeiten",e,async()=>{

   var aktuell=await db.Leistungsobjekte.SingleAsync(x=>x.Id==id);Version(aktuell.Version,e.Version);

   aktuell.Name=name;aktuell.Anschrift=anschrift;aktuell.Aktiv=e.Aktiv;aktuell.Bemerkung=e.Bemerkung.Trim();

   db.Entry(aktuell).Property(x=>x.Version).IsModified=true;

   return new{id=aktuell.Id,version=aktuell.Version+1};

  });

 }

 public async Task<object> Detail(string actor,string id,int seite=0){var o=await Objekt(actor,id);var buero=(await Konto(actor)).Rolle=="Administrator"||o.BueroId==actor;var q=db.Leistungseinsaetze.AsNoTracking().Where(e=>e.ObjektId==id&&(buero||e.MitarbeiterId==actor));return new{objekt=o,buero,plaene=buero?await db.Leistungsplaene.AsNoTracking().Where(p=>p.ObjektId==id).OrderByDescending(p=>p.Nummer).ToArrayAsync():[],einsatzAnzahl=await q.CountAsync(),einsaetze=await q.OrderByDescending(e=>e.Arbeitstag).ThenBy(e=>e.Id).Skip(FirmenDaten.Seite(seite)*20).Take(20).ToArrayAsync()};}



 public async Task<object> PlanAnlegen(string actor,string id,Planeingabe e){var o=await Objekt(actor,id);await Buero(actor,o);if(!o.Aktiv)throw new Arbeitsfehler("Inaktives Objekt: vor einer neuen Planversion aktivieren.",409);

  if(e.Positionen==null||e.Positionen.Length is <1 or >50||e.GiltAb.Year is <1900 or >2200)throw new Arbeitsfehler("1 bis 50 Positionen und Gültigkeitsdatum prüfen.");

  var positionen=e.Positionen.Select(p=>{if(p is null||p.Turnus is not("Bei jedem Einsatz" or "Bestimmte Wochentage" or "Monatlich" or "Bei Bedarf" or "Einmalig")||p.Bereich==null||p.Bereich.Length>100||p.Hinweis==null||p.Hinweis.Length>1000||p.Turnus=="Bestimmte Wochentage"&&(p.Wochentage==null||p.Wochentage.Length is <1 or >7||p.Wochentage.Distinct().Count()!=p.Wochentage.Length||p.Wochentage.Any(x=>x<1||x>7)))throw new Arbeitsfehler("Leistungsposition prüfen.");return p with{Leistung=Text(p.Leistung,"Leistung",160)};}).ToArray();

  if(!positionen.Any(p=>p.Aktiv))throw new Arbeitsfehler("Mindestens eine aktive Leistungsposition ist erforderlich.");
  return await Ausfuehren(actor,e.AnlageId,id,"Planversion anlegen",e,async()=>{var aktuell=await db.Leistungsobjekte.SingleAsync(x=>x.Id==id);Version(aktuell.Version,e.Objektversion);db.Entry(aktuell).Property(x=>x.Version).IsModified=true;var nummer=(await db.Leistungsplaene.Where(x=>x.ObjektId==id).Select(x=>(int?)x.Nummer).MaxAsync()??0)+1;var plan=new Leistungsplan{ObjektId=id,Name=Text(e.Name,"Planname",160),GiltAb=e.GiltAb,Nummer=nummer,Positionen=JsonSerializer.Serialize(positionen,Json)};if(plan.Positionen.Length>64000)throw new Arbeitsfehler("Planumfang zu groß.");db.Leistungsplaene.Add(plan);return plan;});

 }

 public async Task<object> EinsatzAnlegen(string actor,string id,Einsatzeingabe e){var o=await Objekt(actor,id);await Buero(actor,o);if(!o.Aktiv)throw new Arbeitsfehler("Inaktives Objekt: vor einem neuen Einsatz aktivieren.",409);await Konto(e.MitarbeiterId);if(!await Kontext(e.MitarbeiterId,o))throw new Arbeitsfehler("Mitarbeiter benötigt die Objekt-/Kundenfreigabe.",403);

  var plan=await db.Leistungsplaene.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==e.PlanId&&p.ObjektId==id)??throw new Arbeitsfehler("Plan nicht verfügbar.",404);if(e.Arbeitstag<plan.GiltAb||e.Arbeitstag.Year is <1900 or >2200)throw new Arbeitsfehler("Arbeitstag liegt außerhalb des gültigen Plans.");

  if(await db.Leistungsplaene.AnyAsync(p=>p.ObjektId==id&&p.Nummer>plan.Nummer&&p.GiltAb<=e.Arbeitstag))throw new Arbeitsfehler("Für diesen Arbeitstag liegt eine neuere Planversion vor.",409);

  return await Ausfuehren(actor,e.AnlageId,id,"Einsatz anlegen",e,async()=>{var kunde=await db.Kontakte.SingleAsync(k=>k.Id==o.KundeId);var soll=JsonSerializer.Deserialize<Sollposition[]>(plan.Positionen,Json)!.Where(p=>p.Aktiv).ToArray();var stand=new Einsatzstand(kunde.Name,o.Name,o.Anschrift,plan.Name,plan.Nummer,soll,soll.Select((_,i)=>new Istposition(i,"Noch offen","")).ToArray(),"","","");var einsatz=new Leistungseinsatz{ObjektId=id,PlanId=plan.Id,Arbeitstag=e.Arbeitstag,MitarbeiterId=e.MitarbeiterId,Daten=JsonSerializer.Serialize(stand,Json)};db.Leistungseinsaetze.Add(einsatz);return einsatz;});

 }

 public async Task<Leistungseinsatz> Einsatz(string actor,string id){var x=await db.Leistungseinsaetze.AsNoTracking().SingleOrDefaultAsync(e=>e.Id==id)??throw new Arbeitsfehler("Einsatz nicht verfügbar.",404);var o=await Objekt(actor,x.ObjektId);if((await Konto(actor)).Rolle!="Administrator"&&o.BueroId!=actor&&x.MitarbeiterId!=actor)throw new Arbeitsfehler("Einsatz nicht verfügbar.",404);return x;}

 public async Task<object> Einsatzdetail(string actor,string id){var e=await Einsatz(actor,id);var o=await Objekt(actor,e.ObjektId);return new{einsatz=e,stand=JsonSerializer.Deserialize<Einsatzstand>(e.Daten,Json),buero=(await Konto(actor)).Rolle=="Administrator"||o.BueroId==actor,nachweis=await db.Leistungsnachweise.Where(n=>n.EinsatzId==id).Select(n=>new{n.Id,n.Hash,n.Zeitpunkt,n.FreigegebenDurch}).SingleOrDefaultAsync()};}

 public async Task<object> Entscheiden(string actor,string id,Leistungsentscheidung e){var alt=await Einsatz(actor,id);var o=await Objekt(actor,alt.ObjektId);var u=await Konto(actor);if(e.Art!="checkliste"&&e.Art!="melden")await Buero(actor,o);

  if(e.Sonderpositionen is {Length:>0}&&e.Art!="checkliste")throw new Arbeitsfehler("Sonderpositionen nur im Einsatzentwurf ergänzen.");
  if(e.Bemerkung==null||e.Bemerkung.Length>4000)throw new Arbeitsfehler("Bemerkung auf höchstens 4000 Zeichen begrenzen.");

  return await Ausfuehren(actor,e.AnlageId,o.Id,e.Art,new{id,e},async()=>{

   var x=await db.Leistungseinsaetze.SingleAsync(x=>x.Id==id);Version(x.Version,e.Version);var stand=JsonSerializer.Deserialize<Einsatzstand>(x.Daten,Json)!;

   if(e.Art=="checkliste"){

    if(x.Status!="Entwurf")throw new Arbeitsfehler("Gemeldete oder freigegebene Inhalte sind gesperrt. Neue Revision erforderlich.",409);

    if(e.Sonderpositionen is {Length:>0} zusatz){
     if(stand.Soll.Length+zusatz.Length>50||zusatz.Any(p=>p is null||!p.Aktiv||p.Turnus!="Bei Bedarf"||p.Bereich is null||p.Bereich.Length>100||p.Hinweis is null||p.Hinweis.Length>1000||p.Wochentage is {Length:>0}))throw new Arbeitsfehler("Sonderpositionen prüfen; zusammen höchstens 50 Positionen.");
     var geprueft=zusatz.Select(p=>p with{Leistung=Text(p.Leistung,"Sonderleistung",160),Pflicht=false}).ToArray();stand=stand with{Soll=stand.Soll.Concat(geprueft).ToArray()};
    }
    var ist=e.Positionen;if(ist==null||ist.Length!=stand.Soll.Length||ist.Select(p=>p.Nummer).Distinct().Count()!=ist.Length||ist.Any(p=>p is null||p.Nummer<0||p.Nummer>=ist.Length||p.Status is not("Noch offen" or "Erledigt" or "Teilweise erledigt" or "Nicht erledigt" or "Nicht anwendbar")||p.Bemerkung==null||p.Bemerkung.Length>1000||p.Status is "Teilweise erledigt" or "Nicht erledigt" or "Nicht anwendbar"&&string.IsNullOrWhiteSpace(p.Bemerkung)))throw new Arbeitsfehler("Positionen und Begründungen prüfen.");

    stand=stand with{Ist=ist.OrderBy(p=>p.Nummer).ToArray(),Bemerkung=e.Bemerkung,ErfasstDurch=u.Anzeigename,Erfassungsart=actor==o.BueroId||u.Rolle=="Administrator"?"Erfasst durch Büro":"Erfasst durch zugewiesene Person"};

   }else if(e.Art=="melden"){

    if(x.Status!="Entwurf"||stand.Ist.Any(p=>stand.Soll[p.Nummer].Pflicht&&p.Status=="Noch offen"))throw new Arbeitsfehler("Pflichtpositionen zuerst ausdrücklich bewerten.",409);x.Status="Ausführung gemeldet";

   }else if(e.Art=="pruefen"){

    if(x.Status!="Ausführung gemeldet")throw new Arbeitsfehler("Ausführung muss zuerst gemeldet werden.",409);x.Status="In Büroprüfung";

   }else if(e.Art=="freigeben"){

    if(x.Status!="In Büroprüfung")throw new Arbeitsfehler("Zuerst Büroprüfung öffnen.",409);x.Status="Kundenbestätigung ausstehend";

    var nid=Guid.NewGuid().ToString("N");var zeit=clock.GetUtcNow();var bytes=Druck(nid,x,stand,u.Anzeigename,zeit,konfiguration?["FirmenZentrale:Organisation:Name"]??"MBOP – Betrieb nicht konfiguriert");if(dateien==null)throw new Arbeitsfehler("Private Nachweisablage nicht verfügbar. Freigabe bleibt offen.",503);neueDateien.Add(nid);await dateien.Speichern(nid,bytes,CancellationToken.None);db.Leistungsnachweise.Add(new(){Id=nid,EinsatzId=id,FreigegebenDurch=actor,Zeitpunkt=zeit,StorageKey=nid,Hash=Hash(bytes)});

   }else if(e.Art=="rueckmeldung"){

    if(x.Status!="Kundenbestätigung ausstehend"||e.Kundenstatus is not("Vollständig bestätigt" or "Teilweise bestätigt" or "Beanstandet" or "Abgelehnt" or "Rückfrage"))throw new Arbeitsfehler("Rückmeldestatus prüfen; interne Freigabe muss vorliegen.",409);

    var begruendung=Text(e.Bemerkung,"Erhaltene Rückmeldung",4000);if(e.Position is not null&&(e.Position<0||e.Position>=stand.Soll.Length))throw new Arbeitsfehler("Beanstandete Position prüfen.");

    if(e.Nacharbeit&&(e.Kundenstatus is not("Teilweise bestätigt" or "Beanstandet" or "Abgelehnt")||e.Position==null))throw new Arbeitsfehler("Nacharbeit benötigt eine beanstandete Position.");

    string? task=null;if(e.Nacharbeit){var a=await rechte.Speichern(actor,null,new("Nacharbeit: "+stand.Soll[e.Position!.Value].Leistung,"Nachweis "+id+" · "+begruendung,actor,"Normal","Offen",null,null,FirmaId:o.KundeId,BezugArt:o.AkteId==null?"Kontakt":"Akte",BezugId:o.AkteId??o.KundeId));task=a.Id;}

    stand=stand with{Rueckmeldung="Manuell erfasst – externe Identität/Unterschrift nicht automatisch verifiziert. "+begruendung,Rueckmeldestatus=e.Kundenstatus,BeanstandetePosition=e.Position,NacharbeitId=task};x.Status=e.Kundenstatus;

   }else if(e.Art=="revision"){

    if(!await db.Leistungsnachweise.AnyAsync(n=>n.EinsatzId==id))throw new Arbeitsfehler("Revision erst nach interner Freigabe.",409);var neu=new Leistungseinsatz{ObjektId=x.ObjektId,PlanId=x.PlanId,Arbeitstag=x.Arbeitstag,MitarbeiterId=x.MitarbeiterId,VorgaengerId=id,Aenderungsgrund=Text(e.Bemerkung,"Änderungsgrund",1000),Daten=JsonSerializer.Serialize(stand with{Rueckmeldung="",Rueckmeldestatus="",BeanstandetePosition=null,NacharbeitId=null},Json)};db.Leistungseinsaetze.Add(neu);return neu;

   }else throw new Arbeitsfehler("Aktion nicht verfügbar.");

   x.Daten=JsonSerializer.Serialize(stand,Json);if(x.Daten.Length>100000)throw new Arbeitsfehler("Gesamtumfang der Checkliste zu groß.");db.Entry(x).Property(v=>v.Version).IsModified=true;return new{id=x.Id,status=x.Status,version=x.Version+1,stand};

  });

 }

 public async Task<byte[]> Nachweis(string actor,string id){var n=await db.Leistungsnachweise.AsNoTracking().SingleOrDefaultAsync(n=>n.Id==id)??throw new Arbeitsfehler("Nachweis nicht verfügbar.",404);await Einsatz(actor,n.EinsatzId);if(dateien==null||string.IsNullOrWhiteSpace(n.StorageKey))throw new Arbeitsfehler("Nachweisablage nicht verfügbar.",503);var bytes=await dateien.Lesen(n.StorageKey,CancellationToken.None);if(Hash(bytes)!=n.Hash)throw new Arbeitsfehler("Nachweisprüfsumme stimmt nicht. Abruf gesperrt.",409);return bytes;}

 static byte[] Druck(string id,Leistungseinsatz e,Einsatzstand s,string autor,DateTimeOffset zeit,string betrieb){string H(string v)=>HtmlEncoder.Default.Encode(v);var rows=string.Join("",s.Ist.Select(p=>"<tr><td>"+H(s.Soll[p.Nummer].Leistung)+"</td><td>"+H(s.Soll[p.Nummer].Bereich)+"</td><td>"+H(p.Status)+"</td><td>"+H(p.Bemerkung)+"</td></tr>"));return Encoding.UTF8.GetBytes("<!doctype html><html lang=de><meta charset=utf-8><title>Leistungsnachweis "+id+"</title><style>@page{size:A4;margin:18mm}body{font:16px sans-serif;color:#122443}table{border-collapse:collapse;width:100%}td,th{border:1px solid #9aaabd;padding:8px;text-align:left;overflow-wrap:anywhere}tr{break-inside:avoid}thead{display:table-header-group}p{white-space:pre-wrap}</style><h1>Leistungsnachweis</h1><p>Intern freigegebener Leistungsnachweis</p><p>Betrieb: "+H(betrieb)+"<br>Nachweis-ID: "+id+"<br>Dokumentversion: "+e.Id+"/"+e.Version+"<br>Kunde: "+H(s.Kunde)+"<br>Objekt: "+H(s.Objekt)+"<br>"+H(s.Anschrift)+"<br>Arbeitstag: "+e.Arbeitstag.ToString("dd.MM.yyyy")+"<br>Plan: "+H(s.Plan)+" v"+s.Planversion+"<br>Einsatzrevision: "+H(e.VorgaengerId??"Erstfassung")+"<br>Änderungsgrund: "+H(e.Aenderungsgrund)+"</p><table><thead><tr><th>Leistung</th><th>Bereich</th><th>Ausführung</th><th>Bemerkung</th></tr></thead><tbody>"+rows+"</tbody></table><p>"+H(s.Bemerkung)+"</p><p>"+H(s.Erfassungsart)+": "+H(s.ErfasstDurch)+"<br>Intern freigegeben durch "+H(autor)+" am "+H(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(zeit,"Europe/Berlin").ToString("dd.MM.yyyy HH:mm",System.Globalization.CultureInfo.GetCultureInfo("de-DE"))+" Uhr (Europe/Berlin)")+"<br>Kundenbestätigung ausstehend. Die interne Freigabe ersetzt keine Kundenbestätigung.</p><p>Die SHA256-Prüfsumme der gespeicherten Bytes wird getrennt in MBOP ausgewiesen.</p></html>");}

 public static void Endpunkte(WebApplication app){var g=app.MapGroup("/api/leistungen").RequireAuthorization();g.AddEndpointFilter(async(c,next)=>{try{return await next(c);}catch(Arbeitsfehler e){return Results.Json(new ApiFehler(e.Message),statusCode:e.Status);}});static string Actor(HttpContext c)=>c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

  g.MapGet("/katalog",(HttpContext c,Leistungsdienst d)=>d.Katalog(Actor(c),int.TryParse(c.Request.Query["seite"],out var n)?n:0,c.Request.Query["aktiv"]=="true"));g.MapPost("/katalog",(HttpContext c,Leistungsdienst d,Katalogeingabe e)=>d.KatalogSpeichern(Actor(c),null,e));g.MapPost("/katalog/{id}",(HttpContext c,Leistungsdienst d,string id,Katalogeingabe e)=>d.KatalogSpeichern(Actor(c),id,e));

  g.MapGet("/objekte",(HttpContext c,Leistungsdienst d)=>d.Objekte(Actor(c),int.TryParse(c.Request.Query["seite"],out var n)?n:0,c.Request.Query["suche"].ToString(),c.Request.Query["status"].FirstOrDefault()??"alle"));g.MapPost("/objekte",(HttpContext c,Leistungsdienst d,Objekteingabe e)=>d.ObjektAnlegen(Actor(c),e));g.MapPost("/objekte/{id}",(HttpContext c,Leistungsdienst d,string id,Objektaenderung e)=>d.ObjektAendern(Actor(c),id,e));g.MapGet("/objekte/{id}",(HttpContext c,Leistungsdienst d,string id)=>d.Detail(Actor(c),id,int.TryParse(c.Request.Query["seite"],out var n)?n:0));g.MapPost("/objekte/{id}/plaene",(HttpContext c,Leistungsdienst d,string id,Planeingabe e)=>d.PlanAnlegen(Actor(c),id,e));g.MapPost("/objekte/{id}/einsaetze",(HttpContext c,Leistungsdienst d,string id,Einsatzeingabe e)=>d.EinsatzAnlegen(Actor(c),id,e));g.MapGet("/einsaetze/{id}",(HttpContext c,Leistungsdienst d,string id)=>d.Einsatzdetail(Actor(c),id));g.MapPost("/einsaetze/{id}",(HttpContext c,Leistungsdienst d,string id,Leistungsentscheidung e)=>d.Entscheiden(Actor(c),id,e));g.MapGet("/nachweise/{id}/druck",async(HttpContext c,Leistungsdienst d,string id)=>{var b=await d.Nachweis(Actor(c),id);c.Response.Headers.ContentSecurityPolicy="default-src 'none'; style-src 'unsafe-inline'; sandbox";c.Response.Headers.CacheControl="no-store";c.Response.Headers.XContentTypeOptions="nosniff";c.Response.Headers["X-Content-SHA256"]=Hash(b);return Results.Bytes(b,"text/html; charset=utf-8");});

 }

}

