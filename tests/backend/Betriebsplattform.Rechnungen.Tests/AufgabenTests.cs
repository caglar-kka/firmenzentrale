using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class AufgabenTests
{
    [Fact] public async Task ZentraleAufgabenRechteZuweisungStatusVerweiseUndKonflikt()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();var options=new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options;
        await using var db=new FirmenDbContext(options);await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();var pass=MfaTests.Secret();
        var a=await Anmeldung.Anlegen(db,h,new("team-a","Team A",pass,"Mitarbeiter"));var b=await Anmeldung.Anlegen(db,h,new("team-b","Team B",pass,"Mitarbeiter"));var fremd=await Anmeldung.Anlegen(db,h,new("fremd","Fremd",pass,"Mitarbeiter"));
        var f=new KontaktDatensatz{Name="Firma A",Suchname="firma a"};var f2=new KontaktDatensatz{Name="Firma B",Suchname="firma b"};var kat=new KategorieDatensatz{Name="Allgemein",Suchname="allgemein"};db.AddRange(f,f2,kat);
        var akte=new AkteDatensatz{Titel="Testakte",Suchtext="testakte",FirmaId=f.Id,KategorieId=kat.Id};db.Add(akte);var r=new RechnungDatensatz{Nummer="SYN-1",AkteId=akte.Id,AusstellerId=f.Id,EmpfaengerId=f2.Id,Datum=new(2026,10,19),Betrag=10};db.Add(r);
        foreach(var u in new[]{a,b})foreach(var firma in new[]{f,f2})db.Freigaben.Add(new(){BenutzerId=u.Id,KontaktId=firma.Id});await db.SaveChangesAsync();
        var clock=new MfaTests.Clock();var d=new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
        Aufgabeneingabe Input(string art,string id)=>new("Rückfrage","Synthetische Wiedervorlage",b.Id,"Hoch","Offen",d.Heute.AddDays(-1),null,BezugArt:art,BezugId:id);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Mitarbeiter(a.Id,"Ungueltig","irgendetwas",null));
        var task=await d.Speichern(a.Id,null,Input("Rechnung",r.Id));Assert.Equal(task.Id,(await d.Lesen(b.Id,task.Id)).Id);Assert.True(d.Ueberfaellig(task));
        Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Lesen(fremd.Id,task.Id))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Speichern(a.Id,null,Input("Rechnung",r.Id) with{ZustaendigId=fremd.Id}))).Status);db.ChangeTracker.Clear();
        await using var stale=new FirmenDbContext(options);await stale.Aufgaben.SingleAsync(x=>x.Id==task.Id);var old=task.Version;
        task=await d.Speichern(b.Id,task.Id,Input("Rechnung",r.Id) with{Version=old,Status="Erledigt"});Assert.False(d.Ueberfaellig(task));
        var other=new AufgabenDienst(stale,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>other.Speichern(a.Id,task.Id,Input("Rechnung",r.Id) with{Version=old}))).Status);
        task=await d.Speichern(a.Id,task.Id,Input("Akte",akte.Id) with{Version=task.Version});Assert.Equal("Offen",task.Status);Assert.Equal(akte.Id,task.AkteId);Assert.Null(task.RechnungId);
        task=await d.Speichern(a.Id,task.Id,Input("Kontakt",f.Id) with{Version=task.Version});Assert.Equal(f.Id,task.KontaktId);
        db.Freigaben.RemoveRange(db.Freigaben.Where(g=>g.BenutzerId==b.Id));await db.SaveChangesAsync();Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Lesen(b.Id,task.Id))).Status);
        Assert.Equal(4,await db.Aufgabenverlauf.CountAsync(x=>x.AufgabeId==task.Id));
    }
    [Fact] public void KalenderdatumUhrzeitZeitzoneUndSommerzeit()
    {
        using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite("Data Source=:memory:").Options);var clock=new MfaTests.Clock{Now=new DateTimeOffset(2026,10,25,0,45,0,TimeSpan.Zero)};var d=new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
        var a=new Aufgabe{FaelligAm=new(2026,10,25),FaelligUm=new(2,30)};Assert.False(d.Ueberfaellig(a));clock.Now=clock.Now.AddHours(1);Assert.True(d.Ueberfaellig(a));a.FaelligUm=null;Assert.False(d.Ueberfaellig(a));clock.Now=clock.Now.AddDays(2);Assert.True(d.Ueberfaellig(a));a.Status="Erledigt";Assert.False(d.Ueberfaellig(a));
    }
}
