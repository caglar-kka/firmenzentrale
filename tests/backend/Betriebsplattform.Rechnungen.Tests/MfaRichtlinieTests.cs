using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class MfaRichtlinieTests
{
    [Fact] public async Task StarkeBerechtigungVorbereitungRollenwechselUndDeaktivierungssperre()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();
        var h=Anmeldung.Passwortdienst();var pass=MfaTests.Secret();var admin=await Anmeldung.Anlegen(db,h,new("admin","Admin",pass,"Administrator"));var team=await Anmeldung.Anlegen(db,h,new("team","Team",pass,"Mitarbeiter"));
        var p=new EphemeralDataProtectionProvider();var clock=new MfaTests.Clock();var m=new MfaDienst(p,clock);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>MfaRichtlinie.Aendern(db,admin.Id,new(true,true,0,new(pass,"123456")),m,h));
        Assert.Empty(await db.MfaRegeln.ToArrayAsync());
        await m.Vorbereiten(db,h,admin.Id,new(pass,null));await m.Verwalten(db,h,admin.Id,new(pass,MfaTests.Code(p,admin,clock)),"aktivieren");clock.Next();
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>MfaRichtlinie.Aendern(db,team.Id,new(true,true,0,new(pass,"123456")),m,h));
        await MfaRichtlinie.Aendern(db,admin.Id,new(true,true,0,new(pass,MfaTests.Code(p,admin,clock))),m,h);
        Assert.True(await MfaRichtlinie.Einrichtung(db,team.Id));Assert.False(await MfaRichtlinie.Einrichtung(db,admin.Id));
        clock.Next();await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,admin.Id,new(pass,MfaTests.Code(p,admin,clock)),"deaktivieren"));Assert.NotNull(admin.MfaGeheimnis);
        var rule=await db.MfaRegeln.SingleAsync();rule.Mitarbeiter=false;await db.SaveChangesAsync();Assert.False(await MfaRichtlinie.Einrichtung(db,team.Id));team.Rolle="Administrator";await db.SaveChangesAsync();Assert.True(await MfaRichtlinie.Einrichtung(db,team.Id));
    }
}
