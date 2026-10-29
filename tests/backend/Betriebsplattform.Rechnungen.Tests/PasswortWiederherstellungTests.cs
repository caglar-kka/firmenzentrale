using System.Security.Cryptography;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class PasswortWiederherstellungTests
{
    private static string Secret()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    [Fact] public async Task WiederherstellungPrueftIdentitaetZweckAblaufWiderrufUndEinmaligkeit()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();
        var h=Anmeldung.Passwortdienst();var old=Secret();var admin=await Anmeldung.Anlegen(db,h,new("admin","Admin",Secret(),"Administrator"));var u=await Anmeldung.Anlegen(db,h,new("team","Team",old,"Mitarbeiter"));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,false),admin.Id));
        var first=(await Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,true),admin.Id))!;var stamp=u.Sicherheitskennung;
        Assert.Equal(97,first.Code.Length);Assert.True(u.WiederherstellungHash==Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(first.Code))));Assert.InRange(first.GueltigBis-DateTimeOffset.UtcNow,TimeSpan.FromMinutes(59),TimeSpan.FromMinutes(60));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(first.Code,Secret())));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(u.Id+"."+Secret(),Secret()),true));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(first.Code,"kurz"),true));
        u.WiederherstellungBis=DateTimeOffset.UtcNow.AddSeconds(-1);u.Version++;await db.SaveChangesAsync();await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(first.Code,Secret()),true));
        var second=(await Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,true),admin.Id))!;Assert.Equal(stamp,u.Sicherheitskennung);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(first.Code,Secret()),true));
        await Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,true,false),admin.Id);await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(second.Code,Secret()),true));
        var third=(await Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,true),admin.Id))!;
        u.Aktiv=false;u.Version++;await db.SaveChangesAsync();await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(third.Code,Secret()),true));await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,true),admin.Id));
        u.Aktiv=true;u.Version++;await db.SaveChangesAsync();var next=Secret();await Erstzugang.Einloesen(db,h,new(third.Code,next),true);
        Assert.NotEqual(stamp,u.Sicherheitskennung);Assert.Null(u.WiederherstellungHash);Assert.Null(u.WiederherstellungBis);Assert.Equal("Mitarbeiter",u.Rolle);Assert.True(u.PasswortHash!=next);
        Assert.True(h.VerifyHashedPassword(u,u.PasswortHash,next)!=PasswordVerificationResult.Failed);Assert.True(h.VerifyHashedPassword(u,u.PasswortHash,old)==PasswordVerificationResult.Failed);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(third.Code,Secret()),true));
        Assert.Equal(1,await db.Sicherheitsereignisse.CountAsync(x=>x.Aktion=="Passwort wiederhergestellt"));
        Assert.DoesNotContain(third.Code,System.Text.Json.JsonSerializer.Serialize(await db.Sicherheitsereignisse.ToArrayAsync()));
        var pending=await Erstzugang.Anlegen(db,h,new("pending","Pending","Mitarbeiter"),admin.Id);await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.WiederherstellungAendern(db,pending.Id,new(2,false,true),admin.Id));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(pending.Code,Secret()),true));await Erstzugang.Einloesen(db,h,new(pending.Code,Secret()));
    }
    [Fact] public async Task EigenerPasswortwechselWiderruftOffenenReset()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();var pass=Secret();var u=await Anmeldung.Anlegen(db,h,new("admin","Admin",pass,"Administrator"));
        var code=(await Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,true),u.Id))!;await Anmeldung.PasswortAendern(db,h,u.Id,new(pass,Secret()));Assert.Null(u.WiederherstellungHash);await Assert.ThrowsAsync<Arbeitsfehler>(()=>Erstzugang.Einloesen(db,h,new(code.Code,Secret()),true));
    }
}
