using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Betriebsplattform.Web.Akten;

// Optional local executable, never selected by document contents or request parameters.
// Not an OS sandbox or a malware scanner. Distribution/licensing requires separate review.
public sealed class LokalerPdfAdapter(string exe,IOcrAnalyse? ocr=null) : IPdfAnalyse
{
    private static readonly SemaphoreSlim Plaetze=new(1,1);
    public string Version=>"ghostscript-txtwrite-0-v2/"+File.GetLastWriteTimeUtc(exe).Ticks+"/"+(ocr?.Version??"ohne-ocr");
    private async Task<T> Arbeiten<T>(byte[] bytes,Func<string,CancellationToken,Task<T>> aktion,CancellationToken ct)
    {
        if(!Path.IsPathFullyQualified(exe)||!File.Exists(exe)||Path.GetExtension(exe)!=".exe")throw new Arbeitsfehler("Lokaler PDF-Adapter ist nicht bereit.",503);
        await Plaetze.WaitAsync(ct);var dir=Path.Combine(Path.GetTempPath(),"mbop-pdf-"+Guid.NewGuid().ToString("N"));
        try{Directory.CreateDirectory(dir);await File.WriteAllBytesAsync(Path.Combine(dir,"original.pdf"),bytes,ct);return await aktion(dir,ct);}
        catch(Exception e)when(e is XmlException or FormatException or InvalidDataException or OverflowException){throw new Arbeitsfehler("PDF-Analyseformat ist nicht verlässlich lesbar; manuell prüfen.",422);}
        finally{try{if(Directory.Exists(dir))Directory.Delete(dir,true);}finally{Plaetze.Release();}}
    }
    private async Task<string> Start(string dir,string[] args,CancellationToken ct)
    {
        using var limit=CancellationTokenSource.CreateLinkedTokenSource(ct);limit.CancelAfter(TimeSpan.FromSeconds(20));
        var p=new ProcessStartInfo(exe){WorkingDirectory=dir,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var a in new[]{"-q","-dSAFER","-dBATCH","-dNOPAUSE","-dPDFSTOPONERROR","-dNumRenderingThreads=1","-dMaxBitmap=20000000"}.Concat(args))p.ArgumentList.Add(a);
        // Never forward document contents, paths or interpreter diagnostics into API errors/logs.
        using var process=Process.Start(p)??throw new Arbeitsfehler("PDF-Verarbeitung nicht verfügbar.",503);
        var output=Ausgabe(process.StandardOutput,limit.Token);var error=Drain(process.StandardError,limit.Token);
        try{
            while(!process.HasExited){await Task.Delay(100,limit.Token);process.Refresh();if(process.HasExited)break;long memory;try{memory=process.WorkingSet64;}catch(InvalidOperationException)when(process.HasExited){break;}if(memory>384_000_000||Directory.EnumerateFiles(dir).Where(f=>Path.GetFileName(f)!="original.pdf").Any(f=>new FileInfo(f).Length>8_000_000)){limit.Cancel();break;}}
            await process.WaitForExitAsync(limit.Token);await Task.WhenAll(output,error);if(process.ExitCode!=0)throw new Arbeitsfehler("PDF konnte nicht ausgewertet werden. Manuelle Prüfung erforderlich.",422);return await output;
        }
        catch(OperationCanceledException){if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync(CancellationToken.None);throw new Arbeitsfehler("Zeitlimit der PDF-Verarbeitung erreicht.",503);}
    }
    private static async Task Drain(StreamReader r,CancellationToken ct){var b=new char[1024];while(await r.ReadAsync(b,ct)>0){} }
    private static async Task<string> Ausgabe(StreamReader r,CancellationToken ct){var b=new char[1024];var text=new StringBuilder();var ueberlauf=false;int n;while((n=await r.ReadAsync(b,ct))>0){if(text.Length+n<=128000)text.Append(b,0,n);else ueberlauf=true;}if(ueberlauf)throw new Arbeitsfehler("PDF-Metadaten überschreiten die Grenze.",422);return text.ToString();}
    private static void Groesse(string file,long max){if(new FileInfo(file).Length>max)throw new Arbeitsfehler("PDF-Auswertung überschreitet das Ressourcenlimit.",422);}
    public Task<(Dokumentseite[] Seiten,Textabschnitt[] Abschnitte)> Lesen(byte[] bytes,CancellationToken ct)=>Arbeiten(bytes,async(dir,t)=>{
        // PDFINFO is an inspection mode: it does not run the selected output device.
        var info=await Start(dir,["-sDEVICE=nullpage","-dPDFINFO","original.pdf"],t);
        await Start(dir,["-sDEVICE=txtwrite","-dTextFormat=0","-dLastPage=11","-sOutputFile=text.xml","original.pdf"],t);
        var file=Path.Combine(dir,"text.xml");Groesse(file,4_000_000);
        // txtwrite emits sibling page elements, not a single XML document.
        var raw=await File.ReadAllTextAsync(file,t);if(raw.Contains("<?xml",StringComparison.Ordinal)||raw.Contains("<!",StringComparison.Ordinal))throw new Arbeitsfehler("PDF-Textformat nicht unterstützt.",422);
        using var reader=XmlReader.Create(new StringReader("<document>"+raw+"</document>"),new(){DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=4_100_000});
        var pages=XDocument.Load(reader).Root!.Elements("page").ToArray();if(pages.Length>10)throw new Arbeitsfehler("PDF-Auswertung ist auf zehn Seiten begrenzt.",422);
        var seiten=new List<Dokumentseite>();var abschnitte=new List<Textabschnitt>();int chars=0;
        var boxes=Regex.Matches(info,@"(?m)^Page (\d+) MediaBox: \[([^\]\r\n]+)\](?:[ \t]+Rotate = (-?\d+))?[ \t]*\r?$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)).ToArray();
        for(var i=0;i<pages.Length;i++){
            var candidates=boxes.Where(b=>b.Groups[1].Value==(i+1).ToString(CultureInfo.InvariantCulture)).ToArray();if(candidates.Length!=1)throw new Arbeitsfehler("PDF-Seitenmaße sind nicht eindeutig verfügbar.",422);
            var media=candidates[0].Groups[2].Value.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(x=>double.Parse(x,CultureInfo.InvariantCulture)).ToArray();if(media.Length!=4||media.Any(x=>!double.IsFinite(x)))throw new Arbeitsfehler("PDF-Seitenmaße sind ungültig.",422);
            var rotation=candidates[0].Groups[3].Success?((int.Parse(candidates[0].Groups[3].Value,CultureInfo.InvariantCulture)%360)+360)%360:0;if(rotation is not (0 or 90 or 180 or 270))throw new Arbeitsfehler("PDF-Seitenrotation nicht unterstützt.",422);
            var breite=media[2]-media[0];var hoehe=media[3]-media[1];var w=Math.Round(rotation is 90 or 270?hoehe:breite,MidpointRounding.AwayFromZero);var h=Math.Round(rotation is 90 or 270?breite:hoehe,MidpointRounding.AwayFromZero);
            if(w is <=0 or >2000||h is <=0 or >2000)throw new Arbeitsfehler("PDF-Seitenformat überschreitet die Grenze.",422);
            foreach(var span in pages[i].Descendants("span")){
                var text=string.Concat(span.Elements("char").Select(c=>(string?)c.Attribute("c")));chars+=text.Length;if(chars>64000||abschnitte.Count>=2000)throw new Arbeitsfehler("PDF-Text überschreitet die Grenze.",422);
                var box=((string?)span.Attribute("bbox"))?.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(s=>double.Parse(s,CultureInfo.InvariantCulture)).ToArray();Textposition? pos=null;
                if(box?.Length==4&&box.All(double.IsFinite)&&box[0]>=0&&box[1]>=0&&box[2]>=box[0]&&box[2]<=w&&box[3]>=box[1]&&box[3]<=h)pos=new(box[0]/w,box[1]/h,(box[2]-box[0])/w,(box[3]-box[1])/h,box[1]==box[3]?"Grundlinie":"Rahmen");
                if(!string.IsNullOrWhiteSpace(text))abschnitte.Add(new("s"+(i+1)+"-"+abschnitte.Count,i+1,text,pos,"PDF-Digitaltext"));
            }
            var brauchbar=abschnitte.Where(a=>a.Seite==i+1).Sum(a=>a.Text.Count(char.IsLetterOrDigit))>=15;
            var status=brauchbar?"Digitaltext":"OCR erforderlich";
            if(!brauchbar&&ocr is not null){
                await Start(dir,["-sDEVICE=pnggray","-r200","-dFirstPage="+(i+1),"-dLastPage="+(i+1),"-sOutputFile=ocr.png","original.pdf"],t);var scan=Path.Combine(dir,"ocr.png");Groesse(scan,8_000_000);
                var gelesen=await ocr.Lesen(await File.ReadAllBytesAsync(scan,t),i+1,t);abschnitte.RemoveAll(a=>a.Seite==i+1);abschnitte.AddRange(gelesen);status=gelesen.Length>0?"OCR; manuell prüfen":"Kein brauchbarer OCR-Text";
            }
            seiten.Add(new(i+1,w,h,rotation,status));
        }
        return(seiten.ToArray(),abschnitte.ToArray());
    },ct);
    public Task<byte[]> Seite(byte[] bytes,int seite,CancellationToken ct){if(seite is <1 or >10)throw new Arbeitsfehler("Seite nicht verfügbar.",404);return Arbeiten(bytes,async(dir,t)=>{
        await Start(dir,["-sDEVICE=pnggray","-r72","-dFirstPage="+seite,"-dLastPage="+seite,"-sOutputFile=page.png","original.pdf"],t);var file=Path.Combine(dir,"page.png");Groesse(file,8_000_000);return await File.ReadAllBytesAsync(file,t);
    },ct);}
}
