using Betriebsplattform.Web.Sicherheit;
using Betriebsplattform.Web.Persistenz;
using KontaktrechteMigration = Betriebsplattform.Web.Persistenz.Migrationen.Kontaktberechtigungen;
using Betriebsplattform.Web.Akten;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.DataProtection;
using Xunit;
using Betriebsplattform.Web;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class KontaktberechtigungenTests
{
    [Fact] public async Task AdministratorRichtetMfaRegulaerEinUndBestaetigtDanachKontaktrechte()
    {
        var root=Path.Combine(Path.GetTempPath(),"MBOP-Kontaktrechte-Mfa",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var connection="Data Source="+Path.Combine(root,"synthetisch.db");
        FirmenDbContext Db()=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(connection).Options);
        var pass=MfaTests.Secret();var h=Anmeldung.Passwortdienst();string adminId,zielId;
        await using(var db=Db()){await db.Database.EnsureCreatedAsync();adminId=(await Anmeldung.Anlegen(db,h,new("admin","Synthetische Administration",pass,"Administrator"))).Id;zielId=(await Anmeldung.Anlegen(db,h,new("team","Synthetisches Team",pass,"Mitarbeiter"))).Id;}
        using var rsa=RSA.Create(2048);var request=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));using var cert=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12),null,X509KeyStorageFlags.UserKeySet);
        var clock=new MfaTests.Clock();var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.ConfigureKestrel(o=>o.Listen(IPAddress.Loopback,0,l=>l.UseHttps(cert)));builder.Services.AddDbContext<FirmenDbContext>(o=>o.UseSqlite(connection));var pilot=new Pilotoptionen(true,null,null);Anmeldung.Dienste(builder,pilot,root);builder.Services.AddSingleton<TimeProvider>(clock);
        await using var app=builder.Build();Webschutz.Konfigurieren(app,new Arbeitsmodule());Anmeldung.Pipeline(app,pilot);Anmeldung.Endpunkte(app,pilot);await app.StartAsync();
        try{
            var url=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());using var client=new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=(_,s,_,_)=>s?.GetCertHashString()==cert.GetCertHashString()}){BaseAddress=url};client.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));client.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");
            async Task Csrf(){var s=await client.GetFromJsonAsync<JsonElement>("/api/auth/session");client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",s.GetProperty("csrf").GetString());}
            async Task<string> Code(bool falsch=false){await using var db=Db();var u=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==adminId);var p=app.Services.GetRequiredService<IDataProtectionProvider>();return falsch?MfaTests.BadCode(p,u,clock):MfaTests.Code(p,u,clock);}
            var path="/api/admin/benutzer/"+zielId+"/kontaktrechte";
            KontaktrechteEingabe Eingabe(string code)=>new(0,"buero:1",new(),false,new(pass,code));
            await Csrf();Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/login",new{login="admin",passwort=pass})).StatusCode);await Csrf();
            Assert.False((await client.GetFromJsonAsync<JsonElement>("/api/auth/mfa/status")).GetProperty("aktiv").GetBoolean());
            Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(path,Eingabe("123456"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/auth/mfa/vorbereiten",new{passwort="Synthetisch falsch"})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/mfa/vorbereiten",new{passwort=pass})).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/auth/mfa/aktivieren",new{passwort=pass,code=await Code(true)})).StatusCode);
            Assert.False((await client.GetFromJsonAsync<JsonElement>("/api/auth/mfa/status")).GetProperty("aktiv").GetBoolean());
            var verbraucht=await Code();Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/mfa/aktivieren",new{passwort=pass,code=verbraucht})).StatusCode);await Csrf();
            Assert.True((await client.GetFromJsonAsync<JsonElement>("/api/auth/mfa/status")).GetProperty("aktiv").GetBoolean());
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(path,Eingabe(verbraucht))).StatusCode);
            await using(var db=Db()){Assert.Empty(await db.BenutzerRechteprofile.ToArrayAsync());Assert.Empty(await db.Berechtigungsaenderungen.ToArrayAsync());}
            clock.Next();var saved=await client.PostAsJsonAsync(path,Eingabe(await Code()));Assert.Equal(HttpStatusCode.OK,saved.StatusCode);await Csrf();
            var view=(await client.GetFromJsonAsync<KontaktrechteAnsicht>(path))!;Assert.Equal("buero:1",view.VorlageId);Assert.Equal(1,view.Version);Assert.Equal(new[]{"Lesen","Erstellen","Bearbeiten"},view.Aktionen.Where(x=>x.Erlaubt).Select(x=>x.Id));
            await using(var db=Db()){Assert.Equal("buero:1",(await db.BenutzerRechteprofile.SingleAsync()).VorlageId);var audit=await db.Berechtigungsaenderungen.SingleAsync();Assert.Equal(adminId,audit.AkteurId);Assert.Equal(zielId,audit.ZielId);Assert.DoesNotContain(pass,audit.Nachher);Assert.Equal("Administrator",(await db.Benutzer.SingleAsync(x=>x.Id==adminId)).Rolle);}
        }finally{await app.StopAsync();}
    }
    [Fact] public void KatalogVerweigertUnbekanntesUndTrenntVorlageVonAbweichung()
    {
        var buero=Kontaktberechtigungen.Standardvorlagen().Single(x=>x.Id=="buero:1");
        var team=Kontaktberechtigungen.Standardvorlagen().Single(x=>x.Id=="teamleitung:1");
        Assert.True(Kontaktberechtigungen.Auswerten("Lesen",buero.RechteJson,"{}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Archivieren",buero.RechteJson,"{}"));
        Assert.True(Kontaktberechtigungen.Auswerten("Archivieren",team.RechteJson,"{}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Bearbeiten",buero.RechteJson,"{\"Bearbeiten\":false}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Loeschen",team.RechteJson,"{\"Loeschen\":true}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Lesen",null,"{}"));
    }
    [Fact] public async Task MigrationErhaeltBestandsrechteUndNeueKontenBleibenOhneFreigabe()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();
        await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();
        var h=Anmeldung.Passwortdienst();var alt=await Anmeldung.Anlegen(db,h,new("alt","Bestand",new string('x',20),"Mitarbeiter"));
        var hash=alt.PasswortHash;var stamp=alt.Sicherheitskennung;
        await db.Database.ExecuteSqlRawAsync("DROP TABLE BenutzerRechteprofile; DROP TABLE Berechtigungsaenderungen; DROP TABLE Rechtevorlagenversionen;");
        var migration=new KontaktrechteMigration();
        foreach(var generated in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations,db.Model)){await using var cmd=cn.CreateCommand();cmd.CommandText=generated.CommandText;await cmd.ExecuteNonQueryAsync();}
        Assert.True(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Lesen"));Assert.True(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Archivieren"));
        var p=await db.BenutzerRechteprofile.SingleAsync();Assert.Equal("Bestand",p.Modus);Assert.Null(p.VorlageId);Assert.Equal(hash,alt.PasswortHash);Assert.Equal(stamp,alt.Sicherheitskennung);
        var neu=await Anmeldung.Anlegen(db,h,new("neu","Neu",new string('y',20),"Mitarbeiter"));Assert.False(await Kontaktberechtigungen.Erlaubt(db,neu.Id,"Lesen"));
        Assert.False(await Kontaktberechtigungen.Erlaubt(db,neu.Id,"Loeschen"));
        p.AbweichungenJson="{\"Lesen\":true,\"Bearbeiten\":false}";p.Version++;await db.SaveChangesAsync();Assert.True(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Lesen"));Assert.False(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Bearbeiten"));
        var v=await db.Rechtevorlagenversionen.FirstAsync();v.Name="Manipulation";await Assert.ThrowsAsync<Arbeitsfehler>(()=>db.SaveChangesAsync());
        await using var pg=new FirmenDbFactory().CreateDbContext([]);Assert.False(pg.Database.HasPendingModelChanges());
    }
    [Fact] public async Task StarkePruefungSelbstschutzKonflikteUndAudit()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;
        await using var db=new FirmenDbContext(options);await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();var pass=MfaTests.Secret();
        var admin=await Anmeldung.Anlegen(db,h,new("admin","Admin",pass,"Administrator"));var team=await Anmeldung.Anlegen(db,h,new("team","Team",pass,"Mitarbeiter"));
        var protection=new EphemeralDataProtectionProvider();var clock=new MfaTests.Clock();var mfa=new MfaDienst(protection,clock);
        var e=new KontaktrechteEingabe(0,"buero:1",new(){["Bearbeiten"]=false},false,new(pass,"123456"));
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,team.Id,team.Id,e,mfa,h))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,admin.Id,e,mfa,h))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e,mfa,h))).Status);
        await mfa.Vorbereiten(db,h,admin.Id,new(pass,null));await mfa.Verwalten(db,h,admin.Id,new(pass,MfaTests.Code(protection,admin,clock)),"aktivieren");clock.Next();
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Pruefung=new(pass,MfaTests.BadCode(protection,admin,clock))},mfa,h));
        Assert.True((await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==admin.Id)).MfaFehlversuche>0);Assert.Empty(await db.Berechtigungsaenderungen.ToArrayAsync());
        var saved=await Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Pruefung=new(pass,MfaTests.Code(protection,admin,clock))},mfa,h);
        Assert.True(saved.Aktionen.Single(x=>x.Id=="Lesen").Erlaubt);Assert.False(saved.Aktionen.Single(x=>x.Id=="Bearbeiten").Erlaubt);Assert.Equal(1,saved.Version);
        Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e,mfa,h))).Status);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Version=1,VorlageId="administrator:1"},mfa,h));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Version=1,Abweichungen=new(){["Loeschen"]=true}},mfa,h));
        var audit=await db.Berechtigungsaenderungen.SingleAsync();Assert.Equal(admin.Id,audit.AkteurId);Assert.Equal(team.Id,audit.ZielId);Assert.Contains("Bearbeiten",audit.Nachher);Assert.DoesNotContain(pass,audit.Nachher);
        audit.Nachher="Manipuliert";await Assert.ThrowsAsync<Arbeitsfehler>(()=>db.SaveChangesAsync());
    }
}
