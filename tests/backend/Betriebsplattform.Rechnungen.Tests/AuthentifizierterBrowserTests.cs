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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

// Opt-in extension of the existing local PostgreSQL gates. The browser uses the real product
// host and authentication. Control files never expose test-only endpoints or cookie bypasses.
public sealed class AuthentifizierterBrowserTests
{
    // Fixture-only byte verification, not a product malware adapter or HTTP bypass.
    private sealed class PaketTestfreigabe(byte[][] dateien):IMalwarepruefung
    {
        public Task Freigeben(byte[] bytes,CancellationToken ct){ct.ThrowIfCancellationRequested();if(!dateien.Any(b=>b.AsSpan().SequenceEqual(bytes)))throw new Arbeitsfehler("Nur geprüfte fiktive Paketdateien.",503);return Task.CompletedTask;}
    }
    private sealed class BrowserGate : FactAttribute
    {
        public BrowserGate(){if(Environment.GetEnvironmentVariable("FZ_AUTH_BROWSER")!="1")Skip="Explicit supervised local browser/PostgreSQL gate required.";}
    }
    // Deliberately public fictional fixture credential, never used for an existing account.
    // The isolated host is loopback-only and has a bounded lifetime. Do not log this value.
    private const string FixturePasswort="Nur-fuer-diese-lokale-Pruefung-2026!";

    [BrowserGate]
    public async Task EchterBrowserHttpsKontenPostgreSqlUndHostneustart()
    {
        Assert.True(OperatingSystem.IsWindows());
        var workspace=Path.GetFullPath(Environment.GetEnvironmentVariable("FZ_BROWSER_WORKSPACE")??throw new InvalidOperationException("Explicit workspace required."));
        Assert.True(File.Exists(Path.Combine(workspace,"AGENTS.md")));
        var evidenceName=Environment.GetEnvironmentVariable("FZ_BROWSER_EVIDENCE_NAME")??"auth-systemtest-20261020";
        Assert.Matches("^[a-z0-9-]{1,64}$",evidenceName);
        var evidence=Path.Combine(workspace,"private","pruefungen",evidenceName);Directory.CreateDirectory(evidence);
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();
        var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")){Pooling=false,IncludeErrorDetail=false,LogParameters=false,Timeout=5};
        Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);cs.Database="postgres";
        var resume=Environment.GetEnvironmentVariable("FZ_BROWSER_RESUME")=="1";
        var run=Guid.NewGuid().ToString("N");var database="fz_browser_test_"+run;
        if(resume){var prior=JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(Path.Combine(evidence,"ready.json")));database=prior.GetProperty("database").GetString()!;Assert.Matches("^fz_browser_test_[a-f0-9]{32}$",database);run=database["fz_browser_test_".Length..];}
        if(!resume)
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){
            await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);
            await using(var who=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await who.ExecuteScalarAsync());
            await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(database)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();
        }
        await File.WriteAllTextAsync(Path.Combine(evidence,"isoliertes-ziel-"+run+".json"),JsonSerializer.Serialize(new{database,host="127.0.0.1",isolation="new fz_browser_test database; main/customer DB untouched",time=DateTimeOffset.UtcNow}));
        cs.Database=database;var administration=cs.ConnectionString;
        FirmenDbContext Db(string connection)=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(connection,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
        async Task Target(FirmenDbContext db){Assert.Equal(database,new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Database);await db.Database.OpenConnectionAsync();await using var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)db.Database.GetDbConnection());Assert.Equal(database,await q.ExecuteScalarAsync());}
        var hasher=Anmeldung.Passwortdienst();string adminId,teamId,otherId;
        if(!resume)await using(var db=Db(administration)){
            await Target(db);await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());
            await Target(db);adminId=(await Anmeldung.Anlegen(db,hasher,new("nora.lind","Nora Lind",FixturePasswort,"Administrator"),true)).Id;
            await Target(db);teamId=(await Anmeldung.Anlegen(db,hasher,new("anna.berg","Anna Berg",FixturePasswort,"Mitarbeiter"))).Id;
            await Target(db);otherId=(await Anmeldung.Anlegen(db,hasher,new("leon.kruse","Leon Kruse",FixturePasswort,"Mitarbeiter"))).Id;
            await Target(db);
            db.Kontakte.AddRange(
                new KontaktDatensatz{Id="elbequartier",Name="Elbequartier Objektservice GmbH",Suchname="elbequartier objektservice gmbh",Strasse="Kontorweg",Hausnummer="12",Plz="20457",Ort="Musterhafen",Land="DE"},
                new KontaktDatensatz{Id="nordlicht",Name="Nordlicht Haustechnik GmbH",Suchname="nordlicht haustechnik gmbh",Strasse="Werftring",Hausnummer="8",Plz="20457",Ort="Musterhafen",Land="DE"},
                new KontaktDatensatz{Id="mara-falk",Art="Person",Name="Mara Falk",Vorname="Mara",Nachname="Falk",Suchname="mara falk",FirmaId="nordlicht",Rolle="Serviceleitung"},
                new KontaktDatensatz{Id="anderer-bereich",Name="Hafenbogen Innenausbau GmbH",Suchname="hafenbogen innenausbau gmbh"});
            db.Kontaktwerte.AddRange(new KontaktwertDatensatz{KontaktId="nordlicht",Art="E-Mail",Wert="service@nordlicht.example",Vergleichswert="service@nordlicht.example",Aktiv=true,Primaer=true},new KontaktwertDatensatz{KontaktId="mara-falk",Art="E-Mail",Wert="mara.falk@nordlicht.example",Vergleichswert="mara.falk@nordlicht.example",Aktiv=true,Primaer=true});
            db.Kategorien.Add(new(){Id="wartung",Name="Wartung und Gebäudetechnik",Suchname="wartung und gebaeudetechnik"});
            db.Akten.AddRange(new AkteDatensatz{Id="kontorhaus-suedhafen",Titel="Kontorhaus Südhafen – Lüftungswartung 2026",Suchtext="kontorhaus suedhafen lueftungswartung 2026",KategorieId="wartung",FirmaId="elbequartier",Status="In Bearbeitung"},new AkteDatensatz{Id="geschuetzte-akte",Titel="Vertrauliche Vertragsprüfung Hafenbogen",Suchtext="vertrauliche vertragspruefung",KategorieId="wartung",FirmaId="anderer-bereich"});
            db.Freigaben.AddRange(new(){BenutzerId=teamId,KontaktId="elbequartier"},new(){BenutzerId=teamId,KontaktId="nordlicht"});
            if(Environment.GetEnvironmentVariable("FZ_DOCUMENT_RULE")=="1"){
                var betrieb=db.Kontakte.Local.Single(x=>x.Id=="elbequartier");betrieb.Name="Elbequartier Objektservice";betrieb.Suchname="fiktiver kieselbuero";
                var versicherung=db.Kontakte.Local.Single(x=>x.Id=="nordlicht");versicherung.Name="Binnenstern Versicherung";versicherung.Suchname="fiktive binnenstern";
                var fall=db.Akten.Local.Single(x=>x.Id=="kontorhaus-suedhafen");fall.Titel="TEST-INTERN-781 – Versicherungsprüfung";fall.Suchtext="test intern 781 versicherungspruefung";
            }
            await db.SaveChangesAsync();
        }
        else {await using var db=Db(administration);await Target(db);Assert.False(db.Database.HasPendingModelChanges());adminId=(await db.Benutzer.SingleAsync(u=>u.Login=="nora.lind")).Id;teamId=(await db.Benutzer.SingleAsync(u=>u.Login=="anna.berg")).Id;otherId=(await db.Benutzer.SingleAsync(u=>u.Login=="leon.kruse")).Id;Assert.Equal(Environment.GetEnvironmentVariable("FZ_LEISTUNGEN_MANUELL")=="1"?0:5,await db.Dokumente.CountAsync());if(Environment.GetEnvironmentVariable("FZ_LEISTUNGEN_MANUELL")=="1")Assert.True(await db.Leistungsobjekte.CountAsync()>=3);}
        await using(var db=Db(administration)){await Target(db);}
        var role="fz_app_browser_"+Guid.NewGuid().ToString("N")[..16];var runtimeSecret=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(administration,role,runtimeSecret);
        cs.Username=role;cs.Password=runtimeSecret;var runtime=cs.ConnectionString;
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FirmenZentrale","Development","browser-gate",run);var storage=Path.Combine(root,"documents");Directory.CreateDirectory(root);
        using var store=new X509Store(StoreName.My,StoreLocation.CurrentUser);store.Open(OpenFlags.ReadOnly);
        var thumbprint=Environment.GetEnvironmentVariable("FZ_BROWSER_CERT_THUMBPRINT")??throw new InvalidOperationException("Explicit existing trusted certificate required.");
        using var cert=store.Certificates.Find(X509FindType.FindByThumbprint,thumbprint,true).Single();
        using(var chain=new X509Chain())Assert.True(chain.Build(cert));Assert.True(cert.HasPrivateKey);
        var certPath=Path.Combine(root,"test-host.pfx");var certPassword=MfaTests.Secret();await File.WriteAllBytesAsync(certPath,cert.Export(X509ContentType.Pkcs12,certPassword));
        var url=new Uri("https://127.0.0.1:5181");WebApplication? app=null;var generation=resume?1:0;
        async Task Start(){
            await using(var check=Db(runtime))await Target(check);
            app=ApiAnwendung.Erstellen(["--environment=Development","--FirmenZentrale:Organisation:Name=Nordhafen Büroservice","--FirmenZentrale:Leistungsnachweise:Enabled="+(Environment.GetEnvironmentVariable("FZ_LEISTUNGEN_HTTP")=="1"||Environment.GetEnvironmentVariable("FZ_LEISTUNGEN_MANUELL")=="1"||Environment.GetEnvironmentVariable("FZ_ADMIN_HTTP")=="1"),"--FirmenZentrale:Startmodus=Anmeldung","--FirmenZentrale:Persistence:Provider=PostgreSql","--FirmenZentrale:Persistence:DocumentRoot="+storage,"--ConnectionStrings:FirmenZentrale="+runtime,"--FirmenZentrale:Security:Enabled=true","--FirmenZentrale:Security:CertificatePath="+certPath,"--FirmenZentrale:Security:CertificatePassword="+certPassword],5181,Path.Combine(workspace,"src","frontend","dist"));
            await app.StartAsync();generation++;
            using var probe=new HttpClient();var session=await probe.GetFromJsonAsync<JsonElement>(url+"api/auth/session");Assert.True(session.GetProperty("aktiv").GetBoolean());Assert.False(session.GetProperty("angemeldet").GetBoolean());Assert.False(session.TryGetProperty("vorschau",out _));
        }
        async Task Stop(){if(app is not null){await app.StopAsync();await app.DisposeAsync();app=null;}}
        async Task Write(string name,object value)=>await File.WriteAllTextAsync(Path.Combine(evidence,name),JsonSerializer.Serialize(value,new JsonSerializerOptions(JsonSerializerDefaults.Web){WriteIndented=true}));
        async Task Capture(string name){
            await using var db=Db(runtime);await Target(db);
            var docs=await db.Dokumente.AsNoTracking().Select(d=>new{d.Id,d.Titel,d.Dateiname,d.Hash,d.StorageKey,d.AbsenderId,d.Status,d.AnalyseJson}).ToArrayAsync();
            foreach(var d in docs){var bytes=await File.ReadAllBytesAsync(Path.Combine(storage,d.StorageKey+".bin"));Assert.Equal(d.Hash,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));}
            var workflows=await db.Eingangsworkflows.AsNoTracking().ToArrayAsync();var tasks=await db.Aufgaben.AsNoTracking().ToArrayAsync();
            await Write(name,new{database,generation,documents=docs,workflows,tasks,patterns=await db.Dokumentmuster.AsNoTracking().ToArrayAsync(),scanJobs=await db.Scanauftraege.AsNoTracking().ToArrayAsync(),documentReferences=await db.Dokumentzuordnungen.AsNoTracking().ToArrayAsync(),arrivals=await db.Dokumentaktivitaeten.CountAsync(d=>d.Kanal!=null),documentHistory=await db.Dokumentaktivitaeten.Where(d=>d.EingangsId!=null).Select(d=>new{d.EingangsId,d.Text,d.Zeitpunkt}).ToArrayAsync(),taskHistory=await db.Aufgabenverlauf.Select(h=>new{h.AufgabeId,h.Vorher,h.Nachher}).ToArrayAsync(),checkedAt=DateTimeOffset.UtcNow});
        }
        HttpClient Client(){var c=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
        async Task Csrf(HttpClient c){var s=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",s.GetProperty("csrf").GetString());}
        async Task Login(HttpClient c,string login){await Csrf(c);Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/login",new LoginEingabe(login,FixturePasswort))).StatusCode);await Csrf(c);}

        async Task AdminHttp(){
            var checks=new List<object>();
            async Task<JsonElement> Check(string name,HttpResponseMessage r,int status=200){using(r){checks.Add(new{name,status=(int)r.StatusCode,expected=status});await Write("admin-http.json",checks);Assert.Equal(status,(int)r.StatusCode);return r.Content.Headers.ContentType?.MediaType=="application/json"?JsonSerializer.Deserialize<JsonElement>(await r.Content.ReadAsStringAsync()):default;}}
            string pass,id;await using(var db=Db(administration)){await Target(db);var z=await LokalerAdminErstzugang.Erstellen(db,hasher,TimeProvider.System);pass=z.Passwort;id=z.BenutzerId;}
            using var c=Client();await Csrf(c);
            await Check("invalid-password",await c.PostAsJsonAsync("/api/auth/login",new LoginEingabe("admin",MfaTests.Secret())),401);
            await Check("first-password",await c.PostAsJsonAsync("/api/auth/login",new LoginEingabe("admin",pass)));await Csrf(c);
            await Check("before-personal-mfa",await c.GetAsync("/api/leistungen/objekte"),403);
            await using(var db=Db(administration)){await Target(db);var u=await db.Benutzer.SingleAsync(x=>x.Id==id);u.EinladungBis=DateTimeOffset.UtcNow.AddMinutes(-1);u.Version++;await db.SaveChangesAsync();}
            await Check("expired-first-session",await c.GetAsync("/api/auth/mfa/status"),401);await Csrf(c);
            await Check("expired-first-password",await c.PostAsJsonAsync("/api/auth/login",new LoginEingabe("admin",pass)),401);
            await using(var db=Db(administration)){await Target(db);var z=await LokalerAdminErstzugang.Erstellen(db,hasher,TimeProvider.System);pass=z.Passwort;Assert.Equal(id,z.BenutzerId);}
            await Csrf(c);await Check("renew-after-interruption",await c.PostAsJsonAsync("/api/auth/login",new LoginEingabe("admin",pass)));await Csrf(c);
            await Check("prepare-personal-mfa",await c.PostAsJsonAsync("/api/auth/mfa/vorbereiten",new MfaPruefung(pass,null)));
            byte[] key;await using(var db=Db(runtime)){await Target(db);key=MfaTests.Key(app!.Services.GetRequiredService<IDataProtectionProvider>(),await db.Benutzer.SingleAsync(x=>x.Id==id));}
            // Use the permitted previous TOTP window for activation, so current-window login needs no sleep.
            var step=DateTimeOffset.UtcNow.ToUnixTimeSeconds()/30;
            var codes=await Check("activate-personal-mfa",await c.PostAsJsonAsync("/api/auth/mfa/aktivieren",new MfaPruefung(pass,Totp.Code(key,step-1))));Assert.Equal(8,codes.GetProperty("codes").GetArrayLength());await Csrf(c);
            await Check("dashboard-after-mfa",await c.GetAsync("/api/leistungen/objekte"));
            await using(var db=Db(administration)){await Target(db);Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>LokalerAdminErstzugang.Erstellen(db,hasher,TimeProvider.System))).Status);var u=await db.Benutzer.SingleAsync(x=>x.Id==id);Assert.Null(u.EinladungBis);Assert.NotNull(u.MfaGeheimnis);}
            await Check("logout",await c.PostAsJsonAsync("/api/auth/logout",new{}));await Csrf(c);
            var response=await Check("second-login",await c.PostAsJsonAsync("/api/auth/login",new LoginEingabe("admin",pass)));Assert.True(response.GetProperty("mfaErforderlich").GetBoolean());await Csrf(c);
            var bad=Enumerable.Range(0,100).Select(n=>n.ToString("D6")).First(x=>!new[]{step-1,step,step+1}.Select(v=>Totp.Code(key,v)).Contains(x));
            await Check("invalid-second-factor",await c.PostAsJsonAsync("/api/auth/mfa-anmelden",new MfaPruefung(null,bad)),400);
            await Check("valid-second-factor",await c.PostAsJsonAsync("/api/auth/mfa-anmelden",new MfaPruefung(null,Totp.Code(key,step))));await Csrf(c);
            await Check("second-login-data",await c.GetAsync("/api/leistungen/objekte"));
            await Check("logout-before-restart",await c.PostAsJsonAsync("/api/auth/logout",new{}));await Stop();await Start();
            using var after=Client();await Csrf(after);await Check("restart-password",await after.PostAsJsonAsync("/api/auth/login",new LoginEingabe("admin",pass)));await Csrf(after);
            await Check("restart-second-factor",await after.PostAsJsonAsync("/api/auth/mfa-anmelden",new MfaPruefung(null,Totp.Code(key,step+1))));await Csrf(after);
            await Check("restart-data",await after.GetAsync("/api/leistungen/objekte"));
            await using(var db=Db(runtime)){await Target(db);Assert.Equal(1,await db.Benutzer.CountAsync(x=>x.Login=="admin"));Assert.NotNull((await db.Benutzer.SingleAsync(x=>x.Id==id)).MfaGeheimnis);Assert.True(await db.Sicherheitsereignisse.AnyAsync(x=>x.Ressource==id&&x.Aktion==LokalerAdminErstzugang.Marker));}
            await Write("admin-neustart.json",new{database,adminExists=true,personalMfaInAutomatedIsolatedScenario=true,restart=true,credentialInArtifacts=false,realBrowser=false});
        }
        async Task LeistungenHttp(){
            var checks=new List<object>();
            async Task<JsonElement> Check(string name,HttpResponseMessage r,int status=200){using(r){checks.Add(new{name,status=(int)r.StatusCode,expected=status});await Write("leistungen-http.json",checks);Assert.Equal(status,(int)r.StatusCode);return r.Content.Headers.ContentType?.MediaType=="application/json"?JsonSerializer.Deserialize<JsonElement>(await r.Content.ReadAsStringAsync()):default;}}
            static string N()=>Guid.NewGuid().ToString("N");static string Id(JsonElement e)=>e.GetProperty("id").GetString()!;
            using var admin=Client();using var team=Client();using var fremd=Client();using var buero=Client();using var anonym=Client();
            await Check("anonymous",await anonym.GetAsync("/api/leistungen/objekte"),401);await Login(admin,"nora.lind");await Login(team,"anna.berg");await Login(fremd,"leon.kruse");
            string bueroId;await using(var db=Db(runtime)){await Target(db);bueroId=(await Anmeldung.Anlegen(db,hasher,new("fiktives-buero","Lena Winter",FixturePasswort,"Mitarbeiter"))).Id;await Target(db);db.Freigaben.AddRange(new Ressourcenfreigabe{BenutzerId=bueroId,KontaktId="elbequartier"},new Ressourcenfreigabe{BenutzerId=bueroId,KontaktId="nordlicht"});await db.SaveChangesAsync();}
            await Login(buero,"fiktives-buero");
            var input=new Betriebsplattform.Web.Leistungen.Objekteingabe(N(),"elbequartier","Kontorhaus West","Hafenweg 7,12345 Elbstadt",bueroId);
            var o=Id(await Check("object-create",await admin.PostAsJsonAsync("/api/leistungen/objekte",input)));var o2=Id(await Check("second-object",await admin.PostAsJsonAsync("/api/leistungen/objekte",input with{AnlageId=N(),Name="Standort Ost"})));await Check("third-object-second-customer",await admin.PostAsJsonAsync("/api/leistungen/objekte",input with{AnlageId=N(),KundeId="nordlicht",Name="Kontorhaus Südhafen"}));
            var objects=await Check("object-list-page",await buero.GetAsync("/api/leistungen/objekte?seite=0"));Assert.Equal(3,objects.GetProperty("anzahl").GetInt32());
            await Check("object-reload",await buero.GetAsync("/api/leistungen/objekte/"+o));await Check("foreign-object",await fremd.GetAsync("/api/leistungen/objekte/"+o),404);
            var objedit=new Betriebsplattform.Web.Leistungen.Objektaenderung(N(),1,"Hafenbüro","Hafenweg 9,12345 Elbstadt",false,"Zugang vorher abstimmen");var op="/api/leistungen/objekte/"+o2;
            await Check("object-edit",await buero.PostAsJsonAsync(op,objedit));await Check("object-edit-replay",await buero.PostAsJsonAsync(op,objedit));
            await Check("object-edit-stale",await buero.PostAsJsonAsync(op,objedit with{AnlageId=N()}),409);
            await Check("foreign-object-edit",await fremd.PostAsJsonAsync(op,objedit),404);
            var edits=await Task.WhenAll(buero.PostAsJsonAsync(op,objedit with{AnlageId=N(),Version=2}),admin.PostAsJsonAsync(op,objedit with{AnlageId=N(),Version=2}));Assert.Equal(new[]{200,409},edits.Select(r=>(int)r.StatusCode).Order().ToArray());foreach(var rr in edits)rr.Dispose();checks.Add(new{name="parallel-object-edit",statuses=new[]{200,409}});
            var edited=await Check("object-edited-reload",await buero.GetAsync(op));Assert.Equal(3,edited.GetProperty("objekt").GetProperty("version").GetInt32());Assert.False(edited.GetProperty("objekt").GetProperty("aktiv").GetBoolean());
            var filtered=await Check("object-filter",await buero.GetAsync("/api/leistungen/objekte?status=inaktiv&suche=Hafenbüro"));Assert.Equal(1,filtered.GetProperty("anzahl").GetInt32());
            await Check("object-reactivate",await buero.PostAsJsonAsync(op,objedit with{AnlageId=N(),Version=3,Aktiv=true}));
            var catalog=new Betriebsplattform.Web.Leistungen.Katalogeingabe(N(),0,"Boden feucht wischen","Schonend reinigen","Unterhaltsreinigung",10);
            var cid=Id(await Check("catalog-create",await admin.PostAsJsonAsync("/api/leistungen/katalog",catalog)));Assert.Equal(cid,Id(await Check("catalog-replay",await admin.PostAsJsonAsync("/api/leistungen/katalog",catalog))));
            await Check("office-catalog-no-write",await buero.PostAsJsonAsync("/api/leistungen/katalog/"+cid,catalog),403);await Check("staff-catalog-no-read",await team.GetAsync("/api/leistungen/katalog"),403);
            var catalogReload=await Check("catalog-reload",await buero.GetAsync("/api/leistungen/katalog?aktiv=true"));Assert.Equal(1,catalogReload.GetProperty("anzahl").GetInt32());
            var plan=new Betriebsplattform.Web.Leistungen.Planeingabe(N(),1,"Unterhaltsreinigung",new(2026,10,1),[new("Boden feucht wischen","Flur","Bei jedem Einsatz",true,"Umlaute ÄÖÜß"),new("Sanitär reinigen","WC","Bei Bedarf",true,"")]);
            var p=Id(await Check("plan-v1",await buero.PostAsJsonAsync("/api/leistungen/objekte/"+o+"/plaene",plan)));
            await Check("catalog-edit",await admin.PostAsJsonAsync("/api/leistungen/katalog/"+cid,catalog with{AnlageId=N(),Version=1,Name="Boden und Sockelleisten reinigen",Aktiv=false}));
            await Check("catalog-stale",await admin.PostAsJsonAsync("/api/leistungen/katalog/"+cid,catalog with{AnlageId=N(),Version=1}),409);
            var activeCatalog=await Check("catalog-inactive-reload",await buero.GetAsync("/api/leistungen/katalog?aktiv=true"));Assert.Equal(0,activeCatalog.GetProperty("anzahl").GetInt32());
            var run=new Betriebsplattform.Web.Leistungen.Einsatzeingabe(N(),p,new(2026,10,25),teamId);var e=Id(await Check("run-create",await buero.PostAsJsonAsync("/api/leistungen/objekte/"+o+"/einsaetze",run)));Assert.Equal(e,Id(await Check("run-replay",await buero.PostAsJsonAsync("/api/leistungen/objekte/"+o+"/einsaetze",run))));
            await Check("unassigned-object",await team.GetAsync("/api/leistungen/objekte/"+o2),404);await Check("staff-no-plan-write",await team.PostAsJsonAsync("/api/leistungen/objekte/"+o+"/plaene",plan with{AnlageId=N(),Objektversion=2}),403);
            var ep="/api/leistungen/einsaetze/"+e;
            var ist=new Betriebsplattform.Web.Leistungen.Leistungsentscheidung(N(),1,"checkliste",[new(0,"Erledigt","Vollständig gereinigt"),new(1,"Teilweise erledigt","Nebenraum gesperrt")],"Telefonisch an Büro gemeldet");
            await Check("checklist",await team.PostAsJsonAsync(ep,ist));await Check("stale-checklist",await team.PostAsJsonAsync(ep,ist with{AnlageId=N()}),409);var reload=await Check("checklist-reload",await team.GetAsync(ep));Assert.Equal("Teilweise erledigt",reload.GetProperty("stand").GetProperty("ist")[1].GetProperty("status").GetString());
            await Check("report",await team.PostAsJsonAsync(ep,new Betriebsplattform.Web.Leistungen.Leistungsentscheidung(N(),2,"melden")));await Check("staff-no-release",await team.PostAsJsonAsync(ep,new Betriebsplattform.Web.Leistungen.Leistungsentscheidung(N(),3,"freigeben")),403);await Check("office-review",await buero.PostAsJsonAsync(ep,new Betriebsplattform.Web.Leistungen.Leistungsentscheidung(N(),3,"pruefen")));
            var races=await Task.WhenAll(buero.PostAsJsonAsync(ep,new Betriebsplattform.Web.Leistungen.Leistungsentscheidung(N(),4,"freigeben")),buero.PostAsJsonAsync(ep,new Betriebsplattform.Web.Leistungen.Leistungsentscheidung(N(),4,"freigeben")));
            Assert.Equal(new[]{200,409},races.Select(r=>(int)r.StatusCode).Order().ToArray());foreach(var rr in races)rr.Dispose();checks.Add(new{name="parallel-release",statuses=new[]{200,409}});
            var detail=await Check("release-reload",await buero.GetAsync(ep));var nid=detail.GetProperty("nachweis").GetProperty("id").GetString()!;var proofPath="/api/leistungen/nachweise/"+nid+"/druck";
            using var html=await team.GetAsync(proofPath);Assert.Equal(200,(int)html.StatusCode);var bytes=await html.Content.ReadAsByteArrayAsync();var text=System.Text.Encoding.UTF8.GetString(bytes);Assert.DoesNotContain("synthetisch",text,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("Entwicklungsnachweis",text);Assert.Contains("Boden feucht wischen",System.Net.WebUtility.HtmlDecode(text));var hash=Betriebsplattform.Web.Leistungen.Leistungsdienst.Hash(bytes);Assert.Equal(hash,detail.GetProperty("nachweis").GetProperty("hash").GetString());await File.WriteAllBytesAsync(Path.Combine(evidence,"synthetischer-leistungsnachweis.html"),bytes);await Write("nachweis-hash.json",new{nachweisId=nid,sha256=hash,bytes=bytes.Length,format="text/html; generated internal print fallback, no PDF"});
            await Check("foreign-proof",await fremd.GetAsync(proofPath),404);
            await Check("plan-v2",await buero.PostAsJsonAsync("/api/leistungen/objekte/"+o+"/plaene",plan with{AnlageId=N(),Objektversion=2,Name="Unterhaltsreinigung v2",GiltAb=new(2026,11,1),Positionen=[new("Andere Leistung","Raum2","Monatlich",true,"")]}));Assert.Equal(bytes,await team.GetByteArrayAsync(proofPath));
            var feedback=new Betriebsplattform.Web.Leistungen.Leistungsentscheidung(N(),5,"rueckmeldung",Bemerkung:"Nebenraum bitte nacharbeiten",Kundenstatus:"Beanstandet",Position:1,Nacharbeit:true);await Check("feedback",await buero.PostAsJsonAsync(ep,feedback));await Check("feedback-replay",await buero.PostAsJsonAsync(ep,feedback));var last=await Check("feedback-reload",await buero.GetAsync(ep));var task=last.GetProperty("stand").GetProperty("nacharbeitId").GetString()!;await Check("rework-task-reload",await buero.GetAsync("/api/aufgaben/"+task));
            buero.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");await Check("csrf-required",await buero.PostAsJsonAsync(ep,feedback),400);await Csrf(buero);
            await Check("withdraw-contact",await admin.PostAsJsonAsync("/api/admin/benutzer/"+teamId+"/freigaben",new Freigabeeingabe("Firma","elbequartier",false)));await Check("revoked-proof",await team.GetAsync(proofPath),404);
            await using(var db=Db(runtime)){await Target(db);Assert.Equal(3,await db.Leistungsobjekte.CountAsync());Assert.Equal(2,await db.Leistungsplaene.CountAsync());Assert.Single(await db.Leistungsnachweise.ToArrayAsync());Assert.Single(await db.Aufgaben.ToArrayAsync());await Write("leistungen-datenbank.json",new{database,objekte=await db.Leistungsobjekte.CountAsync(),plaene=await db.Leistungsplaene.CountAsync(),nachweise=await db.Leistungsnachweise.Select(n=>new{n.Id,n.Hash,n.EinsatzId}).ToArrayAsync(),einsaetze=await db.Leistungseinsaetze.AsNoTracking().ToArrayAsync(),aufgaben=await db.Aufgaben.AsNoTracking().ToArrayAsync(),audit=await db.Leistungsaktionen.AsNoTracking().ToArrayAsync()});}
            var berlin=TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,"Europe/Berlin").DateTime;
            var appointment=new Betriebsplattform.Web.Aufgaben.Termineingabe("Objektbesprechung",berlin.AddMinutes(-5).ToString("yyyy-MM-dd'T'HH:mm"),berlin.AddMinutes(55).ToString("yyyy-MM-dd'T'HH:mm"),false,"Zugang und Reinigungsplan abstimmen","Kontorhaus",AnlageId:N(),Strasse:"Hafenweg",Hausnummer:"7",Plz:"12345",Stadt:"Elbstadt",Land:"DE",ErinnerungMinuten:0);
            var created=await Check("appointment-with-address-reminder",await admin.PostAsJsonAsync("/api/termine/",appointment));var tid=created.GetProperty("id").GetString()!;
            var due=await Check("own-reminder-due",await admin.GetAsync("/api/termine/erinnerungen"));Assert.Equal(1,due.GetProperty("anzahl").GetInt32());
            var otherDue=await Check("foreign-reminder-list",await fremd.GetAsync("/api/termine/erinnerungen"));Assert.Equal(0,otherDue.GetProperty("anzahl").GetInt32());await Check("foreign-reminder-ack",await fremd.PostAsJsonAsync("/api/termine/"+tid+"/quittieren",new{kennung=created.GetProperty("erinnerungskennung").GetString()}),404);
            await Write("termin-erinnerung.json",new{id=tid,kennung=created.GetProperty("erinnerungskennung").GetString(),beginn=appointment.Beginn});
            await Write("leistungen-http.json",checks);
        }
        async Task FormularHttp(){
            var checks=new List<object>();
            async Task<JsonElement?> Check(string name,HttpResponseMessage response,int status){using(response){checks.Add(new{name,status=(int)response.StatusCode,expected=status});await Write("formular-http.json",checks);Assert.Equal(status,(int)response.StatusCode);return response.Content.Headers.ContentType?.MediaType=="application/json"?JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync()):null;}}
            using var admin=Client();using var other=Client();using var anonymous=Client();
            await Check("anonymous-contact",await anonymous.GetAsync("/api/kontakte"),401);await Login(admin,"nora.lind");await Login(other,"leon.kruse");
            var contact=new Kontakteingabe("Fiktive Moosquarz Büro GmbH","Firma",null,"","","",[new("Telefon","+49 (0)30 123-456",true),new("E-Mail","intern@moosquarz.invalid",true)],Strasse:"Fiktiver Prüfweg",Hausnummer:"17a",Plz:"12345",Ort:"Musterort",Land:"DE");
            await Check("company-missing-address",await admin.PostAsJsonAsync("/api/kontakte",contact with{Strasse=""}),400);
            await Check("company-invalid-phone",await admin.PostAsJsonAsync("/api/kontakte",contact with{Werte=[new("Telefon","abc123",true),new("E-Mail","intern@moosquarz.invalid",true)]}),400);
            await Check("company-invalid-email",await admin.PostAsJsonAsync("/api/kontakte",contact with{Werte=[new("Telefon","+49 30 123456",true),new("E-Mail","ungültig",true)]}),400);
            await Check("company-invalid-postcode",await admin.PostAsJsonAsync("/api/kontakte",contact with{Plz="ABC"}),400);
            var created=(await Check("company-create",await admin.PostAsJsonAsync("/api/kontakte",contact),200))!.Value;var id=created.GetProperty("id").GetString()!;
            await Check("company-reload",await admin.GetAsync("/api/kontakte/"+id),200);
            await Check("company-duplicate",await admin.PostAsJsonAsync("/api/kontakte",contact),409);
            await Check("company-update",await admin.PostAsJsonAsync("/api/kontakte/"+id,contact with{Version=1,Ort="Neuer Musterort"}),200);
            await Check("company-stale",await admin.PostAsJsonAsync("/api/kontakte/"+id,contact with{Version=1}),409);
            var reloaded=(await Check("company-update-reload",await admin.GetAsync("/api/kontakte/"+id),200))!.Value;Assert.Equal("Neuer Musterort",reloaded.GetProperty("ort").GetString());
            await Check("foreign-contact",await other.GetAsync("/api/kontakte/"+id),404);
            var person=new Kontakteingabe(null,"Person",id,"","","",[new("Telefon","+1 (202) 555-0123",true)],Vorname:"Jörg",Nachname:"O'Neill");
            await Check("person-without-own-address",await admin.PostAsJsonAsync("/api/kontakte",person),200);
            var entry=new Termineingabe("Fiktiver Bürotermin","2026-10-23T09:00","2026-10-23T10:00",false,"Prüfnotiz","Fiktives Büro",AnlageId:Guid.NewGuid().ToString("N"));
            var t=(await Check("appointment-create",await admin.PostAsJsonAsync("/api/termine/",entry),200))!.Value;var tid=t.GetProperty("id").GetString()!;
            var replay=(await Check("appointment-idempotent",await admin.PostAsJsonAsync("/api/termine/",entry),200))!.Value;Assert.Equal(tid,replay.GetProperty("id").GetString());
            await Check("appointment-reload",await admin.GetAsync("/api/termine/"+tid),200);await Check("foreign-appointment",await other.GetAsync("/api/termine/"+tid),404);
            await Check("appointment-invalid-times",await admin.PostAsJsonAsync("/api/termine/"+tid,entry with{Version=1,Ende="2026-10-23T08:00"}),400);
            await Check("appointment-update",await admin.PostAsJsonAsync("/api/termine/"+tid,entry with{Version=1,Titel="Bearbeiteter fiktiver Bürotermin"}),200);
            var tr=(await Check("appointment-update-reload",await admin.GetAsync("/api/termine/"+tid),200))!.Value;Assert.Equal("Bearbeiteter fiktiver Bürotermin",tr.GetProperty("titel").GetString());
            await Check("appointment-stale",await admin.PostAsJsonAsync("/api/termine/"+tid,entry with{Version=1}),409);
            await Check("appointment-delete",await admin.PostAsJsonAsync("/api/termine/"+tid+"/loeschen",new{version=2}),200);await Check("appointment-delete-reload",await admin.GetAsync("/api/termine/"+tid),404);
            var task=new Aufgabeneingabe("Fiktive Kontaktprüfung","Nur synthetische Integrationsdaten",adminId,"Normal","Offen",new(2026,10,25),null,BezugArt:"Kontakt",BezugId:id);
            var a=(await Check("task-save",await admin.PostAsJsonAsync("/api/aufgaben/",task),200))!.Value;await Check("task-reload",await admin.GetAsync("/api/aufgaben/"+a.GetProperty("id").GetString()),200);
        }
        async Task Security(){
            var results=new List<object>();
            async Task Expect(string name,HttpResponseMessage r,HttpStatusCode expected){using(r){results.Add(new{name,status=(int)r.StatusCode,expected=(int)expected});await Write("negative-http.json",results);Assert.Equal(expected,r.StatusCode);}}
            using var admin=Client();using var team=Client();using var other=Client();using var anonymous=Client();
            await Expect("anonymous-case",await anonymous.GetAsync("/api/akten/kontorhaus-suedhafen"),HttpStatusCode.Unauthorized);
            await Expect("missing-csrf",await anonymous.PostAsJsonAsync("/api/auth/login",new LoginEingabe("nora.lind",FixturePasswort)),HttpStatusCode.BadRequest);
            await Login(admin,"nora.lind");await Login(team,"anna.berg");await Login(other,"leon.kruse");
            await Expect("employee-admin-api",await team.GetAsync("/api/admin/benutzer"),HttpStatusCode.Forbidden);
            await Expect("employee-allowed-case",await team.GetAsync("/api/akten/kontorhaus-suedhafen"),HttpStatusCode.OK);
            await Expect("foreign-case",await other.GetAsync("/api/akten/kontorhaus-suedhafen"),HttpStatusCode.NotFound);
            string doc,eventId,taskId;int version;
            await using(var db=Db(runtime)){await Target(db);var w=await db.Eingangsworkflows.SingleAsync();eventId=w.Id;version=w.Version;taskId=w.AufgabeId!;doc=(await db.Dokumentaktivitaeten.SingleAsync(e=>e.Id==eventId)).DokumentId;Assert.Single(await db.Aufgaben.ToArrayAsync());}
            await Expect("foreign-download",await other.GetAsync("/api/dokumente/"+doc+"/datei"),HttpStatusCode.NotFound);
            await Expect("foreign-receipt-history",await other.GetAsync("/api/posteingang/"+eventId),HttpStatusCode.NotFound);
            var data=new Eingangsangaben("Wartungsrechnung Lüftungsanlage – Oktober 2026","Eingangsrechnung",new(2026,10,20),"RE-2026-1042",1249.50m,new(2026,10,30),"Kontorhaus Südhafen");
            var input=new Eingangspruefung(version,"Zuordnen",data,"nordlicht","kontorhaus-suedhafen",adminId,"Original und Auftrag geprüft","",true);
            await Expect("inaccessible-sender",await team.PostAsJsonAsync("/api/posteingang/"+eventId+"/pruefen",input with{AbsenderId="anderer-bereich"}),HttpStatusCode.Forbidden);
            await Expect("inaccessible-case",await team.PostAsJsonAsync("/api/posteingang/"+eventId+"/pruefen",input with{AkteId="geschuetzte-akte"}),HttpStatusCode.Forbidden);
            await Expect("invalid-title",await admin.PostAsJsonAsync("/api/posteingang/"+eventId+"/pruefen",input with{Angaben=data with{Betreff=""}}),HttpStatusCode.BadRequest);
            await Expect("stale-receipt",await admin.PostAsJsonAsync("/api/posteingang/"+eventId+"/pruefen",input with{Version=version-1}),HttpStatusCode.Conflict);
            await Expect("missing-task-data",await admin.PostAsJsonAsync("/api/posteingang/"+eventId+"/aufgabe",new Eingangsaufgabe(version,true,true,null!)),HttpStatusCode.BadRequest);
            var repeated=new Eingangsaufgabe(version,true,true,new("Nicht doppelt anlegen","",teamId,"Normal","Offen",new(2026,10,30),null));
            await Expect("repeated-follow-up",await admin.PostAsJsonAsync("/api/posteingang/"+eventId+"/aufgabe",repeated),HttpStatusCode.OK);
            using(var upload=new HttpRequestMessage(HttpMethod.Post,"/api/dokumenteingang")){upload.Content=new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("Nicht freigegebene Fremddatei"));upload.Content.Headers.ContentType=new("application/octet-stream");upload.Headers.Add("X-Dateiname","fremddatei.txt");upload.Headers.Add("X-Dateityp","text/plain");await Expect("malware-hook-still-closed",await admin.SendAsync(upload),HttpStatusCode.ServiceUnavailable);}
            admin.DefaultRequestHeaders.Remove("Origin");admin.DefaultRequestHeaders.Add("Origin","https://fremd.example");await Expect("foreign-origin",await admin.PostAsJsonAsync("/api/posteingang/"+eventId+"/aufgabe",repeated),HttpStatusCode.Forbidden);admin.DefaultRequestHeaders.Remove("Origin");admin.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));
            Aufgabeneingabe TaskInput(Aufgabe a,string assignee)=>new(a.Betreff,a.Beschreibung,assignee,a.Prioritaet,"In Bearbeitung",a.FaelligAm,a.FaelligUm,a.Version,false,null,"Dokument",doc);
            Aufgabe task;await using(var db=Db(runtime)){await Target(db);task=await db.Aufgaben.AsNoTracking().SingleAsync();}
            await Expect("foreign-task-write",await other.PostAsJsonAsync("/api/aufgaben/"+taskId,TaskInput(task,teamId)),HttpStatusCode.NotFound);
            var raced=await Task.WhenAll(admin.PostAsJsonAsync("/api/aufgaben/"+taskId,TaskInput(task,teamId)),admin.PostAsJsonAsync("/api/aufgaben/"+taskId,TaskInput(task,teamId)));
            Assert.Equal(new[]{200,409},raced.Select(r=>(int)r.StatusCode).Order().ToArray());foreach(var r in raced)r.Dispose();results.Add(new{name="parallel-task-edit",statuses=new[]{200,409}});
            await using(var db=Db(runtime)){await Target(db);task=await db.Aufgaben.AsNoTracking().SingleAsync();}
            await Expect("change-assignee",await admin.PostAsJsonAsync("/api/aufgaben/"+taskId,TaskInput(task,adminId)),HttpStatusCode.OK);
            await Expect("old-assignee-task-history",await team.GetAsync("/api/aufgaben/"+taskId),HttpStatusCode.NotFound);
            await using(var db=Db(runtime)){await Target(db);task=await db.Aufgaben.AsNoTracking().SingleAsync();}
            await Expect("restore-assignee",await admin.PostAsJsonAsync("/api/aufgaben/"+taskId,TaskInput(task,teamId)),HttpStatusCode.OK);
            await Expect("revoke-case-grant",await admin.PostAsJsonAsync("/api/admin/benutzer/"+teamId+"/freigaben",new Freigabeeingabe("Firma","elbequartier",false)),HttpStatusCode.OK);
            await Expect("revoked-case",await team.GetAsync("/api/akten/kontorhaus-suedhafen"),HttpStatusCode.NotFound);
            await Expect("revoked-download",await team.GetAsync("/api/dokumente/"+doc+"/datei"),HttpStatusCode.NotFound);
            await Expect("restore-case-grant",await admin.PostAsJsonAsync("/api/admin/benutzer/"+teamId+"/freigaben",new Freigabeeingabe("Firma","elbequartier",true)),HttpStatusCode.OK);
            await Expect("logout",await team.PostAsJsonAsync("/api/auth/logout",new{}),HttpStatusCode.OK);
            await Expect("revoked-session",await team.GetAsync("/api/akten/kontorhaus-suedhafen"),HttpStatusCode.Unauthorized);
            await using(var db=Db(runtime)){await Target(db);Assert.Single(await db.Aufgaben.ToArrayAsync());Assert.Single(await db.Dokumente.ToArrayAsync());Assert.Single(await db.Dokumentaktivitaeten.Where(x=>x.Kanal!=null).ToArrayAsync());}
            await Write("negative-http.json",results);
        }
        try{
            await Start();await Write(resume?"ready-resume.json":"ready.json",new{database,url=url.ToString(),generation,adminId,teamId,otherId,users=new[]{"nora.lind","anna.berg","leon.kruse"},certificate=cert.Thumbprint,storage,started=DateTimeOffset.UtcNow,mode="Anmeldung/PostgreSql; no preview adapter"});
            if(Environment.GetEnvironmentVariable("FZ_ADMIN_HTTP")=="1"){await AdminHttp();return;}
            if(Environment.GetEnvironmentVariable("FZ_LEISTUNGEN_WIEDERANLAUF")=="1"){
                Assert.True(resume);using var c=Client();await Login(c,"nora.lind");var objs=await c.GetFromJsonAsync<JsonElement>("/api/leistungen/objekte");Assert.Equal(3,objs.GetProperty("anzahl").GetInt32());Assert.Contains(objs.GetProperty("eintraege").EnumerateArray(),o=>o.GetProperty("name").GetString()=="Hafenbüro"&&o.GetProperty("aktiv").GetBoolean());
                var kat=await c.GetFromJsonAsync<JsonElement>("/api/leistungen/katalog");Assert.Equal(1,kat.GetProperty("anzahl").GetInt32());Assert.False(kat.GetProperty("eintraege")[0].GetProperty("aktiv").GetBoolean());
                await using var db=Db(runtime);await Target(db);var n=await db.Leistungsnachweise.AsNoTracking().SingleAsync();var b=await c.GetByteArrayAsync("/api/leistungen/nachweise/"+n.Id+"/druck");Assert.Equal(n.Hash,Betriebsplattform.Web.Leistungen.Leistungsdienst.Hash(b));Assert.Single(await db.Aufgaben.ToArrayAsync());
                await Write("wiederanlauf-persistenz.json",new{database,objekte=3,katalog=1,nachweis=n.Id,sha256=n.Hash,aufgaben=1,modus="HTTPS login and reload after host restart, no browser automation"});return;
            }
            if(Environment.GetEnvironmentVariable("FZ_LEISTUNGEN_HTTP")=="1"){
                await LeistungenHttp();
                if(Environment.GetEnvironmentVariable("FZ_NORMAL_START")=="1"){
                    await Stop();using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(90));
                    var normal=Betriebsplattform.Web.Betrieb.IsolierteArbeitsumgebung.Run(Path.GetRelativePath(workspace,Path.Combine(evidence,"ready.json")),stop.Token);
                    try{
                        using var c=Client();bool bereit=false;for(int versuch=0;versuch<100&&!bereit;versuch++){
                            if(normal.IsCompleted)await normal;
                            try{using var probe=await c.GetAsync("/api/auth/session");bereit=probe.IsSuccessStatusCode;}catch(HttpRequestException){}if(!bereit)await Task.Delay(250);
                        }
                        Assert.True(bereit);await Login(c,"nora.lind");var obj=await c.GetFromJsonAsync<JsonElement>("/api/leistungen/objekte");Assert.Equal(3,obj.GetProperty("anzahl").GetInt32());
                        var kat=await c.GetFromJsonAsync<JsonElement>("/api/leistungen/katalog");Assert.Equal(1,kat.GetProperty("anzahl").GetInt32());
                        var due=await c.GetFromJsonAsync<JsonElement>("/api/termine/erinnerungen");Assert.Equal(1,due.GetProperty("anzahl").GetInt32());var rem=due.GetProperty("eintraege")[0];var tid=rem.GetProperty("id").GetString()!;var key=rem.GetProperty("erinnerungskennung").GetString();
                        var ack=await Task.WhenAll(c.PostAsJsonAsync("/api/termine/"+tid+"/quittieren",new{kennung=key}),c.PostAsJsonAsync("/api/termine/"+tid+"/quittieren",new{kennung=key}));Assert.All(ack,r=>Assert.Equal(200,(int)r.StatusCode));foreach(var rr in ack)rr.Dispose();var cleared=await c.GetFromJsonAsync<JsonElement>("/api/termine/erinnerungen");Assert.Equal(0,cleared.GetProperty("anzahl").GetInt32());
                        var term=await c.GetFromJsonAsync<JsonElement>("/api/termine/"+tid);Assert.Equal("Hafenweg",term.GetProperty("strasse").GetString());
                        await Write("erinnerung-neustart-https.json",new{database,id=tid,adresse=true,vorQuittierung=1,nachQuittierung=0,paralleleQuittierung=new[]{200,200}});
                        await Write("normalstart-https.json",new{database,modus="normal Program start implementation, no test-host deadline",anmeldung=true,objekte=3,katalog=1,url="https://127.0.0.1:5181/"});
                    }finally{stop.Cancel();try{await normal;}catch(OperationCanceledException)when(stop.IsCancellationRequested){}}
                }
                return;
            }
            if(Environment.GetEnvironmentVariable("FZ_FORMULAR_HTTP")=="1"){await FormularHttp();await Capture("formular-datenbank.json");return;}
            using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(45));var last="";
            while(true){
                await Task.Delay(300,deadline.Token);var control=Path.Combine(evidence,"control.json");if(!File.Exists(control))continue;
                JsonElement command;try{command=JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(control,deadline.Token));}catch(JsonException){continue;}
                var id=command.GetProperty("id").GetString()!;if(id==last)continue;last=id;var action=command.GetProperty("action").GetString();
                if(action=="capture")await Capture("snapshot-"+id+".json");
                else if(action=="load-package"){
                    var source=Path.GetFullPath(Environment.GetEnvironmentVariable("FZ_DOCUMENT_PACKAGE")??throw new InvalidOperationException("Explicit local fictional package required."));Assert.True(File.Exists(Path.Combine(source,"README_START_HIER.md")));
                    using var manifest=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(source,"03_erwartungswerte","lokale_dateimanifest_sha256.json")));
                    var selected=manifest.RootElement.EnumerateArray().Where((_,i)=>i<4||i==7).ToArray();var originals=new List<(string Name,byte[] Bytes)>();
                    foreach(var entry in selected){var file=entry.GetProperty("datei").GetString()!;Assert.Equal(file,Path.GetFileName(file));var bytes=await File.ReadAllBytesAsync(Path.Combine(source,"01_lokal_erstellte_dokumente",file));Assert.Equal(entry.GetProperty("sha256").GetString()!.ToUpperInvariant(),Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));originals.Add((file,bytes));}
                    await using var db=Db(runtime);await Target(db);Assert.Empty(await db.Dokumente.ToArrayAsync());
                    db.Kontakte.Add(new(){Id="elbefunk",Name="ElbeFunk Telekommunikation GmbH",Suchname="elbefunk telekommunikation"});db.Kategorien.Add(new(){Id="telekommunikation",Name="Telefonie und Internet",Suchname="telefonie und internet"});db.Akten.Add(new(){Id="kontorhaus-telekommunikation",Titel="Kontorhaus Südhafen – Telefonie und Internet",Suchtext="kontorhaus suedhafen telefonie internet",KategorieId="telekommunikation",FirmaId="elbequartier",Status="In Bearbeitung"});db.Freigaben.Add(new(){BenutzerId=teamId,KontaktId="elbefunk"});await db.SaveChangesAsync();
                    var intake=new FirmenDaten(db,new(true,null,null),new PaketTestfreigabe(originals.Select(o=>o.Bytes).ToArray()));var storeFiles=app!.Services.GetRequiredService<IDokumentdateien>();var overview=new List<object>();
                    var titles=new[]{"ElbeFunk – Monatsrechnung August 2026","ElbeFunk – Monatsrechnung September 2026","ElbeFunk – Monatsrechnung Oktober 2026","ElbeFunk – Monatsrechnung November 2026, neues Layout","Nordlicht – gescannte Wartungsrechnung Oktober 2026"};
                    for(var i=0;i<originals.Count;i++){await Target(db);var o=originals[i];var d=await intake.DokumentHinzufuegen(o.Bytes,o.Name,"application/pdf",null,storeFiles);d.Titel=titles[i];await db.SaveChangesAsync();var e=await db.Dokumentaktivitaeten.SingleAsync(e=>e.DokumentId==d.Id&&e.Kanal!=null);overview.Add(new{dokumentId=d.Id,eingangId=e.Id,d.Titel,d.Dateiname,d.Hash});}
                    await Write("package-intake.json",overview);await Capture("package-loaded.json");
                }
                else if(action=="restart"){await Capture("before-restart.json");await Stop();await Start();await Capture("after-restart.json");}
                else if(action=="security"){await Security();await Capture("after-security.json");}
                else if(action=="analyse-security"){
                    using var admin=Client();using var other=Client();using var anonymous=Client();await Login(admin,"nora.lind");await Login(other,"leon.kruse");
                    string docId;Eingangsworkflow workflow;int docVersion;await using(var db=Db(runtime)){await Target(db);var selection=command.TryGetProperty("dokumentId",out var chosen)?chosen.GetString():null;var doc=await db.Dokumente.AsNoTracking().SingleAsync(d=>selection==null||d.Id==selection);docId=doc.Id;workflow=await db.Eingangsworkflows.AsNoTracking().SingleAsync(w=>db.Dokumentaktivitaeten.Any(e=>e.Id==w.Id&&e.DokumentId==doc.Id));}using(var settle=await admin.PostAsJsonAsync("/api/posteingang/"+docId+"/analyse",new{}))Assert.True(settle.IsSuccessStatusCode);await using(var db=Db(runtime)){await Target(db);docVersion=(await db.Dokumente.AsNoTracking().SingleAsync(d=>d.Id==docId)).Version;}
                    var checks=new List<object>();async Task Check(string name,HttpResponseMessage response,int expected){using(response){Assert.Equal(expected,(int)response.StatusCode);checks.Add(new{name,status=(int)response.StatusCode});}}
                    await Check("anonymous-analysis",await anonymous.GetAsync("/api/posteingang/"+docId+"/analyse"),401);
                    await Check("foreign-analysis",await other.GetAsync("/api/posteingang/"+docId+"/analyse"),404);
                    await Check("foreign-page",await other.GetAsync("/api/posteingang/"+docId+"/seite/1"),404);
                    await Check("foreign-analysis-write",await other.PostAsJsonAsync("/api/posteingang/"+docId+"/analyse",new{}),404);
                    await Check("invalid-page",await admin.GetAsync("/api/posteingang/"+docId+"/seite/11"),404);
                    await Check("allowed-page",await admin.GetAsync("/api/posteingang/"+docId+"/seite/1"),200);
                    await Check("cached-analysis-one",await admin.PostAsJsonAsync("/api/posteingang/"+docId+"/analyse",new{}),200);
                    await Check("cached-analysis-two",await admin.PostAsJsonAsync("/api/posteingang/"+docId+"/analyse",new{}),200);
                    admin.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");await Check("analysis-without-csrf",await admin.PostAsJsonAsync("/api/posteingang/"+docId+"/analyse",new{}),400);await Csrf(admin);
                    var data=JsonSerializer.Deserialize<Eingangsangaben>(workflow.AngabenJson)!;var forged=data with{Felder=data.Felder!.Select(f=>f.Name=="rechnungsnummer"?f with{AbschnittId="erfundene-fundstelle"}:f).ToArray()};
                    await Check("forged-source",await admin.PostAsJsonAsync("/api/posteingang/"+docId+"/pruefen",new Eingangspruefung(workflow.Version,"Zuordnen",forged,workflow.AbsenderId,workflow.AkteId,workflow.ZustaendigId,"Negative Quellenprüfung","",true)),400);
                    await using(var db=Db(runtime)){await Target(db);Assert.Equal(docVersion,(await db.Dokumente.SingleAsync(d=>d.Id==docId)).Version);Assert.Equal(workflow.AngabenJson,(await db.Eingangsworkflows.SingleAsync(w=>w.Id==workflow.Id)).AngabenJson);Assert.Single(await db.Aufgaben.ToArrayAsync());}
                    await Check("foreign-scanner-inventory",await other.GetAsync("/api/posteingang/scanner"),403);
                    await Check("foreign-scan-jobs",await other.GetAsync("/api/posteingang/scanordner"),403);
                    await Check("foreign-pattern-write",await other.PostAsJsonAsync("/api/posteingang/"+docId+"/muster",new Musterentwurf("aussteller")),403);
                    await using(var db=Db(runtime)){await Target(db);var pattern=await db.Dokumentmuster.AsNoTracking().FirstOrDefaultAsync();if(pattern is not null)await Check("forbidden-automatic-payment",await admin.PostAsJsonAsync("/api/posteingang/muster/"+pattern.Id,new Musterentscheidung(pattern.Version,"Aktiv",workflow.Id,"Zahlung")),400);}
                    using(var upload=new HttpRequestMessage(HttpMethod.Post,"/api/dokumenteingang")){upload.Content=new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("Nicht freigegebene Fremddatei"));upload.Content.Headers.ContentType=new("application/octet-stream");upload.Headers.Add("X-Dateiname","fremddatei.txt");upload.Headers.Add("X-Dateityp","text/plain");await Check("product-upload-still-closed",await admin.SendAsync(upload),503);}
                    await Write("analyse-http.json",checks);await Capture("after-analyse-security.json");
                }
                else if(action=="finish"){await Capture("final-database.json");await Write("ack.json",new{id,action,generation,status="BESTANDEN"});break;}
                else throw new InvalidOperationException("Unknown supervised test command.");
                await Write("ack.json",new{id,action,generation,status="BESTANDEN"});
            }
        }finally{await Stop();if(File.Exists(certPath))File.Delete(certPath);}
    }
}
