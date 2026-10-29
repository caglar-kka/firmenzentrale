using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Betrieb;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class TermineTests(ITestOutputHelper output)
{
 static Termineingabe Input()=>new("Teambesprechung Musterbetrieb","2026-10-22T09:00","2026-10-22T10:00",false,"Ausschließlich erfundene Testdaten","Besprechungsraum",AnlageId:Guid.NewGuid().ToString("N"));
 [Fact] public async Task ErinnerungenSindPersoenlichQuittierbarUndNachVerschiebungNeuFaellig(){
  using var c=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");await c.OpenAsync();var opt=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options;await using var db=new FirmenDbContext(opt);await db.Database.EnsureCreatedAsync();db.Benutzer.AddRange(new Benutzerkonto{Id="a",Login="a",Anzeigename="Mira Weber",PasswortHash="nur-unit-test"},new Benutzerkonto{Id="b",Login="b",Anzeigename="Nils Horn",PasswortHash="nur-unit-test"});await db.SaveChangesAsync();var clock=new MfaTests.Clock{Now=new(2026,10,22,6,50,0,TimeSpan.Zero)};var d=new Termindienst(db,clock);
  var input=Input() with{ErinnerungMinuten=15,Strasse="Hafenweg",Hausnummer="7",Plz="12345",Stadt="Elbstadt",Land="DE"};var t=await d.Speichern("a",null,input);Assert.Equal("Hafenweg",(await d.Lesen("a",t.Id)).Strasse);
  int Anzahl(object x)=>JsonSerializer.SerializeToElement(x).GetProperty("anzahl").GetInt32();Assert.Equal(1,Anzahl(await d.Erinnerungen("a")));Assert.Equal(0,Anzahl(await d.Erinnerungen("b")));Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Quittieren("b",t.Id,t.Erinnerungskennung))).Status);
  await using var parallel=new FirmenDbContext(opt);await parallel.Termine.SingleAsync(x=>x.Id==t.Id);var d2=new Termindienst(parallel,clock);var alt=t.Erinnerungskennung;await d.Quittieren("a",t.Id,alt);await d2.Quittieren("a",t.Id,alt);Assert.Equal(0,Anzahl(await d.Erinnerungen("a")));Assert.Equal(1,await db.Sicherheitsereignisse.CountAsync(x=>x.Aktion=="Terminerinnerung quittiert"));
  t=await d.Speichern("a",t.Id,input with{Version=2,Beginn="2026-10-22T10:00",Ende="2026-10-22T11:00"});Assert.NotEqual(alt,t.Erinnerungskennung);Assert.Equal(0,Anzahl(await d.Erinnerungen("a")));Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Quittieren("a",t.Id,alt))).Status);
  clock.Now=clock.Now.AddHours(2);await using var neustart=new FirmenDbContext(opt);var d3=new Termindienst(neustart,clock);Assert.Equal(1,Anzahl(await d3.Erinnerungen("a")));await d3.Entfernen("a",t.Id,t.Version);Assert.Equal(0,Anzahl(await d3.Erinnerungen("a")));await Assert.ThrowsAsync<Arbeitsfehler>(()=>d3.Quittieren("a",t.Id,t.Erinnerungskennung));
 }
 [Fact] public async Task GeaenderteBeschreibungInvalidiertGelesenenHinweisUndAdresseBleibtSichtbar(){
  using var c=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");await c.OpenAsync();var opt=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options;await using var db=new FirmenDbContext(opt);await db.Database.EnsureCreatedAsync();db.Benutzer.Add(new(){Id="a",Login="a",Anzeigename="Mira Weber",PasswortHash="unit-test"});await db.SaveChangesAsync();
  var clock=new MfaTests.Clock{Now=new(2026,10,22,8,0,0,TimeSpan.Zero)};var d=new Termindienst(db,clock);var input=Input() with{ErinnerungMinuten=0,Strasse="Hafenweg",Hausnummer="7",Plz="12345",Stadt="Elbstadt"};var t=await d.Speichern("a",null,input);var kennung=t.Erinnerungskennung;await d.Quittieren("a",t.Id,kennung);
  t=await d.Speichern("a",t.Id,input with{Version=t.Version,Beschreibung="Zugang nur über den Hintereingang"});Assert.NotEqual(kennung,t.Erinnerungskennung);Assert.False(t.ErinnerungQuittiert);Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Quittieren("a",t.Id,kennung))).Status);
  var liste=JsonSerializer.SerializeToElement(await d.Erinnerungen("a"));Assert.Equal("Hafenweg 7 · 12345 Elbstadt",liste.GetProperty("eintraege")[0].GetProperty("adresse").GetString());
 }
 [Theory]
 [InlineData("2026-03-29T02:30","2026-03-29T04:00")]
 [InlineData("2026-10-25T02:30","2026-10-25T04:00")]
 [InlineData("2026-02-30T10:00","2026-03-01T10:00")]
 [InlineData("2026-10-22T10:00","2026-10-22T10:00")]
 [InlineData("2026-10-22T10:00Z","2026-10-22T11:00")]
 public void UngueltigeOderMehrdeutigeZeitWirdNichtVerschoben(string b,string e)=>Assert.Throws<Arbeitsfehler>(()=>Termindienst.Pruefen(Input() with{Beginn=b,Ende=e}));
 [Fact] public void GanztagExklusivesEndeUndZeitumstellung(){var t=Termindienst.Pruefen(Input() with{Beginn="2026-03-29T00:00",Ende="2026-03-30T00:00",Ganztaegig=true});Assert.Equal(23,(t.Ende-t.Beginn).TotalHours);var h=Termindienst.Pruefen(Input() with{Beginn="2026-10-25T00:00",Ende="2026-10-26T00:00",Ganztaegig=true});Assert.Equal(25,(h.Ende-h.Beginn).TotalHours);Assert.Throws<Arbeitsfehler>(()=>Termindienst.Pruefen(Input() with{Ganztaegig=true}));}
 [Fact] public void MigrationIstNurAdditiv(){var m=new Betriebsplattform.Web.Persistenz.Migrationen.EigenstaendigeTermine();Assert.Equal(2,m.UpOperations.OfType<CreateTableOperation>().Count());Assert.All(m.UpOperations,o=>Assert.True(o is CreateTableOperation or CreateIndexOperation));}
 sealed class PostgresAttribute:FactAttribute{public PostgresAttribute(){if(Environment.GetEnvironmentVariable("FZ_TERMINE_POSTGRES")!="1")Skip="Explicit isolated PostgreSQL calendar test required.";}}
 [Postgres] public async Task IsoliertePostgresHttpKetteUndPersoenlicheRechte()
 {
  var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../"));var evidence=Path.Combine(workspace,"private/pruefungen/kalender-termine-20261022");Directory.CreateDirectory(evidence);
  var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")){Pooling=false,IncludeErrorDetail=false,LogParameters=false,Timeout=5};Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);cs.Database="postgres";
  var resume=Environment.GetEnvironmentVariable("FZ_TERMINE_RESUME");var fortsetzen=!string.IsNullOrEmpty(resume);if(fortsetzen)Assert.Matches("^fz_termine_test_[a-f0-9]{32}$",resume!);var database=resume??("fz_termine_test_"+Guid.NewGuid().ToString("N"));
  if(!fortsetzen)await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());output.WriteLine("Vor Schreibzugriff: isoliertes Ziel "+database+"; Verwaltungsverbindung postgres/Loopback");await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(database)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
  cs.Database=database;var administration=cs.ConnectionString;
  FirmenDbContext Db(string connection)=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(connection,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
  async Task Target(FirmenDbContext db){Assert.Equal(database,new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Database);await db.Database.OpenConnectionAsync();await using var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)db.Database.GetDbConnection());Assert.Equal(database,await q.ExecuteScalarAsync());}
  const string pass="Nur-dieser-isolierte-Kalendertest-2026!";string teamId,otherId,adminId;
  if(!fortsetzen)await using(var db=Db(administration)){await Target(db);await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());await Target(db);var h=Anmeldung.Passwortdienst();adminId=(await Anmeldung.Anlegen(db,h,new("kalender.admin","Test Administration",pass,"Administrator"),true)).Id;teamId=(await Anmeldung.Anlegen(db,h,new("kalender.team","Test Büro",pass,"Mitarbeiter"))).Id;otherId=(await Anmeldung.Anlegen(db,h,new("kalender.fremd","Anderes Testkonto",pass,"Mitarbeiter"))).Id;await Target(db);db.Aufgaben.Add(new(){Betreff="Aufgabenfrist unverändert",ErstellerId=teamId,ZustaendigId=teamId,FaelligAm=new(2026,10,22)});await db.SaveChangesAsync();}
  else {await using var db=Db(administration);await Target(db);Assert.False(db.Database.HasPendingModelChanges());teamId=(await db.Benutzer.SingleAsync(u=>u.Login=="kalender.team")).Id;otherId=(await db.Benutzer.SingleAsync(u=>u.Login=="kalender.fremd")).Id;adminId=(await db.Benutzer.SingleAsync(u=>u.Login=="kalender.admin")).Id;}
  var role="fz_app_termine_"+Guid.NewGuid().ToString("N")[..16];var secret=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(administration,role,secret);cs.Username=role;cs.Password=secret;var runtime=cs.ConnectionString;
  using var store=new X509Store(StoreName.My,StoreLocation.CurrentUser);store.Open(OpenFlags.ReadOnly);using var cert=store.Certificates.Find(X509FindType.FindByThumbprint,Environment.GetEnvironmentVariable("FZ_BROWSER_CERT_THUMBPRINT")??throw new InvalidOperationException("Existing trusted test certificate required."),true).Single();using(var chain=new X509Chain())Assert.True(chain.Build(cert));Assert.True(cert.HasPrivateKey);
  var root=Path.Combine(Path.GetTempPath(),"fz-termine-"+database[16..]);Directory.CreateDirectory(root);var pfx=Path.Combine(root,"host.pfx");var cp=MfaTests.Secret();await File.WriteAllBytesAsync(pfx,cert.Export(X509ContentType.Pkcs12,cp));
  var url=new Uri("https://127.0.0.1:5182/");Microsoft.AspNetCore.Builder.WebApplication? app=null;
  async Task Start(){await using(var db=Db(runtime))await Target(db);app=ApiAnwendung.Erstellen(["--environment=Development","--FirmenZentrale:Startmodus=Anmeldung","--FirmenZentrale:Persistence:Provider=PostgreSql","--FirmenZentrale:Persistence:DocumentRoot="+Path.Combine(root,"documents"),"--ConnectionStrings:FirmenZentrale="+runtime,"--FirmenZentrale:Security:Enabled=true","--FirmenZentrale:Security:CertificatePath="+pfx,"--FirmenZentrale:Security:CertificatePassword="+cp],5182,Path.Combine(workspace,"src/frontend/dist"));await app.StartAsync();}
  async Task Stop(){if(app is not null){await app.StopAsync();await app.DisposeAsync();app=null;}}
  HttpClient Client(){var c=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
  async Task Csrf(HttpClient c){var s=await c.GetFromJsonAsync<JsonElement>("api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",s.GetProperty("csrf").GetString());}
  async Task Login(HttpClient c,string login){await Csrf(c);Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("api/auth/login",new{login,passwort=pass})).StatusCode);await Csrf(c);}
  var nachweise=new List<object>();async Task Expect(string name,HttpResponseMessage r,int expected){nachweise.Add(new{name,status=(int)r.StatusCode,expected});Assert.Equal(expected,(int)r.StatusCode);r.Dispose();}
  try{
   await Start();if(!fortsetzen){using var a=Client();using var b=Client();using var admin=Client();using var anon=Client();await Login(a,"kalender.team");await Login(b,"kalender.fremd");await Login(admin,"kalender.admin");
   var input=Input();await Expect("anonymous-list",await anon.GetAsync("api/termine/?von=2026-10-01&bis=2026-11-01"),401);await Expect("anonymous-create",await anon.PostAsJsonAsync("api/termine/",input),401);
   var created=await a.PostAsJsonAsync("api/termine/",input);Assert.Equal(200,(int)created.StatusCode);var t=await created.Content.ReadFromJsonAsync<Termin>();Assert.NotNull(t);var id=t.Id;
   Assert.Equal(id,(await (await a.PostAsJsonAsync("api/termine/",input)).Content.ReadFromJsonAsync<Termin>())!.Id);
   var list=await a.GetFromJsonAsync<JsonElement>("api/termine/?von=2026-10-01&bis=2026-11-01");Assert.Equal(1,list.GetProperty("anzahl").GetInt32());Assert.Equal(0,(await b.GetFromJsonAsync<JsonElement>("api/termine/?von=2026-10-01&bis=2026-11-01")).GetProperty("anzahl").GetInt32());
   foreach(var c in new[]{b,admin}){await Expect("foreign-read",await c.GetAsync("api/termine/"+id),404);await Expect("foreign-update",await c.PostAsJsonAsync("api/termine/"+id,input with{Version=1}),404);await Expect("foreign-delete",await c.PostAsJsonAsync("api/termine/"+id+"/loeschen",new{version=1}),404);}
   a.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");await Expect("csrf",await a.PostAsJsonAsync("api/termine/"+id,input with{Version=1}),400);await Csrf(a);
   await Expect("invalid",await a.PostAsJsonAsync("api/termine/",input with{AnlageId=Guid.NewGuid().ToString("N"),Titel=""}),400);
   await Expect("unknown-owner-field",await a.PostAsJsonAsync("api/termine/",new{titel="Manipulation",beginn=input.Beginn,ende=input.Ende,ganztaegig=false,eigentuemerId=otherId}),400);
   await Expect("DST-luecke",await a.PostAsJsonAsync("api/termine/",Input() with{Beginn="2026-03-29T02:30",Ende="2026-03-29T04:00"}),400);
   await Expect("DST-doppelstunde",await a.PostAsJsonAsync("api/termine/",Input() with{Beginn="2026-10-25T02:30",Ende="2026-10-25T04:00"}),400);
   var race=await Task.WhenAll(a.PostAsJsonAsync("api/termine/"+id,input with{Version=1,Titel="Änderung A"}),a.PostAsJsonAsync("api/termine/"+id,input with{Version=1,Titel="Änderung B"}));Assert.Equal(new[]{200,409},race.Select(r=>(int)r.StatusCode).Order().ToArray());foreach(var r in race)r.Dispose();nachweise.Add(new{name="parallel-update",status=new[]{200,409}});
   await Expect("stale-delete",await a.PostAsJsonAsync("api/termine/"+id+"/loeschen",new{version=1}),409);
   await Stop();await Start();using var again=Client();await Login(again,"kalender.team");var reload=await again.GetFromJsonAsync<Termin>("api/termine/"+id);Assert.Equal(2,reload!.Version);await Expect("delete",await again.PostAsJsonAsync("api/termine/"+id+"/loeschen",new{version=2}),200);await Expect("deleted-read",await again.GetAsync("api/termine/"+id),404);await Expect("id-not-resurrected",await again.PostAsJsonAsync("api/termine/",input),409);
   var duplicate=Input();var twice=await Task.WhenAll(again.PostAsJsonAsync("api/termine/",duplicate),again.PostAsJsonAsync("api/termine/",duplicate));Assert.All(twice,r=>Assert.Equal(200,(int)r.StatusCode));foreach(var r in twice)r.Dispose();
   Assert.Equal(1,(await again.GetFromJsonAsync<JsonElement>("api/kalender?von=2026-10-01&bis=2026-11-01")).GetProperty("anzahl").GetInt32());
   await using(var db=Db(runtime)){await Target(db);Assert.Equal(2,await db.Termine.CountAsync());Assert.Equal(4,await db.Terminverlauf.CountAsync());Assert.Equal(4,await db.Sicherheitsereignisse.CountAsync(x=>x.Aktion.StartsWith("Termin ")));Assert.True((await db.Termine.SingleAsync(x=>x.Id==id)).Entfernt);Assert.Single(await db.Aufgaben.ToArrayAsync());}
   // Separate database represents the existing host-per-organisation boundary.
   var zweite="fz_termine_test_"+Guid.NewGuid().ToString("N");var adminCs=new NpgsqlConnectionStringBuilder(administration);adminCs.Database="postgres";
   await using(var c=new NpgsqlConnection(adminCs.ConnectionString)){await c.OpenAsync();await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());output.WriteLine("Zweiter isolierter Organisationskontext vor Schreibzugriff: "+zweite);await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(zweite)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
   adminCs.Database=zweite;
   await using(var db=Db(adminCs.ConnectionString)){Assert.Equal(zweite,db.Database.GetDbConnection().Database);await db.Database.OpenConnectionAsync();await using(var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)db.Database.GetDbConnection()))Assert.Equal(zweite,await q.ExecuteScalarAsync());await db.Database.MigrateAsync();db.Benutzer.Add(new(){Id=teamId,Login="same-id",Anzeigename="Andere Organisation",Rolle="Mitarbeiter",PasswortHash="Synthetisch-kein-echter-Login"});await db.SaveChangesAsync();Assert.Empty(await db.Termine.ToArrayAsync());var error=await Assert.ThrowsAsync<Arbeitsfehler>(()=>new Termindienst(db,TimeProvider.System).Lesen(teamId,id));Assert.Equal(404,error.Status);}
   await using(var db=Db(runtime)){await Target(db);var audit=await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync("UPDATE \"Terminverlauf\" SET \"Vorher\" = 'Manipulation'"));Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,audit.SqlState);}
   await using(var db=Db(administration)){await Target(db);(await db.Benutzer.SingleAsync(x=>x.Id==otherId)).Aktiv=false;await db.SaveChangesAsync();}
   await Expect("disabled-account",await b.GetAsync("api/termine/?von=2026-10-01&bis=2026-11-01"),401);
   nachweise.Add(new{name="organisation-database-boundary",isoliertesZiel=zweite,ergebnis="same user ID cannot read first organisation appointment"});
   await File.WriteAllTextAsync(Path.Combine(evidence,"http-rechte.json"),JsonSerializer.Serialize(nachweise,new JsonSerializerOptions{WriteIndented=true}));
   }
   await File.WriteAllTextAsync(Path.Combine(evidence,"testhost.json"),JsonSerializer.Serialize(new{database,url=url.ToString(),pid=Environment.ProcessId,benutzer="kalender.team",zustand="synthetisch, authentifiziert, persistentes PostgreSQL, kein Vorschauadapter"}));
   output.WriteLine("CRUD/CSRF/Fremdrechte/Admin-Fremdzugriff/Idempotenz/echte parallele Updates/Host-Neustart/Aufgaben unverändert: "+database);
   if(Environment.GetEnvironmentVariable("FZ_TERMINE_BROWSER")=="1"){
    var stop=Path.Combine(evidence,"browser-stop.txt");var limit=DateTimeOffset.UtcNow.AddMinutes(35);while(!File.Exists(stop)&&DateTimeOffset.UtcNow<limit)await Task.Delay(500);Assert.True(File.Exists(stop),"Browser window exceeded bounded lifetime.");
   }
  }finally{await Stop();}
 }
}
