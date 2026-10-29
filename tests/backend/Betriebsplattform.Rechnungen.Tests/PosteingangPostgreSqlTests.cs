using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Betrieb;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class PosteingangPostgreSqlTests(ITestOutputHelper output)
{
    private sealed class Isoliert:FactAttribute{public Isoliert(){if(Environment.GetEnvironmentVariable("FZ_POSTEINGANG_POSTGRES")!="1")Skip="Explicit isolated PostgreSQL test required.";}}
    private sealed class Gleichzeitig:SaveChangesInterceptor
    {
        private int n;private readonly TaskCompletionSource ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData d,InterceptionResult<int> result,CancellationToken ct=default){if(d.Context!.ChangeTracker.Entries<Eingangsworkflow>().Any(e=>e.State==EntityState.Modified)){if(Interlocked.Increment(ref n)==2)ready.TrySetResult();await ready.Task.WaitAsync(TimeSpan.FromSeconds(10),ct);}return result;}
    }
    [Isoliert] public async Task AdditiveMigrationOriginalWorkflowUndParalleleFolgeaufgabeAufIsoliertemPostgreSql18()
    {
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale"));Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);cs.Database="postgres";cs.Pooling=false;cs.IncludeErrorDetail=false;cs.LogParameters=false;cs.Timeout=5;
        var run=Guid.NewGuid().ToString("N");var name="fz_posteingang_test_"+run;
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(name)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
        cs.Database=name;var admin=cs.ConnectionString;
        FirmenDbContext Db(string conn,Gleichzeitig? gate=null){var o=new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(conn,n=>n.SetPostgresVersion(18,0));if(gate is not null)o.AddInterceptors(gate);return new(o.Options);}
        await using(var d=Db(admin)){
            Assert.Equal(name,new NpgsqlConnectionStringBuilder(d.Database.GetConnectionString()).Database);await d.Database.OpenConnectionAsync();await using(var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)d.Database.GetDbConnection()))Assert.Equal(name,await q.ExecuteScalarAsync());
            await d.GetService<IMigrator>().MigrateAsync("20261019210000_MfaRichtlinieUndAufgaben");
            var migration=new Betriebsplattform.Web.Persistenz.Migrationen.PosteingangWorkflow();Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());Assert.Equal(3,migration.UpOperations.OfType<AddColumnOperation>().Count());Assert.DoesNotContain(migration.UpOperations,o=>o is DropColumnOperation or DropTableOperation or AlterColumnOperation);
            await d.Database.MigrateAsync();Assert.False(d.Database.HasPendingModelChanges());
        }
        var role="fz_app_post_"+run[..16];var password=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(admin,role,password);cs.Username=role;cs.Password=password;var runtime=cs.ConnectionString;
        await using(var d=Db(runtime))await PosteingangTests.Durchlauf(d);
        string actor,id;int version,count;
        await using(var d=Db(runtime)){actor=(await d.Benutzer.SingleAsync(u=>u.Login=="post-team")).Id;var w=await d.Eingangsworkflows.SingleAsync(w=>w.Status=="Zugeordnet"&&w.AufgabeId==null);id=w.Id;version=w.Version;count=await d.Aufgaben.CountAsync();}
        var gate=new Gleichzeitig();await using var one=Db(runtime,gate);await using var two=Db(runtime,gate);await one.Eingangsworkflows.SingleAsync(w=>w.Id==id);await two.Eingangsworkflows.SingleAsync(w=>w.Id==id);
        async Task<bool> Task(FirmenDbContext d){var clock=new MfaTests.Clock();var service=new EingangsDienst(d,new AufgabenDienst(d,clock,TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")),clock);try{await service.Folgeaufgabe(actor,id,new(version,true,false,new("Antwort auf erneuten Eingang","Synthetisch",actor,"Normal","Offen",null,null)));return true;}catch(Arbeitsfehler e)when(e.Status==409){return false;}}
        Assert.Single(await System.Threading.Tasks.Task.WhenAll(Task(one),Task(two)),v=>v);
        await using(var d=Db(runtime)){
            Assert.Equal(count+1,await d.Aufgaben.CountAsync());Assert.NotNull((await d.Eingangsworkflows.SingleAsync(w=>w.Id==id)).AufgabeId);
            var ex=await Assert.ThrowsAsync<PostgresException>(()=>d.Database.ExecuteSqlRawAsync("UPDATE \"Dokumentaktivitaeten\" SET \"Text\"='Nicht erlaubt'"));Assert.Equal("42501",ex.SqlState);
        }
        await using(var d=Db(runtime))await Mvp02APersistenzTests.Suchgrenze(d);
        output.WriteLine("Isolated PostgreSQL18: "+name+"; runtime role: "+role+"; main database never opened.");
    }
    [Isoliert] public async Task VersicherungsvorlageAufIsoliertemPostgreSqlMitLaufzeitrolleUndReload()
    {
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();
        var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")){Database="postgres",Pooling=false,IncludeErrorDetail=false,LogParameters=false,Timeout=5};
        Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);
        var run=Guid.NewGuid().ToString("N");var name="fz_regel_test_"+run;
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(name)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
        cs.Database=name;var admin=cs.ConnectionString;
        FirmenDbContext Db(string conn)=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(conn,n=>n.SetPostgresVersion(18,0)).Options);
        async Task Ziel(FirmenDbContext db){Assert.Equal(name,new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Database);await db.Database.OpenConnectionAsync();await using var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)db.Database.GetDbConnection());Assert.Equal(name,await q.ExecuteScalarAsync());}
        await using(var db=Db(admin)){await Ziel(db);await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());}
        var role="fz_app_regel_"+run[..16];var secret=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(admin,role,secret);cs.Username=role;cs.Password=secret;
        await using(var db=Db(cs.ConnectionString)){await Ziel(db);await PosteingangTests.Regelfall(db);}
        await using(var db=Db(cs.ConnectionString)){await Ziel(db);var task=await db.Aufgaben.AsNoTracking().SingleAsync();Assert.Contains("versicherung-antwort-v1",task.Beschreibung);Assert.Equal(task.Id,(await db.Eingangsworkflows.SingleAsync()).AufgabeId);Assert.Single(await db.Aufgabenverlauf.ToArrayAsync());}
        output.WriteLine("Isoliertes PostgreSQL18: "+name+"; eigene Laufzeitrolle; Regelquelle und Aufgabe nach neuem DbContext erhalten. Keine Hauptdatenbank geöffnet.");
    }

}
