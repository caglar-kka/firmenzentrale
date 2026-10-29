using System.Security.Cryptography;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Betrieb;

public sealed record Bestandsbericht(long Dokumente,long Fehlend,long Abweichend,long Verwaist,long Temporaer,string[] Hinweise)
{ public bool Konsistent=>Fehlend+Abweichend+Verwaist+Temporaer==0; }

public static class Dokumentbestand
{
    public static async Task<Bestandsbericht> Pruefen(FirmenDbContext db,string root,CancellationToken ct=default)
    {
        var f=new LokaleDokumentdateien(root);await f.StartAsync(ct);long anzahl=0,fehlend=0,abweichend=0,verwaist=0,temp=0;var hinweise=new List<string>();
        void Meldung(string text){if(hinweise.Count<200)hinweise.Add(text);}
        string? letzte=null;
        while(true){
            var query=db.Dokumente.AsNoTracking();if(letzte is not null)query=query.Where(d=>string.Compare(d.Id,letzte)>0);
            var seite=await query.OrderBy(d=>d.Id).Select(d=>new{d.Id,d.StorageKey,d.Groesse,d.Hash}).Take(200).ToArrayAsync(ct);if(seite.Length==0)break;
            foreach(var d in seite){anzahl++;try{await using var stream=await f.Oeffnen(d.StorageKey,d.Groesse,d.Hash,ct);}catch(Akten.Arbeitsfehler e){if(e.Status==404)fehlend++;else abweichend++;Meldung("Original prüfen: "+d.Id);}letzte=d.Id;}
        }
        // Optional new business artifacts share the same private storage and integrity gate.
        var nachweise=await db.Database.SqlQueryRaw<bool>(db.Database.IsNpgsql()?"SELECT to_regclass('public.\"Leistungsnachweise\"') IS NOT NULL AS \"Value\"":"SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type='table' AND name='Leistungsnachweise') AS Value").SingleAsync(ct);
        if(nachweise){string? ende=null;while(true){var q=db.Leistungsnachweise.AsNoTracking();if(ende!=null)q=q.Where(n=>string.Compare(n.Id,ende)>0);var seite=await q.OrderBy(n=>n.Id).Take(200).ToArrayAsync(ct);if(seite.Length==0)break;foreach(var n in seite){anzahl++;ende=n.Id;if(string.IsNullOrWhiteSpace(n.StorageKey)){fehlend++;Meldung("Nachweis ohne private Speicherreferenz: "+n.Id);continue;}try{var bytes=await f.Lesen(n.StorageKey,ct);if(Betriebsplattform.Web.Leistungen.Leistungsdienst.Hash(bytes)!=n.Hash){abweichend++;Meldung("Nachweisprüfsumme prüfen: "+n.Id);}}catch(Akten.Arbeitsfehler e){if(e.Status==404)fehlend++;else abweichend++;Meldung("Nachweis prüfen: "+n.Id);}}}}
        foreach(var batch in Directory.EnumerateFiles(root,"*.bin").Chunk(200)){
            var keys=batch.Select(Path.GetFileNameWithoutExtension).ToArray();var bekannt=await db.Dokumente.AsNoTracking().Where(d=>keys.Contains(d.StorageKey)).Select(d=>d.StorageKey).ToArrayAsync(ct);var set=bekannt.ToHashSet();if(nachweise)foreach(var key in await db.Leistungsnachweise.AsNoTracking().Where(n=>keys.Contains(n.StorageKey)).Select(n=>n.StorageKey).ToArrayAsync(ct))set.Add(key);
            foreach(var key in keys)if(!set.Contains(key!)){verwaist++;Meldung("Datei ohne Metadaten: "+key);}
        }
        foreach(var path in Directory.EnumerateFiles(root,"*.pending")){temp++;Meldung("Unvollständige Datei: "+Path.GetFileName(path));}
        foreach(var path in Directory.EnumerateFiles(root).Where(p=>Path.GetExtension(p) is not (".bin" or ".pending")&&Path.GetFileName(p)!="restore-complete.json")){temp++;Meldung("Unbekannte Ablagedatei: "+Path.GetFileName(path));}
        foreach(var path in Directory.EnumerateDirectories(root).Where(p=>Path.GetFileName(p) is not ("keyring" or "recovered-keyring"))){temp++;Meldung("Unbekannter Ablageordner: "+Path.GetFileName(path));}
        return new(anzahl,fehlend,abweichend,verwaist,temp,hinweise.ToArray());
    }
}

// A host holds the shared PostgreSQL lease for its lifetime. Maintenance requires exclusivity.
public sealed class Betriebssperre(Persistenzkonfiguration config) : IHostedService,IAsyncDisposable
{
    public const long Schluessel=728164902;
    private Npgsql.NpgsqlConnection? connection;
    public async Task StartAsync(CancellationToken ct){
        connection=new(new Npgsql.NpgsqlConnectionStringBuilder(config.Verbindung){Pooling=false}.ConnectionString);await connection.OpenAsync(ct);
        await using var cmd=new Npgsql.NpgsqlCommand("SELECT pg_try_advisory_lock_shared(728164902)",connection);
        if(!Equals(await cmd.ExecuteScalarAsync(ct),true))throw new InvalidOperationException("Wartung läuft. Anwendung nicht gestartet.");
    }
    public async Task StopAsync(CancellationToken ct){if(connection is not null){await using var cmd=new Npgsql.NpgsqlCommand("SELECT pg_advisory_unlock_shared(728164902)",connection);await cmd.ExecuteNonQueryAsync(ct);await connection.DisposeAsync();connection=null;}}
    public async ValueTask DisposeAsync(){if(connection is not null)await StopAsync(CancellationToken.None);}
}
public sealed class PilotStartpruefung(IDbContextFactory<FirmenDbContext> factory,Persistenzkonfiguration config,Sicherheit.Pilotoptionen pilot,ILogger<PilotStartpruefung> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct){if(!pilot.Aktiv)return;await using var db=await factory.CreateDbContextAsync(ct);
        await using var c=new Npgsql.NpgsqlConnection(config.Verbindung);await c.OpenAsync(ct);
        if(c.PostgreSqlVersion.Major!=18)throw new InvalidOperationException("Pilot benötigt PostgreSQL 18.");
        await using var cmd=new Npgsql.NpgsqlCommand("SELECT r.rolsuper OR r.rolcreatedb OR r.rolcreaterole OR r.rolreplication OR r.rolbypassrls OR has_schema_privilege(current_user,'public','CREATE') OR pg_has_role(current_user,'pg_read_server_files','MEMBER') OR pg_has_role(current_user,'pg_write_server_files','MEMBER') OR pg_has_role(current_user,'pg_execute_server_program','MEMBER') OR EXISTS(SELECT 1 FROM pg_roles p WHERE (p.rolsuper OR p.rolcreatedb OR p.rolcreaterole OR p.rolreplication OR p.rolbypassrls) AND pg_has_role(current_user,p.oid,'MEMBER')) OR EXISTS(SELECT 1 FROM pg_database d WHERE d.datname=current_database() AND d.datdba=r.oid) FROM pg_roles r WHERE r.rolname=current_user",c);
        if(!Equals(await cmd.ExecuteScalarAsync(ct),false))throw new InvalidOperationException("Pilot benötigt eine eingeschränkte Datenbank-Laufzeitrolle; Administrationszugang ist nicht zulässig.");
        if(!await db.Benutzer.AnyAsync(u=>u.Aktiv&&u.Rolle=="Administrator",ct))throw new InvalidOperationException("Initialen Administrator vor Pilotstart kontrolliert anlegen.");
        var bestand=await Dokumentbestand.Pruefen(db,config.Dateiablage!,ct);
        if(!bestand.Konsistent){logger.LogError("Dokumentbestand inkonsistent: {Fehlend} fehlend, {Abweichend} abweichend, {Verwaist} verwaist, {Temporaer} unvollständig/unbekannt.",bestand.Fehlend,bestand.Abweichend,bestand.Verwaist,bestand.Temporaer);throw new InvalidOperationException("Dokumentbestand inkonsistent. Lokale Wartungsprüfung ausführen; es wurde nichts gelöscht.");}
    }
    public Task StopAsync(CancellationToken ct)=>Task.CompletedTask;
}
