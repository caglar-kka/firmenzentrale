using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Persistenz;

public static class Persistenzverwaltung
{
    public static async Task<int> MigrationAnwenden(string[] args)
    {
        try{
            var config=new ConfigurationBuilder().AddUserSecrets(typeof(Persistenzverwaltung).Assembly,optional:true).AddEnvironmentVariables().AddCommandLine(args.Where(a=>a!="--apply-migrations").ToArray()).Build();
            var p=Persistenzkonfiguration.Lesen(config);
            if(p.Provider!="PostgreSql")throw new InvalidOperationException();
            await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(p.Verbindung!,o=>o.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
            await db.Database.MigrateAsync();Console.WriteLine("Die ausdrücklich angeforderte Datenbankmigration wurde angewendet.");return 0;
        }catch(Exception){Console.Error.WriteLine("Migration nicht angewendet. Modus, sichere lokale Verbindung, Rechte und Migrationen prüfen. Es wurde kein Demo-Fallback gestartet.");return 1;}
    }
}
