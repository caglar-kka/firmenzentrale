using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Xunit;
using Xunit.Abstractions;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class DokumentpaketTests(ITestOutputHelper output)
{
    private sealed class PaketFact:FactAttribute{public PaketFact(){if(new[]{"FZ_DOCUMENT_PACKAGE","FZ_PDF_TEST_EXE","FZ_OCR_TEST_EXE","FZ_OCR_TEST_DATA","FZ_DOCUMENT_EVIDENCE"}.Any(k=>string.IsNullOrEmpty(Environment.GetEnvironmentVariable(k))))Skip="Explicit read-only fictional PDF package, local tools and evidence destination required.";}}
    [PaketFact] public async Task ZehnTatsaechlichePdfDateienMitManifestSollwertenUndGezielterOcr(){
        var root=Path.GetFullPath(Environment.GetEnvironmentVariable("FZ_DOCUMENT_PACKAGE")!);Assert.True(File.Exists(Path.Combine(root,"README_START_HIER.md")));
        var manifest=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root,"03_erwartungswerte","lokale_dateimanifest_sha256.json"))).RootElement;
        var soll=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root,"03_erwartungswerte","lokale_sollwerte.json"))).RootElement;
        var ocr=new LokaleOcr(Environment.GetEnvironmentVariable("FZ_OCR_TEST_EXE")!,Environment.GetEnvironmentVariable("FZ_OCR_TEST_DATA")!);
        var pipe=new Dokumentanalysator(new LokalerPdfAdapter(Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")!,ocr));var results=new List<object>();var fehler=new List<string>();
        foreach(var file in manifest.EnumerateArray()){
            var name=file.GetProperty("datei").GetString()!;Assert.Equal(name,Path.GetFileName(name));var bytes=await File.ReadAllBytesAsync(Path.Combine(root,"01_lokal_erstellte_dokumente",name));
            var hash=Convert.ToHexString(SHA256.HashData(bytes));Assert.Equal(file.GetProperty("sha256").GetString()!.ToUpperInvariant(),hash);Assert.Equal(file.GetProperty("bytes").GetInt32(),bytes.Length);Assert.Equal("%PDF-",System.Text.Encoding.ASCII.GetString(bytes,0,5));
            try{
                var a=await pipe.Analysieren(bytes,name,"application/pdf");Assert.Equal(file.GetProperty("seiten").GetInt32(),a.Seiten.Length);
                var expectedChars=file.GetProperty("zeichen_pro_seite_ohne_ocr").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
                for(var page=0;page<expectedChars.Length;page++)if(expectedChars[page]>0)Assert.Equal("Digitaltext",a.Seiten[page].Status);else Assert.Contains("OCR",a.Seiten[page].Status);
                var fields=new List<object>();var expected=soll.GetProperty(name);
                void Compare(string key,string feld,Func<string,string>? normal=null){if(!expected.TryGetProperty(key,out var wanted))return;var value=a.Felder.SingleOrDefault(f=>f.Name==feld);var target=wanted.GetString()!;var actual=value?.Wert;var ok=actual is not null&&(normal is null?actual==target:normal(actual)==normal(target));fields.Add(new{feld,soll=target,ist=actual,status=ok?"BESTANDEN":"FEHLGESCHLAGEN",verfahren=value?.AbschnittId is null?null:a.Abschnitte.Single(s=>s.Id==value.AbschnittId).Verfahren,quelle=value?.AbschnittId});if(!ok)fehler.Add(name+": "+feld);}
                string Geld(string s)=>decimal.Parse(s.Replace(" EUR",""),NumberStyles.Number,s.Contains(',')?CultureInfo.GetCultureInfo("de-DE"):CultureInfo.InvariantCulture).ToString("0.00",CultureInfo.InvariantCulture);
                string Datum(string s)=>DateOnly.TryParseExact(s,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var d)?d.ToString("yyyy-MM-dd"):DateOnly.ParseExact(s,"dd.MM.yyyy",CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                Compare("rechnungsnummer","rechnungsnummer");Compare("kundennummer","kundennummer");Compare("betrag_brutto_eur","betrag",Geld);Compare("bruttobetrag_eur","betrag",Geld);Compare("rechnungsdatum","dokumentdatum",Datum);Compare("datum","dokumentdatum",Datum);Compare("faelligkeit","faelligAm",Datum);Compare("frist","faelligAm",Datum);Compare("aktenzeichen","referenz");
                results.Add(new{datei=name,hash,analyse=a,felder=fields});
            }catch(Exception ex){fehler.Add(name+": "+ex.GetType().Name);results.Add(new{datei=name,hash,fehler=ex.GetType().Name});}
            Assert.Equal(hash,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(root,"01_lokal_erstellte_dokumente",name)))));
        }
        var target=Path.GetFullPath(Environment.GetEnvironmentVariable("FZ_DOCUMENT_EVIDENCE")!);Assert.False(target.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));Directory.CreateDirectory(target);await File.WriteAllTextAsync(Path.Combine(target,"paket-soll-ist.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions(JsonSerializerDefaults.Web){WriteIndented=true}));
        output.WriteLine("Ten read-only local fictional PDFs checked, no external downloads; "+fehler.Count+" deviations.");Assert.Empty(fehler);
    }
}
