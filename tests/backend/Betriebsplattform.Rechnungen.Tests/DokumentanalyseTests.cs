using System.Security.Cryptography;
using System.Text;
using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;

public sealed class DokumentanalyseTests
{
    [Fact] public async Task TextHatKeineErfundenenPositionenUndEinWortIstKeineRechnung(){
        var a=await new Dokumentanalysator().Analysieren(Encoding.UTF8.GetBytes("Rechnung\nKundennummer: 0042"),"Brief.txt","text/plain");
        Assert.Equal("Sonstiges",a.Dokumentart);Assert.All(a.Abschnitte,s=>Assert.Null(s.Position));Assert.Equal("0042",a.Felder.Single(f=>f.Name=="kundennummer").Wert);
    }
    [Theory][InlineData("12.34,50")][InlineData("1e3")][InlineData("12.345")][InlineData("-1")][InlineData("0")]
    public void GeldWirdNichtGeratenOderGerundet(string wert)=>Assert.Null(Dokumentanalysator.Betrag(wert));
    [Fact]public void EindeutigerDeutscherBetragUndDatumWerdenNormalisiert(){Assert.Equal(1249.50m,Dokumentanalysator.Betrag("1.249,50 EUR"));Assert.Equal("2026-10-14",Dokumentanalysator.Normalisieren("dokumentdatum","14.10.2026"));Assert.Equal("1249.50",Dokumentanalysator.Normalisieren("betrag","1.249,50 EUR"));}
    [Fact] public async Task WiderspruchUndUngueltigesDatumBleibenZurPruefung(){
        var a=await new Dokumentanalysator().Analysieren(Encoding.UTF8.GetBytes("Rechnungsnummer: A\nRechnungsnummer: B\nBetrag: 100.00\nDokumentdatum: 2026-02-30"),"Brief.txt","text/plain");
        Assert.Equal("Widerspruch",a.Felder.Single(f=>f.Name=="rechnungsnummer").Status);Assert.Equal("Widerspruch",a.Felder.Single(f=>f.Name=="dokumentdatum").Status);Assert.Equal("Sonstiges",a.Dokumentart);
    }
    private sealed class PdfFact:FactAttribute{public PdfFact(){if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")))Skip="Explicit installed local PDF adapter required.";}}
    [PdfFact] public async Task EchterLokalerPdfAdapterDigitaltextOriginalUndGrundlinien(){
        var exe=Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")!;
        var bytes=SynthetischeEingaenge.Datei("wartung-pdf").Bytes;var hash=SHA256.HashData(bytes);
        var adapter=new LokalerPdfAdapter(exe);var a=await new Dokumentanalysator(adapter).Analysieren(bytes,"Wartungsrechnung.pdf","application/pdf");
        Assert.Equal("Eingangsrechnung",a.Dokumentart);Assert.Single(a.Seiten);Assert.Equal(595,a.Seiten[0].Breite);Assert.Equal(842,a.Seiten[0].Hoehe);
        var f=a.Felder.Single(f=>f.Name=="rechnungsnummer");Assert.Equal("RE-2026-1042",f.Wert);var pos=a.Abschnitte.Single(x=>x.Id==f.AbschnittId).Position!;
        Assert.Equal("Grundlinie",pos.Art);Assert.InRange(pos.X,.070,.071);Assert.InRange(pos.Y,.125,.127);Assert.Equal(0,pos.Hoehe);
        var png=await adapter.Seite(bytes,1,default);Assert.Equal(new byte[]{137,80,78,71},png[..4]);Assert.Equal(hash,SHA256.HashData(bytes));
    }
    [PdfFact] public async Task RealeSeitenrotationUndBeschaedigtesPdfWerdenKontrolliert(){
        var adapter=new LokalerPdfAdapter(Environment.GetEnvironmentVariable("FZ_PDF_TEST_EXE")!);
        var bytes=GedrehtesPdf();
        var a=await new Dokumentanalysator(adapter).Analysieren(bytes,"Gedrehte Rechnung.pdf","application/pdf");
        Assert.Equal(90,a.Seiten[0].Rotation);Assert.Equal(842,a.Seiten[0].Breite);Assert.Equal(595,a.Seiten[0].Hoehe);
        Assert.Contains(a.Abschnitte,x=>x.Text.Contains("RE-2026-1042"));
        var png=await adapter.Seite(bytes,1,default);Assert.Equal(842,System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4)));Assert.Equal(595,System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4)));
        var ex=await Assert.ThrowsAsync<Arbeitsfehler>(()=>adapter.Lesen(Encoding.ASCII.GetBytes("%PDF-1.4\nbroken\n%%EOF"),default));Assert.Equal(422,ex.Status);
    }

    private static byte[] GedrehtesPdf(){
        const string content="BT /F1 12 Tf 42 790 Td (Rechnungsnummer: RE-2026-1042) Tj ET\n";
        string[] objects=["<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [3 0 R] /Count 1 >>","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Rotate 90 /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",$"<< /Length {content.Length} >>\nstream\n{content}endstream"];
        var pdf=new StringBuilder("%PDF-1.4\n");var offsets=new List<int>();for(int i=0;i<objects.Length;i++){offsets.Add(pdf.Length);pdf.Append($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}
        var xref=pdf.Length;pdf.Append("xref\n0 6\n0000000000 65535 f \n");foreach(var offset in offsets)pdf.Append(offset.ToString("D10",System.Globalization.CultureInfo.InvariantCulture)+" 00000 n \n");pdf.Append($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");return Encoding.ASCII.GetBytes(pdf.ToString());
    }

}
