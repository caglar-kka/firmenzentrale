using Betriebsplattform.Web.Betrieb;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Betriebsplattform.Web.Akten;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class LokalerAdminTests {
 [Fact] public async Task ErstzugangIstEinmaligMfaGebundenAblaufendUndNachAbbruchErneuerbar(){
 await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();var clock=new MfaTests.Clock();
 var first=await LokalerAdminErstzugang.Erstellen(db,h,clock);var u=await db.Benutzer.SingleAsync();Assert.Equal("admin",u.Login);Assert.True(u.MfaNeueinrichtung);Assert.Null(u.MfaGeheimnis);Assert.Equal(48,first.Passwort.Length);Assert.True(h.VerifyHashedPassword(u,u.PasswortHash,first.Passwort)!=PasswordVerificationResult.Failed);Assert.DoesNotContain(first.Passwort,first.ToString());Assert.DoesNotContain(first.Passwort,System.Text.Json.JsonSerializer.Serialize(first));
 var stamp=u.Sicherheitskennung;var second=await LokalerAdminErstzugang.Erstellen(db,h,clock);Assert.Single(await db.Benutzer.ToArrayAsync());Assert.True(first.Passwort!=second.Passwort);Assert.True(stamp!=u.Sicherheitskennung);Assert.True(h.VerifyHashedPassword(u,u.PasswortHash,first.Passwort)==PasswordVerificationResult.Failed);
 clock.Now=clock.Now.AddHours(2);Assert.True(LokalerAdminErstzugang.Abgelaufen(u,clock.Now));Assert.Equal(2,await db.Sicherheitsereignisse.CountAsync(x=>x.Aktion==LokalerAdminErstzugang.Marker));
 u.MfaGeheimnis="bereits-eingerichtet";u.Version++;await db.SaveChangesAsync();Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>LokalerAdminErstzugang.Erstellen(db,h,clock))).Status);
 }
 [Theory][InlineData("Administrator")][InlineData("Mitarbeiter")] public async Task BestehendesKontoOhneErstzugangMarkerWirdNieUebernommen(string rolle){
 await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();var u=await Anmeldung.Anlegen(db,h,new("admin","Vorhanden",MfaTests.Secret(),rolle));var hash=u.PasswortHash;
 Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>LokalerAdminErstzugang.Erstellen(db,h,TimeProvider.System))).Status);Assert.True(hash==u.PasswortHash);Assert.Empty(await db.Sicherheitsereignisse.Where(x=>x.Aktion==LokalerAdminErstzugang.Marker).ToArrayAsync());
 }
 [Theory][InlineData("FirmenZentrale")][InlineData("postgres")][InlineData("fz_browser_test_../main")][InlineData("")] public void HauptdatenbankUndFremdeProfileWerdenAbgewiesen(string name)=>Assert.Throws<InvalidOperationException>(()=>IsolierteArbeitsumgebung.Datenbankname(name));
}
