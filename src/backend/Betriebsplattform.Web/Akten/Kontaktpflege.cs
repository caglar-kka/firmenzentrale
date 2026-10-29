using System.Text.RegularExpressions;
namespace Betriebsplattform.Web.Akten;
internal sealed partial class Kontaktpflege
{
 private readonly Dictionary<string,Firmenkontakt> kontakte=[];
 private readonly Dictionary<string,Kontaktvorschlag> vorschlaege=[];
 internal Kontaktpflege(){
  foreach(var (id,name) in DemoDaten.Namen){
   var w=new List<Kontaktwert>();
   if(id=="DEMO-NORD"){w.Add(new("nord-mail","E-Mail","buero@fjordfalter.invalid",true,true));w.Add(new("nord-tel","Telefon","+49 30 23125 001",true,true));}
   if(id=="DEMO-ISD")w.Add(new("isd-mail","E-Mail","kontakt@elbpfad.invalid",true,true));
   if(id=="DEMO-VERS"){w.Add(new("vers-mail","E-Mail","service@kieselanker.invalid",true,true));w.Add(new("vers-tel","Telefon","+49 40 66969 124",true,true));}
   kontakte[id]=new(id,name,1,w.AsReadOnly(),[],Anschrift:id=="DEMO-NORD"?"Fennbogen 8 · 00000 Musterhafen":"",ErstelltAm:DateTimeOffset.Parse("2026-10-01T09:00:00Z"),GeaendertAm:DateTimeOffset.Parse("2026-10-01T09:00:00Z"));
  }
  foreach(var (id,vorname,nachname,firma,abteilung) in new[]{("person-lina","Lina","Fennbrück","DEMO-NORD","Büro"),("person-joris","Joris","Kieswelle","DEMO-ISD","Service"),("person-mara","Mara","Tannhafen","DEMO-VERS","Vertragsservice")})
   kontakte[id]=new(id,vorname+" "+nachname,1,[new(id+"-tel","Telefon",id=="person-lina"?"+49 30 23125 021":id=="person-joris"?"+49 30 23125 022":"+49 40 66969 125",true,true,"Durchwahl")],[],"Person",firma,"",abteilung,"Ansprechpartner",false,DateTimeOffset.Parse("2026-10-01T09:00:00Z"),Vorname:vorname,Nachname:nachname,GeaendertAm:DateTimeOffset.Parse("2026-10-01T09:00:00Z"));
 }
 internal Firmenkontakt? Finden(string? id)=>id is null?null:kontakte.GetValueOrDefault(id);
 internal IReadOnlyList<Firmenkontakt> Liste()=>kontakte.Values.ToArray();
 internal bool HatFirma(string? id)=>id is not null&&kontakte.TryGetValue(id,out var k)&&k.Art=="Firma"&&!k.Archiviert;
 internal IReadOnlyList<Kontaktvorschlag> Vorschlaege(IEnumerable<string> docs)=>vorschlaege.Values.Where(v=>docs.Contains(v.DokumentId)).Select(v=>v.Status=="Zur Prüfung"&&kontakte.TryGetValue(v.FirmaId,out var k)?v with{Kontaktversion=k.Version,Bisher=string.Join(" · ",Aktuell(k,v.Art)),Firma=k.Firma}:v).ToArray();
 private string[] Aktuell(Firmenkontakt k,string art)=>art=="Ansprechpartner"?kontakte.Values.Where(p=>p.Art=="Person"&&p.FirmaId==k.Id&&!p.Archiviert).Select(p=>p.Firma).ToArray():art=="Anschrift"?(k.Anschrift.Length>0?[k.Anschrift]:[]):k.Werte.Where(w=>w.Aktiv&&w.Art==art).OrderByDescending(w=>w.Primaer).Select(w=>w.Wert).ToArray();
 internal void Erkennen(string firma,string doc,string titel,string text){
  if(!kontakte.TryGetValue(firma,out var k)||k.Archiviert)return;
  foreach(Match m in Regex.Matches(text,@"(?im)^\s*(Telefon|E-Mail|Ansprechpartner|Anschrift|Abteilung):\s*([^\r\n]{1,200})\s*$",RegexOptions.CultureInvariant)){
   var art=m.Groups[1].Value;art=art.Equals("E-Mail",StringComparison.OrdinalIgnoreCase)?"E-Mail":char.ToUpperInvariant(art[0])+art[1..].ToLowerInvariant();var wert=m.Groups[2].Value.Trim();
   if(!Gueltig(art,wert)||Aktuell(k,art).Any(a=>Datenvergleich.Wert(art,a,k.Land)==Datenvergleich.Wert(art,wert,k.Land))||vorschlaege.Values.Any(v=>v.FirmaId==firma&&v.DokumentId==doc&&v.Art==art&&v.Neu==wert)||vorschlaege.Count>=2000)continue;
   var id=Guid.NewGuid().ToString("N");vorschlaege[id]=new(id,firma,doc,art,string.Join(" · ",Aktuell(k,art)),wert,titel,"Zur Prüfung",k.Version,k.Firma);
  }
 }
 internal static bool Gueltig(string art,string wert)=>art switch{
  "E-Mail"=>Regex.IsMatch(wert,@"\A[A-Za-z0-9_+.-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\z")&&!wert.Contains("..")&&wert.Length<=254,
  "Website"=>Uri.TryCreate(wert,UriKind.Absolute,out var uri)&&uri.Scheme is "https" or "http"&&uri.UserInfo.Length==0&&!string.IsNullOrWhiteSpace(uri.Host),
  "Telefon"=>Kontaktvalidierung.Telefon(wert),_=>wert.Length is >0 and <=200&&!wert.Any(char.IsControl)
 };
 internal Firmenkontakt Entscheiden(string id,Kontaktfreigabe e){
  if(!vorschlaege.TryGetValue(id,out var v))throw new Arbeitsfehler("Kontaktvorschlag nicht gefunden.",404);
  var k=kontakte[v.FirmaId];Arbeitsregeln.Version(e.Version,k.Version);
  if(k.Archiviert||v.Status!="Zur Prüfung")throw new Arbeitsfehler("Dieser Vorschlag kann nicht mehr übernommen werden.",409);
  if(e.Aktion is not("Übernehmen" or "Zusätzlich speichern" or "Ignorieren"))throw new Arbeitsfehler("Bitte eine Entscheidung wählen.");
  if(k.Werte.Count>=250||k.Historie.Count>=500)throw new Arbeitsfehler("Die lokale Kontakthistorie ist voll.");
  var werte=k.Werte.ToList();var vorher=string.Join(" · ",Aktuell(k,v.Art));
  if(e.Aktion!="Ignorieren"){
   if(v.Art=="Ansprechpartner"){
    if(!kontakte.Values.Any(p=>p.Art=="Person"&&p.FirmaId==k.Id&&p.Firma==v.Neu&&!p.Archiviert))throw new Arbeitsfehler("Bitte den Ansprechpartner zuerst unter Kontakte mit Vorname, Nachname und Telefonnummer anlegen. Der Vorschlag bleibt zur Prüfung erhalten.");
   }else if(v.Art=="Anschrift"){
    if(e.Aktion=="Übernehmen")k=k with {Anschrift=v.Neu,Strasse="",Hausnummer="",Plz="",Ort="",Land=""};else werte.Add(new(Guid.NewGuid().ToString("N"),"Weitere Anschrift",v.Neu,true));
   }else{
    if(e.Aktion=="Übernehmen")werte=werte.Select(w=>w.Art==v.Art?w with{Primaer=false}:w).ToList();
    if(!werte.Any(w=>w.Aktiv&&w.Art==v.Art&&w.Wert==v.Neu))werte.Add(new(Guid.NewGuid().ToString("N"),v.Art,v.Neu,true,e.Aktion=="Übernehmen"||!werte.Any(w=>w.Aktiv&&w.Art==v.Art)));
   }
  }
  var zeit=DateTimeOffset.UtcNow;kontakte[k.Id]=k with{Version=k.Version+1,GeaendertAm=zeit,Werte=werte.AsReadOnly(),Historie=Array.AsReadOnly(k.Historie.Append(new(e.Aktion,vorher,v.Neu,v.DokumentId,Arbeitskontext.Benutzer,zeit)).ToArray())};vorschlaege[id]=v with{Status=e.Aktion};
  foreach(var o in vorschlaege.Values.Where(o=>o.FirmaId==k.Id&&o.Status=="Zur Prüfung").ToArray())vorschlaege[o.Id]=o with{Kontaktversion=k.Version+1,Bisher=string.Join(" · ",Aktuell(kontakte[k.Id],o.Art)),Firma=k.Firma};
  return kontakte[k.Id];
 }
 internal Kontaktansicht? Ansicht(string nameOderId){
  var k=kontakte.GetValueOrDefault(nameOderId)??kontakte.Values.SingleOrDefault(k=>k.Firma==nameOderId);if(k is null)return null;
  var mails=Aktuell(k,"E-Mail");var tel=Aktuell(k,"Telefon");var personen=k.Art=="Firma"?Aktuell(k,"Ansprechpartner"):[k.Firma];
  if(mails.Length+tel.Length+personen.Length==0)return null;
  return new(personen.FirstOrDefault(),mails.FirstOrDefault(),tel.FirstOrDefault(),"Kontaktakte",mails,tel,personen);
 }
}
