using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Betrieb;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Persistenz.Migrationen;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class ErstzugangPostgreSqlTests(ITestOutputHelper output)
{
    private sealed class ErstzugangLiveFactAttribute:FactAttribute
    {
        public ErstzugangLiveFactAttribute(){if(Environment.GetEnvironmentVariable("FZ_ERSTZUGANG_POSTGRES")!="1"||!OperatingSystem.IsWindows())Skip="Explicit isolated PostgreSQL/Windows HTTPS test required.";}
    }
    private static string Secret()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private sealed class GleichzeitigerHasher(IPasswordHasher<Benutzerkonto> inner,Barrier barrier):IPasswordHasher<Benutzerkonto>
    {
        public string HashPassword(Benutzerkonto u,string pass){Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(15)));return inner.HashPassword(u,pass);}
        public PasswordVerificationResult VerifyHashedPassword(Benutzerkonto u,string hash,string pass)=>inner.VerifyHashedPassword(u,hash,pass);
    }
    [ErstzugangLiveFact] public async Task IsolierteMigrationHttpsErstzugangWiederherstellungNeustartUndKonkurrenz()
    {
        // Read the existing local credential through the normal provider; never display or persist it.
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).AddEnvironmentVariables().Build();
        var local=cfg.GetConnectionString("FirmenZentrale")??throw new InvalidOperationException("Local PostgreSQL administration configuration missing.");
        var cs=new NpgsqlConnectionStringBuilder(local);
        Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);
        cs.Database="postgres";cs.Pooling=false;cs.IncludeErrorDetail=false;cs.LogParameters=false;cs.Timeout=5;
        var run=Guid.NewGuid().ToString("N");var database="fz_erstzugang_test_"+run;var role="fz_app_ea_"+run[..16];
        await using(var management=new NpgsqlConnection(cs.ConnectionString)){
            await management.OpenAsync();Assert.Equal(18,management.PostgreSqlVersion.Major);
            await using(var who=new NpgsqlCommand("SELECT current_database()",management))Assert.Equal("postgres",await who.ExecuteScalarAsync());
            // A new random name: CREATE fails rather than reusing an existing database. No main DB is opened.
            await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(database)+" TEMPLATE template0",management);await create.ExecuteNonQueryAsync();
        }
        output.WriteLine("Isolated synthetic database: "+database+"; test runtime role: "+role);
        cs.Database=database;var administration=cs.ConnectionString;
        FirmenDbContext Db(string connection)=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(connection,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
        async Task Isolated(FirmenDbContext db){
            Assert.Matches(@"\Afz_erstzugang_test_[a-f0-9]{32}\z",database);
            var conn=(NpgsqlConnection)db.Database.GetDbConnection();Assert.Equal(database,conn.Database);if(conn.State!=System.Data.ConnectionState.Open)await conn.OpenAsync();
            await using var check=new NpgsqlCommand("SELECT current_database()",conn);Assert.Equal(database,await check.ExecuteScalarAsync());
        }
        async Task<string[]> Schema(FirmenDbContext db,string sql){
            var conn=(NpgsqlConnection)db.Database.GetDbConnection();if(conn.State!=System.Data.ConnectionState.Open)await conn.OpenAsync();
            await using var command=new NpgsqlCommand(sql,conn);await using var read=await command.ExecuteReaderAsync();var values=new List<string>();while(await read.ReadAsync())values.Add(read.GetString(0));return values.ToArray();
        }
        const string columns="SELECT table_name||'.'||column_name||':'||data_type||':'||is_nullable||':'||coalesce(column_default,'') FROM information_schema.columns WHERE table_schema='public' AND table_name NOT IN ('Aufgaben','Aufgabenverlauf','MfaRegeln','MfaNotfaelle') AND column_name NOT IN ('MfaNeueinrichtung','Eingerichtet','EinladungHash','EinladungBis','WiederherstellungHash','WiederherstellungBis','MfaGeheimnis','MfaVorbereitung','MfaRecoveryHashes','MfaLetztePeriode','MfaFehlversuche','MfaChallengeHash','MfaChallengeBis','MfaVorbereitungBis','MfaStepupHash','MfaStepupBis') ORDER BY table_name,ordinal_position";
        const string constraints="SELECT conname||':'||pg_get_constraintdef(oid) FROM pg_constraint WHERE connamespace='public'::regnamespace AND conrelid NOT IN (SELECT oid FROM pg_class WHERE relname IN ('Aufgaben','Aufgabenverlauf','MfaRegeln','MfaNotfaelle')) AND conname <> 'Benutzer_MfaNeueinrichtung_not_null' ORDER BY conname";
        const string indexes="SELECT indexdef FROM pg_indexes WHERE schemaname='public' AND tablename NOT IN ('Aufgaben','Aufgabenverlauf','MfaRegeln','MfaNotfaelle') ORDER BY tablename,indexname";
        var hasher=Anmeldung.Passwortdienst();var adminPassword=Secret();var legacyPassword=Secret();
        var admin=new Benutzerkonto{Login="admin",Anzeigename="Synthetische Administration",Rolle="Administrator"};admin.PasswortHash=hasher.HashPassword(admin,adminPassword);
        var legacy=new Benutzerkonto{Login="bestand",Anzeigename="Synthetischer Bestandsmitarbeiter"};legacy.PasswortHash=hasher.HashPassword(legacy,legacyPassword);
        await using(var db=Db(administration)){
            await Isolated(db);Assert.Empty(await Schema(db,"SELECT tablename FROM pg_tables WHERE schemaname='public'"));
            await db.GetService<IMigrator>().MigrateAsync("20261019114242_PilotSecurity");
            // Insert the exact pre-upgrade account shape; the new columns deliberately do not exist yet.
            foreach(var u in new[]{admin,legacy})await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Benutzer\" (\"Id\",\"Login\",\"Anzeigename\",\"PasswortHash\",\"Rolle\",\"Aktiv\",\"Sicherheitskennung\",\"Fehlversuche\",\"GesperrtBis\",\"ErstelltAm\",\"GeaendertAm\",\"Version\") VALUES ({u.Id},{u.Login},{u.Anzeigename},{u.PasswortHash},{u.Rolle},{u.Aktiv},{u.Sicherheitskennung},{u.Fehlversuche},NULL,{u.ErstelltAm},{u.GeaendertAm},{u.Version})");
            db.Kontakte.AddRange(new KontaktDatensatz{Id="erlaubt",Name="Synthetischer Bestandskontakt",Suchname="synthetischer bestandskontakt"},new KontaktDatensatz{Id="privat",Name="Synthetischer geschuetzter Kontakt",Suchname="synthetischer geschuetzter kontakt"});
            db.Freigaben.Add(new(){BenutzerId=legacy.Id,KontaktId="erlaubt"});await db.SaveChangesAsync();
            var oldColumns=await Schema(db,columns);var oldConstraints=await Schema(db,constraints);var oldIndexes=await Schema(db,indexes);
            Assert.Equal(3,new MitarbeiterErstzugang().UpOperations.Count);Assert.All(new MitarbeiterErstzugang().UpOperations,x=>Assert.IsType<AddColumnOperation>(x));
            Assert.Equal(2,new PasswortWiederherstellung().UpOperations.Count);Assert.All(new PasswortWiederherstellung().UpOperations,x=>Assert.True(x is AddColumnOperation{IsNullable:true}));
            Assert.Equal(10,new AuthenticatorMfa().UpOperations.Count);Assert.All(new AuthenticatorMfa().UpOperations,x=>Assert.IsType<AddColumnOperation>(x));
            Assert.False(db.Database.HasPendingModelChanges());
            await db.Database.CloseConnectionAsync();await Isolated(db);await db.GetService<IMigrator>().MigrateAsync("20261019200000_AuthenticatorMfa");
            Assert.Equal(oldColumns,await Schema(db,columns));var newConstraints=await Schema(db,constraints);
            Assert.All(oldConstraints,x=>Assert.Contains(x,newConstraints));
            // PostgreSQL 18 represents the intended new NOT NULL column as an additional constraint.
            Assert.Equal(new[]{"Benutzer_Eingerichtet_not_null:NOT NULL \"Eingerichtet\"","Benutzer_MfaFehlversuche_not_null:NOT NULL \"MfaFehlversuche\"","Benutzer_MfaLetztePeriode_not_null:NOT NULL \"MfaLetztePeriode\""},newConstraints.Except(oldConstraints));
            Assert.Equal(oldIndexes,await Schema(db,indexes));
            await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());
            foreach(var before in new[]{admin,legacy}){var stored=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==before.Id);Assert.True(stored.Eingerichtet&&stored.Aktiv);Assert.Equal(before.Rolle,stored.Rolle);Assert.True(before.PasswortHash==stored.PasswortHash);Assert.Null(stored.EinladungHash);Assert.Null(stored.EinladungBis);Assert.Null(stored.WiederherstellungHash);Assert.Null(stored.WiederherstellungBis);Assert.Equal(before.Sicherheitskennung,stored.Sicherheitskennung);}
            Assert.Equal(1,await db.Freigaben.CountAsync());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        output.WriteLine("Upgrade from PilotSecurity to MitarbeiterErstzugang passed; old columns, constraints, indexes, hashes, roles and resource grant preserved.");
        var runtimePassword=Secret();await Betriebsverwaltung.Laufzeitrolle(administration,role,runtimePassword);
        var runtime=new NpgsqlConnectionStringBuilder(administration){Username=role,Password=runtimePassword};
        var storage=Path.Combine(Path.GetTempPath(),"FirmenZentrale-ErstzugangPostgreSql",run);Directory.CreateDirectory(storage);
        using var rsa=RSA.Create(2048);var request=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));using var cert=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12),null,X509KeyStorageFlags.UserKeySet);
        async Task<(WebApplication App,Uri Url)> Start(){
            var app=ApiAnwendung.Erstellen(["--FirmenZentrale:Persistence:Provider=PostgreSql","--FirmenZentrale:Persistence:DocumentRoot="+storage,"--ConnectionStrings:FirmenZentrale="+runtime.ConnectionString,"--FirmenZentrale:Security:Enabled=true"],0,testZertifikat:cert);
            try{await app.StartAsync();}catch{await app.DisposeAsync();throw;}
            return(app,new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()));
        }
        HttpClient Client(Uri url){var c=new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=(_,s,_,_)=>s?.GetCertHashString()==cert.GetCertHashString()}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
        async Task Csrf(HttpClient c){var s=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",s.GetProperty("csrf").GetString());}
        async Task Login(HttpClient c,string login,string pass){await Csrf(c);Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/login",new{login,passwort=pass})).StatusCode);await Csrf(c);}
        var host=await Start();string employeeId;var employeePassword=Secret();
        try{
            using var ac=Client(host.Url);using var ec=Client(host.Url);await Login(ac,"admin",adminPassword);await Csrf(ec);
            var response=await ac.PostAsJsonAsync("/api/admin/benutzer",new{login="neuteam",anzeigename="Synthetischer Neuzugang",rolle="Mitarbeiter"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            var invitation=await response.Content.ReadFromJsonAsync<JsonElement>();employeeId=invitation.GetProperty("id").GetString()!;var code=invitation.GetProperty("code").GetString()!;
            await using(var db=Db(runtime.ConnectionString)){var pending=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==employeeId);Assert.False(pending.Eingerichtet);Assert.Equal("Mitarbeiter",pending.Rolle);Assert.True(pending.EinladungHash==Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code))));Assert.True(pending.EinladungBis>DateTimeOffset.UtcNow);}
            Assert.Equal(HttpStatusCode.Unauthorized,(await ec.PostAsJsonAsync("/api/auth/login",new{login="neuteam",passwort=employeePassword})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await ec.PostAsJsonAsync("/api/auth/einrichten",new{code,passwort=employeePassword})).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await ec.PostAsJsonAsync("/api/auth/einrichten",new{code,passwort=Secret()})).StatusCode);await Login(ec,"neuteam",employeePassword);
            Assert.Equal(HttpStatusCode.Forbidden,(await ec.PostAsJsonAsync($"/api/admin/benutzer/{employeeId}",new{aktiv=true,rolle="Administrator",version=3})).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await ec.PostAsJsonAsync("/api/admin/benutzer",new{login="unberechtigt",anzeigename="Unberechtigt",rolle="Administrator"})).StatusCode);
            Assert.DoesNotContain("Synthetischer Bestandskontakt",await ec.GetStringAsync("/api/kontakte?seite=0"));
            Assert.Equal(HttpStatusCode.OK,(await ac.PostAsJsonAsync($"/api/admin/benutzer/{employeeId}/freigaben",new{art="Firma",id="erlaubt",erlaubt=true})).StatusCode);
            var contacts=await ec.GetStringAsync("/api/kontakte?seite=0");Assert.Contains("Synthetischer Bestandskontakt",contacts);Assert.DoesNotContain("geschuetzter",contacts);
        }finally{await host.App.StopAsync();await host.App.DisposeAsync();}
        // A new host and new DB contexts, using the same durable database and protected keyring.
        host=await Start();
        try{
            using var ec=Client(host.Url);using var lc=Client(host.Url);await Login(ec,"neuteam",employeePassword);await Login(lc,"bestand",legacyPassword);
            Assert.Contains("Synthetischer Bestandskontakt",await ec.GetStringAsync("/api/kontakte?seite=0"));Assert.Contains("Synthetischer Bestandskontakt",await lc.GetStringAsync("/api/kontakte?seite=0"));
            Assert.Equal(HttpStatusCode.Forbidden,(await ec.GetAsync("/api/admin/benutzer")).StatusCode);
            await using var db=Db(runtime.ConnectionString);var stored=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==employeeId);
            Assert.True(stored.Eingerichtet&&stored.Aktiv);Assert.Equal("Mitarbeiter",stored.Rolle);Assert.Null(stored.EinladungHash);Assert.Null(stored.EinladungBis);Assert.True(stored.PasswortHash!=employeePassword);
            Assert.True(hasher.VerifyHashedPassword(stored,stored.PasswortHash,employeePassword)!=PasswordVerificationResult.Failed);Assert.Equal(1,await db.Sicherheitsereignisse.CountAsync(x=>x.BenutzerId==employeeId&&x.Aktion=="Erstzugang eingerichtet"));
            Assert.Equal(HttpStatusCode.Forbidden,(await lc.PostAsJsonAsync($"/api/admin/benutzer/{employeeId}/wiederherstellung",new{version=stored.Version,widerrufen=false,identitaetGeprueft=true})).StatusCode);
            using var ac=Client(host.Url);using var reset=Client(host.Url);await Login(ac,"admin",adminPassword);await Csrf(reset);
            var issued=await ac.PostAsJsonAsync($"/api/admin/benutzer/{employeeId}/wiederherstellung",new{version=stored.Version,widerrufen=false,identitaetGeprueft=true});Assert.Equal(HttpStatusCode.OK,issued.StatusCode);
            var resetCode=(await issued.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();var resetPassword=Secret();
            Assert.Equal(HttpStatusCode.OK,(await reset.PostAsJsonAsync("/api/auth/wiederherstellen",new{code=resetCode,passwort=resetPassword})).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,(await ec.GetAsync("/api/kontakte?seite=0")).StatusCode);
            await Csrf(reset);Assert.Equal(HttpStatusCode.Unauthorized,(await reset.PostAsJsonAsync("/api/auth/login",new{login="neuteam",passwort=employeePassword})).StatusCode);await Login(reset,"neuteam",resetPassword);
            Assert.Equal(HttpStatusCode.BadRequest,(await reset.PostAsJsonAsync("/api/auth/wiederherstellen",new{code=resetCode,passwort=Secret()})).StatusCode);
            await using var checkedDb=Db(runtime.ConnectionString);var changed=await checkedDb.Benutzer.SingleAsync(x=>x.Id==employeeId);Assert.NotEqual(stored.Sicherheitskennung,changed.Sicherheitskennung);Assert.Null(changed.WiederherstellungHash);Assert.Null(changed.WiederherstellungBis);Assert.Equal("Mitarbeiter",changed.Rolle);Assert.True(changed.PasswortHash!=resetPassword);Assert.True(hasher.VerifyHashedPassword(changed,changed.PasswortHash,resetPassword)!=PasswordVerificationResult.Failed);
            Assert.Equal(1,await checkedDb.Sicherheitsereignisse.CountAsync(x=>x.BenutzerId==employeeId&&x.Aktion=="Passwort wiederhergestellt"));
        }finally{await host.App.StopAsync();await host.App.DisposeAsync();}
        output.WriteLine("Real HTTPS create/configure/login/least-privilege/grant flow and host restart passed with restricted PostgreSQL runtime role; legacy login remains valid.");
        EinladungAusgabe race;await using(var db=Db(runtime.ConnectionString)){var u=await Anmeldung.Anlegen(db,hasher,new("konkurrenz","Synthetischer Konkurrenztest",Secret(),"Mitarbeiter"));race=(await Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,true),admin.Id))!;}
        using var barrier=new Barrier(2);var sync=new GleichzeitigerHasher(hasher,barrier);var a=Secret();var b=Secret();
        async Task<bool> Redeem(string pass){await using var db=Db(runtime.ConnectionString);try{await Erstzugang.Einloesen(db,sync,new(race.Code,pass),true);return true;}catch(Arbeitsfehler){return false;}}
        var results=await Task.WhenAll(Task.Run(()=>Redeem(a)),Task.Run(()=>Redeem(b)));Assert.Single(results,x=>x);
        await using(var db=Db(runtime.ConnectionString)){
            var u=await db.Benutzer.SingleAsync(x=>x.Id==race.Id);Assert.True(u.Eingerichtet);Assert.Null(u.WiederherstellungHash);Assert.Equal(1,await db.Sicherheitsereignisse.CountAsync(x=>x.BenutzerId==u.Id&&x.Aktion=="Passwort wiederhergestellt"));Assert.True(hasher.VerifyHashedPassword(u,u.PasswortHash,results[0]?a:b)!=PasswordVerificationResult.Failed);
        }
        await MfaTests.ProviderRace(runtime.ConnectionString);
        output.WriteLine("Password reset and additive MFA migration passed. PostgreSQL synchronized reset, TOTP and MFA recovery races each produced exactly one success and one audit. Synthetic database retained; main database never opened.");
    }
}
