using Betriebsplattform.Web;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Betrieb;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class FeaturepaketePostgreSqlTests(ITestOutputHelper output)
{
    private sealed class Rendezvous(Type entity):SaveChangesInterceptor
    {
        private int arrived;private readonly TaskCompletionSource ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e,InterceptionResult<int> result,CancellationToken ct=default)
        {
            if(e.Context!.ChangeTracker.Entries().Any(x=>x.Entity.GetType()==entity&&x.State==EntityState.Modified)){
                if(Interlocked.Increment(ref arrived)==2)ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(10),ct);
            }return result;
        }
    }
    private sealed class IsoliertAttribute:FactAttribute{public IsoliertAttribute(){if(Environment.GetEnvironmentVariable("FZ_FEATURE_POSTGRES")!="1")Skip="Explicit isolated PostgreSQL test required.";}}
    [Isoliert] public async Task AdditiveMigrationBestandskontenRollenAufgabenUndKonkurrierendeSchreibvorgaenge()
    {
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(ApiAnwendung).Assembly,optional:true).Build();var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale"));
        Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);cs.Database="postgres";cs.Pooling=false;cs.IncludeErrorDetail=false;cs.LogParameters=false;cs.Timeout=5;
        var run=Guid.NewGuid().ToString("N");var name="fz_features_test_"+run;
        await using(var c=new NpgsqlConnection(cs.ConnectionString)){await c.OpenAsync();Assert.Equal(18,c.PostgreSqlVersion.Major);await using(var q=new NpgsqlCommand("select current_database()",c))Assert.Equal("postgres",await q.ExecuteScalarAsync());await using var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(name)+" TEMPLATE template0",c);await create.ExecuteNonQueryAsync();}
        cs.Database=name;var adminConnection=cs.ConnectionString;
        FirmenDbContext Db(string conn)=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(conn,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
        async Task Isolated(FirmenDbContext d){Assert.Equal(name,new NpgsqlConnectionStringBuilder(d.Database.GetConnectionString()).Database);await d.Database.OpenConnectionAsync();await using var q=new NpgsqlCommand("select current_database()",(NpgsqlConnection)d.Database.GetDbConnection());Assert.Equal(name,await q.ExecuteScalarAsync());}
        var pass=MfaTests.Secret();var h=Anmeldung.Passwortdienst();var user=new Benutzerkonto{Login="synthetisch",Anzeigename="Synthetisches Team"};user.PasswortHash=h.HashPassword(user,pass);
        await using(var d=Db(adminConnection)){
            await Isolated(d);await d.GetService<IMigrator>().MigrateAsync("20261019200000_AuthenticatorMfa");
            await d.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Benutzer\" (\"Id\",\"Login\",\"Anzeigename\",\"PasswortHash\",\"Rolle\",\"Aktiv\",\"Sicherheitskennung\",\"Fehlversuche\",\"ErstelltAm\",\"GeaendertAm\",\"Version\") VALUES ({user.Id},{user.Login},{user.Anzeigename},{user.PasswortHash},{user.Rolle},{user.Aktiv},{user.Sicherheitskennung},0,{user.ErstelltAm},{user.GeaendertAm},1)");
            var migration=new Betriebsplattform.Web.Persistenz.Migrationen.MfaRichtlinieUndAufgaben();Assert.Single(migration.UpOperations.OfType<AddColumnOperation>());Assert.Equal(4,migration.UpOperations.OfType<CreateTableOperation>().Count());Assert.DoesNotContain(migration.UpOperations,o=>o is DropTableOperation or DropColumnOperation or AlterColumnOperation);
            await Isolated(d);await d.Database.MigrateAsync();Assert.False(d.Database.HasPendingModelChanges());var preserved=await d.Benutzer.SingleAsync();Assert.Equal(user.PasswortHash,preserved.PasswortHash);Assert.Equal("Mitarbeiter",preserved.Rolle);Assert.False(preserved.MfaNeueinrichtung);
        }
        var runtimeName="fz_app_features_"+run[..16];var runtimePass=MfaTests.Secret();await Betriebsverwaltung.Laufzeitrolle(adminConnection,runtimeName,runtimePass);cs.Username=runtimeName;cs.Password=runtimePass;var runtime=cs.ConnectionString;
        var clock=new MfaTests.Clock();var zone=TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");Aufgabeneingabe Input(int v=0)=>new("Synthetische Wiedervorlage","Nur Testdaten",user.Id,"Normal","Offen",new(2026,10,19),new(12,30),v);
        string id;int version;
        await using(var d=Db(runtime)){await Isolated(d);var service=new AufgabenDienst(d,clock,zone);var a=await service.Speichern(user.Id,null,Input());id=a.Id;version=a.Version;Assert.NotNull(a.FaelligUtc);Assert.Single(await (await service.Sichtbar(user.Id)).ToArrayAsync());}
        await using var one=Db(runtime);await using var two=Db(runtime);await one.Aufgaben.SingleAsync(a=>a.Id==id);await two.Aufgaben.SingleAsync(a=>a.Id==id);
        await new AufgabenDienst(one,clock,zone).Speichern(user.Id,id,Input(version) with{Status="Erledigt"});
        var conflict=await Assert.ThrowsAsync<Betriebsplattform.Web.Akten.Arbeitsfehler>(()=>new AufgabenDienst(two,clock,zone).Speichern(user.Id,id,Input(version) with{Status="In Bearbeitung"}));Assert.Equal(409,conflict.Status);
        await using(var d=Db(runtime)){Assert.Equal("Erledigt",(await d.Aufgaben.SingleAsync()).Status);Assert.Equal(2,await d.Aufgabenverlauf.CountAsync());}
        // Actual concurrent PostgreSQL updates, not an in-memory or sequential approximation.
        var protection=new EphemeralDataProtectionProvider();var mfa=new MfaDienst(protection,clock);var admins=new List<string>();
        await using(var d=Db(runtime)){
            for(var i=0;i<4;i++){
                var u=await Anmeldung.Anlegen(d,h,new("race-admin-"+i,"Synthetischer Admin "+i,pass,"Administrator"));
                await mfa.Vorbereiten(d,h,u.Id,new(pass,null));await mfa.Verwalten(d,h,u.Id,new(pass,MfaTests.Code(protection,u,clock)),"aktivieren");admins.Add(u.Id);
            }
            d.MfaRegeln.Add(new(){Version=1});await d.SaveChangesAsync();
        }
        FirmenDbContext Racing(Rendezvous gate)=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(runtime,n=>n.SetPostgresVersion(18,0)).AddInterceptors(gate).Options);
        async Task<MfaPruefung> Proof(string id){await using var d=Db(runtime);return new(pass,MfaTests.Code(protection,await d.Benutzer.SingleAsync(u=>u.Id==id),clock));}
        static async Task<bool> Attempt(Func<Task> action){try{await action();return true;}catch(DbUpdateConcurrencyException){return false;}catch(Betriebsplattform.Web.Akten.Arbeitsfehler e)when(e.Status is 400 or 403 or 409){return false;}}
        clock.Next();var ruleGate=new Rendezvous(typeof(MfaRegel));var proofA=await Proof(admins[0]);var proofB=await Proof(admins[1]);
        async Task<bool> Rule(int actor,MfaPruefung proof){await using var d=Racing(ruleGate);return await Attempt(async()=>{await MfaRichtlinie.Aendern(d,admins[actor],new(true,true,1,proof),mfa,h);});}
        var rules=await Task.WhenAll(Rule(0,proofA),Rule(1,proofB));Assert.Single(rules,x=>x);
        await using(var d=Db(runtime)){Assert.Equal(2,(await d.MfaRegeln.SingleAsync()).Version);Assert.Single(await d.Sicherheitsereignisse.Where(x=>x.Aktion=="MFA-Rollenrichtlinie geändert").ToArrayAsync());}
        clock.Next();string requestId;int requestVersion;int targetVersion;
        await using(var d=Db(runtime)){
            targetVersion=(await d.Benutzer.SingleAsync(u=>u.Id==admins[3])).Version;
            var request=await MfaNotfall.Antrag(d,mfa,h,clock,admins[0],new(admins[3],"Synthetische persönliche Identitätsprüfung",true,targetVersion,await Proof(admins[0])));requestId=request.Id;requestVersion=request.Version;
        }
        clock.Next();var approveGate=new Rendezvous(typeof(MfaNotfallfreigabe));var p1=await Proof(admins[1]);var p2=await Proof(admins[2]);
        async Task<bool> Approve(int actor,MfaPruefung proof){await using var d=Racing(approveGate);return await Attempt(async()=>{await MfaNotfall.Aktion(d,mfa,h,clock,admins[actor],requestId,new(requestVersion,proof),false);});}
        Assert.Single(await Task.WhenAll(Approve(1,p1),Approve(2,p2)),x=>x);
        await using(var d=Db(runtime)){var r=await d.MfaNotfaelle.SingleAsync();Assert.NotNull(r.GenehmigerId);Assert.Null(r.AusgefuehrtAm);requestVersion=r.Version;Assert.Single(await d.Sicherheitsereignisse.Where(x=>x.Ressource==requestId&&x.Aktion=="MFA-Notfallfreigabe genehmigt").ToArrayAsync());}
        clock.Next();var executeProof=await Proof(admins[0]);
        async Task<bool> Execute(){await using var d=Db(runtime);return await Attempt(async()=>{await MfaNotfall.Aktion(d,mfa,h,clock,admins[0],requestId,new(requestVersion,executeProof),true);});}
        Assert.Single(await Task.WhenAll(Execute(),Execute()),x=>x);
        await using(var d=Db(runtime)){
            var target=await d.Benutzer.SingleAsync(u=>u.Id==admins[3]);Assert.Null(target.MfaGeheimnis);Assert.Null(target.MfaRecoveryHashes);Assert.True(target.MfaNeueinrichtung);Assert.True(await MfaRichtlinie.Einrichtung(d,target.Id));Assert.NotNull(target.WiederherstellungHash);Assert.True((await d.MfaRegeln.SingleAsync()).Administrator);
            Assert.Single(await d.Sicherheitsereignisse.Where(x=>x.Ressource==target.Id&&x.Aktion.StartsWith("MFA-Notfallreset")).ToArrayAsync());
        }
        output.WriteLine("Isolierte PostgreSQL-18-Datenbank: "+name+"; Laufzeitrolle: "+runtimeName+"; Hauptdatenbank nicht geöffnet.");
    }
}
