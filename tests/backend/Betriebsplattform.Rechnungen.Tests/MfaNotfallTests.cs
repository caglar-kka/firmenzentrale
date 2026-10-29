using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class MfaNotfallTests
{
    [Fact] public async Task MitarbeiterResetUndAdministratorVierAugenSindGebundenEinmaligUndBefristet()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();
        var h=Anmeldung.Passwortdienst();var pass=MfaTests.Secret();var p=new EphemeralDataProtectionProvider();var clock=new MfaTests.Clock();var m=new MfaDienst(p,clock);
        async Task<Benutzerkonto> User(string login,string rolle){var u=await Anmeldung.Anlegen(db,h,new(login,login,pass,rolle));await m.Vorbereiten(db,h,u.Id,new(pass,null));await m.Verwalten(db,h,u.Id,new(pass,MfaTests.Code(p,u,clock)),"aktivieren");return u;}
        var a=await User("admin-a","Administrator");var b=await User("admin-b","Administrator");var ziel=await User("admin-ziel","Administrator");var team=await User("team","Mitarbeiter");
        MfaPruefung Proof(Benutzerkonto u){clock.Next();return new(pass,MfaTests.Code(p,u,clock));}
        const string grund="Persönliche betriebliche Identitätsprüfung durchgeführt";
        MfaNotfallantrag Antrag(Benutzerkonto u,MfaPruefung proof)=>new(u.Id,grund,true,u.Version,proof);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>MfaNotfall.Antrag(db,m,h,clock,team.Id,Antrag(ziel,Proof(team))));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>MfaNotfall.Antrag(db,m,h,clock,a.Id,Antrag(a,Proof(a))));
        var stamp=team.Sicherheitskennung;var employee=await MfaNotfall.Antrag(db,m,h,clock,a.Id,Antrag(team,Proof(a)));
        Assert.NotNull(employee.AusgefuehrtAm);Assert.NotNull(employee.Zugang);Assert.Equal(Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed,h.VerifyHashedPassword(team,team.PasswortHash,pass));Assert.Null(team.MfaGeheimnis);Assert.Null(team.MfaRecoveryHashes);Assert.NotEqual(stamp,team.Sicherheitskennung);Assert.True(await MfaRichtlinie.Einrichtung(db,team.Id));
        var req=await MfaNotfall.Antrag(db,m,h,clock,a.Id,Antrag(ziel,Proof(a)));Assert.Null(req.AusgefuehrtAm);Assert.NotNull(ziel.MfaGeheimnis);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>MfaNotfall.Aktion(db,m,h,clock,a.Id,req.Id,new(req.Version,Proof(a)),false));
        await MfaNotfall.Aktion(db,m,h,clock,b.Id,req.Id,new(req.Version,Proof(b)),false);Assert.NotNull(ziel.MfaGeheimnis);
        await MfaNotfall.Aktion(db,m,h,clock,a.Id,req.Id,new(req.Version,Proof(a)),true);Assert.Null(ziel.MfaGeheimnis);Assert.True(ziel.MfaNeueinrichtung);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>MfaNotfall.Aktion(db,m,h,clock,a.Id,req.Id,new(req.Version,Proof(a)),true));
        var neuesPasswort=MfaTests.Secret();await Erstzugang.Einloesen(db,h,new(req.Zugang!.Code,neuesPasswort),true);await m.Vorbereiten(db,h,ziel.Id,new(neuesPasswort,null));await m.Verwalten(db,h,ziel.Id,new(neuesPasswort,MfaTests.Code(p,ziel,clock)),"aktivieren");Assert.False(ziel.MfaNeueinrichtung);
        var expired=await MfaNotfall.Antrag(db,m,h,clock,a.Id,Antrag(ziel,Proof(a)));clock.Now=clock.Now.AddMinutes(11);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>MfaNotfall.Aktion(db,m,h,clock,b.Id,expired.Id,new(expired.Version,Proof(b)),false));Assert.NotNull(ziel.MfaGeheimnis);
        Assert.Single(await db.Sicherheitsereignisse.Where(x=>x.Ressource==ziel.Id&&x.Aktion.StartsWith("MFA-Notfallreset")).ToArrayAsync());
    }
}
