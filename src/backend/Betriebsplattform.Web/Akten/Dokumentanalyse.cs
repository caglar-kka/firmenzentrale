using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Betriebsplattform.Web.Akten;

public sealed record Textposition(double X,double Y,double Breite,double Hoehe,string Art="Grundlinie");
public sealed record Textabschnitt(string Id,int Seite,string Text,Textposition? Position,string Verfahren);
public sealed record Dokumentseite(int Nummer,double Breite,double Hoehe,int Rotation,string Status);
public sealed record Prueffeld(string Name,string? Wert,string? AbschnittId,string Status="Vorschlag",string? AnkerAbschnittId=null);
public sealed record Dokumentanalyse(string Hash,string Version,string Status,Dokumentseite[] Seiten,Textabschnitt[] Abschnitte,string Dokumentart,Prueffeld[] Felder,string[] Hinweise,Prueffeld[]? BasisFelder=null,string? OrganisationId=null,string? AbsenderId=null,string[]? MusterIds=null,string? AutomatikMusterId=null);
public interface IPdfAnalyse
{
    string Version {get;}
    Task<(Dokumentseite[] Seiten,Textabschnitt[] Abschnitte)> Lesen(byte[] bytes,CancellationToken ct);
    Task<byte[]> Seite(byte[] bytes,int seite,CancellationToken ct);
}
public sealed class Dokumentanalysator(IPdfAnalyse? pdf=null)
{
    public string Version=>"regeln-2/"+(pdf?.Version??"ohne-pdf-adapter");
    public async Task<Dokumentanalyse> Analysieren(byte[] bytes,string name,string mime,CancellationToken ct=default)
    {
        // Only call after the common intake security gate has accepted the original.
        var grund=new Dokumentpruefung().Pruefen(bytes,name,mime);
        Dokumentseite[] seiten=[];Textabschnitt[] abschnitte=[];
        if(grund.Inhaltstyp=="application/pdf"&&pdf is not null)(seiten,abschnitte)=await pdf.Lesen(bytes,ct);
        else if(grund.Text.Length>0)abschnitte=grund.Text.Split('\n').Select((t,i)=>new Textabschnitt("text-"+i,1,t.TrimEnd('\r'),null,grund.Textmethode)).ToArray();
        var text=string.Join('\n',abschnitte.Select(x=>x.Text));
        var felder=new List<Prueffeld>();var hinweise=new List<string>();
        void Feld(string name,string[] anker,Func<string,bool>? valid=null){
            var treffer=new List<(Textabschnitt Wert,string Text,string Anker)>();
            foreach(var a in abschnitte)foreach(var k in anker){
                var m=Regex.Match(a.Text,@"^\s*"+Regex.Escape(k)+@"\s*:\s*(.{1,160})$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
                if(m.Success)treffer.Add((a,m.Groups[1].Value.Trim(),a.Id));
                else if(a.Text.Trim().TrimEnd(':').Equals(k,StringComparison.OrdinalIgnoreCase)&&a.Position is not null){
                    // Only an unambiguous neighbour in the same visual row. Never guess a value from the next paragraph.
                    var row=abschnitte.Where(b=>b.Seite==a.Seite&&b.Id!=a.Id&&b.Position is not null&&b.Position.X>a.Position.X+a.Position.Breite&&Math.Abs(b.Position.Y-a.Position.Y)<.008).ToArray();
                    if(row.Length==1)treffer.Add((row[0],row[0].Text.Trim(),a.Id));
                }
            }
            var unique=treffer.DistinctBy(t=>(t.Wert.Id,t.Text)).ToArray();
            if(unique.Length==1){var t=unique[0];felder.Add(new(name,Normalisieren(name,t.Text),t.Wert.Id,valid?.Invoke(t.Text)==false?"Widerspruch":"Vorschlag",t.Anker));}
            else felder.Add(new(name,null,null,unique.Length>1?"Widerspruch":"Fehlt"));
        }
        Feld("rechnungsnummer",["Rechnungsnummer","Rechnung Nr.","Beleg-ID"]);
        Feld("betrag",["Betrag","Brutto","Gesamtbetrag","Gesamtbetrag (EUR)","Rechnungsbetrag brutto"],s=>Betrag(s) is not null);
        Feld("dokumentdatum",["Dokumentdatum","Rechnungsdatum","Datum","Ausstellungsdatum"],s=>Datum(s) is not null);
        Feld("faelligAm",["Antwort bis","Fällig am","Faelligkeit","Fälligkeit","Zahlungsziel"],s=>Datum(s) is not null);
        if(felder.Single(f=>f.Name=="faelligAm").Status=="Fehlt"){
            var fristen=abschnitte.Select(a=>(a,m:Regex.Match(a.Text,@"\bbis zum\s+(\d{2}\.\d{2}\.\d{4})\b",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)))).Where(t=>t.m.Success).ToArray();
            if(fristen.Length==1){var f=fristen[0];felder[felder.FindIndex(x=>x.Name=="faelligAm")]=new("faelligAm",Datum(f.m.Groups[1].Value)?.ToString("yyyy-MM-dd"),f.a.Id,Datum(f.m.Groups[1].Value) is null?"Widerspruch":"Vorschlag");}
            else if(fristen.Length>1)felder[felder.FindIndex(x=>x.Name=="faelligAm")]=new("faelligAm",null,null,"Widerspruch");
        }
        Feld("referenz",["Referenz","Aktenzeichen","Vertragsreferenz","Vertragsnummer"]);
        Feld("kundennummer",["Kundennummer","Kunden-Nr.","Ihre Teilnehmer-/Kundennr."]);
        Feld("aussteller",["Aussteller","Absender"]);
        Feld("empfaenger",["Empfänger","Empfaenger","An","Kunde"]);
        Feld("vertragsnummer",["Vertragsnummer","Vertragsreferenz"]);Feld("schadennummer",["Schadennummer"]);
        Feld("leistungszeitraum",["Leistungszeitraum","Zeitraum"]);Feld("netto",["Netto","Nettobetrag"],s=>Betrag(s) is not null);
        Feld("steuer",["Umsatzsteuer 19%","Steuerbetrag"],s=>Betrag(s) is not null);Feld("steuernummer",["Steuernummer"]);Feld("sachbearbeitung",["Sachbearbeiterin","Sachbearbeiter"]);
        var nummer=felder.Single(x=>x.Name=="rechnungsnummer");var betrag=felder.Single(x=>x.Name=="betrag");
        // A keyword alone never produces an invoice classification.
        var rechnung=nummer.Status=="Vorschlag"&&betrag.Status=="Vorschlag";
        var behoerde=felder.Single(x=>x.Name=="referenz").Status=="Vorschlag"&&abschnitte.Any(a=>a.Text.StartsWith("Aktenzeichen:",StringComparison.OrdinalIgnoreCase))&&Regex.IsMatch(text,"Verwaltungsmitteilung|Finanzamt|Behörde",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
        var versicherung=felder.Single(x=>x.Name=="vertragsnummer").Status=="Vorschlag"&&Regex.IsMatch(text,"Versicherung",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
        var art=rechnung&&!behoerde&&!versicherung?"Eingangsrechnung":!rechnung&&behoerde&&!versicherung?"Behördenbrief":!rechnung&&!behoerde&&versicherung?"Versicherungsschreiben":"Sonstiges";
        if(new[]{rechnung,behoerde,versicherung}.Count(x=>x)>1)hinweise.Add("Mehrere Dokumentarten enthalten; getrennte menschliche Prüfung erforderlich.");
        var netto=felder.Single(x=>x.Name=="netto");var steuer=felder.Single(x=>x.Name=="steuer");
        if(netto.Status=="Vorschlag"&&steuer.Status=="Vorschlag"&&betrag.Status=="Vorschlag"&&Betrag(netto.Wert)+Betrag(steuer.Wert)!=Betrag(betrag.Wert)){felder[felder.FindIndex(x=>x.Name=="betrag")]=betrag with{Status="Widerspruch"};hinweise.Add("Netto und Steuer stimmen nicht mit Brutto überein.");}
        if(art=="Sonstiges")hinweise.Add("Dokumentart ungeklärt; menschliche Prüfung erforderlich.");
        if(abschnitte.Length==0)hinweise.Add("Kein brauchbarer Text. OCR ist nicht konfiguriert; keine automatische Wiederholung.");
        if(seiten.Any(s=>s.Status!="Digitaltext"))hinweise.Add("Mindestens eine Seite benötigt OCR oder eine menschliche Prüfung. OCR beweist keine fachliche Richtigkeit.");
        if(abschnitte.Any(a=>a.Position is null))hinweise.Add("Mindestens eine Fundstelle hat keine verlässliche Seitenposition.");
        if(abschnitte.Any(a=>a.Position?.Art=="Grundlinie"))hinweise.Add("PDF-Positionen sind Textgrundlinien, keine exakten Glyphenrahmen.");
        if(felder.Any(x=>x.Status=="Widerspruch"))hinweise.Add("Widersprüchliche oder ungültige Werte; keine automatische Übernahme.");
        return new(Convert.ToHexString(SHA256.HashData(bytes)),Version,abschnitte.Length==0?"OCR erforderlich":"Zur Prüfung",seiten,abschnitte,art,felder.ToArray(),hinweise.ToArray());
    }
    public static decimal? Betrag(string? text)
    {
        if(text is null)return null;var s=text.Replace(" EUR","",StringComparison.Ordinal).Trim();
        // No ambiguous grouping, exponent, rounding or implicit currency conversion.
        if(Regex.IsMatch(s,@"\A\d{1,3}(\.\d{3})+,\d{2}\z",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)))s=s.Replace(".","");
        if(!Regex.IsMatch(s,@"\A\d{1,13}([.,]\d{1,2})?\z",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)))return null;
        return decimal.TryParse(s.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var b)&&b>0&&b<=9999999999999m?b:null;
    }
    public static DateOnly? Datum(string? text)=>DateOnly.TryParseExact(text,["yyyy-MM-dd","dd.MM.yyyy"],CultureInfo.InvariantCulture,DateTimeStyles.None,out var d)?d:null;
    public static string Normalisieren(string feld,string text)=>feld is "betrag" or "netto" or "steuer"&&Betrag(text) is decimal b?b.ToString(CultureInfo.InvariantCulture):feld is "dokumentdatum" or "faelligAm"&&Datum(text) is DateOnly d?d.ToString("yyyy-MM-dd"):text;
    public static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
}
