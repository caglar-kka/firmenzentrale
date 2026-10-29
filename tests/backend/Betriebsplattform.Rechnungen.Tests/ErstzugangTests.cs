using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Betriebsplattform.Web.Persistenz.Migrationen;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class ErstzugangTests
{
    private static string Secret()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    [Fact] public async Task AdministratorErstelltEinladungUndMitarbeiterRichtetEigenesPasswortEin()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();
        var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;var adminPass=Secret();
        await using(var db=new FirmenDbContext(options)){await db.Database.EnsureCreatedAsync();await Anmeldung.Anlegen(db,Anmeldung.Passwortdienst(),new("admin","Admin",adminPass,"Administrator"));}
        using var rsa=RSA.Create(2048);var req=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var generated=req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));
        using var cert=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12),null,X509KeyStorageFlags.UserKeySet);
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.ConfigureKestrel(o=>o.Listen(IPAddress.Loopback,0,l=>l.UseHttps(cert)));
        builder.Services.AddDbContext<FirmenDbContext>(o=>o.UseSqlite(cn));
        var root=Path.Combine(Path.GetTempPath(),"FirmenZentrale-ErstzugangTests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var pilot=new Pilotoptionen(true,null,null);Anmeldung.Dienste(builder,pilot,root);
        await using var app=builder.Build();Webschutz.Konfigurieren(app,new Arbeitsmodule());Anmeldung.Pipeline(app,pilot);Anmeldung.Endpunkte(app,pilot);await app.StartAsync();
        try{
            var url=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            HttpClient Client(){var c=new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=(_,s,_,_)=>s?.GetCertHashString()==cert.GetCertHashString()}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
            async Task Csrf(HttpClient c){var v=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",v.GetProperty("csrf").GetString());}
            using var admin=Client();using var person=Client();await Csrf(admin);await Csrf(person);
            Assert.Equal(HttpStatusCode.Unauthorized,(await person.PostAsJsonAsync("/api/admin/benutzer",new{login="team",anzeigename="Muster",rolle="Mitarbeiter"})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync("/api/auth/login",new{login="admin",passwort=adminPass})).StatusCode);await Csrf(admin);
            var creation=await admin.PostAsJsonAsync("/api/admin/benutzer",new{login="team",anzeigename="Muster",rolle="Mitarbeiter"});Assert.Equal(HttpStatusCode.OK,creation.StatusCode);Assert.True(creation.Headers.CacheControl?.NoStore);
            var invitation=await creation.Content.ReadFromJsonAsync<JsonElement>();var code=invitation.GetProperty("code").GetString();var id=invitation.GetProperty("id").GetString();var pass=Secret();
            // Even a matching hash must not bypass the pending-account gate.
            await using(var db=new FirmenDbContext(options)){var pending=await db.Benutzer.SingleAsync(x=>x.Id==id);pending.PasswortHash=Anmeldung.Passwortdienst().HashPassword(pending,pass);pending.Version++;await db.SaveChangesAsync();}
            Assert.Equal(HttpStatusCode.Unauthorized,(await person.PostAsJsonAsync("/api/auth/login",new{login="team",passwort=pass})).StatusCode);
            person.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await person.PostAsJsonAsync("/api/auth/einrichten",new{code,passwort=pass})).StatusCode);await Csrf(person);
            Assert.Equal(HttpStatusCode.OK,(await person.PostAsJsonAsync("/api/auth/einrichten",new{code,passwort=pass})).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await person.PostAsJsonAsync("/api/auth/einrichten",new{code,passwort=Secret()})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await person.PostAsJsonAsync("/api/auth/login",new{login="team",passwort=pass})).StatusCode);await Csrf(person);
            Assert.Equal(HttpStatusCode.Forbidden,(await person.PostAsJsonAsync($"/api/admin/benutzer/{id}/einladung",new{version=1,widerrufen=false})).StatusCode);
            var session=await person.GetFromJsonAsync<JsonElement>("/api/auth/session");Assert.False(session.GetProperty("admin").GetBoolean());
            Assert.Equal(HttpStatusCode.OK,(await person.PostAsJsonAsync("/api/auth/passwort",new{bisherigesPasswort=pass,neuesPasswort=Secret()})).StatusCode);
            var list=await admin.GetStringAsync("/api/admin/benutzer");Assert.DoesNotContain(code!,list);Assert.DoesNotContain(pass,list);Assert.DoesNotContain("passwortHash",list);
            await using(var db=new FirmenDbContext(options)){
                var owner=await db.Benutzer.SingleAsync(x=>x.Login=="admin");await Erstzugang.Anlegen(db,Anmeldung.Passwortdienst(),new("pendingadmin","Pending","Administrator"),owner.Id);
                Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync($"/api/admin/benutzer/{owner.Id}",new{aktiv=false,rolle="Mitarbeiter",version=owner.Version})).StatusCode);
            }
            var limited=false;for(var n=0;n<11;n++)if((await person.PostAsJsonAsync("/api/auth/einrichten",new{code="falsch",passwort=Secret()})).StatusCode==HttpStatusCode.TooManyRequests){limited=true;break;}Assert.True(limited);
        }finally{await app.StopAsync();}
    }
    [Fact] public async Task UngueltigAbgelaufenDeaktiviertWiderrufenUndNeuausstellung()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();
        var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;
        await using var db=new FirmenDbContext(options);await db.Database.EnsureCreatedAsync();var hasher=Anmeldung.Passwortdienst();
        var admin=await Anmeldung.Anlegen(db,hasher,new("admin","Admin",Secret(),"Administrator"));
        var invitation=await Erstzugang.Anlegen(db,hasher,new("muster","Musterperson","Mitarbeiter"),admin.Id);var u=await db.Benutzer.SingleAsync(x=>x.Id==invitation.Id);
        Assert.False(u.Eingerichtet);Assert.Equal(97,invitation.Code.Length);Assert.DoesNotContain(invitation.Code,u.EinladungHash!);Assert.Equal(64,u.EinladungHash!.Length);
        Assert.InRange(invitation.GueltigBis-DateTimeOffset.UtcNow,TimeSpan.FromHours(23.9),TimeSpan.FromHours(24));
        foreach(var code in new[]{"",invitation.Code[..^1],invitation.Id+"."+Secret()})await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,hasher,new(code,Secret())));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,hasher,new(invitation.Code,"kurz")));
        u.EinladungBis=DateTimeOffset.UtcNow.AddSeconds(-1);u.Version++;await db.SaveChangesAsync();
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,hasher,new(invitation.Code,Secret())));
        var renewed=(await Erstzugang.Aendern(db,u.Id,new(u.Version,false),admin.Id))!;Assert.NotEqual(invitation.Code,renewed.Code);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,hasher,new(invitation.Code,Secret())));
        u.Aktiv=false;u.Version++;await db.SaveChangesAsync();await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,hasher,new(renewed.Code,Secret())));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Aendern(db,u.Id,new(u.Version,false),admin.Id));
        u.Aktiv=true;u.Version++;await db.SaveChangesAsync();await Erstzugang.Aendern(db,u.Id,new(u.Version,true),admin.Id);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,hasher,new(renewed.Code,Secret())));
        var final=(await Erstzugang.Aendern(db,u.Id,new(u.Version,false),admin.Id))!;var pass=Secret();await Erstzugang.Einloesen(db,hasher,new(final.Code,pass));
        Assert.True(u.Eingerichtet);Assert.Null(u.EinladungHash);Assert.Null(u.EinladungBis);Assert.Equal("Mitarbeiter",u.Rolle);
        Assert.NotEqual(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(u,u.PasswortHash,pass));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,hasher,new(final.Code,Secret())));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Aendern(db,u.Id,new(u.Version,false),admin.Id));
        Assert.DoesNotContain(final.Code,JsonSerializer.Serialize(await db.Sicherheitsereignisse.ToArrayAsync()));
        Assert.DoesNotContain(pass,new EinrichtungEingabe(final.Code,pass).ToString());Assert.DoesNotContain(final.Code,final.ToString());
    }
    private sealed class GleichzeitigerHasher(IPasswordHasher<Benutzerkonto> inner,Barrier barrier):IPasswordHasher<Benutzerkonto>
    {
        public string HashPassword(Benutzerkonto u,string pass){Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(15)));return inner.HashPassword(u,pass);}
        public PasswordVerificationResult VerifyHashedPassword(Benutzerkonto u,string hash,string pass)=>inner.VerifyHashedPassword(u,hash,pass);
    }
    [Fact] public async Task ZweiGleichzeitigeEinloesungenNurEinErfolgUndEinAudit()
    {
        var root=Path.Combine(Path.GetTempPath(),"FirmenZentrale-ErstzugangTests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite("Data Source="+Path.Combine(root,"synthetisch.db")).Options;
        var hasher=Anmeldung.Passwortdienst();EinladungAusgabe invitation;
        await using(var db=new FirmenDbContext(options)){await db.Database.EnsureCreatedAsync();var admin=await Anmeldung.Anlegen(db,hasher,new("admin","Admin",Secret(),"Administrator"));invitation=await Erstzugang.Anlegen(db,hasher,new("team","Team","Mitarbeiter"),admin.Id);}
        using var barrier=new Barrier(2);var synchronized=new GleichzeitigerHasher(hasher,barrier);var a=Secret();var b=Secret();
        async Task<bool> Redeem(string pass){await using var db=new FirmenDbContext(options);try{await Erstzugang.Einloesen(db,synchronized,new(invitation.Code,pass));return true;}catch(Arbeitsfehler){return false;}}
        var results=await Task.WhenAll(Task.Run(()=>Redeem(a)),Task.Run(()=>Redeem(b)));Assert.Single(results,x=>x);
        await using var check=new FirmenDbContext(options);var u=await check.Benutzer.SingleAsync(x=>x.Id==invitation.Id);
        Assert.True(u.Eingerichtet);Assert.Null(u.EinladungHash);Assert.Equal(1,await check.Sicherheitsereignisse.CountAsync(x=>x.Aktion=="Erstzugang eingerichtet"));
        Assert.NotEqual(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(u,u.PasswortHash,results[0]?a:b));
        Assert.Equal(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(u,u.PasswortHash,results[0]?b:a));
    }
    [Fact] public void TransportErlaubtNurHttpsOderLoopback()
    {
        var c=new Microsoft.AspNetCore.Http.DefaultHttpContext();c.Connection.RemoteIpAddress=IPAddress.Parse("192.0.2.1");Assert.False(Erstzugang.SichererTransport(c));
        c.Request.Scheme="https";Assert.True(Erstzugang.SichererTransport(c));c.Request.Scheme="http";c.Connection.RemoteIpAddress=IPAddress.Loopback;Assert.True(Erstzugang.SichererTransport(c));
    }
    [Theory][InlineData("neu")][InlineData("widerruf")][InlineData("deaktiviert")]
    public async Task BereitsGelesenerCodeVerliertBeiZeitgleicherAenderung(string art)
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;var hasher=Anmeldung.Passwortdienst();
        await using var stale=new FirmenDbContext(options);await stale.Database.EnsureCreatedAsync();var admin=await Anmeldung.Anlegen(stale,hasher,new("admin","Admin",Secret(),"Administrator"));var invitation=await Erstzugang.Anlegen(stale,hasher,new("team","Team","Mitarbeiter"),admin.Id);
        await using(var change=new FirmenDbContext(options)){
            var u=await change.Benutzer.SingleAsync(x=>x.Id==invitation.Id);
            if(art=="deaktiviert"){u.Aktiv=false;u.Version++;await change.SaveChangesAsync();}
            else await Erstzugang.Aendern(change,u.Id,new(u.Version,art=="widerruf"),admin.Id);
        }
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(stale,hasher,new(invitation.Code,Secret())));
        await using var check=new FirmenDbContext(options);Assert.False((await check.Benutzer.SingleAsync(x=>x.Id==invitation.Id)).Eingerichtet);Assert.Equal(0,await check.Sicherheitsereignisse.CountAsync(x=>x.Aktion=="Erstzugang eingerichtet"));
    }
    [Fact] public async Task MigrationIstAdditivUndErhaeltBestehendeKonten()
    {
        await using var pg=new FirmenDbFactory().CreateDbContext([]);Assert.False(pg.Database.HasPendingModelChanges());
        var sql=pg.GetService<IMigrator>().GenerateScript("20261019114242_PilotSecurity","20261019170000_MitarbeiterErstzugang");
        Assert.Contains("ADD \"Eingerichtet\" boolean NOT NULL DEFAULT TRUE",sql);Assert.DoesNotContain("DROP",sql);Assert.DoesNotContain("PasswortHash",sql);
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();
        await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Benutzer (Id TEXT PRIMARY KEY, PasswortHash TEXT NOT NULL, Aktiv boolean NOT NULL); INSERT INTO Benutzer VALUES ('bestand','synthetischer-hash',1);");
        foreach(var command in db.GetService<IMigrationsSqlGenerator>().Generate(new MitarbeiterErstzugang().UpOperations,db.Model))await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await using var read=cn.CreateCommand();read.CommandText="SELECT Eingerichtet, EinladungHash, EinladungBis, PasswortHash, Aktiv FROM Benutzer WHERE Id='bestand'";
        await using var r=await read.ExecuteReaderAsync();Assert.True(await r.ReadAsync());Assert.True(r.GetBoolean(0));Assert.True(r.IsDBNull(1));Assert.True(r.IsDBNull(2));Assert.Equal("synthetischer-hash",r.GetString(3));Assert.True(r.GetBoolean(4));
    }
}
