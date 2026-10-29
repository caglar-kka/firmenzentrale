using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Betrieb;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class PersistenterScanordnerTests
{
    private sealed class Unterbruch(CancellationTokenSource cts):IMalwarepruefung{public Task Freigeben(byte[] b,CancellationToken ct){cts.Cancel();ct.ThrowIfCancellationRequested();return Task.CompletedTask;}}
    private sealed class Callback(Func<Task> action):IMalwarepruefung{public async Task Freigeben(byte[] b,CancellationToken ct){await action();throw new Arbeitsfehler("Alter Bearbeiter beendet sich nach Leasingwechsel.",503);}}
    [Fact]public Task EchteDateiMalwareSperreNeustartWiederaufnahmeUndEinmaligerEingang()=>Durchlauf();
    private async Task Durchlauf(Func<FirmenDbContext>? database=null){
        var root=Path.Combine(Path.GetTempPath(),"mbop-scan-test-"+Guid.NewGuid().ToString("N"));var input=Path.Combine(root,"eingang");Directory.CreateDirectory(input);var file=SynthetischeEingaenge.Datei("wartung-pdf");var path=Path.Combine(input,file.Name);var state=Path.Combine(root,"scan.sqlite");var files=new LokaleDokumentdateien(Path.Combine(root,"originale"));var clock=new MfaTests.Clock();
        FirmenDbContext Db()=>database?.Invoke()??new(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite("Data Source="+state+";Pooling=False").Options);
        string user;
        try{
            await using(var db=Db()){if(database is null)await db.Database.EnsureCreatedAsync();var u=new Benutzerkonto{Login="nora.scan",Anzeigename="Nora Lind - Dokumenteneingang",PasswortHash="synthetisch"};db.Add(u);await db.SaveChangesAsync();user=u.Id;}
            var options=new Scanordneroptionen("kontorhaus",input,user);var adapter=new PersistenterScanordner(options);
            async Task Lauf(PersistenterScanordner a,IMalwarepruefung gate,CancellationToken ct=default){await using var db=Db();var aufgaben=new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));await a.Pruefen(db,new FirmenDaten(db,new(true,null,null),gate),files,new EingangsDienst(db,aufgaben,clock),new Dokumentanalysator(),clock,ct);}
            await File.WriteAllBytesAsync(path,file.Bytes);await Lauf(adapter,new NichtKonfigurierterScanner());await using(var db=Db())Assert.Empty(await db.Scanauftraege.ToArrayAsync());
            await Lauf(adapter,new NichtKonfigurierterScanner());await using(var db=Db()){Assert.Equal("Blockiert",(await db.Scanauftraege.SingleAsync()).Status);Assert.Empty(await db.Dokumente.ToArrayAsync());}
            // Explicit test-only retry; production gate is never changed.
            await using(var db=Db()){var j=await db.Scanauftraege.SingleAsync();j.Status="Wartend";await db.SaveChangesAsync();}
            adapter=new(options);await Lauf(adapter,new SynthetischeEingaenge.NurKatalog());await Lauf(adapter,new Callback(async()=>{await using var db=Db();var j=await db.Scanauftraege.SingleAsync();j.LeaseId="anderer-bearbeiter";j.LeasingBis=clock.Now.AddMinutes(15);await db.SaveChangesAsync();}));
            await using(var db=Db()){var j=await db.Scanauftraege.SingleAsync();Assert.Equal("In Bearbeitung",j.Status);Assert.Equal("anderer-bearbeiter",j.LeaseId);}
            clock.Now=clock.Now.AddMinutes(16);adapter=new(options);await Lauf(adapter,new SynthetischeEingaenge.NurKatalog());await Lauf(adapter,new SynthetischeEingaenge.NurKatalog());
            int version;
            await using(var db=Db()){Assert.Equal("Erledigt",(await db.Scanauftraege.SingleAsync()).Status);var d=await db.Dokumente.SingleAsync();version=d.Version;Assert.Single(await db.Dokumentaktivitaeten.Where(e=>e.Kanal!=null).ToArrayAsync());Assert.Equal("Scan-Ordner",d.Kanal);Assert.Equal(file.Bytes,await files.Lesen(d.StorageKey,default));var j=await db.Scanauftraege.SingleAsync();j.Status="In Bearbeitung";j.LeasingBis=clock.Now.AddMinutes(-1);await db.SaveChangesAsync();}
            adapter=new(options);await Lauf(adapter,new SynthetischeEingaenge.NurKatalog());await Lauf(adapter,new SynthetischeEingaenge.NurKatalog());await File.WriteAllBytesAsync(Path.Combine(input,"Wartungsrechnung erneut.pdf"),file.Bytes);await Lauf(adapter,new SynthetischeEingaenge.NurKatalog());await Lauf(adapter,new SynthetischeEingaenge.NurKatalog());
            await using(var db=Db()){Assert.Equal("Erledigt",(await db.Scanauftraege.SingleAsync()).Status);Assert.Equal(version,(await db.Dokumente.SingleAsync()).Version);Assert.Single(await db.Dokumentaktivitaeten.Where(e=>e.Kanal!=null).ToArrayAsync());Assert.Empty(await db.Aufgaben.ToArrayAsync());}
            Assert.Equal(file.Bytes,await File.ReadAllBytesAsync(path));
        }finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()),Path.GetFullPath(root));Directory.Delete(root,true);}
    }
    [Fact]public async Task SchreibendeDateiUndAbbruchBleibenWiederaufnehmbar(){
        var root=Path.Combine(Path.GetTempPath(),"mbop-scan-test-"+Guid.NewGuid().ToString("N"));var input=Path.Combine(root,"eingang");Directory.CreateDirectory(input);var file=SynthetischeEingaenge.Datei("wartung-pdf");var path=Path.Combine(input,file.Name);var clock=new MfaTests.Clock();var files=new LokaleDokumentdateien(Path.Combine(root,"originale"));
        FirmenDbContext Db()=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite("Data Source="+Path.Combine(root,"state.sqlite")+";Pooling=False").Options);
        try{
            string actor;await using(var db=Db()){await db.Database.EnsureCreatedAsync();var u=new Benutzerkonto{Login="scan",Anzeigename="Dokumenteneingang",PasswortHash="synthetisch"};db.Add(u);await db.SaveChangesAsync();actor=u.Id;}
            var options=new Scanordneroptionen("eingang",input,actor);var adapter=new PersistenterScanordner(options);
            async Task Lauf(IMalwarepruefung gate,CancellationToken ct=default){await using var db=Db();await adapter.Pruefen(db,new FirmenDaten(db,new(true,null,null),gate),files,new EingangsDienst(db,new AufgabenDienst(db,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")),clock),new Dokumentanalysator(),clock,ct);}
            await using(var writer=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.ReadWrite)){await writer.WriteAsync(file.Bytes);await writer.FlushAsync();await Lauf(new SynthetischeEingaenge.NurKatalog());await Lauf(new SynthetischeEingaenge.NurKatalog());await using var db=Db();Assert.Empty(await db.Scanauftraege.ToArrayAsync());}
            using var cancelled=new CancellationTokenSource();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Lauf(new Unterbruch(cancelled),cancelled.Token));
            await using(var db=Db()){Assert.Equal("In Bearbeitung",(await db.Scanauftraege.SingleAsync()).Status);Assert.Empty(await db.Dokumente.ToArrayAsync());}
            clock.Now=clock.Now.AddMinutes(16);adapter=new(options);await Lauf(new SynthetischeEingaenge.NurKatalog());await Lauf(new SynthetischeEingaenge.NurKatalog());
            await using(var db=Db()){Assert.Equal("Erledigt",(await db.Scanauftraege.SingleAsync()).Status);Assert.Single(await db.Dokumente.ToArrayAsync());Assert.Single(await db.Dokumentaktivitaeten.Where(e=>e.Kanal!=null).ToArrayAsync());}
        }finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()),Path.GetFullPath(root));Directory.Delete(root,true);}
    }
    private sealed class PgFact:FactAttribute{public PgFact(){if(Environment.GetEnvironmentVariable("FZ_ANALYSE_POSTGRES")!="1")Skip="Explicit isolated PostgreSQL18 gate required.";}}
    [PgFact]public async Task ScanMigrationNeustartUndIdempotenzAufIsoliertemPostgreSql18(){
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")){Database="postgres",Pooling=false,IncludeErrorDetail=false,LogParameters=false,Timeout=5};Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);var run=Guid.NewGuid().ToString("N");var name="fz_scan_test_"+run;
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(name)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
        cs.Database=name;FirmenDbContext Db(){var d=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(cs.ConnectionString,n=>n.SetPostgresVersion(18,0)).Options);d.Database.OpenConnection();using var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)d.Database.GetDbConnection());Assert.Equal(name,q.ExecuteScalar());return d;}
        await using(var d=Db()){await d.Database.MigrateAsync();Assert.False(d.Database.HasPendingModelChanges());}
        var role="fz_app_scan_"+run[..16];var secret=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(cs.ConnectionString,role,secret);cs.Username=role;cs.Password=secret;
        await Durchlauf(Db);await using(var d=Db()){Assert.Equal("Erledigt",(await d.Scanauftraege.SingleAsync()).Status);Assert.Single(await d.Dokumente.ToArrayAsync());Assert.Single(await d.Dokumentaktivitaeten.Where(e=>e.Kanal!=null).ToArrayAsync());}
    }

}
