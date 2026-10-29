using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class AufgabenNachweisTests
{
    [Fact] public async Task HistorieRedigiertAltenBezugUndFrueherePrivateZustaendigkeit()
    {
        await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        var a=new Benutzerkonto{Login="alt",Anzeigename="Alt",PasswortHash="synthetisch"};var b=new Benutzerkonto{Login="neu",Anzeigename="Neu",PasswortHash="synthetisch"};var x=new KontaktDatensatz{Name="Geheim",Suchname="geheim"};var y=new KontaktDatensatz{Name="Freigegeben",Suchname="freigegeben"};db.AddRange(a,b,x,y);
        db.Freigaben.AddRange(new(){BenutzerId=a.Id,KontaktId=x.Id},new(){BenutzerId=a.Id,KontaktId=y.Id},new(){BenutzerId=b.Id,KontaktId=y.Id});await db.SaveChangesAsync();
        var d=new AufgabenDienst(db,new MfaTests.Clock(),TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));var e=new Aufgabeneingabe("VERTRAULICHER ALTINHALT","Vertrauliche Beschreibung",a.Id,"Normal","Offen",null,null,BezugArt:"Kontakt",BezugId:x.Id);
        var t=await d.Speichern(a.Id,null,e);t=await d.Speichern(a.Id,t.Id,e with{Version=t.Version,ZustaendigId=b.Id,BezugId=y.Id,Betreff="Freigegebene Aufgabe",Beschreibung="Freigegeben"});
        var fremd=JsonSerializer.Serialize(await d.Detail(b.Id,t.Id));Assert.DoesNotContain("VERTRAULICHER ALTINHALT",fremd);Assert.DoesNotContain(x.Id,fremd);Assert.Contains("Freigegeben",fremd);
        Assert.Contains("VERTRAULICHER ALTINHALT",JsonSerializer.Serialize(await d.Detail(a.Id,t.Id)));
    }
    [Fact] public async Task VerlaufIstPaginiertUndZuweisungFindetBerechtigteHinterErsten200()
    {
        await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        var a=new Benutzerkonto{Login="admin",Anzeigename="AAA",PasswortHash="synthetisch",Rolle="Administrator"};var b=new Benutzerkonto{Login="letzte",Anzeigename="ZZZ",PasswortHash="synthetisch"};db.AddRange(a,b);
        for(var i=0;i<201;i++)db.Benutzer.Add(new(){Login="synthetisch"+i,Anzeigename="Test "+i.ToString("D3"),PasswortHash="synthetisch"});await db.SaveChangesAsync();
        var d=new AufgabenDienst(db,new MfaTests.Clock(),TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));Assert.Contains(b.Id,JsonSerializer.Serialize(await d.Mitarbeiter(a.Id,null,null,null,"ZZZ",0)));
        Assert.Contains(b.Id,JsonSerializer.Serialize(await d.Mitarbeiter(a.Id,null,null,null,"",10)));
        var e=new Aufgabeneingabe("Verlauf","",a.Id,"Normal","Offen",null,null);var t=await d.Speichern(a.Id,null,e);
        for(var i=0;i<102;i++)t=await d.Speichern(a.Id,t.Id,e with{Version=t.Version,Betreff="Version "+i});
        var page=JsonSerializer.SerializeToElement(await d.Detail(a.Id,t.Id,5));Assert.Equal(103,page.GetProperty("historieAnzahl").GetInt32());Assert.Equal(3,page.GetProperty("historie").GetArrayLength());
        t=await d.Speichern(a.Id,t.Id,e with{Version=t.Version,Status="Erledigt"});t=await d.Speichern(a.Id,t.Id,e with{Version=t.Version,Status="Offen"});Assert.Equal("Offen",t.Status);
        t=await d.Speichern(a.Id,t.Id,e with{Version=t.Version,Archiviert=true});Assert.True(t.Archiviert);Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Speichern(a.Id,t.Id,e with{Version=t.Version}))).Status);
        var invalid=e with{FaelligAm=new(2026,3,29),FaelligUm=new(2,30)};await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.Speichern(a.Id,null,invalid));
    }
}
