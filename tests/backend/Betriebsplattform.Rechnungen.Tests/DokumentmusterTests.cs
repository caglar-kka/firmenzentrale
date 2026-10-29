using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Betrieb;
using Npgsql;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class DokumentmusterTests
{
    private sealed class PaketFact:FactAttribute{public PaketFact(){if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FZ_DOCUMENT_PACKAGE"))||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")))Skip="Explicit local fictional PDF package and installed PDF adapter required.";}}
    private sealed class NurDateien(byte[][] dateien):IMalwarepruefung{public Task Freigeben(byte[] b,CancellationToken ct){ct.ThrowIfCancellationRequested();if(!dateien.Any(d=>d.AsSpan().SequenceEqual(b)))throw new Arbeitsfehler("Nur explizite unveränderte Testdateien",503);return Task.CompletedTask;}}
    internal static async Task Durchlauf(FirmenDbContext db){
        var root=Environment.GetEnvironmentVariable("FZ_DOCUMENT_PACKAGE")!;var raw=new List<byte[]>();
        using var manifest=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root,"03_erwartungswerte","lokale_dateimanifest_sha256.json")));
        foreach(var e in manifest.RootElement.EnumerateArray().Take(4)){var name=e.GetProperty("datei").GetString()!;Assert.Equal(name,Path.GetFileName(name));var b=await File.ReadAllBytesAsync(Path.Combine(root,"01_lokal_erstellte_dokumente",name));Assert.Equal(e.GetProperty("sha256").GetString()!.ToUpperInvariant(),Convert.ToHexString(SHA256.HashData(b)));raw.Add(b);}
        var admin=new Benutzerkonto{Login="nora.lind",Anzeigename="Nora Lind",PasswortHash="synthetisch",Rolle="Administrator"};var team=new Benutzerkonto{Login="anna.berg",Anzeigename="Anna Berg",PasswortHash="synthetisch"};var other=new Benutzerkonto{Login="leon.kruse",Anzeigename="Leon Kruse",PasswortHash="synthetisch"};
        var org=new KontaktDatensatz{Name="Elbequartier Objektservice GmbH",Suchname="elbequartier"};var org2=new KontaktDatensatz{Name="Hafenbogen Objektpflege GmbH",Suchname="hafenbogen"};var sender=new KontaktDatensatz{Name="ElbeFunk Telekommunikation GmbH",Suchname="elbefunk"};var kat=new KategorieDatensatz{Name="Telekommunikation",Suchname="telekommunikation"};db.AddRange(admin,team,other,org,org2,sender,kat);
        var akte=new AkteDatensatz{Titel="Kontorhaus Südhafen - Internet",Suchtext="kontorhaus",FirmaId=org.Id,KategorieId=kat.Id};var fremd=new AkteDatensatz{Titel="Hafenbogen Internet",Suchtext="hafenbogen",FirmaId=org2.Id,KategorieId=kat.Id};db.AddRange(akte,fremd);db.Freigaben.AddRange(new(){BenutzerId=team.Id,KontaktId=org.Id},new(){BenutzerId=team.Id,KontaktId=sender.Id});await db.SaveChangesAsync();
        var files=new PosteingangTests.Dateien();var intake=new FirmenDaten(db,new(true,null,null),new NurDateien(raw.ToArray()));var clock=new MfaTests.Clock();var rights=new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));var workflow=new EingangsDienst(db,rights,clock);var muster=new DokumentmusterDienst(db,rights,clock);var parser=new Dokumentanalysator(new LokalerPdfAdapter(Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")!));var docs=new List<DokumentDatensatz>();var eingang=new List<string>();
        foreach(var b in raw){var d=await intake.DokumentHinzufuegen(b,"Monatsrechnung "+(docs.Count+8)+" 2026.pdf","application/pdf",null,files);docs.Add(d);db.Freigaben.Add(new(){BenutzerId=team.Id,DokumentId=d.Id});await db.SaveChangesAsync();eingang.Add((await db.Dokumentaktivitaeten.SingleAsync(e=>e.DokumentId==d.Id&&e.Kanal!=null)).Id);}
        async Task<Dokumentanalyse> Analyse(int n,Analysekontext? kontext=null){var result=PosteingangTests.Json(await workflow.Analysieren(admin.Id,docs[n].Id,parser,files,default,kontext));return result.GetProperty("analyse").Deserialize<Dokumentanalyse>(Dokumentanalysator.Json)!;}
        async Task Bestaetigen(int n){var a=await Analyse(n);Assert.Null(a.Felder.Single(f=>f.Name=="aussteller").Wert);var source=a.Abschnitte.First(s=>s.Text==sender.Name);var fields=a.Felder.Select(f=>f.Status=="Vorschlag"?f with{Status="Bestätigt"}:f with{Status="Manuell"}).Select(f=>f.Name=="aussteller"?new Prueffeld("aussteller",source.Text,source.Id,"Korrigiert"):f).ToArray();string? V(string name)=>fields.Single(f=>f.Name==name).Wert;
            var data=new Eingangsangaben("Telekommunikationsrechnung Kontorhaus Südhafen","Eingangsrechnung",Dokumentanalysator.Datum(V("dokumentdatum")),V("rechnungsnummer"),Dokumentanalysator.Betrag(V("betrag")),Dokumentanalysator.Datum(V("faelligAm")),V("referenz"),fields);
            await workflow.Pruefen(admin.Id,eingang[n],new(0,"Zuordnen",data,sender.Id,akte.Id,team.Id,"Ausstellerrolle anhand des Originals korrigiert","",true));
        }
        await Bestaetigen(0);var draft=PosteingangTests.Json(await muster.Entwerfen(admin.Id,eingang[0],"aussteller"));var id=draft.GetProperty("id").GetString()!;Assert.Equal("Entwurf",draft.GetProperty("status").GetString());
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>muster.Entscheiden(admin.Id,id,new(1,"Aktiv",eingang[0])));Assert.Equal(403,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>muster.Entscheiden(team.Id,id,new(1,"Aktiv",eingang[0])))).Status);
        await Bestaetigen(1);await Assert.ThrowsAsync<Arbeitsfehler>(()=>muster.Entscheiden(admin.Id,id,new(1,"Aktiv",eingang[1],"Zahlung")));
        await muster.Entscheiden(admin.Id,id,new(1,"Aktiv",eingang[1]));
        var baseline=await Analyse(2);Assert.Null(baseline.Felder.Single(f=>f.Name=="aussteller").Wert);
        var improved=await Analyse(2,new(akte.Id,sender.Id));Assert.Equal(sender.Name,improved.Felder.Single(f=>f.Name=="aussteller").Wert);Assert.Contains(id,improved.MusterIds!);Assert.Equal("EF-2026-1001",improved.Felder.Single(f=>f.Name=="rechnungsnummer").Wert);Assert.Equal(92.30m,Dokumentanalysator.Betrag(improved.Felder.Single(f=>f.Name=="betrag").Wert));Assert.Null(improved.AutomatikMusterId);
        var wrongOrg=await Analyse(2,new(fremd.Id,sender.Id));Assert.Null(wrongOrg.Felder.Single(f=>f.Name=="aussteller").Wert);Assert.Empty(wrongOrg.MusterIds!);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>muster.Liste(team.Id,new(fremd.Id,sender.Id)));Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>workflow.AnalyseLesen(other.Id,docs[2].Id))).Status);
        await muster.Entscheiden(admin.Id,id,new(2,"Aktiv",eingang[1],"Klassifizieren"));var automatic=await Analyse(2,new(akte.Id,sender.Id));Assert.Equal(id,automatic.AutomatikMusterId);Assert.Equal("Eingangsrechnung",docs[2].Art);
        var events=await db.Dokumentaktivitaeten.CountAsync(e=>e.DokumentId==docs[2].Id&&e.Text.StartsWith("Automatische interne"));Assert.Equal(1,events);await Analyse(2,new(akte.Id,sender.Id));Assert.Equal(events,await db.Dokumentaktivitaeten.CountAsync(e=>e.DokumentId==docs[2].Id&&e.Text.StartsWith("Automatische interne")));
        await muster.Entscheiden(admin.Id,id,new(3,"Pausiert",null));var paused=await Analyse(2,new(akte.Id,sender.Id));Assert.Null(paused.AutomatikMusterId);Assert.Null(paused.Felder.Single(f=>f.Name=="aussteller").Wert);
        await muster.Entscheiden(admin.Id,id,new(4,"Aktiv",eingang[1],"Klassifizieren"));var drift=await Analyse(3,new(akte.Id,sender.Id));Assert.Null(drift.AutomatikMusterId);Assert.Empty(drift.MusterIds!);Assert.Contains(drift.Hinweise,h=>h.Contains("Layout"));
        var next=PosteingangTests.Json(await muster.Entwerfen(admin.Id,eingang[1],"aussteller"));Assert.Equal(2,next.GetProperty("musterVersion").GetInt32());Assert.NotEqual(id,next.GetProperty("id").GetString());Assert.Equal(2,await db.Dokumentmuster.CountAsync());Assert.Empty(await db.Rechnungen.ToArrayAsync());Assert.Empty(await db.Aufgaben.ToArrayAsync());
        // Later changes to a training decision invalidate its old permission rather than silently training on new data.
        var w=await db.Eingangsworkflows.SingleAsync(w=>w.Id==eingang[0]);w.Version++;await db.SaveChangesAsync();var invalid=await Analyse(2,new(akte.Id,sender.Id));Assert.Null(invalid.AutomatikMusterId);Assert.Contains(invalid.Hinweise,h=>h.Contains("Prüfbeleg geändert"));
    }
    [PaketFact] public async Task EchteMonatsPdfKorrekturLerntAufDrittemDokumentUndAutomatikBleibtBegrenzt(){await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();await Durchlauf(db);}
    private sealed class PgPaketFact:FactAttribute{public PgPaketFact(){if(Environment.GetEnvironmentVariable("FZ_ANALYSE_POSTGRES")!="1"||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FZ_DOCUMENT_PACKAGE"))||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")))Skip="Explicit isolated PostgreSQL gate and local fictional PDF package required.";}}
    [PgPaketFact]public async Task MusterUpgradeUndEchteMonatsPdfAufIsoliertemPostgreSql18(){
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")){Database="postgres",Pooling=false,IncludeErrorDetail=false,LogParameters=false,Timeout=5};Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);var run=Guid.NewGuid().ToString("N");var name="fz_muster_test_"+run;
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(name)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
        cs.Database=name;FirmenDbContext Db()=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(cs.ConnectionString,n=>n.SetPostgresVersion(18,0)).Options);
        async Task Ziel(FirmenDbContext d){await d.Database.OpenConnectionAsync();await using var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)d.Database.GetDbConnection());Assert.Equal(name,await q.ExecuteScalarAsync());}
        await using(var d=Db()){await Ziel(d);await d.GetService<IMigrator>().MigrateAsync("20261021010000_Dokumentanalyse");d.Benutzer.Add(new(){Login="vorhandenes-konto",Anzeigename="Bestehender synthetischer Zugang",PasswortHash="unveraenderter-synthetischer-hash"});await d.SaveChangesAsync();await d.Database.MigrateAsync();Assert.False(d.Database.HasPendingModelChanges());Assert.Equal("unveraenderter-synthetischer-hash",(await d.Benutzer.SingleAsync()).PasswortHash);}
        var role="fz_app_muster_"+run[..16];var secret=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(cs.ConnectionString,role,secret);cs.Username=role;cs.Password=secret;
        await using(var d=Db()){await Ziel(d);await Durchlauf(d);}
        await using(var d=Db()){await Ziel(d);Assert.Equal(2,await d.Dokumentmuster.CountAsync());Assert.Contains(await d.Dokumente.Select(d=>d.AnalyseJson).ToArrayAsync(),j=>j!=null&&j.Contains("Muster:"));Assert.True(await d.Dokumentaktivitaeten.AnyAsync(a=>a.Text.StartsWith("Automatische interne Klassifizierung")));}
    }
}
