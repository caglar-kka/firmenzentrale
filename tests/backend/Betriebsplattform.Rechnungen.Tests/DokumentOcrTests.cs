using System.Diagnostics;
using Betriebsplattform.Web.Akten;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;

public sealed class DokumentOcrTests
{
    private sealed class OcrFact:FactAttribute{public OcrFact(){if(new[]{"FZ_PDF_TEST_EXE","FZ_OCR_TEST_EXE","FZ_OCR_TEST_DATA"}.Any(k=>string.IsNullOrEmpty(Environment.GetEnvironmentVariable(k))))Skip="Explicit installed local PDF/OCR tools and German model required.";}}
    private sealed class Zaehler(IOcrAnalyse inner):IOcrAnalyse{public int Aufrufe;public string Version=>inner.Version;public Task<Textabschnitt[]> Lesen(byte[] png,int seite,CancellationToken ct){Aufrufe++;return inner.Lesen(png,seite,ct);}}
    [OcrFact] public async Task EchteOcrNurBeiBildtextUndGemischteSeitenOriginalUnveraendert(){
        var gs=Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")!;var ocr=new Zaehler(new LokaleOcr(Environment.GetEnvironmentVariable("FZ_OCR_TEST_EXE")!,Environment.GetEnvironmentVariable("FZ_OCR_TEST_DATA")!));var pdf=new LokalerPdfAdapter(gs,ocr);var pipe=new Dokumentanalysator(pdf);
        var bytes=SynthetischeEingaenge.Datei("wartung-pdf").Bytes;var hash=System.Security.Cryptography.SHA256.HashData(bytes);
        var digital=await pipe.Analysieren(bytes,"Wartungsrechnung.pdf","application/pdf");Assert.Equal(0,ocr.Aufrufe);Assert.Equal("Digitaltext",digital.Seiten[0].Status);
        var dir=Path.Combine(Path.GetTempPath(),"mbop-ocr-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{
            await File.WriteAllBytesAsync(Path.Combine(dir,"digital.pdf"),bytes);
            async Task Gs(params string[] args){var psi=new ProcessStartInfo(gs){WorkingDirectory=dir,UseShellExecute=false,CreateNoWindow=true};foreach(var s in new[]{"-q","-dSAFER","-dBATCH","-dNOPAUSE","-sDEVICE=pdfwrite"}.Concat(args))psi.ArgumentList.Add(s);using var p=Process.Start(psi)!;using var limit=new CancellationTokenSource(TimeSpan.FromSeconds(30));try{await p.WaitForExitAsync(limit.Token);}finally{if(!p.HasExited)p.Kill(true);}Assert.Equal(0,p.ExitCode);}
            // Controlled synthetic scan equivalent: glyph outlines have no extractable text layer.
            await Gs("-dNoOutputFonts","-sOutputFile=scan.pdf","digital.pdf");await Gs("-sOutputFile=mixed.pdf","digital.pdf","scan.pdf");
            var mixed=await File.ReadAllBytesAsync(Path.Combine(dir,"mixed.pdf"));var a=await pipe.Analysieren(mixed,"Wartungsunterlagen.pdf","application/pdf");
            Assert.Equal(2,a.Seiten.Length);Assert.Equal("Digitaltext",a.Seiten[0].Status);Assert.Contains("OCR",a.Seiten[1].Status);Assert.Equal(1,ocr.Aufrufe);
            var gelesen=a.Abschnitte.Where(x=>x.Seite==2).ToArray();Assert.Contains(gelesen,x=>x.Text.Contains("RE-2026-1042"));Assert.All(gelesen,x=>{Assert.NotNull(x.Position);Assert.Equal("OCR-Rahmen",x.Position!.Art);Assert.InRange(x.Position.X,0,1);Assert.InRange(x.Position.Y,0,1);Assert.True(x.Position.Hoehe>0);});
            Assert.Equal(hash,System.Security.Cryptography.SHA256.HashData(bytes));
        }finally{Directory.Delete(dir,true);}
    }
    [Fact] public async Task FehlendesOcrWerkzeugBleibtGeschlossen(){await Assert.ThrowsAsync<Arbeitsfehler>(()=>new LokaleOcr(Path.GetFullPath("nicht-vorhanden.exe"),Path.GetFullPath("keine-sprachdaten")).Lesen(new byte[24],1,default));}
}
