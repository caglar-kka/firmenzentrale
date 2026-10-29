using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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
public sealed class AufgabenListenTests
{
    // Only this isolated test host uses a synthetic identity. No application authentication is replaced.
    private sealed class Identitaet(IOptionsMonitor<AuthenticationSchemeOptions> o,ILoggerFactory l,UrlEncoder e):AuthenticationHandler<AuthenticationSchemeOptions>(o,l,e)
    {protected override Task<AuthenticateResult> HandleAuthenticateAsync()=>Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"synthetisch")],Scheme.Name)),Scheme.Name)));}
    [Fact] public async Task EchteAufgabenEndpunkteFilternSortierenUndPaginierenMitKalendergrenzen()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;
        var clock=new MfaTests.Clock{Now=new(2026,10,19,10,0,0,TimeSpan.Zero)};var zone=TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        await using(var db=new FirmenDbContext(options)){
            await db.Database.EnsureCreatedAsync();db.Benutzer.AddRange(new Benutzerkonto{Id="synthetisch",Login="team",Anzeigename="Team",PasswortHash="synthetisch"},new Benutzerkonto{Id="fremd",Login="fremd",Anzeigename="Fremd",PasswortHash="synthetisch"});await db.SaveChangesAsync();
            var d=new AufgabenDienst(db,clock,zone);var input=new Aufgabeneingabe("Heute","", "synthetisch","Normal","Offen",d.Heute,null);
            await d.Speichern("synthetisch",null,input);await d.Speichern("synthetisch",null,input with{Betreff="Uhrzeit",FaelligUm=new(11,0),Prioritaet="Hoch"});await d.Speichern("synthetisch",null,input with{Betreff="Morgen",FaelligAm=d.Heute.AddDays(1)});await d.Speichern("synthetisch",null,input with{Betreff="Archiv",Archiviert=true});await d.Speichern("synthetisch",null,input with{Betreff="Fertig",Status="Erledigt"});await d.Speichern("fremd",null,input with{Betreff="Verborgene Aufgabe",ZustaendigId="fremd"});
            for(var i=0;i<20;i++)await d.Speichern("synthetisch",null,input with{Betreff="Ohne Termin "+i,FaelligAm=null});
        }
        var b=WebApplication.CreateBuilder();b.Logging.ClearProviders();b.WebHost.ConfigureKestrel(k=>k.Listen(IPAddress.Loopback,0));b.Services.AddDbContext<FirmenDbContext>(o=>o.UseSqlite(cn));b.Services.AddSingleton<TimeProvider>(clock);b.Services.AddSingleton(zone);b.Services.AddScoped<AufgabenDienst>();b.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions,Identitaet>("fixture",_=>{});b.Services.AddAuthorization();await using var app=b.Build();app.UseAuthentication();app.UseAuthorization();AufgabenEndpunkte.Registrieren(app);await app.StartAsync();
        try{
            using var c=new HttpClient{BaseAddress=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single())};
            async Task<JsonElement> List(string q)=>await c.GetFromJsonAsync<JsonElement>("/api/aufgaben/?"+q);
            Assert.Equal(2,(await List("ansicht=heute")).GetProperty("anzahl").GetInt32());Assert.Equal(1,(await List("ansicht=ueberfaellig")).GetProperty("anzahl").GetInt32());Assert.Equal(1,(await List("ansicht=morgen")).GetProperty("anzahl").GetInt32());Assert.Equal(1,(await List("ansicht=zukuenftig")).GetProperty("anzahl").GetInt32());Assert.Equal(1,(await List("ansicht=erledigt")).GetProperty("anzahl").GetInt32());Assert.Equal(1,(await List("ansicht=archiv")).GetProperty("anzahl").GetInt32());
            Assert.Equal(1,(await List("prioritaet=Hoch&status=Offen&text=Uhrzeit&zustaendig=synthetisch")).GetProperty("anzahl").GetInt32());
            var all=await List("ansicht=alle&sort=faellig");Assert.Equal(24,all.GetProperty("anzahl").GetInt32());Assert.Equal(20,all.GetProperty("eintraege").GetArrayLength());Assert.Equal("Uhrzeit",all.GetProperty("eintraege")[0].GetProperty("betreff").GetString());Assert.DoesNotContain("Verborgene",all.ToString());Assert.Equal(4,(await List("ansicht=alle&seite=1")).GetProperty("eintraege").GetArrayLength());Assert.Equal(24,(await List("ansicht=alle&sort=neu")).GetProperty("anzahl").GetInt32());
        }finally{await app.StopAsync();}
    }
}
