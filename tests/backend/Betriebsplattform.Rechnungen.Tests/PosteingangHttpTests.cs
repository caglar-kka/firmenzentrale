using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class PosteingangHttpTests
{
    private sealed class Identitaet(IOptionsMonitor<AuthenticationSchemeOptions> o,ILoggerFactory l,UrlEncoder e):AuthenticationHandler<AuthenticationSchemeOptions>(o,l,e)
    {protected override Task<AuthenticateResult> HandleAuthenticateAsync(){var id=Request.Headers["X-Testkonto"].FirstOrDefault();return Task.FromResult(id is null?AuthenticateResult.NoResult():AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,id)],Scheme.Name)),Scheme.Name)));}}
    [Fact] public async Task ProduktendpunkteVerlangenAnmeldungRessourcenrechtUndExpliziteEntscheidung()
    {
        await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options;
        var u=new Benutzerkonto{Id="team",Login="team",Anzeigename="Synthetisches Team",PasswortHash="synthetisch"};var f=new Benutzerkonto{Id="fremd",Login="fremd",Anzeigename="Fremd",PasswortHash="synthetisch"};var d=new DokumentDatensatz{Titel="Unklar",Dateiname="unklar.txt",Inhaltstyp="text/plain",Groesse=16,Hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("MBOP-SYNTHETISCH"))),StorageKey=Guid.NewGuid().ToString("N"),Text="MBOP-SYNTHETISCH",Vorschau="text"};var e=new DokumentaktivitaetDatensatz{DokumentId=d.Id,Kanal="Upload",Text="Dokument eingegangen",Benutzer="Team"};
        await using(var db=new FirmenDbContext(options)){await db.Database.EnsureCreatedAsync();db.AddRange(u,f,d,e);db.Freigaben.Add(new(){BenutzerId=u.Id,DokumentId=d.Id});await db.SaveChangesAsync();}
        var b=WebApplication.CreateBuilder();b.Logging.ClearProviders();b.WebHost.ConfigureKestrel(k=>k.Listen(IPAddress.Loopback,0));b.Services.AddDbContext<FirmenDbContext>(o=>o.UseSqlite(c));b.Services.AddSingleton<TimeProvider>(new MfaTests.Clock());b.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));b.Services.AddScoped<AufgabenDienst>();b.Services.AddScoped<EingangsDienst>();b.Services.AddSingleton<IDokumentdateien>(new PosteingangTests.Dateien());b.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions,Identitaet>("fixture",_=>{});b.Services.AddAuthorization();await using var app=b.Build();app.UseAuthentication();app.UseAuthorization();EingangsDienst.Endpunkte(app);await app.StartAsync();
        try{
            using var client=new HttpClient{BaseAddress=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single())};
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/posteingang/")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/posteingang/"+e.Id+"/aufgabenvorschlag")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/posteingang/scanner")).StatusCode);client.DefaultRequestHeaders.Add("X-Testkonto",f.Id);Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/posteingang/"+e.Id)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/posteingang/"+e.Id+"/aufgabenvorschlag")).StatusCode);Assert.Equal(0,(await client.GetFromJsonAsync<JsonElement>("/api/posteingang/?sicht=alle")).GetProperty("anzahl").GetInt32());client.DefaultRequestHeaders.Remove("X-Testkonto");client.DefaultRequestHeaders.Add("X-Testkonto",u.Id);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/posteingang/scanner")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/posteingang/scanordner")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/posteingang/"+e.Id)).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/posteingang/"+e.Id+"/aufgabe",new Eingangsaufgabe(0,true,true,null!))).StatusCode);var p=new Eingangspruefung(0,"Zur Prüfung zurückstellen",new("Unklar","Sonstiges"),null,null,u.Id,"Absender und Akte noch offen","Bitte nachfragen",false);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/posteingang/"+e.Id+"/pruefen",p)).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/posteingang/"+e.Id+"/pruefen",p with{Bestaetigt=true})).StatusCode);
            var result=await client.GetFromJsonAsync<JsonElement>("/api/posteingang/"+e.Id);Assert.Equal("Zur Prüfung",result.GetProperty("eingang").GetProperty("status").GetString());Assert.Equal(1,result.GetProperty("historieAnzahl").GetInt32());
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/posteingang/"+e.Id+"/aufgabe",new Eingangsaufgabe(1,true,true,new("Nicht ungeprüft anlegen","",u.Id,"Normal","Offen",null,null)))).StatusCode);
        }finally{await app.StopAsync();}
    }
}
