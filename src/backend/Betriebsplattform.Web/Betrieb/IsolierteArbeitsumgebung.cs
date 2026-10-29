using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Betriebsplattform.Web.Betrieb;

// Explicit developer-operated start of the regular application. No seeding or migration here.
public static class IsolierteArbeitsumgebung
{
    public static string Datenbankname(string name)=>Regex.IsMatch(name,@"\Afz_browser_test_[a-f0-9]{32}\z")?name:throw new InvalidOperationException("Kein isolierter Entwicklungsbestand.");
    internal sealed record Startdaten(string Repo,string Root,string Storage,string Verbindung,string Datenbank,string Zertifikat){public override string ToString()=>nameof(Startdaten);}
    internal static async Task<Startdaten> Vorbereiten(string profil,CancellationToken ct=default){
        if(!OperatingSystem.IsWindows())throw new InvalidOperationException("Windows-Zertifikatsspeicher erforderlich.");
        var repo=new DirectoryInfo(Directory.GetCurrentDirectory());while(repo is not null&&!File.Exists(Path.Combine(repo.FullName,"AGENTS.md")))repo=repo.Parent;
        if(repo is null)throw new InvalidOperationException("Aus dem Repository starten.");
        profil=Path.GetFullPath(profil,repo.FullName);var grenze=Path.Combine(repo.FullName,"private","pruefungen")+Path.DirectorySeparatorChar;
        if((!profil.StartsWith(grenze,StringComparison.OrdinalIgnoreCase)&&!string.Equals(profil,Path.Combine(repo.FullName,"private","arbeitsumgebung","ready.json"),StringComparison.OrdinalIgnoreCase))||Path.GetFileName(profil)!="ready.json")throw new InvalidOperationException("Vorbereiteten privaten Entwicklungsbestand wählen.");
        using var meta=JsonDocument.Parse(await File.ReadAllTextAsync(profil,ct));var m=meta.RootElement;
        var database=Datenbankname(m.GetProperty("database").GetString()!);
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FirmenZentrale","Development","browser-gate",database["fz_browser_test_".Length..]);
        var storage=Path.Combine(root,"documents");
        if(!string.Equals(Path.GetFullPath(m.GetProperty("storage").GetString()!),storage,StringComparison.OrdinalIgnoreCase)||!Directory.Exists(storage))throw new InvalidOperationException("Private Ablage stimmt nicht mit dem isolierten Bestand überein.");
        for(var d=new DirectoryInfo(root);d is not null;d=d.Parent)if((d.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Verknüpfte Entwicklungsablage nicht erlaubt.");
        var cfg=new ConfigurationBuilder().AddUserSecrets(typeof(IsolierteArbeitsumgebung).Assembly,optional:true).Build();
        var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("FirmenZentrale")){Pooling=false,IncludeErrorDetail=false,LogParameters=false};
        if(cs.Host is not("127.0.0.1" or "localhost")||cs.Port!=5432)throw new InvalidOperationException("Nur lokaler isolierter PostgreSQL-Host.");
        cs.Database=database;var administration=cs.ConnectionString;
        await using(var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(administration,n=>n.SetPostgresVersion(18,0)).Options)){
            await db.Database.OpenConnectionAsync(ct);if(db.Database.GetDbConnection().Database!=database||(await db.Database.GetPendingMigrationsAsync(ct)).Any())throw new InvalidOperationException("Isoliertes Schema muss separat vorbereitet werden; keine automatische Migration.");
        }
        return new(repo.FullName,root,storage,administration,database,m.GetProperty("certificate").GetString()??throw new InvalidOperationException("Zertifikat fehlt."));
    }
    public static async Task Run(string profil,CancellationToken ct=default){
        var d=await Vorbereiten(profil,ct);var administration=d.Verbindung;var root=d.Root;var storage=d.Storage;var cs=new NpgsqlConnectionStringBuilder(administration);
        var abnahme=Path.Combine(d.Repo,"private","arbeitsumgebung","lokale-admin-abnahme.json");
        var optionen=new List<string>();if(File.Exists(abnahme)){
            var regel=System.Text.Json.JsonSerializer.Deserialize<Betriebsplattform.Web.Sicherheit.LokaleAbnahmeregel>(await File.ReadAllTextAsync(abnahme,ct))!;
            if(regel.Aktiv){if(regel.Datenbank!=d.Datenbank||Path.GetFullPath(profil,d.Repo)!=Path.Combine(d.Repo,"private","arbeitsumgebung","ready.json"))throw new InvalidOperationException("Abnahmeprofil ist an den bestehenden isolierten Bestand gebunden.");optionen.Add("--FirmenZentrale:LokaleAbnahme:Datei="+abnahme);}
        }
        using var store=new X509Store(StoreName.My,StoreLocation.CurrentUser);store.Open(OpenFlags.ReadOnly);
        using var cert=store.Certificates.Find(X509FindType.FindByThumbprint,d.Zertifikat,true).Single();
        using(var chain=new X509Chain())if(!cert.HasPrivateKey||!chain.Build(cert))throw new InvalidOperationException("Vertrauenswürdiges lokales HTTPS-Zertifikat erforderlich.");
        var rolle="fz_app_local_"+Guid.NewGuid().ToString("N")[..16];var geheim=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await Betriebsverwaltung.Laufzeitrolle(administration,rolle,geheim,ct);cs.Username=rolle;cs.Password=geheim;
        var pfx=Path.Combine(root,"host-"+Guid.NewGuid().ToString("N")+".pfx");var pfxPasswort=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        try{
            await File.WriteAllBytesAsync(pfx,cert.Export(X509ContentType.Pkcs12,pfxPasswort),ct);
            await using var app=ApiAnwendung.Erstellen(["--environment=Development","--FirmenZentrale:Startmodus=Anmeldung","--FirmenZentrale:Persistence:Provider=PostgreSql","--ConnectionStrings:FirmenZentrale="+cs.ConnectionString,"--FirmenZentrale:Persistence:DocumentRoot="+storage,"--FirmenZentrale:Security:Enabled=true","--FirmenZentrale:Security:CertificatePath="+pfx,"--FirmenZentrale:Security:CertificatePassword="+pfxPasswort,"--FirmenZentrale:Leistungsnachweise:Enabled=true","--FirmenZentrale:Organisation:Name=Nordhafen Büroservice",..optionen],5181,Path.Combine(d.Repo,"src","frontend","dist"));
            Console.WriteLine("MBOP: https://127.0.0.1:5181/ – isolierter dauerhafter Datenbestand. Beenden mit Strg+C.");
            await app.RunAsync(ct);
        }finally{if(File.Exists(pfx))File.Delete(pfx);}
    }
}
