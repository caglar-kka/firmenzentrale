using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Betriebsplattform.Web.Persistenz;

public sealed record Persistenzkonfiguration(string Provider,string? Verbindung,string? Dateiablage)
{
    public static Persistenzkonfiguration Lesen(IConfiguration config)
    {
        var provider=config["FirmenZentrale:Persistence:Provider"]??"Demo";
        if(provider is not ("Demo" or "PostgreSql"))throw new InvalidOperationException("Unbekannter Datenhaltungsmodus. Demo oder PostgreSql ausdrücklich wählen.");
        if(provider=="Demo")return new(provider,null,null);
        var value=config.GetConnectionString("FirmenZentrale");
        if(string.IsNullOrWhiteSpace(value))throw new InvalidOperationException("PostgreSQL ist aktiviert. Die sichere lokale Verbindungskonfiguration fehlt; kein Demo-Fallback.");
        try{var cs=new NpgsqlConnectionStringBuilder(value);if(string.IsNullOrWhiteSpace(cs.Host)||string.IsNullOrWhiteSpace(cs.Database))throw new ArgumentException();cs.IncludeErrorDetail=false;cs.LogParameters=false;value=cs.ConnectionString;}
        catch(ArgumentException){throw new InvalidOperationException("Die PostgreSQL-Verbindungskonfiguration ist ungültig. Bitte lokal prüfen.");}
        return new(provider,value,config["FirmenZentrale:Persistence:DocumentRoot"]);
    }
    public override string ToString()=>"Persistence: "+Provider; // Never print credentials.
}

// Used only to scaffold/review migrations; creating a context never opens a connection.
public sealed class FirmenDbFactory : IDesignTimeDbContextFactory<FirmenDbContext>
{
    public FirmenDbContext CreateDbContext(string[] args)=>new(new DbContextOptionsBuilder<FirmenDbContext>()
        .UseNpgsql("Host=127.0.0.1;Database=firmenzentrale_design",o=>o.SetPostgresVersion(18,0)).Options);
}

public sealed class DatenbankStartpruefung(IDbContextFactory<FirmenDbContext> factory) : IHostedService
{
    public async Task StartAsync(CancellationToken token)
    {
        try{
            await using var db=await factory.CreateDbContextAsync(token);
            if(!await db.Database.CanConnectAsync(token))throw new InvalidOperationException();
            if((await db.Database.GetPendingMigrationsAsync(token)).Any())throw new InvalidOperationException();
        }catch(OperationCanceledException){throw;}
        catch(Exception){throw new InvalidOperationException("PostgreSQL ist nicht bereit: Erreichbarkeit, Rechte und ausdrücklich angewendete Migrationen lokal prüfen. Kein Demo-Fallback.");}
    }
    public Task StopAsync(CancellationToken token)=>Task.CompletedTask;
}

public static class Persistenzfehler
{
    public static (int Status,string Meldung) Beschreiben(Exception e)=>e switch{
        DbUpdateConcurrencyException=>(409,"Dieser Eintrag wurde zwischenzeitlich geändert. Bitte prüfen Sie den aktuellen Stand."),
        DbUpdateException{InnerException:PostgresException{SqlState:PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.RestrictViolation}}=>(409,"Die Zuordnung ist nicht mehr gültig. Bitte die beteiligten Einträge prüfen."),
        DbUpdateException{InnerException:PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}}=>(409,"Dieser Eintrag oder diese Zuordnung ist bereits vorhanden."),
        DbUpdateException{InnerException:PostgresException{SqlState:PostgresErrorCodes.CheckViolation or PostgresErrorCodes.NotNullViolation or PostgresErrorCodes.StringDataRightTruncation}}=>(400,"Bitte Pflichtfelder, Längen und Zuordnungen prüfen."),
        _=>(503,"Die Daten konnten nicht gespeichert oder geladen werden. Bitte später erneut versuchen.")};
}
