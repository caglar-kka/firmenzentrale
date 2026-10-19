using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Betriebsplattform.Web.Akten;

namespace Betriebsplattform.Web.Persistenz;

public interface IDokumentdateien
{
    Task Speichern(string key,byte[] bytes,CancellationToken token);
    Task<byte[]> Lesen(string key,CancellationToken token);
    Task Entfernen(string key,CancellationToken token);
    Task<Stream> Oeffnen(string key,long groesse,string hash,CancellationToken token);
}
// Only generated keys cross this boundary. Original names are never paths.
public sealed class LokaleDokumentdateien(string? root,params string[] gesperrteWurzeln) : IDokumentdateien,IHostedService
{
    private string Basis()
    {
        if(string.IsNullOrWhiteSpace(root)||!Path.IsPathFullyQualified(root)||root.StartsWith(@"\\")||root.StartsWith("//"))throw Unbrauchbar();
        var basis=Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if(basis==Path.TrimEndingDirectorySeparator(Path.GetPathRoot(basis)!))throw Unbrauchbar();
        foreach(var sperre in gesperrteWurzeln.Append(AppContext.BaseDirectory)){
            var verboten=Path.TrimEndingDirectorySeparator(Path.GetFullPath(sperre));
            if(basis.Equals(verboten,StringComparison.OrdinalIgnoreCase)||basis.StartsWith(verboten+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw Unbrauchbar();
        }
        for(var d=new DirectoryInfo(basis);d is not null;d=d.Parent){
            if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw Unbrauchbar();
            if(Directory.Exists(Path.Combine(d.FullName,".git"))||File.Exists(Path.Combine(d.FullName,".git")))throw Unbrauchbar();
        }
        return basis;
    }
    private static Arbeitsfehler Unbrauchbar()=>new("Die lokale Dokumentablage ist nicht verfügbar. Bitte die Speicherkonfiguration prüfen.",503);
    private string Pfad(string key){if(!Regex.IsMatch(key,@"\A[a-f0-9]{32}\z"))throw new Arbeitsfehler("Ungültige Dokumentkennung.",404);return Path.Combine(Basis(),key+".bin");}
    public async Task StartAsync(CancellationToken token)
    {
        try{
            Directory.CreateDirectory(Basis());var key=Guid.NewGuid().ToString("N");
            try{await Speichern(key,[42],token);var bytes=await Lesen(key,token);if(bytes.Length!=1||bytes[0]!=42)throw Unbrauchbar();}
            finally{await Entfernen(key,CancellationToken.None);}
        }catch(OperationCanceledException){throw;}
        catch(Exception){throw new InvalidOperationException("Die konfigurierte lokale Dokumentablage ist nicht nutzbar. Kein temporärer Speicher-Fallback.");}
    }
    public Task StopAsync(CancellationToken token)=>Task.CompletedTask;
    public async Task Speichern(string key,byte[] bytes,CancellationToken token)
    {
        if(bytes.Length is 0 or >Dokumentpruefung.MaxBytes)throw new Arbeitsfehler("Die Datei ist leer oder überschreitet 20 MB.",413);
        var path=Pfad(key);Directory.CreateDirectory(Basis());
        var temp=Path.Combine(Basis(),Guid.NewGuid().ToString("N")+".pending");
        try{
            await using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,FileOptions.Asynchronous)){
                await file.WriteAsync(bytes,token);await file.FlushAsync(token);file.Flush(flushToDisk:true);
            }
            token.ThrowIfCancellationRequested();File.Move(temp,path,overwrite:false);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public Task Entfernen(string key,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();var path=Pfad(key);
        if(File.Exists(path)){if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw Unbrauchbar();File.Delete(path);}
        return Task.CompletedTask;
    }
    private FileStream Lesestrom(string key)
    {
        var path=Pfad(key);var info=new FileInfo(path);
        if(!info.Exists||(info.Attributes&FileAttributes.ReparsePoint)!=0||info.Length is 0 or >Dokumentpruefung.MaxBytes)throw new Arbeitsfehler("Das Originaldokument ist nicht verfügbar.",404);
        return new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,FileOptions.Asynchronous|FileOptions.SequentialScan);
    }
    public async Task<byte[]> Lesen(string key,CancellationToken token)
    {
        await using var file=Lesestrom(key);var bytes=new byte[checked((int)file.Length)];await file.ReadExactlyAsync(bytes,token);return bytes;
    }
    public async Task<Stream> Oeffnen(string key,long groesse,string hash,CancellationToken token)
    {
        var file=Lesestrom(key);
        try{
            if(file.Length!=groesse||Convert.ToHexString(await SHA256.HashDataAsync(file,token))!=hash)throw new Arbeitsfehler("Das Originaldokument konnte nicht verifiziert werden.",409);
            file.Position=0;return file;
        }catch{await file.DisposeAsync();throw;}
    }
}
