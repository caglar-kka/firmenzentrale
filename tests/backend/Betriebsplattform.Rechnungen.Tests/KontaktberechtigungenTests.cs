using Betriebsplattform.Web.Sicherheit;
using Betriebsplattform.Web.Persistenz;
using KontaktrechteMigration = Betriebsplattform.Web.Persistenz.Migrationen.Kontaktberechtigungen;
using Betriebsplattform.Web.Akten;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class KontaktberechtigungenTests
{
    [Fact] public void KatalogVerweigertUnbekanntesUndTrenntVorlageVonAbweichung()
    {
        var buero=Kontaktberechtigungen.Standardvorlagen().Single(x=>x.Id=="buero:1");
        var team=Kontaktberechtigungen.Standardvorlagen().Single(x=>x.Id=="teamleitung:1");
        Assert.True(Kontaktberechtigungen.Auswerten("Lesen",buero.RechteJson,"{}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Archivieren",buero.RechteJson,"{}"));
        Assert.True(Kontaktberechtigungen.Auswerten("Archivieren",team.RechteJson,"{}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Bearbeiten",buero.RechteJson,"{\"Bearbeiten\":false}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Loeschen",team.RechteJson,"{\"Loeschen\":true}"));
        Assert.False(Kontaktberechtigungen.Auswerten("Lesen",null,"{}"));
    }
    [Fact] public async Task MigrationErhaeltBestandsrechteUndNeueKontenBleibenOhneFreigabe()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();
        await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();
        var h=Anmeldung.Passwortdienst();var alt=await Anmeldung.Anlegen(db,h,new("alt","Bestand",new string('x',20),"Mitarbeiter"));
        var hash=alt.PasswortHash;var stamp=alt.Sicherheitskennung;
        await db.Database.ExecuteSqlRawAsync("DROP TABLE BenutzerRechteprofile; DROP TABLE Berechtigungsaenderungen; DROP TABLE Rechtevorlagenversionen;");
        var migration=new KontaktrechteMigration();
        foreach(var generated in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations,db.Model)){await using var cmd=cn.CreateCommand();cmd.CommandText=generated.CommandText;await cmd.ExecuteNonQueryAsync();}
        Assert.True(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Lesen"));Assert.True(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Archivieren"));
        var p=await db.BenutzerRechteprofile.SingleAsync();Assert.Equal("Bestand",p.Modus);Assert.Null(p.VorlageId);Assert.Equal(hash,alt.PasswortHash);Assert.Equal(stamp,alt.Sicherheitskennung);
        var neu=await Anmeldung.Anlegen(db,h,new("neu","Neu",new string('y',20),"Mitarbeiter"));Assert.False(await Kontaktberechtigungen.Erlaubt(db,neu.Id,"Lesen"));
        Assert.False(await Kontaktberechtigungen.Erlaubt(db,neu.Id,"Loeschen"));
        p.AbweichungenJson="{\"Lesen\":true,\"Bearbeiten\":false}";p.Version++;await db.SaveChangesAsync();Assert.True(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Lesen"));Assert.False(await Kontaktberechtigungen.Erlaubt(db,alt.Id,"Bearbeiten"));
        var v=await db.Rechtevorlagenversionen.FirstAsync();v.Name="Manipulation";await Assert.ThrowsAsync<Arbeitsfehler>(()=>db.SaveChangesAsync());
        await using var pg=new FirmenDbFactory().CreateDbContext([]);Assert.False(pg.Database.HasPendingModelChanges());
    }
    [Fact] public async Task StarkePruefungSelbstschutzKonflikteUndAudit()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;
        await using var db=new FirmenDbContext(options);await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();var pass=MfaTests.Secret();
        var admin=await Anmeldung.Anlegen(db,h,new("admin","Admin",pass,"Administrator"));var team=await Anmeldung.Anlegen(db,h,new("team","Team",pass,"Mitarbeiter"));
        var protection=new EphemeralDataProtectionProvider();var clock=new MfaTests.Clock();var mfa=new MfaDienst(protection,clock);
        var e=new KontaktrechteEingabe(0,"buero:1",new(){["Bearbeiten"]=false},false,new(pass,"123456"));
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,team.Id,team.Id,e,mfa,h))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,admin.Id,e,mfa,h))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e,mfa,h))).Status);
        await mfa.Vorbereiten(db,h,admin.Id,new(pass,null));await mfa.Verwalten(db,h,admin.Id,new(pass,MfaTests.Code(protection,admin,clock)),"aktivieren");clock.Next();
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Pruefung=new(pass,MfaTests.BadCode(protection,admin,clock))},mfa,h));
        Assert.True((await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==admin.Id)).MfaFehlversuche>0);Assert.Empty(await db.Berechtigungsaenderungen.ToArrayAsync());
        var saved=await Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Pruefung=new(pass,MfaTests.Code(protection,admin,clock))},mfa,h);
        Assert.True(saved.Aktionen.Single(x=>x.Id=="Lesen").Erlaubt);Assert.False(saved.Aktionen.Single(x=>x.Id=="Bearbeiten").Erlaubt);Assert.Equal(1,saved.Version);
        Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e,mfa,h))).Status);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Version=1,VorlageId="administrator:1"},mfa,h));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Kontaktberechtigungen.Speichern(db,admin.Id,team.Id,e with{Version=1,Abweichungen=new(){["Loeschen"]=true}},mfa,h));
        var audit=await db.Berechtigungsaenderungen.SingleAsync();Assert.Equal(admin.Id,audit.AkteurId);Assert.Equal(team.Id,audit.ZielId);Assert.Contains("Bearbeiten",audit.Nachher);Assert.DoesNotContain(pass,audit.Nachher);
        audit.Nachher="Manipuliert";await Assert.ThrowsAsync<Arbeitsfehler>(()=>db.SaveChangesAsync());
    }
}
