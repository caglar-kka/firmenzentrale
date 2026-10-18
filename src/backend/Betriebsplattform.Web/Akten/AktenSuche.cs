using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Betriebsplattform.Web.Akten;

// Suche ist bewusst großzügiger als die unabhängige Dublettenentscheidung.
public static class Suchvergleich
{
 public static string[] Woerter(string text)=>Regex.Matches(text.ToLowerInvariant().Replace("ä","a").Replace("ö","o").Replace("ü","u").Replace("ß","ss").Replace("ae","a").Replace("oe","o").Replace("ue","u").Normalize(NormalizationForm.FormD),@"[\p{L}\p{N}]+").Select(m=>m.Value).ToArray();
 public static bool Passt(string? frage,string text){
  if((frage?.Length??0)>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");
  var q=Woerter(frage??"");if(q.Length==0)return string.IsNullOrWhiteSpace(frage);
  var w=Woerter(text);return q.All(t=>w.Any(s=>s.Contains(t,StringComparison.Ordinal)||t.Length>=4&&s.Length>=4&&EinFehler(t,s)));
 }
 private static bool EinFehler(string a,string b){
  if(Math.Abs(a.Length-b.Length)>1)return false;int i=0,j=0,fehler=0;
  while(i<a.Length&&j<b.Length){if(a[i]==b[j]){i++;j++;continue;}if(++fehler>1)return false;
   if(a.Length==b.Length){if(i+1<a.Length&&a[i]==b[j+1]&&a[i+1]==b[j]){i+=2;j+=2;}else{i++;j++;}}
   else if(a.Length>b.Length)i++;else j++;
  }return fehler+(a.Length-i)+(b.Length-j)<=1;
 }
}
public sealed class AktenSuche(Aktenbestand akten,Func<Uebersicht> rechnungen,Arbeitsmodule module,ILesezugriff zugriff)
{
 private bool Sichtbar(Vorgang v)=>zugriff.Erlaubt("Vorgang",v.Id)&&(v.Rechnungsnummer is null||module.Aktiv("Finanzen"))&&(v.FirmaId is null||zugriff.Erlaubt("Firma",v.FirmaId));
 private bool BelegSichtbar(Rechnungsansicht r)=>zugriff.Erlaubt("Rechnung",r.VorgangId??r.Nummer)&&(r.VorgangId is null||zugriff.Erlaubt("Vorgang",r.VorgangId))&&(r.AusstellerId is null||zugriff.Erlaubt("Firma",r.AusstellerId))&&(r.EmpfaengerId is null||zugriff.Erlaubt("Firma",r.EmpfaengerId));
 public IReadOnlyList<Vorgang> FindeAkten(string? text){
  if((text?.Length??0)>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");
  var docs=module.Aktiv("Dokumente")?akten.Eingang(true).Where(d=>zugriff.Erlaubt("Dokument",d.Id)&&(d.AbsenderId is null||zugriff.Erlaubt("Firma",d.AbsenderId))).ToArray():[];
  var dokumente=docs.SelectMany(d=>d.Vorgaenge.Select(id=>(id,d))).ToLookup(x=>x.id,x=>x.d);
  var sichtbare=akten.Vorgaenge().Where(Sichtbar).Select(v=>v with{Dokumentanzahl=dokumente[v.Id].Count()});
  if(string.IsNullOrWhiteSpace(text))return sichtbare.ToArray();
  var kontakte=akten.Kontakte().Where(k=>zugriff.Erlaubt("Firma",k.Id)).ToLookup(k=>k.Art=="Person"?k.FirmaId:k.Id);
  var belege=(module.Aktiv("Finanzen")?rechnungen().Rechnungen.Where(BelegSichtbar):[]).ToLookup(r=>r.VorgangId);
  return sichtbare.Where(v=>Suchvergleich.Passt(text,"Akte "+v.Titel+" "+v.Kategorie+" "+v.Firma+" "+v.Rechnungsnummer+" "+string.Join(" ",dokumente[v.Id].Select(d=>d.Titel+" "+d.Kategorie+" "+d.Absender))+" "+string.Join(" ",kontakte[v.FirmaId].Select(k=>k.Firma))+" "+string.Join(" ",belege[v.Id].Select(r=>"Rechnung "+r.Nummer+" "+r.Aussteller+" "+r.Empfaenger)))).ToArray();
 }
 public IReadOnlyList<Suchtreffer> Finden(string? eingabe){
  var text=eingabe?.Trim()??"";if(text.Length<2)return[];if(text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");if(Suchvergleich.Woerter(text).Length==0)return[];
  var kandidaten=new List<(Suchtreffer Treffer,string Text)>();var firmen=akten.Kontakte().Where(k=>zugriff.Erlaubt("Firma",k.Id)).ToDictionary(k=>k.Id);
  var vorgaenge=akten.Vorgaenge().Where(Sichtbar).ToDictionary(v=>v.Id);
  string Partner(string? id)=>id is not null&&firmen.TryGetValue(id,out var k)?k.Firma+" "+string.Join(" ",firmen.Values.Where(p=>p.FirmaId==id&&!p.Archiviert).Select(p=>p.Firma)):"";
  if(module.Aktiv("Finanzen"))foreach(var r in rechnungen().Rechnungen.Where(BelegSichtbar)){
   var t=new Suchtreffer(r.VorgangId??r.Nummer,"Rechnung",r.Nummer,r.Aussteller+" → "+r.Empfaenger,"rechnung",r.VorgangId);kandidaten.Add((t,"Rechnung "+r.Nummer+" "+r.Aussteller+" "+r.Empfaenger+" "+Partner(r.AusstellerId)+" "+Partner(r.EmpfaengerId)));
  }
  if(module.Aktiv("Akten"))foreach(var v in FindeAkten(text)){var t=new Suchtreffer(v.Id,"Akte",v.Titel,v.Kategorie+" · "+v.Firma,"vorgang",v.Id);kandidaten.Add((t,text));}
  if(module.Aktiv("Dokumente"))foreach(var d in akten.Eingang(true).Where(d=>zugriff.Erlaubt("Dokument",d.Id)&&(d.AbsenderId is null||zugriff.Erlaubt("Firma",d.AbsenderId)))){
   var corpus="Dokument "+d.Titel+" "+d.Dateiname+" "+d.Kategorie+" "+d.Absender;
   if(d.Vorgaenge.Count==0)kandidaten.Add((new(d.Id,"Dokument",d.Titel,"Posteingang · "+d.Kategorie,"eingang"),corpus));
   else if(module.Aktiv("Akten"))foreach(var id in d.Vorgaenge.Where(vorgaenge.ContainsKey))kandidaten.Add((new(d.Id,"Dokument",d.Titel,vorgaenge[id].Titel,"dokument",id),corpus+" "+vorgaenge[id].Titel+" "+Partner(vorgaenge[id].FirmaId)));
  }
  foreach(var k in firmen.Values.Where(k=>!k.Archiviert)){var t=new Suchtreffer(k.Id,k.Art=="Person"?"Person":"Firma",k.Firma,k.Art=="Person"?Partner(k.FirmaId):"Kontaktakte","kontakt");kandidaten.Add((t,t.Art+" Kontakt "+k.Firma+" "+k.Anschrift+" "+t.Zusatz+" "+k.Rolle+" "+string.Join(" ",k.Werte.Where(w=>w.Aktiv).Select(w=>w.Wert))));}
  return kandidaten.Where(k=>Suchvergleich.Passt(text,k.Text)).OrderByDescending(k=>Suchvergleich.Woerter(text).Contains(Suchvergleich.Woerter(k.Treffer.Art).FirstOrDefault())).ThenByDescending(k=>Suchvergleich.Woerter(k.Treffer.Titel).Any(w=>w==Suchvergleich.Woerter(text).FirstOrDefault())).ThenBy(k=>k.Treffer.Art).ThenBy(k=>k.Treffer.Titel,StringComparer.OrdinalIgnoreCase).Select(k=>k.Treffer).DistinctBy(t=>(t.Art,t.Id,t.VorgangId)).Take(10).ToArray();
 }
}
