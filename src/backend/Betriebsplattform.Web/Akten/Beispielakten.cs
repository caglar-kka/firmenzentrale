using System.IO.Compression;
using System.Text;
namespace Betriebsplattform.Web.Akten;

public sealed partial class Aktenbestand
{
    private string Kontaktname(string? id,string fallback)=>kontakte.Finden(id)?.Firma??fallback;
    private Vorgang Anreichern(Vorgang v)
    {
        var docs=dateien.Values.Where(d=>d.Ansicht.Vorgaenge.Contains(v.Id)).Select(d=>d.Ansicht).ToArray();
        var zeiten=docs.Select(d=>d.Aktivitaeten?.LastOrDefault()?.Zeitpunkt??d.ErfasstAm).Concat(notizen.Liste(v.Id).Select(n=>n.GeaendertAm)).Concat(post.Values.Where(p=>p.VorgangId==v.Id).Select(p=>p.ErfasstAm)).Concat(v.LetztesEreignis is null?[]:new[]{v.LetztesEreignis.Value}).ToArray();
        return v with {Status=v.Status=="Neu"?"Offen":v.Status,Kategorie=Kategorien.Finden(v.KategorieId??v.Kategorie,false).Name,Firma=Kontaktname(v.FirmaId,"Nicht zugeordnet"),Dokumentanzahl=docs.Length,LetztesEreignis=zeiten.Length>0?zeiten.Max():null,Kurzinfo=v.Rechnungsnummer is null?"Dokumente und Schriftverkehr":"Rechnungsakte · "+v.Rechnungsnummer};
    }
    // Nur feste, selbst erzeugte Beispiele. Keine Nutzerdaten werden durch diesen Generator verarbeitet.
    private void BeispieleEinrichten()
    {
        foreach(var (id,titel,kategorie,firma) in new[]{("fahrzeug-versicherung","Kfz-Versicherung Transporter","kfz-versicherung","DEMO-VERS"),("beitraege-2026","Sozialversicherungsbeitrag Oktober 2026","sozialversicherung","DEMO-KASSE"),("auskunft-2026","Rückfrage zur Gewerbeauskunft","behoerde","DEMO-AMT"),("mobilfunk-2026","Vertragsänderung Mobilfunk","vertrag","DEMO-MOBIL"),("buero-mahnung","Mahnung Büromaterial September 2026","mahnung","DEMO-ISD")})
            akten[id]=new(id,titel,Kategorien.Finden(kategorie).Name,firma,KategorieId:kategorie,Status:id=="auskunft-2026"?"Wartet auf Rückmeldung":id=="mobilfunk-2026"?"In Bearbeitung":"Offen");
        Aktendokument Datei(string titel,string name,byte[] bytes,string mime,string? akte,string art,string firma,string kat,string? vorschlag=null,bool eigenesPdf=false)
        {
            var d=Hinzufuegen(bytes,name,mime,akte);var meta=d with {Titel=titel,Art=art,KategorieId=kat,Kategorie=Kategorien.Finden(kat).Name,AbsenderId=firma,Status=akte is null?"Neu":"Zugeordnet",Vorgangsvorschlag=vorschlag,Pruefbedarf=akte is null,Kurzfassung=art=="Beitragsmitteilung"?"Mitteilung zum Beitrag für den im Dokument genannten Zeitraum.":titel=="Rückfrage zur Gewerbeauskunft"?"Die Verwaltungsstelle bittet um Ergänzung der Tätigkeitsbeschreibung.":titel=="Neue Erreichbarkeit im Vertragsservice"?"Der Vertragsservice teilt neue Kontaktangaben mit.":"",
                Handlungsbedarf=titel=="Rückfrage zur Gewerbeauskunft"?"Tätigkeitsbeschreibung ergänzen. Es wurde keine verbindliche Frist erfasst.":"Inhalt vor weiterer Bearbeitung prüfen. Es wurde keine Frist oder Zahlung automatisch bestätigt.",Auswertungsquelle="Redaktionell erfasste Beispielangaben; keine KI-/OCR-Auswertung",Vorschau=eigenesPdf?"pdf":d.Vorschau};dateien[d.Id].Ansicht=meta;return meta;
        }
        byte[] Pdf(params string[] zeilen)=>DemoDokumente.TestPdf(zeilen);
        var vers=Datei("Beitragsmitteilung Oktober 2026","Beitragsmitteilung_10-2026.pdf",Pdf("KIESELANKER VERSICHERUNG AG","Betriebshaftpflicht 2026","Versicherungsnehmer: Fjordfalter Objektpflege GmbH","Vertragszeitraum: Oktober bis Dezember 2026","Beitrag: 184,50 EUR","Datum: 15.10.2026"),"application/pdf","versicherung-2026","Beitragsmitteilung","DEMO-VERS","versicherung",eigenesPdf:true);
        var kontakt=Datei("Neue Erreichbarkeit im Vertragsservice","Versicherungsschreiben_18-10-2026.txt",Encoding.UTF8.GetBytes("Kieselanker Versicherung AG\nSchreiben vom 18.10.2026\nBetreff: Erreichbarkeit im Vertragsservice\nTelefon: +49 40 66969 138\nE-Mail: vertrag@kieselanker.invalid\nAnsprechpartner: Niko Elbfenn\nBitte prüfen Sie die Angaben vor einer Änderung Ihrer Kontaktakte."),"text/plain","versicherung-2026","Schreiben","DEMO-VERS","versicherung");
        Datei("Antwort zur Beitragsmitteilung","Antwortschreiben_18-10-2026.docx",Word("Rückfrage zur Beitragsmitteilung","Bitte erläutern Sie die Änderung des Versicherungsumfangs. Ansprechpartnerin: Lina Fennbrück."),"application/vnd.openxmlformats-officedocument.wordprocessingml.document","versicherung-2026","Antwortschreiben","DEMO-NORD","versicherung");
        Datei("Briefpost vom 18. Oktober 2026","Scan_Briefpost_18-10-2026.png",Bild(),"image/png","versicherung-2026","Scan","DEMO-VERS","versicherung");
        Datei("Sozialversicherungsbeitrag Oktober 2026","Mitteilung_Beitraege_Oktober.pdf",Pdf("FENNQUELL GESUNDHEITSKASSE","Sozialversicherungsbeitrag Oktober 2026","Fjordfalter Objektpflege GmbH","Mitteilung vom 16.10.2026","Bitte die Angaben vor Bearbeitung pruefen."),"application/pdf",null,"Beitragsmitteilung","DEMO-KASSE","sozialversicherung","beitraege-2026",true);
        Datei("Rückfrage zur Gewerbeauskunft","Rueckfrage_Gewerbeauskunft.txt",Encoding.UTF8.GetBytes("Verwaltungsstelle Musterhafen\nRückfrage zur Gewerbeauskunft\nBitte ergänzen Sie die Tätigkeitsbeschreibung.\nEs wurde keine verbindliche Frist erfasst."),"text/plain",null,"Allgemeines Schreiben","DEMO-AMT","behoerde","auskunft-2026");
        Datei("Vertragsänderung Mobilfunk","Vertragsaenderung_Mobilfunk.pdf",Pdf("TANNUFER KOMMUNIKATION GMBH","Vertragsaenderung Mobilfunk","Fjordfalter Objektpflege GmbH","Angebot zur Vertragspruefung","Keine automatische Zustimmung oder Buchung."),"application/pdf",null,"Vertrag","DEMO-MOBIL","vertrag","mobilfunk-2026",true);
        Datei("Briefpost zur Zuordnung","Briefpost_18-10-2026.txt",Encoding.UTF8.GetBytes("Eingegangene Briefpost\nAbsender noch nicht zugeordnet.\nBitte Inhalt und Vorgangsbezug prüfen."),"text/plain",null,"Briefpost","","ungeklaert");
        NotizSpeichern("versicherung-2026",null,new("Deckungsumfang prüfen","Beitragsmitteilung mit dem bisherigen Vertragsumfang vergleichen. Es wurde keine Vertragsänderung freigegeben."));
        KorrespondenzSpeichern("versicherung-2026",new("Rückfrage zum Beitrag","Bitte die Veränderung des Deckungsumfangs erläutern.","Anfrage",DokumentIds:[vers.Id,kontakt.Id],Richtung:"Gesendet",KontaktId:"DEMO-VERS",KommuniziertAm:DateTimeOffset.UtcNow));
    }
    private static byte[] Word(string titel,string text)
    {
        using var ms=new MemoryStream();using(var zip=new ZipArchive(ms,ZipArchiveMode.Create,true)){
            void Teil(string name,string inhalt){using var w=new StreamWriter(zip.CreateEntry(name).Open());w.Write(inhalt);}
            Teil("[Content_Types].xml","<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/><Override PartName='/word/document.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml'/></Types>");
            Teil("_rels/.rels","<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='r1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='word/document.xml'/></Relationships>");
            Teil("word/document.xml","<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>"+System.Security.SecurityElement.Escape(titel)+"</w:t></w:r></w:p><w:p><w:r><w:t>"+System.Security.SecurityElement.Escape(text)+"</w:t></w:r></w:p></w:body></w:document>");
        }return ms.ToArray();
    }
    private static byte[] Bild()
    {
        using var ms=new MemoryStream();ms.Write([137,80,78,71,13,10,26,10]);
        void Chunk(string typ,byte[] b){Span<byte> l=stackalloc byte[4];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(l,b.Length);ms.Write(l);var t=Encoding.ASCII.GetBytes(typ);ms.Write(t);ms.Write(b);uint crc=0xffffffff;foreach(var v in t.Concat(b)){crc^=v;for(int i=0;i<8;i++)crc=(crc&1)!=0?(crc>>1)^0xedb88320:crc>>1;}System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(l,~crc);ms.Write(l);}
        var head=new byte[13];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(head.AsSpan(0,4),640);System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(head.AsSpan(4,4),400);head[8]=8;head[9]=2;Chunk("IHDR",head);
        using var pixels=new MemoryStream();using(var z=new ZLibStream(pixels,CompressionLevel.Optimal,true))for(int y=0;y<400;y++){z.WriteByte(0);for(int x=0;x<640;x++){byte c=(byte)(x>45&&x<540&&(y is >50 and <62||y>110&&y<320&&y%30<3)?75:250);z.Write([c,c,c]);}}
        Chunk("IDAT",pixels.ToArray());Chunk("IEND",[]);return ms.ToArray();
    }
}
