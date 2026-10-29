using System.Security.Cryptography;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
namespace Betriebsplattform.Web.Akten;

public sealed class Scanauftrag:VersionierterDatensatz
{
    public string QuelleId{get;set;}="";
    public string Dateiname{get;set;}="";
    public string Hash{get;set;}="";
    public string Status{get;set;}="Wartend";
    public int Versuche{get;set;}
    public DateTimeOffset? LeasingBis{get;set;}
    public string? LeaseId{get;set;}
    public string? DokumentId{get;set;}
    public string? Fehlercode{get;set;}
}
public sealed record Scanordneroptionen(string QuelleId,string Ordner,string BenutzerId);
public sealed class PersistenterScanordner(Scanordneroptionen optionen)
{
    public string QuelleId=>optionen.QuelleId;
    public void Ausfall()=>Status="Scanquelle oder Datenbank nicht bereit; Originale bleiben erhalten";
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle,StringBuilder path,uint length,uint flags);
    private static void HandlePruefen(FileStream input,string root){
        if(!OperatingSystem.IsWindows())throw new Arbeitsfehler("Der lokale Scanordner benötigt hier eine geprüfte Windows-Dateipfadprüfung.",503);
        var buffer=new StringBuilder(32768);var n=GetFinalPathNameByHandleW(input.SafeFileHandle,buffer,(uint)buffer.Capacity,0);if(n==0||n>=buffer.Capacity)throw new Arbeitsfehler("Geöffneter Scanpfad konnte nicht bestätigt werden.",403);
        var path=buffer.ToString();if(path.StartsWith(@"\\?\",StringComparison.Ordinal))path=path[4..];if(!Path.GetFullPath(path).StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Arbeitsfehler("Geöffnete Datei liegt außerhalb des Scanordners.",403);
    }
    private readonly Dictionary<string,(long Laenge,DateTime Zeit)> beobachtet=new(StringComparer.OrdinalIgnoreCase);
    public string Status{get;private set;}="Scanordner wartet";
    private string Basis(){
        if(!Path.IsPathFullyQualified(optionen.Ordner)||optionen.Ordner.StartsWith(@"\\")||optionen.Ordner.StartsWith("//")||!System.Text.RegularExpressions.Regex.IsMatch(optionen.QuelleId,"^[a-z0-9-]{1,64}$"))throw new Arbeitsfehler("Scanquelle benötigt einen ausdrücklich freigegebenen lokalen Ordner.",503);
        var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(optionen.Ordner));if(root==Path.TrimEndingDirectorySeparator(Path.GetPathRoot(root)!))throw new Arbeitsfehler("Laufwerkswurzel ist kein Scanordner.",503);
        for(var p=new DirectoryInfo(root);p is not null;p=p.Parent)if(!p.Exists||(p.Attributes&FileAttributes.ReparsePoint)!=0)throw new Arbeitsfehler("Scanordner fehlt oder enthält einen Verzeichnisverweis.",503);return root;
    }
    public async Task Pruefen(FirmenDbContext db,FirmenDaten eingang,IDokumentdateien dateien,EingangsDienst workflow,Dokumentanalysator analyse,TimeProvider clock,CancellationToken ct=default){
        var root=Basis();var actor=await db.Benutzer.SingleOrDefaultAsync(u=>u.Id==optionen.BenutzerId&&u.Aktiv&&u.Eingerichtet,ct)??throw new Arbeitsfehler("Scanquellen-Benutzer ist nicht aktiv.",503);
        var pfade=Directory.EnumerateFiles(root).Take(1001).ToArray();if(pfade.Length>1000)throw new Arbeitsfehler("Scanordner enthält mehr als 1000 Dateien. Eingang organisatorisch aufteilen.",503);
        var namen=pfade.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);foreach(var old in beobachtet.Keys.Where(k=>!namen.Contains(k)).ToArray())beobachtet.Remove(old);
        var offene=await db.Scanauftraege.Where(j=>j.QuelleId==optionen.QuelleId&&(j.Status=="Wartend"||j.Status=="In Bearbeitung")&&(j.LeasingBis==null||j.LeasingBis<=clock.GetUtcNow())).ToArrayAsync(ct);
        foreach(var fehlend in offene.Where(j=>!namen.Contains(j.Dateiname))){fehlend.Status="Fehler";fehlend.Fehlercode="Quelldatei fehlt; gespeichertes Original prüfen";fehlend.LeasingBis=null;}if(offene.Any(j=>j.Status=="Fehler"))await db.SaveChangesAsync(ct);
        Status="Scanordner prüft abgeschlossene PDF-Dateien";var anzahl=0;
        foreach(var pfad in pfade){
            ct.ThrowIfCancellationRequested();var info=new FileInfo(pfad);if(!info.Extension.Equals(".pdf",StringComparison.OrdinalIgnoreCase)||(info.Attributes&FileAttributes.ReparsePoint)!=0)continue;
            if(!Path.GetFullPath(pfad).StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Arbeitsfehler("Scanpfad nicht freigegeben.",403);
            var stamp=(info.Length,info.LastWriteTimeUtc);var stabil=beobachtet.TryGetValue(info.Name,out var alt)&&alt==stamp;beobachtet[info.Name]=stamp;if(!stabil)continue;
            if(info.Length is 0 or >Dokumentpruefung.MaxBytes){Status="Datei leer oder zu groß; Original bleibt erhalten";continue;}
            byte[] bytes;try{await using var input=new FileStream(pfad,FileMode.Open,FileAccess.Read,FileShare.Read,81920,FileOptions.Asynchronous);HandlePruefen(input,root);bytes=new byte[checked((int)input.Length)];await input.ReadExactlyAsync(bytes,ct);info.Refresh();if((info.Length,info.LastWriteTimeUtc)!=stamp)continue;}catch(IOException){continue;}
            var hash=Convert.ToHexString(SHA256.HashData(bytes));var job=await db.Scanauftraege.SingleOrDefaultAsync(j=>j.QuelleId==optionen.QuelleId&&j.Hash==hash,ct);
            if(job?.Status is "Erledigt" or "Blockiert" or "Fehler"||job?.Status=="In Bearbeitung"&&job.LeasingBis>clock.GetUtcNow())continue;
            if(++anzahl>10)break;
            if(job is null){job=new(){QuelleId=optionen.QuelleId,Dateiname=info.Name,Hash=hash};db.Scanauftraege.Add(job);try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){db.Entry(job).State=EntityState.Detached;continue;}}
            if(job.Versuche>=3){
                var accepted=await db.Dokumentaktivitaeten.IgnoreQueryFilters().Where(e=>e.ScanauftragId==job.Id).Join(db.Dokumente.IgnoreQueryFilters(),e=>e.DokumentId,d=>d.Id,(_,d)=>d).SingleOrDefaultAsync(ct);
                var saved=accepted?.AnalyseJson is null?null:System.Text.Json.JsonSerializer.Deserialize<Dokumentanalyse>(accepted.AnalyseJson,Dokumentanalysator.Json);
                if(accepted is not null&&saved?.Hash==job.Hash&&saved.Version==analyse.Version&&saved.Status!="OCR erforderlich"&&!saved.Seiten.Any(s=>s.Status is "OCR erforderlich" or "Kein brauchbarer OCR-Text")){
                    var original=await dateien.Lesen(accepted.StorageKey,ct);if(original.LongLength!=accepted.Groesse||Convert.ToHexString(SHA256.HashData(original))!=job.Hash)throw new Arbeitsfehler("Originalintegrität gestört.",409);
                    job.Status="Erledigt";job.DokumentId=accepted.Id;job.LeaseId=null;job.LeasingBis=null;job.Fehlercode=null;
                }else{job.Status="Fehler";job.Fehlercode="Wiederholungsgrenze";}
                await db.SaveChangesAsync(ct);continue;
            }
            var leaseId=Guid.NewGuid().ToString("N");job.Status="In Bearbeitung";job.LeasingBis=clock.GetUtcNow().AddMinutes(15);job.LeaseId=leaseId;job.Versuche++;
            try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){db.Entry(job).State=EntityState.Detached;continue;}
            try{
                // Recover an acknowledged intake without creating another arrival after a crash.
                var existing=await db.Dokumentaktivitaeten.IgnoreQueryFilters().Where(e=>e.ScanauftragId==job.Id).Select(e=>e.DokumentId).SingleOrDefaultAsync(ct);
                DokumentDatensatz doc;
                if(existing is not null){doc=await db.Dokumente.IgnoreQueryFilters().SingleAsync(d=>d.Id==existing,ct);var original=await dateien.Lesen(doc.StorageKey,ct);if(doc.Hash!=job.Hash||Convert.ToHexString(SHA256.HashData(original))!=doc.Hash)throw new Arbeitsfehler("Originalintegrität gestört.",409);}
                else doc=await eingang.DokumentHinzufuegen(bytes,info.Name,"application/pdf",null,dateien,ct,"Scan-Ordner",job.Id,actor.Anzeigename);
                job.DokumentId=doc.Id;
                if(!await db.Freigaben.AnyAsync(g=>g.BenutzerId==actor.Id&&g.DokumentId==doc.Id,ct)){db.Freigaben.Add(new(){BenutzerId=actor.Id,DokumentId=doc.Id});await db.SaveChangesAsync(ct);}
                var result=await workflow.Analysieren(actor.Id,doc.Id,analyse,dateien,ct);var manuell=result.Analyse.Status=="OCR erforderlich"||result.Analyse.Seiten.Any(s=>s.Status is "OCR erforderlich" or "Kein brauchbarer OCR-Text");job.Status=manuell?"Blockiert":"Erledigt";job.Fehlercode=manuell?"Texterkennung unvollständig; manuell prüfen":null;job.LeasingBis=null;job.LeaseId=null;await db.SaveChangesAsync(ct);
            }catch(Arbeitsfehler e){db.ChangeTracker.Clear();job=await db.Scanauftraege.SingleAsync(j=>j.Id==job.Id,ct);if(job.LeaseId!=leaseId)continue;job.Status=e.Status==503?"Blockiert":e.Status is 400 or 413 or 415 or 422||job.Versuche>=3?"Fehler":"Wartend";job.Fehlercode=e.Status==503?"Sicherheitsgate oder lokales Werkzeug nicht bereit":"Verarbeitung manuell prüfen";job.LeasingBis=null;job.LeaseId=null;await db.SaveChangesAsync(ct);}
            catch(DbUpdateException){db.ChangeTracker.Clear();Status="Gleichzeitiger Eingang; gespeicherten Stand im nächsten Lauf prüfen";}
        }
    }
}
public sealed class PersistenterScanordnerDienst(IServiceScopeFactory scopes,PersistenterScanordner adapter,TimeProvider clock):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(3));
        try{do{try{await using var scope=scopes.CreateAsyncScope();var s=scope.ServiceProvider;await adapter.Pruefen(s.GetRequiredService<FirmenDbContext>(),s.GetRequiredService<FirmenDaten>(),s.GetRequiredService<IDokumentdateien>(),s.GetRequiredService<EingangsDienst>(),s.GetRequiredService<Dokumentanalysator>(),clock,stoppingToken);}catch(Exception e)when(e is Arbeitsfehler or IOException or UnauthorizedAccessException or DbUpdateException or Npgsql.NpgsqlException){adapter.Ausfall();/* No document contents/credentials in background logs. Persisted jobs remain recoverable. */}}while(await timer.WaitForNextTickAsync(stoppingToken));}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){}
    }
}
