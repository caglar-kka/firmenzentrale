using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class MfaHttpTests
{
    [Fact] public async Task HttpsEnrollmentNeustartChallengeRecoveryResetUndSichereDeaktivierung()
    {
        Assert.True(OperatingSystem.IsWindows());var root=Path.Combine(Path.GetTempPath(),"FirmenZentrale-MfaHttp",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite("Data Source="+Path.Combine(root,"synthetisch.db")).Options;var h=Anmeldung.Passwortdienst();var pass=MfaTests.Secret();var adminPass=MfaTests.Secret();string id,adminId;
        await using(var db=new FirmenDbContext(options)){await db.Database.EnsureCreatedAsync();id=(await Anmeldung.Anlegen(db,h,new("team","Team",pass,"Mitarbeiter"))).Id;adminId=(await Anmeldung.Anlegen(db,h,new("admin","Admin",adminPass,"Administrator"))).Id;}
        using var rsa=RSA.Create(2048);var request=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));using var cert=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12),null,X509KeyStorageFlags.UserKeySet);
        var clock=new MfaTests.Clock();
        async Task<(WebApplication App,Uri Url)> Start(){var b=WebApplication.CreateBuilder();b.Logging.ClearProviders();b.WebHost.ConfigureKestrel(o=>o.Listen(IPAddress.Loopback,0,l=>l.UseHttps(cert)));b.Services.AddDbContext<FirmenDbContext>(o=>o.UseSqlite("Data Source="+Path.Combine(root,"synthetisch.db")));var pilot=new Pilotoptionen(true,null,null);Anmeldung.Dienste(b,pilot,root);b.Services.AddSingleton<TimeProvider>(clock);b.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));b.Services.AddScoped<AufgabenDienst>();var app=b.Build();Webschutz.Konfigurieren(app,new Arbeitsmodule());Anmeldung.Pipeline(app,pilot);Anmeldung.Endpunkte(app,pilot);AufgabenEndpunkte.Registrieren(app);app.MapGet("/api/probe",()=>Results.Ok()).RequireAuthorization();app.MapGet("/api/admin/probe",()=>Results.Ok()).RequireAuthorization(p=>p.RequireRole("Administrator"));await app.StartAsync();return(app,new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()));}
        HttpClient Client(Uri url){var c=new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=(_,s,_,_)=>s?.GetCertHashString()==cert.GetCertHashString()}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
        async Task Csrf(HttpClient c){var v=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",v.GetProperty("csrf").GetString());}
        async Task<HttpResponseMessage> Login(HttpClient c,string password){await Csrf(c);return await c.PostAsJsonAsync("/api/auth/login",new{login="team",passwort=password});}
        async Task<string> AppCode(WebApplication app,bool wrong=false,string? userId=null){await using var db=new FirmenDbContext(options);var u=await db.Benutzer.SingleAsync(x=>x.Id==(userId??id));var p=app.Services.GetRequiredService<IDataProtectionProvider>();return wrong?MfaTests.BadCode(p,u,clock):MfaTests.Code(p,u,clock);}
        string[] codes;var host=await Start();
        try{
            using var a=Client(host.Url);using var old=Client(host.Url);Assert.Equal(HttpStatusCode.OK,(await Login(a,pass)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await a.GetAsync("/api/probe")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await Login(old,pass)).StatusCode);await Csrf(a);
            var setup=await a.PostAsJsonAsync("/api/auth/mfa/vorbereiten",new{passwort=pass});Assert.Equal(HttpStatusCode.OK,setup.StatusCode);Assert.True(setup.Headers.CacheControl?.NoStore);
            Assert.Equal(HttpStatusCode.BadRequest,(await a.PostAsJsonAsync("/api/auth/mfa/aktivieren",new{passwort=pass,code=await AppCode(host.App,true)})).StatusCode);
            var activated=await a.PostAsJsonAsync("/api/auth/mfa/aktivieren",new{passwort=pass,code=await AppCode(host.App)});Assert.Equal(HttpStatusCode.OK,activated.StatusCode);codes=(await activated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codes").EnumerateArray().Select(x=>x.GetString()!).ToArray();Assert.Equal(8,codes.Length);
            Assert.Equal(HttpStatusCode.Unauthorized,(await old.GetAsync("/api/probe")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await a.GetAsync("/api/probe")).StatusCode);
        }finally{await host.App.StopAsync();await host.App.DisposeAsync();}
        host=await Start(); // Real persisted Windows-protected keyring, not a new ephemeral protector.
        try{
            var next=MfaTests.Secret();await using(var db=new FirmenDbContext(options)){var u=await db.Benutzer.SingleAsync(x=>x.Id==id);var invitation=(await Erstzugang.WiederherstellungAendern(db,id,new(u.Version,false,true),adminId))!;await Erstzugang.Einloesen(db,h,new(invitation.Code,next),true);Assert.NotNull(u.MfaGeheimnis);}pass=next;
            using var a=Client(host.Url);var first=await Login(a,pass);Assert.Equal(HttpStatusCode.OK,first.StatusCode);Assert.True((await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mfaErforderlich").GetBoolean());Assert.Contains("httponly",string.Join(';',first.Headers.GetValues("Set-Cookie")).ToLowerInvariant());
            Assert.Equal(HttpStatusCode.Unauthorized,(await a.GetAsync("/api/probe")).StatusCode);await Csrf(a);
            Assert.Equal(HttpStatusCode.Unauthorized,(await a.GetAsync("/api/auth/mfa/status")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await a.PostAsJsonAsync("/api/auth/mfa-anmelden",new{code=await AppCode(host.App,true)})).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await a.PostAsJsonAsync("/api/auth/mfa-anmelden",new{code=await AppCode(host.App)})).StatusCode);
            clock.Next();Assert.Equal(HttpStatusCode.OK,(await a.PostAsJsonAsync("/api/auth/mfa-anmelden",new{code=await AppCode(host.App)})).StatusCode);await Csrf(a);Assert.Equal(HttpStatusCode.OK,(await a.GetAsync("/api/probe")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await a.GetAsync("/api/admin/probe")).StatusCode);
            var ownPassword=MfaTests.Secret();Assert.Equal(HttpStatusCode.OK,(await a.PostAsJsonAsync("/api/auth/passwort",new{bisherigesPasswort=pass,neuesPasswort=ownPassword})).StatusCode);pass=ownPassword;Assert.Equal(HttpStatusCode.OK,(await a.GetAsync("/api/probe")).StatusCode);
            using var b=Client(host.Url);Assert.Equal(HttpStatusCode.OK,(await Login(b,pass)).StatusCode);await Csrf(b);Assert.Equal(HttpStatusCode.OK,(await b.PostAsJsonAsync("/api/auth/mfa-anmelden",new{code=codes[0],wiederherstellung=true})).StatusCode);await Csrf(b);Assert.Equal(HttpStatusCode.Unauthorized,(await a.GetAsync("/api/probe")).StatusCode);
            var used=codes[0];Assert.True((await b.GetFromJsonAsync<JsonElement>("/api/auth/mfa/status")).GetProperty("wiederherstellungGeprueft").GetBoolean());var refreshed=await b.PostAsJsonAsync("/api/auth/mfa/codes",new{passwort=pass});Assert.Equal(HttpStatusCode.OK,refreshed.StatusCode);codes=(await refreshed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codes").EnumerateArray().Select(x=>x.GetString()!).ToArray();await Csrf(b);
            using var replay=Client(host.Url);Assert.Equal(HttpStatusCode.OK,(await Login(replay,pass)).StatusCode);await Csrf(replay);Assert.Equal(HttpStatusCode.BadRequest,(await replay.PostAsJsonAsync("/api/auth/mfa-anmelden",new{code=used,wiederherstellung=true})).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await b.PostAsJsonAsync("/api/auth/mfa/deaktivieren",new{passwort=pass,code=await AppCode(host.App,true)})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await b.PostAsJsonAsync("/api/auth/mfa/deaktivieren",new{passwort=pass,code=codes[1],wiederherstellung=true})).StatusCode);await Csrf(b);Assert.Equal(HttpStatusCode.OK,(await b.GetAsync("/api/probe")).StatusCode);
            using var normal=Client(host.Url);Assert.Equal(HttpStatusCode.OK,(await Login(normal,pass)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await normal.GetAsync("/api/probe")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await normal.GetAsync("/api/admin/benutzer")).StatusCode);
            await Csrf(normal);Assert.Equal(HttpStatusCode.OK,(await normal.PostAsJsonAsync("/api/auth/logout",new{})).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,(await normal.GetAsync("/api/probe")).StatusCode);
            await Login(normal,pass);await Csrf(normal);
            await using(var db=new FirmenDbContext(options)){db.MfaRegeln.Add(new(){Mitarbeiter=true});await db.SaveChangesAsync();}
            Assert.Equal(HttpStatusCode.Forbidden,(await normal.GetAsync("/api/probe")).StatusCode);
            Assert.True((await normal.GetFromJsonAsync<JsonElement>("/api/auth/session")).GetProperty("mfaEinrichten").GetBoolean());
            Assert.Equal(HttpStatusCode.Forbidden,(await normal.PostAsJsonAsync("/api/auth/passwort",new{bisherigesPasswort=pass,neuesPasswort=MfaTests.Secret()})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await normal.GetAsync("/api/auth/mfa/status")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await normal.GetAsync("/api/admin/benutzer")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await normal.PostAsJsonAsync("/api/auth/mfa/vorbereiten",new{passwort=pass})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await normal.PostAsJsonAsync("/api/auth/mfa/aktivieren",new{passwort=pass,code=await AppCode(host.App)})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await normal.GetAsync("/api/probe")).StatusCode);
            Assert.False((await normal.GetFromJsonAsync<JsonElement>("/api/auth/session")).GetProperty("mfaEinrichten").GetBoolean());
            await Csrf(normal);
            var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.Now,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")).DateTime);
            var taskInput=new Aufgabeneingabe("Synthetischer Rückruf","Wiedervorlage",id,"Normal","Offen",today.AddDays(-1),null);
            var created=await normal.PostAsJsonAsync("/api/aufgaben/",taskInput);Assert.Equal(HttpStatusCode.OK,created.StatusCode);
            var task=(await created.Content.ReadFromJsonAsync<Aufgabe>())!;
            var list=await normal.GetFromJsonAsync<JsonElement>("/api/aufgaben/?ansicht=ueberfaellig&seite=0");Assert.Equal(1,list.GetProperty("anzahl").GetInt32());
            Assert.Equal(1,(await normal.GetFromJsonAsync<JsonElement>("/api/aufgaben/meine-uebersicht")).GetProperty("ueberfaellig").GetInt32());
            Assert.Equal(HttpStatusCode.OK,(await normal.PostAsJsonAsync("/api/aufgaben/"+task.Id,taskInput with{Version=task.Version,Status="Erledigt"})).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,(await normal.PostAsJsonAsync("/api/aufgaben/"+task.Id,taskInput with{Version=task.Version})).StatusCode);
            Assert.Equal(1,(await normal.GetFromJsonAsync<JsonElement>("/api/aufgaben/?ansicht=erledigt&seite=0")).GetProperty("anzahl").GetInt32());
            Assert.Equal(0,(await normal.GetFromJsonAsync<JsonElement>("/api/aufgaben/meine-uebersicht")).GetProperty("offen").GetInt32());

            // Independent administration flow on a fresh limiter window; persistent data/keyring remain.
            await host.App.StopAsync();await host.App.DisposeAsync();host=await Start();
            using var nachNeustart=Client(host.Url);await Login(nachNeustart,pass);await Csrf(nachNeustart);clock.Next();
            Assert.Equal(HttpStatusCode.OK,(await nachNeustart.PostAsJsonAsync("/api/auth/mfa-anmelden",new{code=await AppCode(host.App)})).StatusCode);await Csrf(nachNeustart);
            using var admin=Client(host.Url);await Csrf(admin);Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync("/api/auth/login",new{login="admin",passwort=adminPass})).StatusCode);await Csrf(admin);
            Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync("/api/auth/mfa/vorbereiten",new{passwort=adminPass})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync("/api/auth/mfa/aktivieren",new{passwort=adminPass,code=await AppCode(host.App,userId:adminId)})).StatusCode);await Csrf(admin);
            clock.Next();Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync("/api/admin/mfa/richtlinie",new MfaRegelEingabe(true,true,1,new(adminPass,await AppCode(host.App,userId:adminId))))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await nachNeustart.GetAsync("/api/admin/mfa/notfaelle/")).StatusCode);
            int targetVersion;await using(var db=new FirmenDbContext(options))targetVersion=(await db.Benutzer.SingleAsync(x=>x.Id==id)).Version;
            clock.Next();var notfall=await admin.PostAsJsonAsync("/api/admin/mfa/notfaelle/",new MfaNotfallantrag(id,"Persönlich geprüfter synthetischer Identitätsnachweis",true,targetVersion,new(adminPass,await AppCode(host.App,userId:adminId))));Assert.Equal(HttpStatusCode.OK,notfall.StatusCode);var wiederherstellung=(await notfall.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("zugang").GetProperty("code").GetString();
            Assert.Equal(HttpStatusCode.Unauthorized,(await nachNeustart.GetAsync("/api/aufgaben/")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,(await Login(nachNeustart,pass)).StatusCode);await Csrf(nachNeustart);var neuesPasswort=MfaTests.Secret();Assert.Equal(HttpStatusCode.OK,(await nachNeustart.PostAsJsonAsync("/api/auth/wiederherstellen",new{code=wiederherstellung,passwort=neuesPasswort})).StatusCode);Assert.Equal(HttpStatusCode.OK,(await Login(nachNeustart,neuesPasswort)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await nachNeustart.GetAsync("/api/aufgaben/")).StatusCode);


        }finally{await host.App.StopAsync();await host.App.DisposeAsync();}
    }
}
