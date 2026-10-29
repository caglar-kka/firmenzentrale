using System.Diagnostics;
using System.Globalization;
namespace Betriebsplattform.Web.Akten;

public interface IOcrAnalyse
{
    string Version {get;}
    Task<Textabschnitt[]> Lesen(byte[] png,int seite,CancellationToken ct);
}
// Opt-in local OCR. Uses an existing installation and language data, never downloads models.
public sealed class LokaleOcr(string exe,string sprachdaten) : IOcrAnalyse
{
    public string Version=>"tesseract-deu-tsv-1/"+File.GetLastWriteTimeUtc(exe).Ticks+"/"+File.GetLastWriteTimeUtc(Path.Combine(sprachdaten,"deu.traineddata")).Ticks;
    public async Task<Textabschnitt[]> Lesen(byte[] png,int seite,CancellationToken ct)
    {
        if(!Path.IsPathFullyQualified(exe)||!File.Exists(exe)||!Path.IsPathFullyQualified(sprachdaten)||!File.Exists(Path.Combine(sprachdaten,"deu.traineddata")))throw new Arbeitsfehler("Lokale deutsche OCR ist nicht bereit.",503);
        if(png.Length>8_000_000||png.Length<24)throw new Arbeitsfehler("OCR-Bild überschreitet die Grenze.",422);
        var w=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16,4));var h=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20,4));if(w==0||h==0||(ulong)w*h>20_000_000)throw new Arbeitsfehler("OCR-Bildabmessungen überschreiten die Grenze.",422);
        var dir=Path.Combine(Path.GetTempPath(),"mbop-ocr-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{
            await File.WriteAllBytesAsync(Path.Combine(dir,"scan.png"),png,ct);var psi=new ProcessStartInfo(exe){WorkingDirectory=dir,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var a in new[]{"scan.png","result","--tessdata-dir",sprachdaten,"-l","deu","--psm","3","-c","tessedit_create_tsv=1"})psi.ArgumentList.Add(a);psi.Environment["OMP_THREAD_LIMIT"]="1";
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(30));using var p=Process.Start(psi)??throw new Arbeitsfehler("OCR ist nicht verfügbar.",503);
            async Task Drain(StreamReader r){var b=new char[1024];while(await r.ReadAsync(b,deadline.Token)>0){}}
            var tasks=new[]{Drain(p.StandardOutput),Drain(p.StandardError)};
            try{
                while(!p.HasExited){await Task.Delay(100,deadline.Token);p.Refresh();if(p.HasExited)break;long memory;try{memory=p.WorkingSet64;}catch(InvalidOperationException)when(p.HasExited){break;}if(memory>384_000_000||Directory.EnumerateFiles(dir,"*.tsv").Any(f=>new FileInfo(f).Length>4_000_000))deadline.Cancel();}
                await p.WaitForExitAsync(deadline.Token);await Task.WhenAll(tasks);if(p.ExitCode!=0)throw new Arbeitsfehler("OCR fehlgeschlagen; manuell prüfen.",422);
            }catch(OperationCanceledException){if(!p.HasExited)p.Kill(true);await p.WaitForExitAsync(CancellationToken.None);throw new Arbeitsfehler("OCR-Zeit- oder Ressourcenlimit erreicht.",503);}
            var file=Path.Combine(dir,"result.tsv");if(!File.Exists(file)||new FileInfo(file).Length>4_000_000)throw new Arbeitsfehler("OCR-Ergebnis nicht verfügbar.",422);
            var rows=(await File.ReadAllLinesAsync(file,ct)).Skip(1).Select(l=>l.Split('\t',12)).Where(a=>a.Length==12&&a[0]=="5"&&!string.IsNullOrWhiteSpace(a[11])).ToArray();if(rows.Length>10000)throw new Arbeitsfehler("OCR-Ergebnis zu umfangreich.",422);
            double Zahl(string[] a,int i)=>double.Parse(a[i],CultureInfo.InvariantCulture);
            var result=new List<Textabschnitt>();int chars=0;
            foreach(var group in rows.GroupBy(a=>string.Join('-',a.Take(5)))){
                var text=string.Join(' ',group.Select(a=>a[11]));chars+=text.Length;if(chars>64000)throw new Arbeitsfehler("OCR-Text zu umfangreich.",422);
                var x=group.Min(a=>Zahl(a,6));var y=group.Min(a=>Zahl(a,7));var right=group.Max(a=>Zahl(a,6)+Zahl(a,8));var bottom=group.Max(a=>Zahl(a,7)+Zahl(a,9));
                if(x<0||y<0||right>w||bottom>h||right<x||bottom<y)throw new Arbeitsfehler("OCR-Positionen ungültig.",422);
                result.Add(new("ocr-"+seite+"-"+result.Count,seite,text,new(x/w,y/h,(right-x)/w,(bottom-y)/h,"OCR-Rahmen"),"Tesseract deu; menschlich prüfen"));
            }
            return result.ToArray();
        }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
}
