using System.Text.RegularExpressions;
using Betriebsplattform.Web.Akten;

namespace Betriebsplattform.Web.Persistenz;

public interface IDokumentdateien
{
    Task Speichern(string key,byte[] bytes,CancellationToken token);
    Task<byte[]> Lesen(string key,CancellationToken token);
}
// Deliberately local development storage, not a production archive/backup solution.
public sealed class LokaleDokumentdateien(string? root) : IDokumentdateien
{
    private string Pfad(string key)
    {
        if(string.IsNullOrWhiteSpace(root)||!Path.IsPathFullyQualified(root)||!Regex.IsMatch(key,@"\A[a-f0-9]{32}\z"))throw new Arbeitsfehler("Eine sichere lokale Dokumentablage ist noch nicht konfiguriert.",503);
        var basis=Path.GetFullPath(root);var dir=new DirectoryInfo(basis);
        for(var d=dir;d is not null;d=d.Parent)if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Arbeitsfehler("Verknüpfte Ablageordner sind nicht zugelassen.",503);
        Directory.CreateDirectory(basis);return Path.Combine(basis,key+".bin");
    }
    public async Task Speichern(string key,byte[] bytes,CancellationToken token)
    {
        if(bytes.Length is 0 or >Dokumentpruefung.MaxBytes)throw new Arbeitsfehler("Die Datei ist leer oder überschreitet 20 MB.",413);
        await using var file=new FileStream(Pfad(key),FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,FileOptions.Asynchronous);
        await file.WriteAsync(bytes,token);await file.FlushAsync(token);
    }
    public async Task<byte[]> Lesen(string key,CancellationToken token)
    {
        var path=Pfad(key);var info=new FileInfo(path);
        if(!info.Exists||(info.Attributes&FileAttributes.ReparsePoint)!=0||info.Length is 0 or >Dokumentpruefung.MaxBytes)throw new Arbeitsfehler("Das Originaldokument ist nicht verfügbar.",404);
        return await File.ReadAllBytesAsync(path,token);
    }
}
