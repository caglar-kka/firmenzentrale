using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Betriebsplattform.Web.Betrieb;

public sealed record Sicherungsdatei(string Name,long Bytes,string Sha256);
public sealed record Sicherungsmanifest(int Version,int PostgreSqlMajor,string[] Migrationen,DateTimeOffset Zeitpunkt,string Anwendung,Sicherungsdatei Datenbank,Sicherungsdatei[] Dokumente,Sicherungsdatei[] Schluessel);

public sealed class Sicherung(string werkzeuge)
{
    private static bool Key(string name)=>Regex.IsMatch(name,@"\A[a-f0-9]{32}\.bin\z");
    private static bool Ring(string name)=>Regex.IsMatch(name,@"\Akey-[a-fA-F0-9-]{36}\.xml\z");
    private static async Task<Sicherungsdatei> Hash(string path,string name,CancellationToken ct){if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Verknüpfte Sicherungsdatei unzulässig.");await using var f=File.OpenRead(path);return new(name,f.Length,Convert.ToHexString(await SHA256.HashDataAsync(f,ct)));}
    private static async Task Kopieren(string quelle,string ziel,CancellationToken ct){
        var info=new FileInfo(quelle);if((info.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Verknüpfte Dateien nicht zulässig.");
        await using var input=new FileStream(quelle,FileMode.Open,FileAccess.Read,FileShare.Read,81920,FileOptions.Asynchronous);
        await using var output=new FileStream(ziel,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,FileOptions.Asynchronous);await input.CopyToAsync(output,ct);await output.FlushAsync(ct);output.Flush(true);
    }
    private async Task Tool(string name,string verbindung,string datei,CancellationToken ct)
    {
        if(!Path.IsPathFullyQualified(werkzeuge))throw new InvalidOperationException("PostgreSQL-Werkzeugpfad ausdrücklich konfigurieren.");
        var cs=new NpgsqlConnectionStringBuilder(verbindung);var start=new ProcessStartInfo(Path.Combine(werkzeuge,name+".exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
        start.Environment["PGHOST"]=cs.Host;start.Environment["PGPORT"]=cs.Port.ToString();start.Environment["PGDATABASE"]=cs.Database;start.Environment["PGUSER"]=cs.Username;start.Environment["PGPASSWORD"]=cs.Password;start.Environment["PGCONNECT_TIMEOUT"]="5";
        if(name=="pg_dump"){foreach(var a in new[]{"--no-password","--format=custom","--no-owner","--no-acl","--file",datei})start.ArgumentList.Add(a);}
        else{foreach(var a in new[]{"--no-password","--exit-on-error","--single-transaction","--no-owner","--no-acl","--dbname",cs.Database!,datei})start.ArgumentList.Add(a);}
        using var proc=Process.Start(start)??throw new IOException("Datenbankwerkzeug nicht verfügbar.");var err=proc.StandardError.ReadToEndAsync(ct);var stdout=proc.StandardOutput.ReadToEndAsync(ct);
        try{using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromMinutes(10));await proc.WaitForExitAsync(timeout.Token);await err;await stdout;if(proc.ExitCode!=0)throw new IOException("Datenbank-Sicherung/Wiederherstellung fehlgeschlagen.");}
        catch{if(!proc.HasExited)proc.Kill(entireProcessTree:true);throw;}
        finally{start.Environment.Remove("PGPASSWORD");}
    }
    private static async Task<NpgsqlConnection> Exklusiv(string verbindung,CancellationToken ct){var c=new NpgsqlConnection(new NpgsqlConnectionStringBuilder(verbindung){Pooling=false}.ConnectionString);try{await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand("SELECT pg_try_advisory_lock(728164902)",c);if(!Equals(await cmd.ExecuteScalarAsync(ct),true))throw new InvalidOperationException("Anwendung vor Wartung stoppen.");return c;}catch{await c.DisposeAsync();throw;}}
    private static void Verschieden(string a,string b){a=Path.TrimEndingDirectorySeparator(Path.GetFullPath(a));b=Path.TrimEndingDirectorySeparator(Path.GetFullPath(b));if(a.Equals(b,StringComparison.OrdinalIgnoreCase)||a.StartsWith(b+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||b.StartsWith(a+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Getrennte Speicherorte erforderlich.");}
    public async Task<string> Erstellen(FirmenDbContext db,string verbindung,string storage,string ziel,CancellationToken ct=default)
    {
        Verschieden(storage,ziel);await new LokaleDokumentdateien(ziel).StartAsync(ct);await using var lease=await Exklusiv(verbindung,ct);
        if(lease.PostgreSqlVersion.Major!=18)throw new InvalidOperationException("Sicherung benötigt PostgreSQL 18.");
        if(!(await Dokumentbestand.Pruefen(db,storage,ct)).Konsistent)throw new InvalidDataException("Dokumentbestand inkonsistent; keine Sicherung freigegeben.");
        var path=Path.Combine(ziel,"backup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);Directory.CreateDirectory(Path.Combine(path,"documents"));Directory.CreateDirectory(Path.Combine(path,"keyring"));
        var dump=Path.Combine(path,"database.dump");await Tool("pg_dump",verbindung,dump,ct);
        var dateien=new List<Sicherungsdatei>();
        foreach(var batch in Directory.EnumerateFiles(storage,"*.bin").Chunk(200))foreach(var src in batch){var name=Path.GetFileName(src);if(!Key(name))throw new InvalidDataException("Ungültiger Storage-Key.");var dst=Path.Combine(path,"documents",name);await Kopieren(src,dst,ct);dateien.Add(await Hash(dst,name,ct));}
        var ring=new List<Sicherungsdatei>();var ringRoot=Path.Combine(storage,"keyring");if(Directory.Exists(ringRoot)){if((File.GetAttributes(ringRoot)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException();foreach(var src in Directory.EnumerateFiles(ringRoot)){var name=Path.GetFileName(src);if(!Ring(name))throw new InvalidDataException();var dst=Path.Combine(path,"keyring",name);await Kopieren(src,dst,ct);ring.Add(await Hash(dst,name,ct));}}
        var manifest=new Sicherungsmanifest(1,18,(await db.Database.GetAppliedMigrationsAsync(ct)).ToArray(),DateTimeOffset.UtcNow,typeof(Sicherung).Assembly.GetName().Version!.ToString(),await Hash(dump,"database.dump",ct),dateien.ToArray(),ring.ToArray());
        await File.WriteAllTextAsync(Path.Combine(path,"manifest.json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}),ct);await Pruefen(path,ct);return path;
    }
    public async Task<Sicherungsmanifest> Pruefen(string path,CancellationToken ct=default)
    {
        var file=new FileInfo(Path.Combine(path,"manifest.json"));if(!file.Exists||file.Length>32_000_000||(file.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Sicherungsmanifest fehlt oder ist unzulässig.");
        var m=JsonSerializer.Deserialize<Sicherungsmanifest>(await File.ReadAllTextAsync(file.FullName,ct),new JsonSerializerOptions{UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow})??throw new InvalidDataException();
        if(m.Version!=1||m.PostgreSqlMajor!=18||m.Migrationen is null||m.Migrationen.Length==0||m.Datenbank is null||m.Datenbank.Name!="database.dump"||m.Dokumente is null||m.Schluessel is null||string.IsNullOrWhiteSpace(m.Anwendung)||m.Zeitpunkt==default)throw new InvalidDataException("Sicherungsformat unvollständig oder inkompatibel.");
        async Task Check(Sicherungsdatei f,string ordner,bool key,bool ring=false){if(f.Bytes<=0||!Regex.IsMatch(f.Sha256??"",@"\A[A-F0-9]{64}\z")||(key&&!Key(f.Name))||(ring&&!Ring(f.Name)))throw new InvalidDataException("Ungültiger Sicherungseintrag.");var dir=Path.Combine(path,ordner);if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException();var actual=await Hash(Path.Combine(dir,f.Name),f.Name,ct);if(actual!=f)throw new InvalidDataException("Sicherungsdatei fehlt oder Integrität stimmt nicht.");}
        await Check(m.Datenbank,"",false);if(m.Dokumente.Select(x=>x.Name).Distinct().Count()!=m.Dokumente.Length||m.Schluessel.Select(x=>x.Name).Distinct().Count()!=m.Schluessel.Length)throw new InvalidDataException();
        foreach(var f in m.Dokumente)await Check(f,"documents",true);foreach(var f in m.Schluessel)await Check(f,"keyring",false,true);return m;
    }
    public async Task Wiederherstellen(string backup,string administration,string datenbank,string ziel,CancellationToken ct=default)
    {
        var manifest=await Pruefen(backup,ct);using(var modell=new FirmenDbFactory().CreateDbContext([]))if(!manifest.Migrationen.SequenceEqual(modell.Database.GetMigrations().Take(manifest.Migrationen.Length))||manifest.Migrationen.Length!=modell.Database.GetMigrations().Count())throw new InvalidDataException("Schema-Version ist nicht kompatibel.");
        if(!Regex.IsMatch(datenbank,@"\Afirmenzentrale_restore_[a-z0-9_]{1,32}\z")||Directory.Exists(ziel)||File.Exists(ziel))throw new InvalidOperationException("Restore benötigt eine neue getrennte Zieldatenbank und ein neues Verzeichnis.");
        Verschieden(backup,ziel);await new LokaleDokumentdateien(ziel).StartAsync(ct); // Prove actual access before touching the database.
        var cs=new NpgsqlConnectionStringBuilder(administration);if(cs.Database==datenbank)throw new InvalidOperationException();await using var admin=new NpgsqlConnection(administration);await admin.OpenAsync(ct);
        await using(var exists=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname=@name)",admin)){exists.Parameters.AddWithValue("name",datenbank);if(Equals(await exists.ExecuteScalarAsync(ct),true))throw new InvalidOperationException("Restore-Zieldatenbank existiert bereits.");}
        var owner="fz_restore_"+Guid.NewGuid().ToString("N");var password=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var ownerSql=new NpgsqlCommandBuilder().QuoteIdentifier(owner);
        await using(var createRole=new NpgsqlCommand("CREATE ROLE "+ownerSql+" LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS NOINHERIT PASSWORD '"+password+"'",admin))await createRole.ExecuteNonQueryAsync(ct);
        await using(var create=new NpgsqlCommand("CREATE DATABASE "+new NpgsqlCommandBuilder().QuoteIdentifier(datenbank)+" OWNER "+ownerSql,admin))await create.ExecuteNonQueryAsync(ct);
        var restoreConnection=new NpgsqlConnectionStringBuilder(administration){Database=datenbank,Username=owner,Password=password};
        try{await Tool("pg_restore",restoreConnection.ConnectionString,Path.Combine(backup,"database.dump"),ct);}
        finally{await using var disable=new NpgsqlCommand("ALTER ROLE "+ownerSql+" NOLOGIN",admin);await disable.ExecuteNonQueryAsync(CancellationToken.None);}
        cs.Database=datenbank;
        foreach(var f in manifest.Dokumente)await Kopieren(Path.Combine(backup,"documents",f.Name),Path.Combine(ziel,f.Name),ct);
        // Restores invalidate sessions. Archived encrypted keys are retained but not activated on another OS identity.
        if(manifest.Schluessel.Length>0){Directory.CreateDirectory(Path.Combine(ziel,"recovered-keyring"));foreach(var f in manifest.Schluessel)await Kopieren(Path.Combine(backup,"keyring",f.Name),Path.Combine(ziel,"recovered-keyring",f.Name),ct);}
        await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(cs.ConnectionString).Options);if(!(await Dokumentbestand.Pruefen(db,ziel,ct)).Konsistent)throw new InvalidDataException("Wiederhergestellter Bestand ist inkonsistent.");
        var stamp=Guid.NewGuid().ToString("N");await db.Benutzer.ExecuteUpdateAsync(s=>s.SetProperty(u=>u.Sicherheitskennung,stamp),ct);
        await File.WriteAllTextAsync(Path.Combine(ziel,"restore-complete.json"),JsonSerializer.Serialize(new{version=1,zeitpunkt=DateTimeOffset.UtcNow}),ct);
    }
}
