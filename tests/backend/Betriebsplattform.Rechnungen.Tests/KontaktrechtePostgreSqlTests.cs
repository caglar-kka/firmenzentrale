using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Betrieb;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class KontaktrechtePostgreSqlTests(ITestOutputHelper output)
{
    private sealed class LiveAttribute:FactAttribute{public LiveAttribute(){if(Environment.GetEnvironmentVariable("FZ_KONTAKTRECHTE_POSTGRES")!="1"||!OperatingSystem.IsWindows())Skip="Explicit isolated PostgreSQL/HTTPS permission gate required.";}}
    [Live] public async Task KontrollierteMigrationUndEchteKontaktApisMitEingeschraenkterLaufzeitrolle()
    {
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).AddEnvironmentVariables().Build();
        var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")??throw new InvalidOperationException("Local management configuration required."));
        Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);cs.Database="postgres";cs.Pooling=false;cs.IncludeErrorDetail=false;cs.LogParameters=false;
        var run=Guid.NewGuid().ToString("N");var database="fz_kontaktrechte_test_"+run;var role="fz_app_kr_"+run[..16];
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using var cmd=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(database)+" TEMPLATE template0",c);await cmd.ExecuteNonQueryAsync();}
        cs.Database=database;
        FirmenDbContext Db(string connection)=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(connection,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
        var h=Anmeldung.Passwortdienst();var pass=MfaTests.Secret();string adminId,altId,neuId;
        await using(var db=Db(cs.ConnectionString)){
            Assert.Equal(database,db.Database.GetDbConnection().Database);
            await db.GetService<IMigrator>().MigrateAsync("20261023122655_TerminadresseUndErinnerung");
            adminId=(await Anmeldung.Anlegen(db,h,new("admin","Synthetische Administration",pass,"Administrator"))).Id;
            altId=(await Anmeldung.Anlegen(db,h,new("bestand","Synthetischer Bestand",pass,"Mitarbeiter"))).Id;
            db.Kontakte.AddRange(new KontaktDatensatz{Id="erlaubt",Name="Synthetischer Kontakt",Suchname="synthetischer kontakt"},new KontaktDatensatz{Id="geschuetzt",Name="Geschuetzter Kontakt",Suchname="geschuetzter kontakt"});
            db.Freigaben.Add(new(){BenutzerId=altId,KontaktId="erlaubt"});await db.SaveChangesAsync();
            var vor=await db.Benutzer.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>new{x.Rolle,x.PasswortHash,x.Sicherheitskennung,x.Aktiv});
            await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            foreach(var u in await db.Benutzer.AsNoTracking().ToArrayAsync()){Assert.Equal(vor[u.Id].PasswortHash,u.PasswortHash);Assert.Equal(vor[u.Id].Sicherheitskennung,u.Sicherheitskennung);Assert.Equal(vor[u.Id].Rolle,u.Rolle);Assert.Equal(vor[u.Id].Aktiv,u.Aktiv);}
            Assert.Equal("Bestand",(await db.BenutzerRechteprofile.SingleAsync(x=>x.BenutzerId==altId)).Modus);Assert.Equal(1,await db.Freigaben.CountAsync());
            neuId=(await Anmeldung.Anlegen(db,h,new("neuteam","Synthetisches Team",pass,"Mitarbeiter"))).Id;
        }
        var runtimePass=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(cs.ConnectionString,role,runtimePass);
        var runtime=new NpgsqlConnectionStringBuilder(cs.ConnectionString){Username=role,Password=runtimePass};
        var root=Path.Combine(Path.GetTempPath(),"MBOP-Kontaktrechte",run);Directory.CreateDirectory(root);
        using var rsa=RSA.Create(2048);var request=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));using var cert=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12),null,X509KeyStorageFlags.UserKeySet);
        var app=ApiAnwendung.Erstellen(["--FirmenZentrale:Persistence:Provider=PostgreSql","--FirmenZentrale:Persistence:DocumentRoot="+root,"--ConnectionStrings:FirmenZentrale="+runtime.ConnectionString,"--FirmenZentrale:Security:Enabled=true"],0,testZertifikat:cert);
        await using var host=app;await app.StartAsync();
        try{
            var url=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            HttpClient Client(){var c=new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=(_,s,_,_)=>s?.GetCertHashString()==cert.GetCertHashString()}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
            async Task Csrf(HttpClient c){var s=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",s.GetProperty("csrf").GetString());}
            async Task Login(HttpClient c,string login){await Csrf(c);Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/login",new{login,passwort=pass})).StatusCode);await Csrf(c);}
            using var alt=Client();using var neu=Client();using var admin=Client();await Login(alt,"bestand");await Login(neu,"neuteam");
            Assert.Contains("Synthetischer Kontakt",await alt.GetStringAsync("/api/kontakte?seite=0"));Assert.DoesNotContain("Geschuetzter Kontakt",await alt.GetStringAsync("/api/kontakte?seite=0"));
            Assert.Equal(HttpStatusCode.Forbidden,(await neu.GetAsync("/api/kontakte?seite=0")).StatusCode);
            var mfa=app.Services.GetRequiredService<MfaDienst>();string[] codes;
            await using(var db=Db(runtime.ConnectionString)){await mfa.Vorbereiten(db,h,adminId,new(pass,null));var u=await db.Benutzer.SingleAsync(x=>x.Id==adminId);var code=MfaTests.Code(app.Services.GetRequiredService<IDataProtectionProvider>(),u,new MfaTests.Clock());var result=await mfa.Verwalten(db,h,adminId,new(pass,code),"aktivieren");codes=result.Codes!.Codes;}
            await Login(admin,"admin");Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync("/api/auth/mfa-anmelden",new{code=codes[0],wiederherstellung=true})).StatusCode);await Csrf(admin);
            string mfaRegeln,adminGeheimnis,adminPasswortHash;
            await using(var db=Db(runtime.ConnectionString)){mfaRegeln=JsonSerializer.Serialize(await db.MfaRegeln.AsNoTracking().ToArrayAsync());var a=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==adminId);adminGeheimnis=a.MfaGeheimnis!;adminPasswortHash=a.PasswortHash;}
            var index=1;
            async Task<KontaktrechteAnsicht> Save(string id,string? vorlage,Dictionary<string,bool> overrides,bool bestand=false){
                var path="/api/admin/benutzer/"+id+"/kontaktrechte";var stand=await admin.GetFromJsonAsync<KontaktrechteAnsicht>(path);
                var r=await admin.PostAsJsonAsync(path,new KontaktrechteEingabe(stand!.Version,vorlage,overrides,bestand,new(pass,codes[index++],true)));Assert.Equal(HttpStatusCode.OK,r.StatusCode);await Csrf(admin);
                var saved=(await r.Content.ReadFromJsonAsync<KontaktrechteAnsicht>())!;var reloaded=(await admin.GetFromJsonAsync<KontaktrechteAnsicht>(path))!;
                Assert.Equal(JsonSerializer.Serialize(saved),JsonSerializer.Serialize(reloaded));Assert.Equal(vorlage,reloaded.VorlageId);Assert.Equal(stand.Version+1,reloaded.Version);Assert.Equal("Explizit",reloaded.Modus);
                // A new context proves persisted state, independently of the HTTP response/context.
                await using var db=Db(runtime.ConnectionString);var profile=await db.BenutzerRechteprofile.AsNoTracking().SingleAsync(x=>x.BenutzerId==id&&x.Bereich=="Kontakte");
                Assert.Equal(saved.Version,profile.Version);Assert.Equal(vorlage,profile.VorlageId);Assert.Equal(JsonSerializer.Serialize(overrides),profile.AbweichungenJson);
                var audit=(await db.Berechtigungsaenderungen.AsNoTracking().Where(x=>x.ZielId==id).ToArrayAsync()).Single(x=>JsonDocument.Parse(x.Nachher).RootElement.GetProperty("Version").GetInt32()==saved.Version);
                Assert.Equal(adminId,audit.AkteurId);Assert.Equal("Kontakte",audit.Bereich);Assert.Equal(JsonSerializer.Serialize(stand),audit.Vorher);
                using var nachher=JsonDocument.Parse(audit.Nachher);Assert.Equal(vorlage,nachher.RootElement.GetProperty("VorlageId").GetString());Assert.Equal(JsonSerializer.Serialize(overrides),nachher.RootElement.GetProperty("Abweichungen").GetRawText());
                foreach(var a in saved.Aktionen)Assert.Equal(a.Erlaubt,nachher.RootElement.GetProperty("Wirksam").GetProperty(a.Id).GetBoolean());
                Assert.DoesNotContain(pass,audit.Nachher);Assert.DoesNotContain(codes[index-1],audit.Nachher);return saved;
            }
            async Task AbgelehnteBestaetigung(MfaPruefung pruefung){
                var path="/api/admin/benutzer/"+neuId+"/kontaktrechte";var before=(await admin.GetFromJsonAsync<KontaktrechteAnsicht>(path))!;
                await using var db=Db(runtime.ConnectionString);var count=await db.Berechtigungsaenderungen.CountAsync();
                Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(path,new KontaktrechteEingabe(before.Version,"buero:1",new(),false,pruefung))).StatusCode);
                Assert.Equal(JsonSerializer.Serialize(before),JsonSerializer.Serialize(await admin.GetFromJsonAsync<KontaktrechteAnsicht>(path)));Assert.Equal(count,await db.Berechtigungsaenderungen.CountAsync());
                Assert.True((await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==adminId)).MfaFehlversuche>0);
            }
            Assert.Equal(HttpStatusCode.Forbidden,(await neu.PostAsJsonAsync("/api/admin/benutzer/"+neuId+"/kontaktrechte",new{})).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await admin.GetAsync("/api/admin/benutzer/"+adminId+"/kontaktrechte")).StatusCode);
            await AbgelehnteBestaetigung(new("Synthetisches falsches Passwort",codes[1],true));
            await AbgelehnteBestaetigung(new(pass,"ungueltiger-synthetischer-faktor",true));
            await Save(neuId,"buero:1",new());
            await AbgelehnteBestaetigung(new(pass,codes[1],true)); // A consumed recovery factor is never reusable.
            // Action grants do not replace the existing resource boundary.
            Assert.DoesNotContain("Synthetischer Kontakt",await neu.GetStringAsync("/api/kontakte?seite=0"));Assert.Equal(HttpStatusCode.NotFound,(await neu.GetAsync("/api/kontakte/geschuetzt")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync("/api/admin/benutzer/"+neuId+"/freigaben",new{art="Firma",id="erlaubt",erlaubt=true})).StatusCode);
            Assert.Contains("Synthetischer Kontakt",await neu.GetStringAsync("/api/kontakte?seite=0"));
            var eingabe=new Kontakteingabe(null,"Person",null,"","Büro","Ansprechpartner",[new("Telefon","+49 30 5550105",true)],Vorname:"Neue",Nachname:"Musterperson");
            var created=await neu.PostAsJsonAsync("/api/kontakte",eingabe);Assert.Equal(HttpStatusCode.OK,created.StatusCode);var kontakt=(await created.Content.ReadFromJsonAsync<Firmenkontakt>())!;
            Assert.Equal(HttpStatusCode.OK,(await neu.PostAsJsonAsync("/api/kontakte/"+kontakt.Id,eingabe with{Version=kontakt.Version,Abteilung="Verwaltung"})).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await neu.PostAsJsonAsync("/api/kontakte/"+kontakt.Id+"/archivieren",new{version=kontakt.Version+1})).StatusCode);
            await Save(neuId,"buero:1",new(){["Bearbeiten"]=false,["Erstellen"]=false});
            foreach(var path in new[]{"/api/kontakte","/api/kontakte/erlaubt","/api/kontakte/erlaubt/archivieren","/api/kontaktvorschlaege/ungueltig"})Assert.Equal(HttpStatusCode.Forbidden,(await neu.PostAsJsonAsync(path,new{})).StatusCode);
            await Save(neuId,"teamleitung:1",new(){["Archivieren"]=false});
            Assert.Equal(HttpStatusCode.Forbidden,(await neu.PostAsJsonAsync("/api/kontakte/erlaubt/archivieren",new{version=1})).StatusCode);
            await Save(neuId,"teamleitung:1",new());
            Assert.DoesNotContain("Geschuetzter Kontakt",await neu.GetStringAsync("/api/kontakte?seite=0"));Assert.Equal(HttpStatusCode.NotFound,(await neu.GetAsync("/api/kontakte/geschuetzt")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await neu.PostAsJsonAsync("/api/kontakte/erlaubt/archivieren",new{version=1})).StatusCode);
            await Save(neuId,null,new());
            Assert.Equal(HttpStatusCode.Forbidden,(await neu.GetAsync("/api/kontakte/erlaubt")).StatusCode);
            Assert.DoesNotContain("\"ziel\":\"kontakt\"",await neu.GetStringAsync("/api/suche?text=Kontakt"));
            await using(var db=Db(runtime.ConnectionString)){Assert.Equal(5,await db.Berechtigungsaenderungen.CountAsync(x=>x.ZielId==neuId));Assert.All(await db.Berechtigungsaenderungen.ToArrayAsync(),a=>{Assert.Equal(adminId,a.AkteurId);Assert.Contains("Lesen",a.Vorher);Assert.Contains("Wirksam",a.Nachher);Assert.DoesNotContain(pass,a.Nachher);});Assert.Equal("Mitarbeiter",(await db.Benutzer.SingleAsync(x=>x.Id==neuId)).Rolle);}
            await using(var db=Db(runtime.ConnectionString)){Assert.Equal(mfaRegeln,JsonSerializer.Serialize(await db.MfaRegeln.AsNoTracking().ToArrayAsync()));var a=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==adminId);Assert.Equal("Administrator",a.Rolle);Assert.Equal(adminGeheimnis,a.MfaGeheimnis);Assert.Equal(adminPasswortHash,a.PasswortHash);Assert.True(a.Aktiv);Assert.False(await db.BenutzerRechteprofile.AnyAsync(x=>x.BenutzerId==adminId));}
            await using(var db=Db(runtime.ConnectionString)){await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync("UPDATE \"Rechtevorlagenversionen\" SET \"Name\"='Manipulation'"));await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync("DELETE FROM \"Berechtigungsaenderungen\""));}
        }finally{await app.StopAsync();}
        output.WriteLine("Isolated synthetic PostgreSQL database retained: "+database+". Migration, restricted runtime, real HTTPS APIs, MFA-confirmed saves, action/resource intersection and audit passed; main database never opened.");
    }
}
