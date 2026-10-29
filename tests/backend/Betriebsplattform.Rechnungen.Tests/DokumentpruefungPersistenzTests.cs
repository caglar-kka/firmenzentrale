using System.Text.Json;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class DokumentpruefungPersistenzTests(ITestOutputHelper output)
{
    internal static async Task Durchlauf(FirmenDbContext db){
        var actor=new Benutzerkonto{Login="nora.lind",Anzeigename="Nora Lind",PasswortHash="synthetisch"};var other=new Benutzerkonto{Login="leon.kruse",Anzeigename="Leon Kruse",PasswortHash="synthetisch"};db.AddRange(actor,other);await db.SaveChangesAsync();
        var files=new PosteingangTests.Dateien();var intake=new FirmenDaten(db,new(true,null,null),new SynthetischeEingaenge.NurKatalog());var f=SynthetischeEingaenge.Datei("wartung");var doc=await intake.DokumentHinzufuegen(f.Bytes,f.Name,"text/plain",null,files);db.Freigaben.Add(new(){BenutzerId=actor.Id,DokumentId=doc.Id});await db.SaveChangesAsync();
        var clock=new MfaTests.Clock();var service=new EingangsDienst(db,new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")),clock);var analyse=new Dokumentanalysator();
        Assert.Equal(404,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Analysieren(other.Id,doc.Id,analyse,files,default))).Status);
        await service.Analysieren(actor.Id,doc.Id,analyse,files,default);var initial=doc.AnalyseJson;Assert.NotNull(initial);var version=doc.Version;
        await service.Analysieren(actor.Id,doc.Id,analyse,files,default);Assert.Equal(version,doc.Version);Assert.Equal(initial,doc.AnalyseJson);
        db.ChangeTracker.Clear();var loaded=PosteingangTests.Json(await service.AnalyseLesen(actor.Id,doc.Id));Assert.Equal("RE-2026-1042",loaded.GetProperty("analyse").GetProperty("felder").EnumerateArray().Single(x=>x.GetProperty("name").GetString()=="rechnungsnummer").GetProperty("wert").GetString());
        var a=JsonSerializer.Deserialize<Dokumentanalyse>(initial!,Dokumentanalysator.Json)!;var fields=a.Felder.Select(x=>x.Status=="Vorschlag"?x with{Status="Bestätigt"}:x with{Wert=null,AbschnittId=null,AnkerAbschnittId=null,Status="Manuell"}).ToArray();
        var data=EingangsDienst.Vorschlag(doc) with{Felder=fields};
        var bad=data with{Felder=fields.Select(x=>x.Name=="rechnungsnummer"?x with{AbschnittId="erfundene-position"}:x).ToArray()};
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Pruefen(actor.Id,doc.Id,new(0,"Zur Prüfung zurückstellen",bad,null,null,null,"Original geprüft","",true)));Assert.Empty(await db.Eingangsworkflows.ToArrayAsync());
        var corrected=data with{Referenz="Kontorhaus Südhafen - Korrektur",Felder=fields.Select(x=>x.Name=="referenz"?x with{Wert="Kontorhaus Südhafen - Korrektur",AbschnittId=null,AnkerAbschnittId=null,Status="Manuell"}:x).ToArray()};
        await service.Pruefen(actor.Id,doc.Id,new(0,"Zur Prüfung zurückstellen",corrected,null,null,null,"Prüfung fortsetzen","",true));db.ChangeTracker.Clear();
        var saved=await db.Eingangsworkflows.AsNoTracking().SingleAsync();var read=JsonSerializer.Deserialize<Eingangsangaben>(saved.AngabenJson)!;Assert.Equal("Kontorhaus Südhafen - Korrektur",read.Referenz);Assert.Null(read.Felder!.Single(x=>x.Name=="referenz").AbschnittId);Assert.True(await db.Dokumentaktivitaeten.AnyAsync(x=>x.EingangsId==saved.Id&&x.Text.Contains("Prüffeld referenz")));
        Assert.Equal(f.Bytes,await files.Lesen(doc.StorageKey,default));Assert.Empty(await db.Aufgaben.ToArrayAsync());Assert.Empty(await db.Rechnungen.ToArrayAsync());
        files.Werte[doc.StorageKey][0]^=1;
        Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>service.Analysieren(actor.Id,doc.Id,analyse,files,default))).Status);
        files.Werte[doc.StorageKey][0]^=1;
    }
    [Fact] public async Task AnalyseKorrekturCacheRechteUndOriginalAufSqlite(){await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();await Durchlauf(db);}
    private sealed class PgFact:FactAttribute{public PgFact(){if(Environment.GetEnvironmentVariable("FZ_ANALYSE_POSTGRES")!="1")Skip="Explicit isolated PostgreSQL gate required.";}}
    [PgFact] public async Task AdditivesUpgradeUndKorrekturenAufNeuemPostgreSql18(){
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")){Database="postgres",Pooling=false,IncludeErrorDetail=false,LogParameters=false,Timeout=5};Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);
        var name="fz_analyse_test_"+Guid.NewGuid().ToString("N");
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(name)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
        cs.Database=name;var options=new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(cs.ConnectionString,n=>n.SetPostgresVersion(18,0)).Options;
        async Task Ziel(FirmenDbContext db){await db.Database.OpenConnectionAsync();await using var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)db.Database.GetDbConnection());Assert.Equal(name,await q.ExecuteScalarAsync());}
        await using(var db=new FirmenDbContext(options)){await Ziel(db);await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());await Durchlauf(db);}
        await using(var db=new FirmenDbContext(options)){await Ziel(db);Assert.NotNull((await db.Dokumente.AsNoTracking().SingleAsync()).AnalyseJson);Assert.NotNull(JsonSerializer.Deserialize<Eingangsangaben>((await db.Eingangsworkflows.SingleAsync()).AngabenJson)!.Felder);}
        output.WriteLine("Isolated PostgreSQL18 database: "+name+"; source and correction retained after new DbContext.");
    }
}
