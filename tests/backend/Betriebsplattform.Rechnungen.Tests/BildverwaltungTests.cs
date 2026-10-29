using Betriebsplattform.Web.Akten;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Betriebsplattform.Web;
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
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class BildverwaltungTests
{
    [Theory][InlineData(-1)][InlineData(99)]
    public void UnbekanntesScannerergebnisBleibtUnbestimmt(int status)
    {
        var hash=new string('A',64);var b=new Malwarebefund("Fiktiver Testadapter","test-1","test-signaturen",(Malwarestatus)status,hash,DateTimeOffset.UtcNow,"Kontrolliertes Testdouble");
        Assert.Equal(Malwarestatus.ResultIndeterminate,b.Bewerten(hash,hash));
    }

    [Theory]
    [InlineData(Malwarestatus.CleanVerified)][InlineData(Malwarestatus.ThreatDetected)][InlineData(Malwarestatus.SuspiciousOrPolicyBlocked)][InlineData(Malwarestatus.ScanUnavailable)][InlineData(Malwarestatus.ScanFailed)][InlineData(Malwarestatus.TimedOut)][InlineData(Malwarestatus.ResultIndeterminate)]
    public void ScannervertragVerlangtHashUndBelegUndHebtSperrenNichtAuf(Malwarestatus status)
    {
        var hash=new string('A',64);var b=new Malwarebefund("Fiktiver Testadapter","test-1","test-signaturen",status,hash,DateTimeOffset.UtcNow,"Kontrolliertes Testdouble, keine echte Inferenz oder Scannerprüfung");
        Assert.Equal(status,b.Bewerten(hash,hash));Assert.Equal(Malwarestatus.FileChangedAfterScan,b.Bewerten(hash,new string('B',64)));
        Assert.Equal(Malwarestatus.ResultIndeterminate,(b with{Status=Malwarestatus.CleanVerified,Ergebnisquelle=""}).Bewerten(hash,hash));
    }

 sealed class FreigebenderTestscanner:IMalwarepruefung {public int Aufrufe;public Task Freigeben(byte[] b,CancellationToken ct){Aufrufe++;return Task.CompletedTask;}}
 sealed class KeinLesen(Stream inner):Stream
 {
  public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
  public override int Read(byte[] b,int o,int c)=>throw new InvalidOperationException("Bildbody darf bei gesperrtem Gate nicht gelesen werden.");
  public override ValueTask<int> ReadAsync(Memory<byte> b,CancellationToken ct=default)=>throw new InvalidOperationException("Gesperrten Bildbody gelesen.");
  public override Task<int> ReadAsync(byte[] b,int o,int c,CancellationToken ct)=>throw new InvalidOperationException("Gesperrten Bildbody gelesen.");
  public override void Flush()=>inner.Flush();public override long Seek(long o,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
 }
 [Fact] public void KeineKonfigurationKannSperreOeffnen(){var status=JsonSerializer.SerializeToElement(Bildverwaltung.Status(true));Assert.False(status.GetProperty("uploadAktiv").GetBoolean());Assert.False(status.GetProperty("bildVorhanden").GetBoolean());Assert.Contains("Neuencodierung",status.GetProperty("grund").GetString());Assert.Equal(2_000_000,status.GetProperty("maxBytes").GetInt64());}
 [Fact] public async Task EchteHttpsSperreRechteBodygrenzenUndNullSchreibzugriffe()
 {
  var root=Path.Combine(Path.GetTempPath(),"MBOP-Bildgate-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;var pass=MfaTests.Secret();string team;
  await using(var db=new FirmenDbContext(options)){await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();await Anmeldung.Anlegen(db,h,new("admin","Fiktive Administration",pass,"Administrator"),true);team=(await Anmeldung.Anlegen(db,h,new("team","Fiktives Team",pass,"Mitarbeiter"))).Id;}
  using var rsa=RSA.Create(2048);var request=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));using var cert=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pkcs12),null,X509KeyStorageFlags.UserKeySet);
  var b=WebApplication.CreateBuilder();b.Logging.ClearProviders();b.WebHost.ConfigureKestrel(o=>o.Listen(IPAddress.Loopback,0,l=>l.UseHttps(cert)));b.Services.AddDbContext<FirmenDbContext>(o=>o.UseSqlite(cn));var pilot=new Pilotoptionen(true,null,null);Anmeldung.Dienste(b,pilot,root);var scanner=new FreigebenderTestscanner();b.Services.AddSingleton<IMalwarepruefung>(scanner);
  await using var app=b.Build();app.Use(async(c,next)=>{if(c.Request.Path.StartsWithSegments("/api/bildverwaltung")&&c.Request.Method=="POST")c.Request.Body=new KeinLesen(c.Request.Body);await next();});Webschutz.Konfigurieren(app,new Arbeitsmodule());Anmeldung.Pipeline(app,pilot);Anmeldung.Endpunkte(app,pilot);Bildverwaltung.Endpunkte(app);await app.StartAsync();var url=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
  HttpClient Client(){var c=new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=(_,s,_,_)=>s?.GetCertHashString()==cert.GetCertHashString()}){BaseAddress=url};c.DefaultRequestHeaders.Add("Origin",url.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return c;}
  async Task Csrf(HttpClient c){var s=await c.GetFromJsonAsync<JsonElement>("/api/auth/session");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",s.GetProperty("csrf").GetString());}
  async Task Login(HttpClient c,string login){await Csrf(c);Assert.Equal(200,(int)(await c.PostAsJsonAsync("/api/auth/login",new{login,passwort=pass})).StatusCode);await Csrf(c);}
  async Task<int> Send(HttpClient c,string path,byte[] bytes,string mime="application/octet-stream",string filename="../fremd.svg"){using var r=new HttpRequestMessage(HttpMethod.Post,path){Content=new ByteArrayContent(bytes)};if(bytes.Length>Bildverwaltung.MaxBytes)r.Headers.ExpectContinue=true;r.Content.Headers.ContentType=new(mime);r.Headers.Add("X-Dateiname",filename);using var result=await c.SendAsync(r);return(int)result.StatusCode;}
  try{
   using var a=Client();using var t=Client();using var anon=Client();Assert.Equal(401,(int)(await anon.GetAsync("/api/bildverwaltung/status")).StatusCode);Assert.Equal(401,await Send(anon,"/api/bildverwaltung/profil",[1,2,3]));await Login(a,"admin");await Login(t,"team");
   var status=await t.GetFromJsonAsync<JsonElement>("/api/bildverwaltung/status");Assert.False(status.GetProperty("uploadAktiv").GetBoolean());Assert.False(status.GetProperty("firmenlogoVerwalten").GetBoolean());Assert.True((await a.GetFromJsonAsync<JsonElement>("/api/bildverwaltung/status")).GetProperty("firmenlogoVerwalten").GetBoolean());
   int audits;await using(var db=new FirmenDbContext(options))audits=await db.Sicherheitsereignisse.CountAsync();var dateien=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Order().ToArray();
   // Complete tiny PNG, MIME spoof, active contents, truncation, fake giant IHDR,
   // and polyglot: all rejected BEFORE decoding, never described as a safe parser.
   var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a/1sAAAAASUVORK5CYII=");
   var cases=new[]{png,png[..16],Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>"),Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"),new byte[]{0xff,0xd8,0xff,0xe1,0,12,1,2,3},png.Concat(Encoding.UTF8.GetBytes("<script>active</script>")).ToArray()};var bomb=(byte[])png.Clone();Array.Fill(bomb,(byte)0x7f,16,8);
   foreach(var bytes in cases.Append(bomb))Assert.Equal(503,await Send(t,"/api/bildverwaltung/profil",bytes));
   Assert.Equal(503,await Send(a,"/api/bildverwaltung/firmenlogo",png,"image/jpeg","photo.jpg"));
   Assert.Equal(413,await Send(a,"/api/bildverwaltung/firmenlogo",new byte[2_000_001]));
   Assert.Equal(403,await Send(t,"/api/bildverwaltung/firmenlogo",png));Assert.Equal(400,await Send(t,"/api/bildverwaltung/profil?benutzerId=fremd",png));Assert.Equal(400,await Send(a,"/api/bildverwaltung/firmenlogo?firmaId=andere-firma",png));Assert.Equal(400,(int)(await a.GetAsync("/api/bildverwaltung/firmenlogo/datei?pfad=..%2Fgeheim")).StatusCode);
   Assert.Equal(404,(int)(await t.GetAsync("/api/bildverwaltung/profil/fremder-benutzer/datei")).StatusCode);Assert.Equal(404,(int)(await t.GetAsync("/api/bildverwaltung/profil/datei")).StatusCode);Assert.Equal(404,(int)(await t.GetAsync("/api/bildverwaltung/firmenlogo/anderer-tenant/datei")).StatusCode);
   a.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(400,await Send(a,"/api/bildverwaltung/firmenlogo",png));await Csrf(a);var parallel=await Task.WhenAll(Send(a,"/api/bildverwaltung/firmenlogo",png),Send(a,"/api/bildverwaltung/firmenlogo",png));Assert.All(parallel,s=>Assert.Equal(503,s));
   Assert.Equal(503,(int)(await t.PostAsJsonAsync("/api/bildverwaltung/profil/entfernen",new{})).StatusCode);Assert.Equal(503,(int)(await a.PostAsJsonAsync("/api/bildverwaltung/firmenlogo/entfernen",new{})).StatusCode);
   var statuses=new List<int>();for(var i=0;i<22;i++){var code=await Send(a,"/api/bildverwaltung/firmenlogo",png);statuses.Add(code);if(code==429)break;}Assert.Contains(429,statuses);
   await using(var db=new FirmenDbContext(options)){Assert.Equal(audits,await db.Sicherheitsereignisse.CountAsync());Assert.Empty(await db.Dokumente.ToArrayAsync());Assert.Empty(await db.Termine.ToArrayAsync());(await db.Benutzer.SingleAsync(u=>u.Id==team)).Aktiv=false;await db.SaveChangesAsync();}
   Assert.Equal(401,(int)(await t.GetAsync("/api/bildverwaltung/status")).StatusCode);Assert.Equal(0,scanner.Aufrufe);Assert.Equal(dateien,Directory.GetFiles(root,"*",SearchOption.AllDirectories).Order().ToArray());
  }finally{await app.StopAsync();}
 }
}
