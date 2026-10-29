using System.Security.Cryptography;
using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class PosteingangTests
{
    internal sealed class Dateien:IDokumentdateien
    {
        internal readonly Dictionary<string,byte[]> Werte=[];
        public Task Speichern(string k,byte[] b,CancellationToken t){Werte.Add(k,b.ToArray());return Task.CompletedTask;}
        public Task<byte[]> Lesen(string k,CancellationToken t)=>Task.FromResult(Werte[k].ToArray());
        public Task Entfernen(string k,CancellationToken t){Werte.Remove(k);return Task.CompletedTask;}
        public Task<Stream> Oeffnen(string k,long groesse,string hash,CancellationToken t){Assert.Equal(groesse,Werte[k].LongLength);Assert.Equal(hash,Convert.ToHexString(SHA256.HashData(Werte[k])));return Task.FromResult<Stream>(new MemoryStream(Werte[k].ToArray()));}
    }
    internal static JsonElement Json(object value)=>JsonSerializer.SerializeToElement(value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
    internal static async Task Durchlauf(FirmenDbContext db)
    {
        var u=new Benutzerkonto{Login="post-team",Anzeigename="Synthetisches Team",PasswortHash="synthetischer-pruefwert"};var fremd=new Benutzerkonto{Login="post-fremd",Anzeigename="Fremd",PasswortHash="synthetischer-pruefwert"};
        var firma=new KontaktDatensatz{Name="Beispielfirma",Suchname="beispielfirma"};var geheim=new KontaktDatensatz{Name="Geschuetzte Partei",Suchname="geschuetzt"};var k=new KategorieDatensatz{Name="Unterlagen",Suchname="unterlagen"};db.AddRange(u,fremd,firma,geheim,k);
        var akte=new AkteDatensatz{Titel="Beispielakte",Suchtext="beispielakte",FirmaId=firma.Id,KategorieId=k.Id};db.Add(akte);db.Freigaben.Add(new(){BenutzerId=u.Id,KontaktId=firma.Id});db.Freigaben.Add(new(){BenutzerId=u.Id,KontaktId=geheim.Id});await db.SaveChangesAsync();
        var dateien=new Dateien();var storage=new FirmenDaten(db,new(true,null,null),new SynthetischeEingaenge.NurKatalog());var clock=new MfaTests.Clock();var tasks=new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));var service=new EingangsDienst(db,tasks,clock);
        var f=SynthetischeEingaenge.Datei("rechnung");var first=await storage.DokumentHinzufuegen(f.Bytes,f.Name,"text/plain",null,dateien);db.Freigaben.Add(new(){BenutzerId=u.Id,DokumentId=first.Id});await db.SaveChangesAsync();
        var again=await storage.DokumentHinzufuegen(f.Bytes,f.Name,"text/plain",null,dateien);Assert.Equal(first.Id,again.Id);Assert.Single(dateien.Werte);Assert.Equal(2,await db.Dokumentaktivitaeten.CountAsync(d=>d.DokumentId==first.Id&&d.Kanal!=null));
        Assert.Equal(f.Bytes,await dateien.Lesen(first.StorageKey,default));Assert.Equal(Convert.ToHexString(SHA256.HashData(f.Bytes)),first.Hash);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>storage.DokumentHinzufuegen([0,255,0],"ungut.txt","text/plain",null,dateien));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>storage.DokumentHinzufuegen(f.Bytes,"../pfad.txt","text/plain",null,dateien));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>storage.DokumentHinzufuegen(f.Bytes,"falsch.pdf","application/pdf",null,dateien));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>storage.DokumentHinzufuegen(System.Text.Encoding.UTF8.GetBytes("Nicht freigegebene Fremddatei"),"fremd.txt","text/plain",null,dateien));
        var events=await db.Dokumentaktivitaeten.Where(e=>e.DokumentId==first.Id&&e.Kanal!=null).OrderBy(e=>e.Zeitpunkt).ToArrayAsync();
        Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Detail(fremd.Id,events[0].Id))).Status);
        var proposal=EingangsDienst.Vorschlag(first);Assert.Equal(125.50m,proposal.Betrag);Assert.Null(proposal.FaelligAm);Assert.Equal("Eingangsrechnung",proposal.Art);
        var task=new Aufgabeneingabe("Antwort vorbereiten","Quelle: synthetische Rechnung",u.Id,"Normal","Offen",null,null);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Folgeaufgabe(u.Id,events[0].Id,new(0,true,true,task)));Assert.Empty(await db.Aufgaben.ToArrayAsync());
        var pruefung=new Eingangspruefung(0,"Zuordnen",proposal,firma.Id,akte.Id,u.Id,"Original geprueft und vorhandene Akte gewaehlt","",true);
        var detail=Json(await service.Pruefen(u.Id,events[0].Id,pruefung));var version=detail.GetProperty("eingang").GetProperty("version").GetInt32();Assert.Equal("Zugeordnet",detail.GetProperty("eingang").GetProperty("status").GetString());Assert.Equal(akte.Id,(await db.Dokumentzuordnungen.SingleAsync()).AkteId);
        Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Pruefen(u.Id,events[1].Id,pruefung with{ZustaendigId=fremd.Id}))).Status);
        Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Pruefen(u.Id,events[0].Id,pruefung))).Status);db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Folgeaufgabe(u.Id,events[0].Id,new(version,true,false,task with{FaelligAm=new(2026,11,1)})));Assert.Empty(await db.Aufgaben.ToArrayAsync());
        akte=await db.Akten.SingleAsync(x=>x.Id==akte.Id);akte.Status="Archiviert";await db.SaveChangesAsync();
        Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Folgeaufgabe(u.Id,events[0].Id,new(version,true,false,task)))).Status);
        Assert.Empty(await db.Aufgaben.ToArrayAsync());db.ChangeTracker.Clear();
        akte=await db.Akten.SingleAsync(x=>x.Id==akte.Id);akte.Status="Offen";await db.SaveChangesAsync();
        var result=Json(await service.Folgeaufgabe(u.Id,events[0].Id,new(version,true,false,task)));var id=result.GetProperty("aufgabeId").GetString()!;Assert.Equal(id,Json(await service.Folgeaufgabe(u.Id,events[0].Id,new(version,true,false,task))).GetProperty("aufgabeId").GetString());Assert.Single(await db.Aufgaben.ToArrayAsync());Assert.Equal(first.Id,(await db.Aufgaben.SingleAsync()).DokumentId);
        Assert.Equal(events[0].Id,Json(await tasks.Detail(u.Id,id)).GetProperty("eingangId").GetString());Assert.Equal(f.Bytes,await dateien.Lesen(first.StorageKey,default));Assert.Empty(await db.Rechnungen.ToArrayAsync());
        var unknown=SynthetischeEingaenge.Datei("unklar");var d=await storage.DokumentHinzufuegen(unknown.Bytes,unknown.Name,"text/plain",null,dateien);db.Freigaben.Add(new(){BenutzerId=u.Id,DokumentId=d.Id});await db.SaveChangesAsync();var incoming=await db.Dokumentaktivitaeten.SingleAsync(e=>e.DokumentId==d.Id&&e.Kanal!=null);
        var offen=Json(await service.Pruefen(u.Id,incoming.Id,new(0,"Zur Prüfung zurückstellen",EingangsDienst.Vorschlag(d),null,null,u.Id,"Absender ungeklärt","Bitte Absender nachfragen",true)));Assert.Equal("Zur Prüfung",offen.GetProperty("eingang").GetProperty("status").GetString());Assert.Null(offen.GetProperty("eingang").GetProperty("akteId").GetString());
        await service.Pruefen(u.Id,incoming.Id,new(1,"Erledigen",EingangsDienst.Vorschlag(d),null,akte.Id,u.Id,"Nach Rueckfrage ausdruecklich zugeordnet","",true));Assert.True(await db.Dokumentzuordnungen.AnyAsync(z=>z.DokumentId==d.Id&&z.AkteId==akte.Id));
        // Historical reasons cannot leak after sender context and assignee change.
        var old=await service.Pruefen(u.Id,events[1].Id,pruefung with{AbsenderId=geheim.Id,Grund="VERTRAULICHER ALTKONTEXT"});var v=Json(old).GetProperty("eingang").GetProperty("version").GetInt32();await service.Pruefen(u.Id,events[1].Id,pruefung with{Version=v,AbsenderId=firma.Id});db.Freigaben.RemoveRange(db.Freigaben.Where(g=>g.BenutzerId==u.Id&&g.KontaktId==geheim.Id));await db.SaveChangesAsync();Assert.DoesNotContain("VERTRAULICHER ALTKONTEXT",Json(await service.Detail(u.Id,events[1].Id)).ToString());
    }
    [Fact] public async Task OriginalDublettenPruefungRechteUndEinmaligeFolgeaufgabe()
    {await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();await Durchlauf(db);}
    [Fact] public void KatalogFehlendeAngabenUndSichereXmlGrenze()
    {
        foreach(var key in SynthetischeEingaenge.Schluessel){var f=SynthetischeEingaenge.Datei(key);Assert.Contains("MBOP-SYNTHETISCH",new Dokumentpruefung().Pruefen(f.Bytes,f.Name,f.Name.EndsWith(".pdf")?"application/pdf":"text/plain").Text);}
        var d=new DokumentDatensatz{Titel="Unklar",Text="Ohne passende Felder"};var v=EingangsDienst.Vorschlag(d);Assert.Null(v.Dokumentdatum);Assert.Null(v.Betrag);Assert.Null(v.FaelligAm);Assert.Equal("Sonstiges",v.Art);
        using var bytes=new MemoryStream();using(var zip=new System.IO.Compression.ZipArchive(bytes,System.IO.Compression.ZipArchiveMode.Create,true)){
            foreach(var name in new[]{"[Content_Types].xml","_rels/.rels","word/document.xml"}){using var writer=new StreamWriter(zip.CreateEntry(name).Open());writer.Write(name=="word/document.xml"?"<!DOCTYPE x [<!ENTITY y SYSTEM 'file:///nonexistent-mbop-synthetic-fixture.xml'>]><x>&y;</x>":"<x/>");}
        }
        Assert.Throws<Arbeitsfehler>(()=>new Dokumentpruefung().Pruefen(bytes.ToArray(),"angriff.docx","application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
    }
    internal static async Task Regelfall(FirmenDbContext db,string fall="ok")
    {
        var team=new Benutzerkonto{Login="regel-team",Anzeigename="Fiktives Prüfteam",PasswortHash="synthetisch"};
        var fremd=new Benutzerkonto{Login="regel-fremd",Anzeigename="Anderer isolierter Bereich",PasswortHash="synthetisch"};
        var firma=new KontaktDatensatz{Name="Fiktiver Kieselbüro Betrieb",Suchname="fiktiver kieselbuero"};
        var sender=new KontaktDatensatz{Name="Fiktive Binnenstern Versicherung",Suchname="fiktive binnenstern"};
        var kat=new KategorieDatensatz{Name="Synthetische Verträge",Suchname="synthetische vertraege"};
        var akte=new AkteDatensatz{Titel="TEST-INTERN-781 – Versicherungsprüfung",Suchtext="test intern 781",FirmaId=firma.Id,KategorieId=kat.Id};
        db.AddRange(team,fremd,firma,sender,kat,akte);db.Freigaben.AddRange(new(){BenutzerId=team.Id,KontaktId=firma.Id},new(){BenutzerId=team.Id,KontaktId=sender.Id});await db.SaveChangesAsync();
        var files=new Dateien();var fixture=SynthetischeEingaenge.Datei("regel-versicherung");
        var doc=await new FirmenDaten(db,new(true,null,null),new SynthetischeEingaenge.NurKatalog()).DokumentHinzufuegen(fixture.Bytes,fixture.Name,"text/plain",null,files);
        db.Freigaben.Add(new(){BenutzerId=team.Id,DokumentId=doc.Id});await db.SaveChangesAsync();
        var clock=new MfaTests.Clock();var tasks=new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));var service=new EingangsDienst(db,tasks,clock);
        var e=await db.Dokumentaktivitaeten.SingleAsync(x=>x.DokumentId==doc.Id&&x.Kanal!=null);
        Assert.NotNull((await service.Aufgabenregel(team.Id,e.Id)).Sperrgrund);Assert.Empty(await db.Aufgaben.ToArrayAsync());
        Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Aufgabenregel(fremd.Id,e.Id))).Status);
        var analyse=(await service.Analysieren(team.Id,e.Id,new Dokumentanalysator(),files,default)).Analyse;
        Assert.Equal("Versicherungsschreiben",analyse.Dokumentart);Assert.DoesNotContain(analyse.Felder,f=>f.Status=="Widerspruch");
        var fields=analyse.Felder.Select(f=>f.Status=="Vorschlag"?f with{Status="Bestätigt"}:f with{Status="Manuell",Wert=null,AbschnittId=null,AnkerAbschnittId=null}).ToArray();
        var stored=JsonSerializer.Deserialize<Dokumentanalyse>(doc.AnalyseJson!,Dokumentanalysator.Json)!;
        foreach(var f in fields)Assert.True(stored.Felder.Any(x=>x.Name==f.Name)&&f.Wert?.Length is not >160&&(f.AbschnittId is null||stored.Abschnitte.Any(x=>x.Id==f.AbschnittId)),JsonSerializer.Serialize(f));
        var data=EingangsDienst.Vorschlag(doc) with{Felder=fields,Referenz=analyse.Felder.Single(x=>x.Name=="referenz").Wert};
        await service.Pruefen(team.Id,e.Id,new(0,"Zuordnen",data,sender.Id,akte.Id,team.Id,"Synthetisches Original und Aktenzuordnung geprüft","",true));
        var v=await service.Aufgabenregel(team.Id,e.Id);Assert.Null(v.Sperrgrund);Assert.NotNull(v.Bezug);Assert.Equal(new DateOnly(2026,11,12),v.Aufgabe!.FaelligAm);
        Assert.Equal(v,await service.Aufgabenregel(team.Id,e.Id));Assert.Empty(await db.Aufgaben.ToArrayAsync()); // GET / cancel / reject have no write.
        var p=new Eingangsaufgabe(v.Version,true,true,v.Aufgabe with{Betreff="Unterlagen intern prüfen ? freigegebener Entwurf"},v.Bezug);
        if(fall=="archiviert"){akte.Status="Archiviert";await db.SaveChangesAsync();}
        if(fall=="fremde-akte"){db.Freigaben.RemoveRange(db.Freigaben.Where(x=>x.BenutzerId==team.Id&&x.KontaktId==firma.Id));await db.SaveChangesAsync();}
        if(fall=="sender-inaktiv"){sender.Archiviert=true;await db.SaveChangesAsync();}
        if(fall=="version"){var w=await db.Eingangsworkflows.SingleAsync();w.Version++;await db.SaveChangesAsync();}
        if(fall=="falsche-quelle")p=p with{Regelbezug=new string('0',64)};
        if(fall=="ohne-freigabe")p=p with{Bestaetigt=false};
        if(fall=="frist-ungeprueft")p=p with{FristBestaetigt=false};
        if(fall=="original-manipuliert")files.Werte[doc.StorageKey][0]^=1;
        if(fall=="fehlendes-feld"){var w=await db.Eingangsworkflows.SingleAsync();w.AngabenJson=JsonSerializer.Serialize(data with{FaelligAm=null});await db.SaveChangesAsync();}
        if(fall=="rollback")await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER regel_rollback BEFORE INSERT ON Dokumentaktivitaeten WHEN NEW.Text LIKE 'Folgeaufgabe%' BEGIN SELECT RAISE(ABORT, 'synthetic audit failure'); END;");
        if(fall!="ok"){
            if(fall=="rollback")await Assert.ThrowsAsync<DbUpdateException>(()=>service.Folgeaufgabe(team.Id,e.Id,p,files));
            else{var error=await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Folgeaufgabe(team.Id,e.Id,p,files));Assert.Equal(fall=="fremde-akte"?404:fall is "ohne-freigabe" or "frist-ungeprueft"?400:409,error.Status);}
            db.ChangeTracker.Clear();
            Assert.Empty(await db.Aufgaben.ToArrayAsync());Assert.Empty(await db.Aufgabenverlauf.ToArrayAsync());Assert.Null((await db.Eingangsworkflows.SingleAsync()).AufgabeId);
            Assert.False(await db.Dokumentaktivitaeten.AnyAsync(x=>x.Text.StartsWith("Folgeaufgabe")));return;
        }
        // A separate organisation host has its own DbContext/database; identical actor identity
        // must not make an original from this context resolvable in the other one.
        await using(var otherConnection=new SqliteConnection("Data Source=:memory:")){
            await otherConnection.OpenAsync();await using var otherDb=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(otherConnection).Options);await otherDb.Database.EnsureCreatedAsync();
            otherDb.Benutzer.Add(new(){Id=team.Id,Login="isolierte-firma",Anzeigename="Isolierte Testfirma",PasswortHash="synthetisch",Rolle="Administrator"});await otherDb.SaveChangesAsync();
            var otherService=new EingangsDienst(otherDb,new AufgabenDienst(otherDb,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")),clock);
            Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>otherService.Aufgabenregel(team.Id,e.Id))).Status);
            Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>otherService.Folgeaufgabe(team.Id,e.Id,p,files))).Status);Assert.Empty(await otherDb.Aufgaben.ToArrayAsync());
        }
        var first=Json(await service.Folgeaufgabe(team.Id,e.Id,p,files)).GetProperty("aufgabeId").GetString();db.ChangeTracker.Clear();
        var second=Json(await service.Folgeaufgabe(team.Id,e.Id,p,files)).GetProperty("aufgabeId").GetString();Assert.Equal(first,second);
        var task=await tasks.Lesen(team.Id,first!);Assert.Contains(v.DokumentHash,task.Beschreibung);Assert.Contains(v.Regel,task.Beschreibung);Assert.Equal(p.Aufgabe.Betreff,task.Betreff);
        Assert.Single(await db.Aufgaben.ToArrayAsync());Assert.Single(await db.Aufgabenverlauf.ToArrayAsync());Assert.Single(await db.Dokumentaktivitaeten.Where(x=>x.Text.StartsWith("Folgeaufgabe")).ToArrayAsync());
        Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>tasks.Lesen(fremd.Id,first!))).Status);
        Assert.Equal(fixture.Bytes,await files.Lesen(doc.StorageKey,default));Assert.Empty(await db.Rechnungen.ToArrayAsync());
    }
    [Theory]
    [InlineData("ok")][InlineData("archiviert")][InlineData("fremde-akte")][InlineData("sender-inaktiv")][InlineData("version")][InlineData("falsche-quelle")][InlineData("ohne-freigabe")][InlineData("frist-ungeprueft")][InlineData("original-manipuliert")][InlineData("fehlendes-feld")][InlineData("rollback")]
    public async Task VersicherungsvorlageQuelleFreigabeUndAtomaritaet(string fall)
    {await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();await Regelfall(db,fall);}

}
