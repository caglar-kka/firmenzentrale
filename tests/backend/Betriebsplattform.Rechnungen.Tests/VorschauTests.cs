using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Betriebsplattform.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class VorschauTests
{
    private static string[] Args(string env="Development",string provider="Demo")=>["--environment="+env,"--FirmenZentrale:Startmodus=Vorschau","--FirmenZentrale:Persistence:Provider="+provider,"--FirmenZentrale:Security:Enabled=false","--urls=http://0.0.0.0:9999"];
    [Fact] public async Task ExpliziteVorschauBleibtLoopbackOhneDbUndSperrtEchteKontenApis()
    {
        var frontend=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../src/frontend/dist"));
        await using var app=ApiAnwendung.Erstellen(Args(),0,frontend);await app.StartAsync();
        try{
            var addresses=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
            Assert.Single(addresses);var url=new Uri(addresses.Single());Assert.Equal("127.0.0.1",url.Host);
            Assert.Null(app.Services.GetService<Betriebsplattform.Web.Persistenz.FirmenDbContext>());
            using var c=new HttpClient{BaseAddress=url};var s=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");Assert.True(s.GetProperty("vorschau").GetBoolean());Assert.False(s.GetProperty("aktiv").GetBoolean());
            Assert.Equal(await File.ReadAllTextAsync(Path.Combine(frontend,"index.html")),await c.GetStringAsync("/"));
            foreach(var path in new[]{"/api/uebersicht","/api/kontakte","/api/akten","/api/posteingang","/api/importe"})Assert.True((await c.GetAsync(path)).IsSuccessStatusCode,path);
            foreach(var path in new[]{"/api/admin/benutzer","/api/auth/mfa/status","/api/aufgaben/"})Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(path)).StatusCode);
            using var foreign=new HttpRequestMessage(HttpMethod.Get,"/");foreign.Headers.Host="example.invalid";Assert.Equal(HttpStatusCode.Forbidden,(await c.SendAsync(foreign)).StatusCode);
        }finally{await app.StopAsync();}
    }
    [Fact] public void VorschauVerweigertProductionUndPostgreSql()
    {Assert.Throws<InvalidOperationException>(()=>ApiAnwendung.Erstellen(Args("Production"),0));Assert.Throws<InvalidOperationException>(()=>ApiAnwendung.Erstellen(Args(provider:"PostgreSql"),0));}
}
