using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Betriebsplattform.Web.Akten;

// Konservativer lokaler Formatfilter, kein Virenscanner oder allgemeiner PDF-Interpreter.
public sealed class Dokumentpruefung : IDocumentTextExtractor
{
    public const int MaxBytes = 20_000_000;
    private const int MaxOfficeTeil = 2 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    public Dateiergebnis Pruefen(byte[] bytes, string dateiname, string mime)
    {
        if (bytes.Length is 0 or > MaxBytes) throw new Arbeitsfehler("Bitte eine Datei bis 20 MB auswählen.");
        if (dateiname.Length is 0 or > 180 || dateiname.Any(c => char.IsControl(c) || c is '/' or '\\' or ':'))
            throw new Arbeitsfehler("Der Dateiname ist nicht zulässig.");
        var ext = Path.GetExtension(dateiname).ToLowerInvariant();
        var erwartet = ext switch {
            ".pdf" => "application/pdf", ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".txt" => "text/plain", ".csv" => "text/csv",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _ => ""
        };
        if (erwartet == "" || (mime != "" && mime != "application/octet-stream" && mime.Split(';')[0] != erwartet && !(ext==".csv"&&mime.Split(';')[0] is "text/plain" or "application/vnd.ms-excel")))
            throw new Arbeitsfehler("Dateityp passt nicht. Erlaubt sind PDF, PNG, JPG, TXT, CSV, DOCX und XLSX ohne Makros.");
        try {
            return ext switch {
                ".txt" => Text(bytes), ".csv" => Text(bytes) with {Inhaltstyp="text/csv",Vorschau="download",Textmethode="UTF-8-Text; CSV wird nicht ausgewertet, Formeln werden nicht ausgeführt"}, ".pdf" => Pdf(bytes), ".png" => Png(bytes), ".jpg" or ".jpeg" => Jpeg(bytes),
                _ => Office(bytes, ext, erwartet)
            };
        }
        catch (Arbeitsfehler) { throw; }
        catch (Exception e) when (e is InvalidDataException or XmlException or DecoderFallbackException or IOException or ArgumentException or OverflowException)
        { throw new Arbeitsfehler("Die Datei ist beschädigt oder enthält nicht unterstützte Inhalte. Es wurde nichts abgelegt."); }
    }
    private static Dateiergebnis Text(byte[] b)
    {
        var text = Utf8.GetString(b).TrimStart('\uFEFF');
        if (text.Length > 64000 || text.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t'))
            throw new Arbeitsfehler("Text benötigt lesbaren UTF-8-Text mit höchstens 64.000 Zeichen.");
        return new("text/plain", "text", text, "UTF-8-Text, keine Interpretation");
    }
    private static Dateiergebnis Pdf(byte[] b)
    {
        var text = Encoding.Latin1.GetString(b);
        if (!text.StartsWith("%PDF-1.", StringComparison.Ordinal) || !text.TrimEnd().EndsWith("%%EOF", StringComparison.Ordinal))
            throw new Arbeitsfehler("Die Datei besitzt keine unterstützte PDF-Struktur.");
        if(!Regex.IsMatch(text,@"/Type\s*/Catalog\b")||!Regex.IsMatch(text,@"startxref\s+\d+\s+%%EOF\s*\z"))
            throw new Arbeitsfehler("Diese PDF-Struktur kann ohne zusätzlichen PDF-Prüfer nicht sicher angenommen werden.");
        var namen = Regex.Replace(text, @"#([0-9a-fA-F]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        if (Regex.IsMatch(namen, @"/(JavaScript|JS|OpenAction|AA|Launch|EmbeddedFile|Filespec|XFA|Encrypt|SubmitForm|GoToR|URI)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            throw new Arbeitsfehler("Aktive, eingebettete oder verschlüsselte PDF-Inhalte werden in dieser Demo nicht angenommen.");
        // Nur unkomprimierte einfache Test-PDFs: Textoperationen werden gelesen, niemals ausgeführt.
        var einfach = !Regex.IsMatch(namen, @"/(Filter|ObjStm|AcroForm|Annots)\b") && text.Length < 100000
            && Regex.IsMatch(text, @"/Type\s*/Catalog\b") && Regex.IsMatch(text, @"/Type\s*/Page\b");
        var extrahiert = einfach ? string.Join('\n', Regex.Matches(text, @"\(((?:\\.|[^()\\])*)\)\s*Tj\b")
            .Select(m => Regex.Replace(m.Groups[1].Value, @"\\([\\()])", "$1"))) : "";
        // Unbekannte PDFs erhalten keine Inlinefreigabe. Nur feste Katalogbytes werden später freigeschaltet.
        return new("application/pdf", "download", extrahiert[..Math.Min(extrahiert.Length, 64000)],
            einfach ? "Begrenzte unkomprimierte PDF-Textoperationen; keine OCR" : "Kein passender Textextraktor; keine OCR");
    }
    private static Dateiergebnis Png(byte[] b)
    {
        if (b.Length < 45 || !b.AsSpan(0, 8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) throw new InvalidDataException();
        var pos = 8; var ihdr = false; var idat = false; var ende = false;
        while (pos + 12 <= b.Length) {
            var len = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(pos,4)));
            if (len > MaxBytes || pos + 12L + len > b.Length) throw new InvalidDataException();
            var typ = Encoding.ASCII.GetString(b,pos+4,4);
            if (!ihdr && typ != "IHDR") throw new InvalidDataException();
            if (typ == "IHDR") {
                if (ihdr || len != 13) throw new InvalidDataException();
                Dimension(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(pos+8,4)),System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(pos+12,4)));
                ihdr=true;
            }
            uint crc=0xffffffff;
            foreach(var value in b.AsSpan(pos+4,len+4)){crc^=value;for(var i=0;i<8;i++)crc=(crc&1)!=0?(crc>>1)^0xedb88320:crc>>1;}
            if (~crc != System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(pos+8+len,4)))throw new InvalidDataException();
            if (typ == "IDAT") idat=true;
            if (typ == "IEND") { if(len!=0 || pos+12!=b.Length)throw new InvalidDataException();ende=true;break; }
            pos += 12+len;
        }
        if(!ihdr||!idat||!ende)throw new InvalidDataException();
        return new("image/png","bild","","Scanbild; OCR nicht eingerichtet");
    }
    private static Dateiergebnis Jpeg(byte[] b)
    {
        if(b.Length<8||b[0]!=255||b[1]!=216||b[^2]!=255||b[^1]!=217)throw new InvalidDataException();
        var pos=2;var dimension=false;
        while(pos+4<b.Length){
            if(b[pos++]!=255)throw new InvalidDataException();while(pos<b.Length&&b[pos]==255)pos++;
            if(pos>=b.Length)throw new InvalidDataException();var marker=b[pos++];if(marker==218)break;
            if(marker is 216 or 217)continue;if(pos+2>b.Length)throw new InvalidDataException();var length=(b[pos]<<8)|b[pos+1];if(length<2||pos+length>b.Length)throw new InvalidDataException();
            if(marker is 192 or 193 or 194){if(length<8)throw new InvalidDataException();Dimension((uint)((b[pos+5]<<8)|b[pos+6]),(uint)((b[pos+3]<<8)|b[pos+4]));dimension=true;}pos+=length;
        }
        if(!dimension)throw new InvalidDataException();return new("image/jpeg","bild","","Scanbild; OCR nicht eingerichtet");
    }
    private static void Dimension(uint w,uint h){if(w is 0 or >10000||h is 0 or >10000||(ulong)w*h>20000000)throw new Arbeitsfehler("Das Bild überschreitet die erlaubten Abmessungen.");}
    private static Dateiergebnis Office(byte[] b,string ext,string mime)
    {
        using var zip=new ZipArchive(new MemoryStream(b),ZipArchiveMode.Read);
        if(zip.Entries.Count is 0 or >128)throw new InvalidDataException();
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long sum=0;XDocument? inhalt=null;
        foreach(var e in zip.Entries){
            if(!names.Add(e.FullName)||e.FullName.StartsWith('/')||e.FullName.Split('/').Any(p=>p is ".." or ".")||e.FullName.Contains('\\')
                ||e.FullName.Contains(':')||e.FullName.EndsWith(".bin",StringComparison.OrdinalIgnoreCase)||e.FullName.Contains("embeddings",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException();
            sum+=e.Length;if(e.Length>MaxOfficeTeil||sum>8*MaxOfficeTeil||e.Length>Math.Max(1,e.CompressedLength)*100)throw new InvalidDataException();
            if(!e.FullName.EndsWith(".xml",StringComparison.OrdinalIgnoreCase)&&!e.FullName.EndsWith(".rels",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException();
            var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaxOfficeTeil};
            using(var stream=e.Open())using(var check=XmlReader.Create(stream,settings))
                while(check.Read())if(check.Depth>32)throw new InvalidDataException();
            using var xml=e.Open();using var reader=XmlReader.Create(xml,settings);
            var doc=XDocument.Load(reader);
            if(doc.Descendants().Attributes().Any(a=>(a.Name.LocalName=="TargetMode"&&a.Value.Equals("External",StringComparison.OrdinalIgnoreCase))
                ||(a.Name.LocalName=="Target"&&(a.Value.Contains(':')||a.Value.StartsWith('/')))
                ||a.Value.Contains("macroEnabled",StringComparison.OrdinalIgnoreCase)||a.Value.Contains("vbaProject",StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException();
            if(e.FullName==(ext==".docx"?"word/document.xml":"xl/workbook.xml"))inhalt=doc;
        }
        if(!names.Contains("[Content_Types].xml")||!names.Contains("_rels/.rels")||inhalt is null)throw new InvalidDataException();
        var text=ext==".docx"?string.Join('\n',inhalt.Descendants().Where(e=>e.Name.LocalName=="t").Select(e=>e.Value)):"";
        if(text.Length>64000)throw new InvalidDataException();
        return new(mime,"download",text,ext==".docx"?"DOCX-Absatztexte; keine Office-Ausführung":"Office-Anhang; kein Datenimport und keine Formelberechnung");
    }
}
