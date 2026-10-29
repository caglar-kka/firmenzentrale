using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Betriebsplattform.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class StartintegrationTests
{
    [Theory]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    public async Task HostneustartVerwirftNurLokaleDevelopmentSitzung(string environment,bool bleibtAngemeldet)
    {
        Assert.True(OperatingSystem.IsWindows());
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();
        var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;
        var pass=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using(var db=new FirmenDbContext(options)){await db.Database.EnsureCreatedAsync();await Anmeldung.Anlegen(db,Anmeldung.Passwortdienst(),new("team","Synthetisches Team",pass,"Mitarbeiter"));}
        using var rsa=RSA.Create(2048);var req=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var generated=req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));
        using var cert=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12),null,X509KeyStorageFlags.UserKeySet);
        var root=Path.Combine(Path.GetTempPath(),"MBOP-Sitzungstest",Guid.NewGuid().ToString("N"));
        async Task<(WebApplication App,Uri Url)> Start(){
            var b=WebApplication.CreateBuilder(new WebApplicationOptions{EnvironmentName=environment});b.Logging.ClearProviders();
            b.WebHost.ConfigureKestrel(o=>o.Listen(IPAddress.Loopback,0,l=>l.UseHttps(cert)));b.Services.AddDbContext<FirmenDbContext>(o=>o.UseSqlite(cn));
            var p=new Pilotoptionen(true,null,null);Anmeldung.Dienste(b,p,root);
            var app=b.Build();Webschutz.Konfigurieren(app,new Arbeitsmodule());Anmeldung.Pipeline(app,p);Anmeldung.Endpunkte(app,p);
            app.MapGet("/api/probe",()=>Results.Ok()).RequireAuthorization();await app.StartAsync();
            return(app,new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()));
        }
        var cookies=new CookieContainer();
        HttpClient Client(Uri url){var c=new HttpClient(new HttpClientHandler{CookieContainer=cookies,ServerCertificateCustomValidationCallback=(_,s,_,_)=>s?.GetCertHashString()==cert.GetCertHashString()}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
        async Task Csrf(HttpClient c){var s=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",s.GetProperty("csrf").GetString());}
        var host=await Start();
        try{
            using var c=Client(host.Url);await Csrf(c);Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/probe")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/login",new{login="team",passwort=pass})).StatusCode);
            Assert.False((await c.GetFromJsonAsync<JsonElement>("/api/auth/session")).GetProperty("admin").GetBoolean());
            for(var i=0;i<3;i++)Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/probe")).StatusCode);
            Assert.True((await c.GetFromJsonAsync<JsonElement>("/api/auth/session")).GetProperty("angemeldet").GetBoolean());
        }finally{await host.App.StopAsync();await host.App.DisposeAsync();}
        // Same protected keyring, browser cookies, account and database. Only the host instance changes.
        host=await Start();
        try{
            using var c=Client(host.Url);var s=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");Assert.Equal(bleibtAngemeldet,s.GetProperty("angemeldet").GetBoolean());
            Assert.Equal(bleibtAngemeldet?HttpStatusCode.OK:HttpStatusCode.Unauthorized,(await c.GetAsync("/api/probe")).StatusCode);
            await Csrf(c);
            if(!bleibtAngemeldet){Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/auth/login",new{login="team",passwort="falsch"})).StatusCode);await Csrf(c);Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/login",new{login="team",passwort=pass})).StatusCode);await Csrf(c);}
            Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/probe")).StatusCode);
            // The shared session-creation path must also retain the host binding after password/MFA renewal.
            Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/passwort",new{bisherigesPasswort=pass,neuesPasswort=Convert.ToHexString(RandomNumberGenerator.GetBytes(32))})).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/probe")).StatusCode);await Csrf(c);
            Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/logout",new{})).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/probe")).StatusCode);
        }finally{await host.App.StopAsync();await host.App.DisposeAsync();}
    }
    [Fact]
    public async Task NormalstartOhneSicherheitskonfigurationLiefertAktuelleAssetsAberKeineDemoApis()
    {
        var args=Startmodus.Argumente(["--FirmenZentrale:Persistence:Provider=Demo","--FirmenZentrale:Security:Enabled=false"]);
        Assert.Contains("--FirmenZentrale:Startmodus=Anmeldung",args);
        var frontend=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../src/frontend/dist"));
        Assert.True(File.Exists(Path.Combine(frontend,"index.html")));
        await using var app=ApiAnwendung.Erstellen(args,0,frontend);
        await app.StartAsync();
        try
        {
            using var client=new HttpClient{BaseAddress=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single())};
            var html=await client.GetStringAsync("/");
            Assert.Equal(await File.ReadAllTextAsync(Path.Combine(frontend,"index.html")),html);
            var asset=System.Text.RegularExpressions.Regex.Match(html,"src=\"([^\"]+\\.js)\"").Groups[1].Value;
            Assert.NotEmpty(asset);
            Assert.Equal(await File.ReadAllTextAsync(Path.Combine(frontend,asset.TrimStart('/'))),await client.GetStringAsync(asset));
            var session=await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
            Assert.True(session.GetProperty("aktiv").GetBoolean());Assert.False(session.GetProperty("bereit").GetBoolean());
            foreach(var path in new[]{"/api/uebersicht","/api/akten","/api/admin/benutzer","/api/auth/login"})
                Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/assets/veraltet.js")).StatusCode);
            Assert.Equal(html,await client.GetStringAsync("/profil/sicherheit"));
        }
        finally{await app.StopAsync();}
    }

    [Fact]
    public void DemoIstExplizitUndKannSicherheitskonfigurationNichtUebergehen()
    {
        Assert.Contains("--FirmenZentrale:Startmodus=Anmeldung",Startmodus.Argumente(["--FirmenZentrale:StartmodusTippfehler=Demo"]));
        var args=Startmodus.Argumente(["--FirmenZentrale:Startmodus=Demo","--FirmenZentrale:Security:Enabled=true","--FirmenZentrale:Persistence:Provider=Demo"]);
        Assert.DoesNotContain("--FirmenZentrale:Startmodus=Anmeldung",args);
        Assert.Throws<InvalidOperationException>(()=>ApiAnwendung.Erstellen(args,0));
    }
}
