using System.Text.RegularExpressions;
namespace Betriebsplattform.Web.Akten;

public sealed record Kontaktwerteingabe(string Art,string Wert,bool Primaer,string Typ="");
public sealed record Kontakteingabe(string? Name,string? Art,string? FirmaId,string? Anschrift,string? Abteilung,string? Rolle,IReadOnlyList<Kontaktwerteingabe>? Werte,int Version=0,bool DublettenBestaetigt=false,string? Strasse=null,string? Plz=null,string? Ort=null,string? Land=null,string? Hausnummer=null,string? Vorname=null,string? Nachname=null);

internal sealed partial class Kontaktpflege
{
    internal Firmenkontakt Speichern(string? id,Kontakteingabe e)
    {
        if(e.Art is not ("Firma" or "Person"))throw new Arbeitsfehler("Bitte Firma oder Person wählen.");
        var alt=id is null?null:kontakte.GetValueOrDefault(id)??throw new Arbeitsfehler("Kontakt nicht gefunden.",404);
        if(alt is not null){Arbeitsregeln.Version(e.Version,alt.Version);if(alt.Archiviert)throw new Arbeitsfehler("Archivierter Kontakt bleibt schreibgeschützt.",409);if(alt.Art!=e.Art)throw new Arbeitsfehler("Firma und Person können nicht ineinander umgewandelt werden.");}
        var vorname=e.Art=="Person"?Arbeitsregeln.Text(e.Vorname??alt?.Vorname,"Vorname",80):"";
        var nachname=e.Art=="Person"?Arbeitsregeln.Text(e.Nachname??alt?.Nachname,"Nachname",80):"";
        if(vorname.Any(char.IsControl)||nachname.Any(char.IsControl))throw new Arbeitsfehler("Bitte Vorname und Nachname ohne Zeilenumbrüche eingeben.");
        var name=e.Art=="Person"?vorname+" "+nachname:Arbeitsregeln.Text(e.Name,"Firmenname",160);
        if(e.FirmaId is not null&&(!kontakte.TryGetValue(e.FirmaId,out var firma)||firma.Art!="Firma"||firma.Archiviert||e.Art!="Person"))throw new Arbeitsfehler("Bitte eine aktive Firma für den Ansprechpartner wählen.");
        if((e.Anschrift?.Length??0)>500||(e.Abteilung?.Length??0)>100||(e.Rolle?.Length??0)>100||e.Werte is null||e.Werte.Count>30)throw new Arbeitsfehler("Kontaktfelder oder Kontaktwerte sind zu lang.");
        var eingaben=e.Werte.Select(w=>w with {Wert=Arbeitsregeln.Text(w.Wert,w.Art,200)}).ToArray();
        if(eingaben.Any(w=>w.Art is not ("Telefon" or "E-Mail" or "Website")||!Gueltig(w.Art,w.Wert)))throw new Arbeitsfehler("Bitte gültige E-Mail-Adressen, Telefonnummern oder Websites mit https:// eingeben.");
        if(!eingaben.Any(w=>w.Art=="Telefon"))throw new Arbeitsfehler("Bitte mindestens eine Telefonnummer eingeben.");
        if(e.Art=="Firma"&&!eingaben.Any(w=>w.Art=="E-Mail"))throw new Arbeitsfehler("Bitte mindestens eine E-Mail-Adresse für die Firma eingeben.");
        if(eingaben.GroupBy(w=>(w.Art,Datenvergleich.Wert(w.Art,w.Wert,e.Land??alt?.Land))).Any(g=>g.Count()>1))throw new Arbeitsfehler("Kontaktwert ist doppelt angegeben.");
        foreach(var art in new[]{"Telefon","E-Mail","Website"})if(eingaben.Count(w=>w.Art==art&&w.Primaer)>1)throw new Arbeitsfehler("Bitte je Kontaktart eine bevorzugte Angabe bestimmen.");
        var hinweise=kontakte.Values.Where(k=>k.Id!=id&&!k.Archiviert&&k.Art==e.Art).Select(k=>{
            var gruende=new List<string>();if(Datenvergleich.NameAehnlich(k.Firma,name)||(e.Art=="Person"&&k.Vorname.Length>0&&Datenvergleich.NameAehnlich(k.Vorname,vorname)&&Datenvergleich.NameAehnlich(k.Nachname,nachname)))gruende.Add("Ähnlicher Name – kann ein anderer Kontakt sein");
            if(e.FirmaId is not null&&k.FirmaId==e.FirmaId&&gruende.Count>0)gruende.Add("Gleiche Firma");
            if(!string.IsNullOrWhiteSpace(e.Ort)&&Datenvergleich.Normal(e.Ort)==Datenvergleich.Normal(k.Ort)&&gruende.Count>0)gruende.Add("Gleicher Ort");
            if(!string.IsNullOrWhiteSpace(e.Strasse)&&!string.IsNullOrWhiteSpace(e.Hausnummer)&&!string.IsNullOrWhiteSpace(e.Plz)&&Datenvergleich.Normal(e.Strasse)==Datenvergleich.Normal(k.Strasse)&&Datenvergleich.Normal(e.Hausnummer)==Datenvergleich.Normal(k.Hausnummer)&&Datenvergleich.Normal(e.Plz)==Datenvergleich.Normal(k.Plz))gruende.Add("Gleiche Anschrift: Straße, Hausnummer und PLZ");
            foreach(var w in eingaben)if(k.Werte.Any(a=>a.Aktiv&&a.Art==w.Art&&Datenvergleich.Wert(a.Art,a.Wert,k.Land)==Datenvergleich.Wert(w.Art,w.Wert,e.Land??alt?.Land)))gruende.Add("Gleiche Angabe: "+w.Art);
            return new Dublettenhinweis(k.Id,k.Firma,k.Art,k.Anschrift,k.Werte.Where(w=>w.Aktiv).Select(w=>w.Art+": "+w.Wert).ToArray(),gruende.Distinct().ToArray());
        }).Where(h=>h.Gruende.Count>0).Take(10).ToArray();
        if(hinweise.Length>0&&!e.DublettenBestaetigt)throw new Arbeitsfehler("Ähnliche Kontaktdaten vorhanden. Bitte vergleichen und die Neuanlage oder Änderung bewusst bestätigen.",409,hinweise);
        if(kontakte.Count>=500&&id is null)throw new Arbeitsfehler("Die lokale Kontaktgrenze ist erreicht.");
        if(alt?.Historie.Count>=500)throw new Arbeitsfehler("Die lokale Kontakthistorie ist voll.");
        var werte=(alt?.Werte??[]).Select(w=>w.Art is "Telefon" or "E-Mail" or "Website"?w with{Aktiv=false,Primaer=false}:w).ToList();
        foreach(var art in new[]{"Telefon","E-Mail","Website"}){
            var liste=eingaben.Where(w=>w.Art==art).ToArray();
            for(int i=0;i<liste.Length;i++){var w=liste[i];werte.Add(new(Guid.NewGuid().ToString("N"),art,w.Wert,true,w.Primaer||(!liste.Any(x=>x.Primaer)&&i==0),w.Typ));}
        }
        if(werte.Count>1500)throw new Arbeitsfehler("Die lokale Wertehistorie ist voll.");
        var key=id??Guid.NewGuid().ToString("N");var zeit=DateTimeOffset.UtcNow;
        foreach(var (wert,max) in new[]{(e.Strasse,160),(e.Hausnummer,20),(e.Plz,20),(e.Ort,120),(e.Land,80)})if(wert is not null&&(wert.Length>max||wert.Any(char.IsControl)))throw new Arbeitsfehler("Bitte Anschrift und Feldlängen prüfen.");
        if(eingaben.Any(w=>w.Art=="Telefon"&&!new[]{"","Zentrale","Festnetz","Durchwahl","Mobil","Fax","Sonstiges"}.Contains(w.Typ)))throw new Arbeitsfehler("Bitte eine angebotene Telefonart wählen.");
        var strasse=e.Strasse?.Trim()??alt?.Strasse??"";var hausnummer=e.Hausnummer?.Trim()??alt?.Hausnummer??"";
        var anschrift=e.Strasse is null&&e.Hausnummer is null?e.Anschrift?.Trim()??alt?.Anschrift??"":string.Join(" · ",new[]{string.Join(" ",new[]{strasse,hausnummer}.Where(s=>s.Length>0)),string.Join(" ",new[]{e.Plz?.Trim(),e.Ort?.Trim()}.Where(s=>!string.IsNullOrWhiteSpace(s))),e.Land?.Trim()}.Where(s=>!string.IsNullOrWhiteSpace(s)));
        var neu=new Firmenkontakt(key,name,(alt?.Version??0)+1,werte.AsReadOnly(),alt?.Historie??[],e.Art,e.FirmaId,anschrift,e.Abteilung?.Trim()??"",e.Rolle?.Trim()??"",false,alt?.ErstelltAm??zeit,zeit,strasse,e.Plz?.Trim()??alt?.Plz??"",e.Ort?.Trim()??alt?.Ort??"",e.Land?.Trim()??alt?.Land??"",hausnummer,vorname,nachname);
        var aktion=alt is null?"Angelegt":"Bearbeitet";
        if(hinweise.Length>0&&e.DublettenBestaetigt)aktion+=" trotz Ähnlichkeit nach Prüfung";
        var h=new Kontaktentscheidung(aktion,alt is null?"":Beschreibung(alt),Beschreibung(neu),"",Arbeitskontext.Benutzer,zeit);
        kontakte[key]=neu with {Historie=Array.AsReadOnly(neu.Historie.Append(h).ToArray())};return kontakte[key];
    }
    internal Firmenkontakt Archivieren(string id,int version)
    {
        var k=kontakte.GetValueOrDefault(id)??throw new Arbeitsfehler("Kontakt nicht gefunden.",404);Arbeitsregeln.Version(version,k.Version);
        if(k.Archiviert)throw new Arbeitsfehler("Kontakt ist bereits archiviert.",409);
        if(k.Historie.Count>=500)throw new Arbeitsfehler("Die lokale Kontakthistorie ist voll.");
        var h=new Kontaktentscheidung("Archiviert",Beschreibung(k),"Archiviert – Verweise bleiben erhalten","",Arbeitskontext.Benutzer,DateTimeOffset.UtcNow);
        return kontakte[id]=k with {Archiviert=true,Version=k.Version+1,GeaendertAm=h.Zeitpunkt,Historie=Array.AsReadOnly(k.Historie.Append(h).ToArray())};
    }
    private string Beschreibung(Firmenkontakt k)=>string.Join(" · ",new[]{k.Art,k.Firma,k.FirmaId is not null?kontakte.GetValueOrDefault(k.FirmaId)?.Firma??"":"",k.Anschrift,k.Abteilung,k.Rolle}.Concat(k.Werte.Where(w=>w.Aktiv).Select(w=>w.Art+": "+w.Wert+(w.Primaer?" (bevorzugt)":""))).Where(s=>!string.IsNullOrWhiteSpace(s)));
}
// Vergleichsschlüssel verändern niemals sichtbare Originalwerte. Ähnlichkeit ist keine Identität.
public static class Datenvergleich
{
 public static string Normal(string s)=>Regex.Replace(s.ToLowerInvariant().Replace("ä","ae").Replace("ö","oe").Replace("ü","ue").Replace("ß","ss"),@"[^a-z0-9]","");
 public static string Wert(string art,string s,string? land=null){
  if(art=="E-Mail")return s.Trim().ToLowerInvariant();
  if(art=="Telefon"){
   var n=Regex.Replace(s,@"[^0-9+]","");if(n.StartsWith("00"))n="+"+n[2..];
   if(land is "Deutschland" or "DE"&&n.StartsWith("0"))n="+49"+n[1..];return n;
  }
  if(art=="Website"&&Uri.TryCreate(s,UriKind.Absolute,out var u))return u.Host.ToLowerInvariant()+u.AbsolutePath.TrimEnd('/');
  return Normal(s);
 }
 public static bool NameAehnlich(string a,string b){
  string Key(string v)=>Normal(Regex.Replace(v.ToLowerInvariant(),@"\b(gmbh|ag|ug|kg|ohg|mbh)\b",""));
  var x=Key(a);var y=Key(b);if(x==y)return true;if(x.Length<4||y.Length<4||Math.Abs(x.Length-y.Length)>2)return false;
  var row=Enumerable.Range(0,y.Length+1).ToArray();for(int i=1;i<=x.Length;i++){var prev=row[0];row[0]=i;for(int j=1;j<=y.Length;j++){var alt=row[j];row[j]=Math.Min(Math.Min(row[j]+1,row[j-1]+1),prev+(x[i-1]==y[j-1]?0:1));prev=alt;}}return row[y.Length]<=2;
 }
}
