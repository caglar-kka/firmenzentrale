using System.Text;

using System.Text.Json;

using Betriebsplattform.Web.Akten;

using Betriebsplattform.Web.Aufgaben;

using Betriebsplattform.Web.Leistungen;

using Betriebsplattform.Web.Persistenz;

using Betriebsplattform.Web.Sicherheit;

using Microsoft.Data.Sqlite;

using Microsoft.EntityFrameworkCore;

using Xunit;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class LeistungsnachweiseTests {

 static string Nonce()=>Guid.NewGuid().ToString("N");static string Id(object o)=>((JsonElement)o).GetProperty("id").GetString()!;

 sealed class Testdaten: IAsyncDisposable {

  public readonly string Root=Path.Combine(Path.GetTempPath(),"MBOP-Leistungsnachweis-"+Guid.NewGuid().ToString("N"));public LokaleDokumentdateien Dateien=null!;

  public bool Schreibfehler;

  sealed class Fehler(Func<bool> aktiv):SaveChangesInterceptor{public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e,InterceptionResult<int> r,CancellationToken c=default){if(aktiv())throw new DbUpdateException("Synthetischer Fehler vor Persistenz");return new(r);}}

  public readonly SqliteConnection C=new("Data Source=:memory:");public FirmenDbContext Db=null!;public Leistungsdienst D=null!;

  public async Task Start(){await C.OpenAsync();Db=new(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(C).AddInterceptors(new Fehler(()=>Schreibfehler)).Options);await Db.Database.EnsureCreatedAsync();

   foreach(var id in new[]{"admin","buero","team","fremd"})Db.Benutzer.Add(new(){Id=id,Login=id,Anzeigename="Fiktive Person "+id,PasswortHash="nur-synthetischer-unit-testhash",Rolle=id=="admin"?"Administrator":"Mitarbeiter"});

   Db.Kontakte.AddRange(new KontaktDatensatz{Id="kunde",Name="Elbequartier Objektservice",Suchname="elbequartier"},new KontaktDatensatz{Id="kunde2",Name="Kontorhaus Südhafen",Suchname="kontorhaus"});

   foreach(var id in new[]{"buero","team"})Db.Freigaben.Add(new(){BenutzerId=id,KontaktId="kunde"});await Db.SaveChangesAsync();Dateien=new(Root);await Dateien.StartAsync(default);D=new(Db,new AufgabenDienst(Db,TimeProvider.System,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")),TimeProvider.System,dateien:Dateien);

  }

  public async ValueTask DisposeAsync(){await Db.DisposeAsync();await C.DisposeAsync();if(Directory.Exists(Root)){foreach(var file in Directory.GetFiles(Root))File.Delete(file);Directory.Delete(Root);} }

 }

 static async Task<(string Objekt,string Plan,string Einsatz)> Anlage(Testdaten f){var o=Id(await f.D.ObjektAnlegen("admin",new(Nonce(),"kunde","Kontorhaus West","Fiktiver Prüfweg 7,12345 Musterort","buero")));var p=Id(await f.D.PlanAnlegen("buero",o,new(Nonce(),1,"Unterhaltsreinigung",new(2026,10,1),[new("Boden feucht wischen","Flur","Bei jedem Einsatz",true,""),new("Sanitärbereich reinigen","WC","Monatlich",true,"")])));var e=Id(await f.D.EinsatzAnlegen("buero",o,new(Nonce(),p,new(2026,10,25),"team")));return(o,p,e);}

 [Fact] public async Task SonderpositionBleibtBeimEinsatzUndAendertWederPlanNochKatalog(){
  await using var f=new Testdaten();await f.Start();var(o,p,e)=await Anlage(f);var plan=(await f.Db.Leistungsplaene.AsNoTracking().SingleAsync(x=>x.Id==p)).Positionen;
  var json=JsonSerializer.Serialize(new{anlageId=Nonce(),version=1,art="checkliste",positionen=new[]{new Istposition(0,"Erledigt",""),new Istposition(1,"Teilweise erledigt","Raum gesperrt"),new Istposition(2,"Erledigt","Zusätzlich vereinbart")},bemerkung="Übergabe: größere Fläche einschließlich Türgriffe.",sonderpositionen=new[]{new Sollposition("Zusätzliche Türgriffe","Eingang","Bei Bedarf",false,"Nur dieser Einsatz")}},new JsonSerializerOptions(JsonSerializerDefaults.Web));
  var input=JsonSerializer.Deserialize<Leistungsentscheidung>(json,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
  await f.D.Entscheiden("team",e,input);await f.D.Entscheiden("team",e,input);var stand=JsonSerializer.Deserialize<Einsatzstand>((await f.D.Einsatz("team",e)).Daten,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
  Assert.Equal(3,stand.Soll.Length);Assert.Equal("Zusätzliche Türgriffe",stand.Soll[2].Leistung);Assert.Equal(plan,(await f.Db.Leistungsplaene.AsNoTracking().SingleAsync(x=>x.Id==p)).Positionen);Assert.Empty(await f.Db.Leistungskatalog.ToArrayAsync());
  var vorher=(await f.D.Einsatz("team",e)).Daten;await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("fremd",e,input));Assert.Equal(vorher,(await f.D.Einsatz("team",e)).Daten);
 }
 [Fact] public async Task InaktivePositionBleibtInPlanversionAberNichtImNeuenEinsatz(){
  await using var f=new Testdaten();await f.Start();var(o,p,e)=await Anlage(f);var alt=(await f.D.Einsatz("team",e)).Daten;
  var inaktiv=JsonSerializer.Deserialize<Sollposition>("{\"leistung\":\"Alte Leistung\",\"bereich\":\"Flur\",\"turnus\":\"Bei Bedarf\",\"pflicht\":true,\"hinweis\":\"\",\"aktiv\":false}",new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
  var neu=Id(await f.D.PlanAnlegen("buero",o,new(Nonce(),2,"Sonderreinigung",new(2026,10,26),[inaktiv,new("Grundreinigung","Büro","Einmalig",true,"")])));
  var lauf=Id(await f.D.EinsatzAnlegen("buero",o,new(Nonce(),neu,new(2026,10,26),"team")));var stand=JsonSerializer.Deserialize<Einsatzstand>((await f.D.Einsatz("team",lauf)).Daten,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;Assert.Single(stand.Soll);Assert.Equal("Grundreinigung",stand.Soll[0].Leistung);Assert.Equal(alt,(await f.D.Einsatz("team",e)).Daten);
 }
 [Fact] public async Task KatalogBleibtVonHistorischenPlaenenGetrenntUndIstBerechtigt(){

  await using var f=new Testdaten();await f.Start();var(o,_,e)=await Anlage(f);var alt=(await f.D.Einsatz("team",e)).Daten;

  var input=new Katalogeingabe(Nonce(),0,"Fenster reinigen","Rahmen abwischen","Glas",10);var id=Id(await f.D.KatalogSpeichern("admin",null,input));Assert.Equal(id,Id(await f.D.KatalogSpeichern("admin",null,input)));Assert.Single(await f.Db.Leistungskatalog.ToArrayAsync());

  Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.KatalogSpeichern("buero",id,input))).Status);Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Katalog("team"))).Status);await f.D.Katalog("buero");

  await f.D.KatalogSpeichern("admin",id,input with{AnlageId=Nonce(),Version=1,Name="Fenster und Rahmen reinigen",Aktiv=false});

  Assert.Equal(0,JsonSerializer.SerializeToElement(await f.D.Katalog("buero",nurAktiv:true)).GetProperty("anzahl").GetInt32());Assert.Equal(alt,(await f.D.Einsatz("team",e)).Daten);

  Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.KatalogSpeichern("admin",id,input with{AnlageId=Nonce(),Version=1}))).Status);

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.KatalogSpeichern("admin",null,input with{AnlageId=Nonce(),Reihenfolge=-1}));

  await f.Db.Freigaben.Where(x=>x.BenutzerId=="buero").ExecuteDeleteAsync();Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Katalog("buero"))).Status);

 }

 [Fact] public async Task ObjektpflegeVersionReplayFilterUndHistorischerStand(){

  await using var f=new Testdaten();await f.Start();var(o,p,e)=await Anlage(f);var alterStand=(await f.D.Einsatz("team",e)).Daten;

  var eingabe=new Objektaenderung(Nonce(),2,"Kontorhaus Süd","Hafenweg 9, 12345 Elbstadt",false,"Zugang vorher abstimmen");

  await f.D.ObjektAendern("buero",o,eingabe);await f.D.ObjektAendern("buero",o,eingabe);

  f.Db.ChangeTracker.Clear();var neu=await f.D.Objekt("buero",o);Assert.Equal(3,neu.Version);Assert.False(neu.Aktiv);Assert.Equal("Kontorhaus Süd",neu.Name);Assert.Equal(alterStand,(await f.D.Einsatz("team",e)).Daten);

  Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.ObjektAendern("buero",o,eingabe with{AnlageId=Nonce()}))).Status);

  Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.ObjektAendern("team",o,eingabe))).Status);

  Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.ObjektAendern("fremd",o,eingabe))).Status);

  Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.EinsatzAnlegen("buero",o,new(Nonce(),p,new(2026,10,25),"team")))).Status);

  Assert.Equal(0,JsonSerializer.SerializeToElement(await f.D.Objekte("buero",status:"aktiv")).GetProperty("anzahl").GetInt32());

  Assert.Equal(1,JsonSerializer.SerializeToElement(await f.D.Objekte("buero",suche:"SÜD",status:"inaktiv")).GetProperty("anzahl").GetInt32());

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.ObjektAendern("buero",o,eingabe with{AnlageId=Nonce(),Version=3,Name=" "}));

  await f.D.ObjektAendern("buero",o,eingabe with{AnlageId=Nonce(),Version=3,Aktiv=true});Assert.True((await f.D.Objekt("buero",o)).Aktiv);

  Assert.Equal(2,await f.Db.Leistungsaktionen.CountAsync(x=>x.Art=="Objekt bearbeiten"));

 }

 [Fact] public async Task ObjektpflegeSchreibfehlerAendertWederDatenNochAudit(){

  await using var f=new Testdaten();await f.Start();var(o,_,_)=await Anlage(f);var vorher=await f.Db.Leistungsaktionen.CountAsync();f.Schreibfehler=true;

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.ObjektAendern("buero",o,new(Nonce(),2,"Geändert","Neue Anschrift",false)));

  f.Schreibfehler=false;Assert.Equal("Kontorhaus West",(await f.D.Objekt("buero",o)).Name);Assert.True((await f.D.Objekt("buero",o)).Aktiv);Assert.Equal(vorher,await f.Db.Leistungsaktionen.CountAsync());

 }

 [Fact] public async Task VollstaendigerInternerPfadSnapshotRueckmeldungGenauEineNacharbeit(){

  await using var f=new Testdaten();await f.Start();var(o,p,e)=await Anlage(f);

  await f.D.Entscheiden("buero",e,new(Nonce(),1,"checkliste",[new(0,"Erledigt","Gründlich gereinigt"),new(1,"Teilweise erledigt","Zugang zum Nebenraum gesperrt")],"Fiktive Büroprüfung"));

  Assert.Equal("Erfasst durch Büro",JsonSerializer.Deserialize<JsonElement>((await f.Db.Leistungseinsaetze.AsNoTracking().SingleAsync(x=>x.Id==e)).Daten).GetProperty("erfassungsart").GetString());await f.D.Entscheiden("team",e,new(Nonce(),2,"melden"));await f.D.Entscheiden("buero",e,new(Nonce(),3,"pruefen"));

  var freigabe=new Leistungsentscheidung(Nonce(),4,"freigeben");await f.D.Entscheiden("buero",e,freigabe);await f.D.Entscheiden("buero",e,freigabe);Assert.Single(await f.Db.Leistungsnachweise.ToArrayAsync());

  var n=await f.Db.Leistungsnachweise.AsNoTracking().SingleAsync();var bytes=await f.D.Nachweis("team",n.Id);Assert.Equal(n.Hash,Leistungsdienst.Hash(bytes));Assert.True((await Betriebsplattform.Web.Betrieb.Dokumentbestand.Pruefen(f.Db,f.Root)).Konsistent);Assert.Contains("Teilweise erledigt",Encoding.UTF8.GetString(bytes));

  var plan2=Id(await f.D.PlanAnlegen("buero",o,new(Nonce(),2,"Unterhaltsreinigung geändert",new(2026,11,1),[new("Neue Leistung","Flur","Bei Bedarf",true,"")])));Assert.NotEqual(p,plan2);Assert.Equal(bytes,await f.D.Nachweis("team",n.Id));

  var a=await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("team",e,new(Nonce(),5,"checkliste",[new(0,"Erledigt",""),new(1,"Erledigt","")])));Assert.Equal(409,a.Status);

  var rueck=new Leistungsentscheidung(Nonce(),5,"rueckmeldung",Bemerkung:"Fiktive telefonische Beanstandung: Nebenraum nacharbeiten",Kundenstatus:"Beanstandet",Position:1,Nacharbeit:true);

  await f.D.Entscheiden("buero",e,rueck);await f.D.Entscheiden("buero",e,rueck);Assert.Single(await f.Db.Aufgaben.ToArrayAsync());Assert.Equal(bytes,await f.D.Nachweis("buero",n.Id));

  var neu=Id(await f.D.Entscheiden("buero",e,new(Nonce(),6,"revision",Bemerkung:"Interne Korrektur nach Rückmeldung")));Assert.NotEqual(e,neu);Assert.Equal(e,(await f.Db.Leistungseinsaetze.SingleAsync(x=>x.Id==neu)).VorgaengerId);Assert.Equal(2,await f.Db.Leistungsplaene.CountAsync());

 }

 [Fact] public async Task RechteReplayPflichtfelderUndVersionen(){

  await using var f=new Testdaten();await f.Start();var(o,p,e)=await Anlage(f);

  Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Objekt("fremd",o))).Status);Assert.Equal(0,JsonSerializer.SerializeToElement(await f.D.Objekte("fremd")).GetProperty("anzahl").GetInt32());Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Einsatz("fremd",e))).Status);

  Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.PlanAnlegen("team",o,new(Nonce(),2,"Unerlaubt",new(2026,10,1),[new("Test","","Bei Bedarf",true,"")])))).Status);

  Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("team",e,new(Nonce(),1,"freigeben")))).Status);

  Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("team",e,new(Nonce(),1,"melden")))).Status);

  Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("team",e,new(Nonce(),99,"checkliste",[new(0,"Erledigt",""),new(1,"Erledigt","")])))).Status);

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("team",e,new(Nonce(),1,"checkliste",[new(0,"Nicht anwendbar",""),new(1,"Erledigt","")])));

  var q=new Einsatzeingabe(Nonce(),p,new(2026,10,26),"team");var id=Id(await f.D.EinsatzAnlegen("buero",o,q));Assert.Equal(id,Id(await f.D.EinsatzAnlegen("buero",o,q)));Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.EinsatzAnlegen("buero",o,q with{Arbeitstag=new(2026,10,27)}))).Status);

  await f.Db.Freigaben.Where(x=>x.BenutzerId=="team").ExecuteDeleteAsync();Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Einsatz("team",e))).Status);

 }

 [Fact] public async Task UnveraenderlichePlaeneKeinXssUndIntegritaetssperre(){

  await using var f=new Testdaten();await f.Start();var(o,p,e)=await Anlage(f);await f.D.Entscheiden("team",e,new(Nonce(),1,"checkliste",[new(0,"Erledigt","<script>alert(1)</script>"),new(1,"Nicht anwendbar","An diesem Tag nicht erforderlich")],"ÄÖÜß "+new string('x',2000)));await f.D.Entscheiden("team",e,new(Nonce(),2,"melden"));await f.D.Entscheiden("buero",e,new(Nonce(),3,"pruefen"));await f.D.Entscheiden("buero",e,new(Nonce(),4,"freigeben"));var n=await f.Db.Leistungsnachweise.AsNoTracking().SingleAsync();var b=await f.D.Nachweis("team",n.Id);Assert.DoesNotContain("<script>",Encoding.UTF8.GetString(b));Assert.Contains("&lt;script&gt;",Encoding.UTF8.GetString(b));

  f.Db.ChangeTracker.Clear();var plan=await f.Db.Leistungsplaene.SingleAsync(x=>x.Id==p);plan.Name="Still geändert";await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.Db.SaveChangesAsync());f.Db.ChangeTracker.Clear();

  await File.WriteAllBytesAsync(Path.Combine(f.Root,n.StorageKey+".bin"),new byte[]{1,2,3});Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Nachweis("team",n.Id))).Status);

 }

 [Fact] public async Task RueckmeldungUndNacharbeitRollenBeiSchreibfehlerGemeinsamZurueck(){

  await using var f=new Testdaten();await f.Start();var(_,_,id)=await Anlage(f);await f.D.Entscheiden("team",id,new(Nonce(),1,"checkliste",[new(0,"Erledigt",""),new(1,"Nicht erledigt","Fiktiver Bereich unzugänglich")]));await f.D.Entscheiden("team",id,new(Nonce(),2,"melden"));await f.D.Entscheiden("buero",id,new(Nonce(),3,"pruefen"));await f.D.Entscheiden("buero",id,new(Nonce(),4,"freigeben"));var vorher=await f.Db.Leistungsaktionen.CountAsync();f.Schreibfehler=true;

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("buero",id,new(Nonce(),5,"rueckmeldung",Bemerkung:"Fiktive Beanstandung",Kundenstatus:"Beanstandet",Position:1,Nacharbeit:true)));f.Schreibfehler=false;

  Assert.Empty(await f.Db.Aufgaben.ToArrayAsync());Assert.Equal(vorher,await f.Db.Leistungsaktionen.CountAsync());Assert.Equal("Kundenbestätigung ausstehend",(await f.Db.Leistungseinsaetze.SingleAsync(x=>x.Id==id)).Status);

 }

 [Fact] public async Task UngueltigeTurnusDatenKennungUndHistorischeLoeschungGesperrt(){

  await using var f=new Testdaten();await f.Start();var(o,p,_)=await Anlage(f);

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.PlanAnlegen("buero",o,new(Nonce(),2,"Plan",new(2026,10,1),[new("Leistung","","Bestimmte Wochentage",true,"",[8])])));

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.EinsatzAnlegen("buero",o,new("ungueltig",p,new(2026,10,25),"team")));

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.EinsatzAnlegen("buero",o,new(Nonce(),p,new(2026,9,30),"team")));

  f.Db.ChangeTracker.Clear();f.Db.Remove(await f.Db.Leistungsplaene.SingleAsync(x=>x.Id==p));await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.Db.SaveChangesAsync());

 }

 [Fact] public async Task SiebzigSynthetischeObjekteWerdenBerechtigtGelistet(){

  await using var f=new Testdaten();await f.Start();var watch=System.Diagnostics.Stopwatch.StartNew();for(var i=0;i<70;i++)await f.D.ObjektAnlegen("admin",new(Nonce(),"kunde","Fiktives Objekt "+i,"Prüfweg "+(i+1),"buero"));var ids=new HashSet<string>();for(var seite=0;seite<4;seite++){var data=JsonSerializer.SerializeToElement(await f.D.Objekte("buero",seite));Assert.Equal(70,data.GetProperty("anzahl").GetInt32());foreach(var row in data.GetProperty("eintraege").EnumerateArray())Assert.True(ids.Add(row.GetProperty("Id").GetString()!));}Assert.Equal(70,ids.Count);Console.WriteLine("70 synthetische SQLite-Objekte anlegen/listen: "+watch.ElapsedMilliseconds+"ms; kein Hardware-/Mehrbenutzerbenchmark.");

 }



 [Fact] public async Task FreigabeKompensiertNeueDateiBeiDbFehlerUndBleibtOhneStorageGesperrt(){

  await using var f=new Testdaten();await f.Start();var(_,_,id)=await Anlage(f);await f.D.Entscheiden("team",id,new(Nonce(),1,"checkliste",[new(0,"Erledigt",""),new(1,"Erledigt","")]));await f.D.Entscheiden("team",id,new(Nonce(),2,"melden"));await f.D.Entscheiden("buero",id,new(Nonce(),3,"pruefen"));f.Schreibfehler=true;

  await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.D.Entscheiden("buero",id,new(Nonce(),4,"freigeben")));f.Schreibfehler=false;Assert.Empty(Directory.GetFiles(f.Root));Assert.Empty(await f.Db.Leistungsnachweise.ToArrayAsync());Assert.Equal("In Büroprüfung",(await f.Db.Leistungseinsaetze.SingleAsync(x=>x.Id==id)).Status);

  var ohne=new Leistungsdienst(f.Db,new AufgabenDienst(f.Db,TimeProvider.System,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")),TimeProvider.System);Assert.Equal(503,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>ohne.Entscheiden("buero",id,new(Nonce(),4,"freigeben")))).Status);Assert.Empty(await f.Db.Leistungsnachweise.ToArrayAsync());await f.D.Entscheiden("buero",id,new(Nonce(),4,"freigeben"));Assert.Single(await f.Db.Leistungsnachweise.ToArrayAsync());

 }



}

