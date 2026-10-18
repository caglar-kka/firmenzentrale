using Betriebsplattform.Web.Akten;
using Xunit;
using Betriebsplattform.Web;
using System.Net;
using System.Net.Http.Json;
using System.Text;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class ProduktreifeTests
{
 [Fact] public void KategorienTrennenStatusUndErhaltenReferenzBeiUmbenennung(){
  var k=new Kategorienverwaltung();var neu=k.Speichern(null,new("Objektpflege","Eigene Rubrik",null,15,0));
  Assert.Equal(neu.Id,k.Speichern(neu.Id,new("Objektservice","Beschreibung",null,20,1)).Id);
  Assert.Throws<Arbeitsfehler>(()=>k.Speichern(null,new("Bezahlt","",null,1,0)));
  var sub=k.Speichern(null,new("Glasreinigung","",neu.Id,1,0));
  Assert.Throws<Arbeitsfehler>(()=>k.Speichern(null,new("Tiefer","",sub.Id,1,0)));
  Assert.Throws<Arbeitsfehler>(()=>k.Archivieren(neu.Id,2));
  Assert.True(k.Archivieren(sub.Id,1).Archiviert);
 }
 [Fact] public void KontakteHabenMehrereHauptwerteVersionHistorieUndArchiv(){
  var b=new Aktenbestand();var k=b.KontaktSpeichern(null,new("Kieselwelle Service GmbH","Firma",null,"Beispielweg 8","","",[new("Telefon","+49 30 23125 081",true),new("Telefon","+49 30 23125 082",false),new("E-Mail","post@kieselwelle.invalid",true)],0));
  Assert.Equal(3,k.Werte.Count);Assert.Single(k.Werte,w=>w.Art=="Telefon"&&w.Primaer);
  var p=b.KontaktSpeichern(null,new("Mara Kiesfenn","Person",k.Id,"","Einkauf","Ansprechpartnerin",[new("Telefon","+49 30 23125 083",true)],0,Vorname:"Mara",Nachname:"Kiesfenn"));Assert.Equal(k.Id,p.FirmaId);
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(k.Id,new("Geändert","Firma",null,"","","",[],0)));
  var a=b.KontaktArchivieren(k.Id,k.Version);Assert.True(a.Archiviert);Assert.NotEmpty(a.Historie);
 }

 [Fact] public void KontaktBearbeitungErhaeltVorwertePrimaerwahlUndRechnungsreferenzen(){
  var b=new Aktenbestand();var alt=b.Kontakte().Single(k=>k.Id=="DEMO-NORD");
  var neu=b.KontaktSpeichern(alt.Id,new("Fjordfalter Hauspflege GmbH","Firma",null,"Fennbogen 9 · 00000 Musterhafen","","",[new("Telefon","+49 30 23125 011",true),new("Telefon","+49 30 23125 012",false),new("E-Mail","post@fjordfalter.invalid",true),new("E-Mail","service@fjordfalter.invalid",false)],alt.Version));
  Assert.Equal(2,neu.Werte.Count(w=>w.Art=="Telefon"&&w.Aktiv));Assert.Single(neu.Werte,w=>w.Art=="Telefon"&&w.Aktiv&&w.Primaer);
  Assert.Contains(neu.Werte,w=>!w.Aktiv&&w.Wert==alt.Werte[0].Wert);Assert.Contains(alt.Firma,neu.Historie.Single().Vorher);
  var u=b.KontakteAnwenden(DemoDaten.UebersichtErstellen());Assert.Equal(neu.Firma,u.Rechnungen[0].Aussteller);Assert.Equal(neu.Firma,u.Rechnungen[1].Empfaenger);Assert.Equal("910,00 €",u.OffenerBetrag);
  var fremd=DemoDaten.UebersichtErstellen([new Betriebsplattform.Rechnungen.Rechnung(Guid.NewGuid(),"RE-2026-1088","DEMO-VERS","DEMO-KASSE",new(2026,10,18),100m,new("Beleg","Quelle"))]);Assert.Null(fremd.OffeneForderungen);Assert.Null(fremd.OffeneVerbindlichkeiten);
  var a=b.KontaktArchivieren(neu.Id,neu.Version);Assert.Equal(a.Firma,b.KontakteAnwenden(DemoDaten.UebersichtErstellen()).Rechnungen[0].Aussteller);
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(a.Id,new("Nein","Firma",null,"","","",[],a.Version)));
 }
 [Fact] public void KontaktDuplikateUndMehrereHauptwerteBrauchenBewussteKorrektur(){
  var b=new Aktenbestand();var e=new Kontakteingabe("Neue Firma","Firma",null,"","","",[new("Telefon","+49 30 23125 001",true),new("E-Mail","post@neuefirma.invalid",true)]);
  Assert.Equal(409,Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e)).Status);
  Assert.NotNull(b.KontaktSpeichern(null,e with{DublettenBestaetigt=true}));
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Name="Andere",Werte=[new("Telefon","+49 30 23125 021",true),new("Telefon","+49 30 23125 022",true)]}));
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Werte=[new("E-Mail","javascript:alert(1)",true)]}));
 }
 [Fact] public void OffenerKontaktvorschlagBleibtNachBearbeitungEntscheidbarUndErhaeltZusatzanschrift(){
  var b=new Aktenbestand();var d=b.Hinzufuegen(Encoding.UTF8.GetBytes("Telefon: +49 40 66969 139\nAnschrift: Fennsteg 7"),"Schreiben.txt","text/plain","versicherung-2026");
  var v=b.Lesen("versicherung-2026").Kontaktvorschlaege.Single(v=>v.Art=="Anschrift");
  var k=b.KontaktEntscheiden(v.Id,new("Zusätzlich speichern",v.Kontaktversion));
  k=b.KontaktSpeichern(k.Id,new(k.Firma,"Firma",null,k.Anschrift,"","",k.Werte.Where(w=>w.Aktiv&&w.Art is "Telefon" or "E-Mail").Select(w=>new Kontaktwerteingabe(w.Art,w.Wert,w.Primaer)).ToArray(),k.Version));
  Assert.Contains(k.Werte,w=>w.Art=="Weitere Anschrift"&&w.Aktiv);
  v=b.Lesen("versicherung-2026").Kontaktvorschlaege.Single(v=>v.Art=="Telefon");Assert.Equal(k.Version,v.Kontaktversion);
  k=b.KontaktEntscheiden(v.Id,new("Übernehmen",v.Kontaktversion));Assert.Contains(k.Werte,w=>w.Wert=="+49 40 66969 124"&&w.Aktiv&&!w.Primaer);Assert.Contains(k.Werte,w=>w.Wert==v.Neu&&w.Primaer);Assert.Equal(d.Id,k.Historie.Last().DokumentId);
 }
 [Theory][InlineData("Brief")][InlineData("E-Mail")][InlineData("Antwortschreiben")][InlineData("Anfrage")][InlineData("Erinnerung")][InlineData("Gesprächsvermerk")][InlineData("Sonstige Korrespondenz")]
 public void KorrespondenzVerweistAufBeliebigNeueDokumenteOhneKopie(string art){
  var b=new Aktenbestand();var d=b.Hinzufuegen(Encoding.UTF8.GetBytes("Anlage A"),"Anlage-A.txt","text/plain","versicherung-2026");var z=b.Hinzufuegen(Encoding.UTF8.GetBytes("Anlage B"),"Anlage-B.txt","text/plain","versicherung-2026");
  Assert.Empty(b.KorrespondenzSpeichern("versicherung-2026",new("Ohne Anhang","Inhalt",art,Richtung:"Empfangen",KontaktId:"DEMO-VERS",KommuniziertAm:DateTimeOffset.UtcNow)).DokumentIds!);
  var p=b.KorrespondenzSpeichern("versicherung-2026",new("Mehrere Anlagen","Inhalt",art,DokumentIds:[d.Id,z.Id,d.Id],Richtung:"Empfangen",KontaktId:"DEMO-VERS",KommuniziertAm:DateTimeOffset.UtcNow));Assert.Equal(2,p.DokumentIds!.Count);Assert.Equal(2,b.Lesen("versicherung-2026").Dokumente.Count);
  Assert.Throws<Arbeitsfehler>(()=>b.KorrespondenzSpeichern("demo-nord-rechnung-002",new("Fremde Akte","Inhalt",art,DokumentIds:[d.Id])));
 }
 [Fact] public void KategorieUmbenennungErhaeltVorgangsbezugUndArchivierteKategorieBleibtLesbar(){
  var b=new Aktenbestand(beispielszenario:true);var alt=b.Kategorien.Finden("versicherung");
  b.Kategorien.Speichern(alt.Id,new("Versicherungsunterlagen","Neue Bezeichnung",null,alt.Reihenfolge,alt.Version));Assert.Equal("Versicherungsunterlagen",b.Lesen("versicherung-2026").Vorgang.Kategorie);
  var sub=b.Kategorien.Finden("betriebshaftpflicht");b.Kategorien.Archivieren(sub.Id,sub.Version);Assert.True(b.Kategorien.Finden(sub.Id,false).Archiviert);Assert.Throws<Arbeitsfehler>(()=>b.Kategorien.Finden(sub.Id));
  Assert.Throws<Arbeitsfehler>(()=>b.Kategorien.Archivieren("ungeklaert",1));
 }
 sealed class KeineVerwaltung:IArbeitsrechte{public bool Darf(string aktion)=>false;}
 [Fact] public async Task VerwaltungWirdServerseitigGeschuetztUndDiagnoseIstStandardmaessigVerborgen(){
  await using var app=ApiAnwendung.Erstellen([],0,Path.GetTempPath(),arbeitsrechte:new KeineVerwaltung(),umgebung:new(false,false));await app.StartAsync();
  try{using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};c.DefaultRequestHeaders.Add("Origin",c.BaseAddress.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");
   var u=await c.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/arbeitsumgebung");Assert.False(u.GetProperty("beispielkennzeichnung").GetBoolean());Assert.False(u.GetProperty("kategorienVerwalten").GetBoolean());
   Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/akten",new Vorgangseingabe("Titel","vertrag","DEMO-NORD"))).StatusCode);
   Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/kategorien",new Kategorieeingabe("Test","",null,1,0))).StatusCode);
   Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/kontakte",new Kontakteingabe("Firma","Firma",null,"","","",[]))).StatusCode);
   Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/dokumente/original-002-v1/technik")).StatusCode);
  }finally{await app.StopAsync();}
 }
 [Fact] public async Task DiagnoseBleibtImExplizitenModusTestbar(){
  await using var app=ApiAnwendung.Erstellen([],0,Path.GetTempPath(),umgebung:new(Diagnose:true));await app.StartAsync();try{using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};var d=await c.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/dokumente/original-002-v1/technik");Assert.Equal(64,d.GetProperty("dateiHash").GetString()!.Length);}finally{await app.StopAsync();}
 }
 [Fact] public void BeispielszenarioHatZentraleKontakteMehrereFormateUndKeineFinanziellenNebenwirkungen(){
  var b=new Aktenbestand(beispielszenario:true);var a=b.Lesen("versicherung-2026");Assert.Null(b.Lesen("fahrzeug-versicherung").Vorgang.LetztesEreignis);Assert.Equal(4,a.Dokumente.Count);Assert.Equal(2,a.Korrespondenz.Single().DokumentIds!.Count);Assert.NotEmpty(a.Kontaktvorschlaege);Assert.Contains(a.Dokumente,d=>d.Vorschau=="pdf");Assert.Contains(a.Dokumente,d=>d.Vorschau=="bild");Assert.Contains(a.Dokumente,d=>d.Dateiname.EndsWith(".docx"));
  Assert.All(b.Kontakte().SelectMany(k=>k.Werte).Where(w=>w.Art=="E-Mail"),w=>Assert.EndsWith(".invalid",w.Wert));Assert.All(DemoDaten.UebersichtErstellen().Rechnungen,r=>Assert.DoesNotContain("SYNTH",r.Nummer));Assert.Equal("910,00 €",b.KontakteAnwenden(DemoDaten.UebersichtErstellen()).OffenerBetrag);
 }

 [Fact] public void UxDeltaVorgangsstatusIstUnabhaengigVonZahlungUndEingang(){
  var b=new Aktenbestand();var v=b.VorgangAnlegen(new("Vertragsrückfrage","vertrag","DEMO-NORD"));Assert.Equal("Offen",v.Status);Assert.Empty(b.Lesen(v.Id).Dokumente);Assert.NotNull(v.LetztesEreignis);
  foreach(var status in Vorgangsbearbeitung.Werte){v=b.BearbeitungsstatusSetzen(v.Id,new(status,v.Version));Assert.Equal(status,b.Lesen(v.Id).Vorgang.Status);}
  Assert.Throws<Arbeitsfehler>(()=>b.BearbeitungsstatusSetzen(v.Id,new("Bezahlt",v.Version)));Assert.Throws<Arbeitsfehler>(()=>b.BearbeitungsstatusSetzen(v.Id,new("Zur Prüfung",v.Version)));Assert.Throws<Arbeitsfehler>(()=>b.BearbeitungsstatusSetzen(v.Id,new("Neu",1)));
  Assert.Throws<Arbeitsfehler>(()=>b.VorgangAnlegen(new("","vertrag","DEMO-NORD")));Assert.Throws<Arbeitsfehler>(()=>b.VorgangAnlegen(new("Titel","unbekannt","DEMO-NORD")));Assert.Throws<Arbeitsfehler>(()=>b.VorgangAnlegen(new("Titel","vertrag","unbekannt")));
  Assert.Equal("910,00 €",b.KontakteAnwenden(DemoDaten.UebersichtErstellen()).OffenerBetrag);
 }
 [Fact] public void UxDeltaReihenfolgeBewegtNurGeschwisterUndIstVersioniert(){
  var b=new Kategorienverwaltung();var k=b.Finden("versicherung");b.Verschieben(k.Id,new("hoch",k.Version));Assert.Equal("versicherung",b.Liste()[0].Id);Assert.Equal("kfz-versicherung",b.Liste()[1].Id);
  Assert.Throws<Arbeitsfehler>(()=>b.Verschieben(k.Id,new("runter",k.Version)));var sub=b.Finden("betriebshaftpflicht");b.Verschieben(sub.Id,new("hoch",sub.Version));Assert.Equal("betriebshaftpflicht",b.Liste()[1].Id);Assert.Equal("versicherung",b.Liste()[0].Id);
  Assert.Throws<Arbeitsfehler>(()=>b.Verschieben("rechnung",new("falsch",b.Finden("rechnung").Version)));
 }
 [Fact] public void UxDeltaForderungenVerbindlichkeitenUndHerkunftBleibenGetrennt(){
  var u=DemoDaten.UebersichtErstellen();Assert.Equal("450,00 €",u.OffeneForderungen);Assert.Equal("460,00 €",u.OffeneVerbindlichkeiten);Assert.Equal("910,00 €",u.OffenerBetrag);
  var fremd=DemoDaten.UebersichtErstellen([new Betriebsplattform.Rechnungen.Rechnung(Guid.NewGuid(),"RE-2026-1088","DEMO-VERS","DEMO-KASSE",new(2026,10,18),100m,new("Beleg","Quelle"))]);Assert.Null(fremd.OffeneForderungen);Assert.Null(fremd.OffeneVerbindlichkeiten);
  var b=new Aktenbestand(beispielszenario:true);var d=b.Hinzufuegen(Encoding.UTF8.GetBytes("Eigenes Beispiel"),"Brief.txt","text/plain",null,"Drag & Drop");Assert.Equal("Upload",d.Kanal);Assert.Equal("Drag & Drop",b.Technik(d.Id).Quelle);Assert.Contains(b.Eingang(),d=>d.Titel=="Rückfrage zur Gewerbeauskunft");Assert.All(b.Vorgaenge(),v=>Assert.Contains(v.Status,Vorgangsbearbeitung.Werte));Assert.All(b.Lesen("versicherung-2026").Kontaktvorschlaege,v=>Assert.Equal("Neue Erreichbarkeit im Vertragsservice",v.Quelle));
 }

 [Fact] public async Task UxDeltaHttpVorgangUndSortierungVerwendenBestehendeSchreibgrenzen(){
  await using var app=ApiAnwendung.Erstellen([],0,Path.GetTempPath());await app.StartAsync();try{using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};
   Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/akten",new Vorgangseingabe("Titel","vertrag","DEMO-NORD"))).StatusCode);
   c.DefaultRequestHeaders.Add("Origin",c.BaseAddress.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");
   var response=await c.PostAsJsonAsync("/api/akten",new Vorgangseingabe("Neue Vertragsakte","vertrag","DEMO-NORD"));response.EnsureSuccessStatusCode();var v=(await response.Content.ReadFromJsonAsync<Vorgang>())!;Assert.Equal("Offen",v.Status);
   (await c.PostAsJsonAsync($"/api/akten/{v.Id}/bearbeitungsstatus",new Bearbeitungsentscheidung("Erledigt",v.Version))).EnsureSuccessStatusCode();Assert.Equal("Erledigt",(await c.GetFromJsonAsync<Aktenansicht>($"/api/akten/{v.Id}"))!.Vorgang.Status);
   Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync($"/api/akten/{v.Id}/bearbeitungsstatus",new Bearbeitungsentscheidung("Bezahlt",2))).StatusCode);
   (await c.PostAsJsonAsync("/api/kategorien/versicherung/verschieben",new Kategorieverschiebung("hoch",1))).EnsureSuccessStatusCode();Assert.Equal("versicherung",(await c.GetFromJsonAsync<Kategorie[]>("/api/kategorien"))![0].Id);
   Assert.Equal("910,00 €",(await c.GetFromJsonAsync<Uebersicht>("/api/uebersicht"))!.OffenerBetrag);
  }finally{await app.StopAsync();}
 }

 [Theory]
 [InlineData("Müller Service GmbH","Mueller Service")]
 [InlineData("KIESELWELLE GmbH","kieselwelle")]
 [InlineData("Anna Meier","Anna Maier")]
 public void DatenqualitaetAehnlichkeitIstNurHinweis(string a,string z){Assert.True(Datenvergleich.NameAehnlich(a,z));}
 [Fact] public void DatenqualitaetNormalisierungUndBewussteNeuanlage(){
  Assert.Equal(Datenvergleich.Wert("Telefon","040 123456","Deutschland"),Datenvergleich.Wert("Telefon","+49 40 123456","Deutschland"));
  Assert.Equal(Datenvergleich.Wert("E-Mail","INFO@FENN.invalid"),Datenvergleich.Wert("E-Mail","info@fenn.invalid"));
  var b=new Aktenbestand();var e=new Kontakteingabe("Maya Fennmeier","Person",null,"","","",[new("E-Mail","eins@fenn.invalid",true),new("Telefon","+49 30 23125 085",true)],Vorname:"Maya",Nachname:"Fennmeier");
  var k=b.KontaktSpeichern(null,e);var x=e with{Werte=[new("E-Mail","zwei@fenn.invalid",true),new("Telefon","+49 30 23125 086",true)]};
  var warn=Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,x));Assert.Contains(warn.Hinweise,h=>h.Id==k.Id);
  var neu=b.KontaktSpeichern(null,x with{DublettenBestaetigt=true});Assert.NotEqual(k.Id,neu.Id);Assert.Equal("eins@fenn.invalid",b.Kontakte().Single(c=>c.Id==k.Id).Werte.Single(w=>w.Aktiv&&w.Art=="E-Mail").Wert);
 }
 [Fact] public void DatenqualitaetKontaktFelderWebsiteUndTelefonart(){
  var b=new Aktenbestand();var e=new Kontakteingabe("Fennbogen Werkservice","Firma",null,"","","",[new("Telefon","040 123456",true,"Zentrale"),new("Website","https://fennbogen.invalid",true),new("E-Mail","post@fennbogen.invalid",true)],Strasse:"Fiktivbogen 3",Plz:"00000",Ort:"Beispielhafen",Land:"Deutschland");
  var k=b.KontaktSpeichern(null,e);Assert.Contains("Beispielhafen",k.Anschrift);Assert.Contains(k.Werte,w=>w.Typ=="Zentrale");Assert.Contains(k.Werte,w=>w.Art=="Website");
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Werte=[new("Website","javascript:alert(1)",true)]}));
 }
 [Fact] public void DatenqualitaetAkteWarntOhneZusammenfuehrung(){
  var b=new Aktenbestand();var e=new Vorgangseingabe("Betriebshaftpflicht 2026","versicherung","DEMO-VERS");
  Assert.Equal(409,Assert.Throws<Arbeitsfehler>(()=>b.VorgangAnlegen(e)).Status);
  var neu=b.VorgangAnlegen(e with{DublettenBestaetigt=true});Assert.NotEqual("versicherung-2026",neu.Id);
 }
 [Fact] public void DatenqualitaetDateigrenzeUndCsv(){
  Assert.Equal(20_000_000,Dokumentpruefung.MaxBytes);var p=new Dokumentpruefung();
  var csv=p.Pruefen(Encoding.UTF8.GetBytes("Name;Wert\nBeispiel;=1+1"),"Brief.csv","text/csv");Assert.Equal("download",csv.Vorschau);Assert.Equal("text/csv",csv.Inhaltstyp);
  Assert.Throws<Arbeitsfehler>(()=>p.Pruefen(new byte[20_000_001],"gross.pdf","application/pdf"));
  Assert.Throws<Arbeitsfehler>(()=>p.Pruefen([1,2,3],"Makro.xlsm","application/octet-stream"));
 }

 [Theory]
 [InlineData("Fennbogen Gebäudeservice GmbH","FENnBogen Gebäudeservice")]
 [InlineData("Fennbogen Gebäudeservice GmbH","Fennbogen Gebaeudeservice GmbH")]
 public void DatenqualitaetFirmenvergleichBewahrtOriginal(string original,string variante){
  var b=new Aktenbestand();var e=new Kontakteingabe(original,"Firma",null,"Fiktivbogen 3","","",[new("E-Mail","INFO@FENNFELD.invalid",true),new("Telefon","+49 30 23125 087",true)]);
  var alt=b.KontaktSpeichern(null,e);var warn=Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Name=variante}));Assert.Contains(warn.Hinweise,h=>h.Id==alt.Id);
  Assert.Equal(original,b.Kontakte().Single(k=>k.Id==alt.Id).Firma);
 }
 [Fact] public void DatenqualitaetIdentischeDateiBehaeltOriginalUndEingangsnachweis(){
  var b=new Aktenbestand();var bytes=Encoding.UTF8.GetBytes("Derselbe frei erfundene Brief");var a=b.Hinzufuegen(bytes,"Brief.txt","text/plain",null,"Scan-Ordner");var z=b.Hinzufuegen(bytes,"Andere_Benennung.txt","text/plain",null,"Dateiauswahl");
  Assert.Equal(a.Id,z.Id);Assert.Equal("Upload",z.Kanal);Assert.NotNull(z.LetzterEingangAm);Assert.Single(b.Eingang());Assert.Equal(bytes,b.Bytes(a.Id));Assert.Contains(z.Aktivitaeten!,x=>x.Text.Contains("Identische Datei")&&x.Text.Contains("Upload"));
 }
 [Fact] public void DatenqualitaetKategorieDndIstAtomarUndVersioniert(){
  var k=new Kategorienverwaltung();var a=k.Finden("rechnung");var z=k.Finden("vertrag");k.Verschieben(a.Id,new("position",a.Version,z.Id,z.Version));var neu=k.Liste().Where(x=>x.ElternId is null).ToArray();Assert.True(Array.FindIndex(neu,x=>x.Id==a.Id)>Array.FindIndex(neu,x=>x.Id=="versicherung"));
  Assert.Throws<Arbeitsfehler>(()=>k.Verschieben(a.Id,new("position",a.Version,"versicherung",1)));
 }
 [Fact] public async Task DatenqualitaetHttpUploadGrenzeUndImportVorschlag(){
  await using var app=ApiAnwendung.Erstellen([],0,Path.GetTempPath());await app.StartAsync();try{
   using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};c.DefaultRequestHeaders.Add("Origin",app.Urls.Single());c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");c.DefaultRequestHeaders.Add("X-Dateiname","Gross.pdf");c.DefaultRequestHeaders.Add("X-Dateityp","application/pdf");
   var bytes=Encoding.ASCII.GetBytes("%PDF-1.4\n/Type /Catalog\n"+new string(' ',2_200_000)+"\nstartxref\n0\n%%EOF");using var body=new ByteArrayContent(bytes);body.Headers.ContentType=new("application/octet-stream");Assert.Equal(HttpStatusCode.OK,(await c.PostAsync("/api/dokumenteingang",body)).StatusCode);
   using var gross=new ByteArrayContent(new byte[20_000_001]);gross.Headers.ContentType=new("application/octet-stream");Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await c.PostAsync("/api/dokumenteingang",gross)).StatusCode);
  }finally{await app.StopAsync();}
 }

 [Fact] public void DatenqualitaetSucheNormalisiertOhneBestandZuVeraendern(){
  var b=new Aktenbestand();var k=b.KontaktSpeichern(null,new("Fennmüller Service","Firma",null,"","","",[new("E-Mail","INFO@FENNQUELL.invalid",true),new("Telefon","+49 30 23125 089",true)]));
  var suche=new AktenSuche(b,()=>DemoDaten.UebersichtErstellen(),new(),new LokalerDemozugriff());
  Assert.Contains(suche.Finden("Fennmueller"),t=>t.Id==k.Id);Assert.Contains(suche.Finden("info@fennquell"),t=>t.Id==k.Id);Assert.Empty(suche.Finden("??"));
  Assert.Equal("Fennmüller Service",b.Kontakte().Single(c=>c.Id==k.Id).Firma);
 }

 [Fact] public void DatenqualitaetNeueAkteAuchImEingangNurBewusstTrotzWarnung(){
  var b=new Aktenbestand();var d=b.Hinzufuegen(Encoding.UTF8.GetBytes("Eigener Brief zur Versicherung"),"Brief.txt","text/plain",null);
  var e=new Zuordnung(null,"Betriebshaftpflicht 2026","versicherung","DEMO-VERS",d.Version);
  Assert.Equal(409,Assert.Throws<Arbeitsfehler>(()=>b.Zuordnen(d.Id,e)).Status);Assert.Empty(b.Dokument(d.Id).Vorgaenge);
  var z=b.Zuordnen(d.Id,e with{DublettenBestaetigt=true});Assert.Single(z.Vorgaenge);Assert.NotEqual("versicherung-2026",z.Vorgaenge[0]);
 }

 [Fact] public void StammdatenFirmaUndPersonHabenFachlichePflichtangaben(){
  var b=new Aktenbestand();
  var firma=new Kontakteingabe("Fennfeder Bürodienst","Firma",null,"","","",[]);
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,firma));
  firma=firma with{Werte=[new("Telefon","+49 30 23125 091",true),new("E-Mail","post@fennfeder.invalid",true)]};
  var f=b.KontaktSpeichern(null,firma with{Strasse="Fiktivbogen",Hausnummer="8a",Plz="00000",Ort="Musterhafen",Land="Deutschland"});
  Assert.Equal("8a",f.Hausnummer);Assert.Contains("Fiktivbogen 8a",f.Anschrift);
  var person=new Kontakteingabe(null,"Person",f.Id,"","","Büro",[new("Telefon","+49 30 23125 092",true)],Vorname:"Maya",Nachname:"Fennmeier");
  var p=b.KontaktSpeichern(null,person);Assert.Equal("Maya",p.Vorname);Assert.Equal("Fennmeier",p.Nachname);Assert.Equal("Maya Fennmeier",p.Firma);
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,person with{Nachname=""}));
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,person with{Werte=[]}));
 }
 [Fact] public void StammdatenStrukturVerstaerktNurHinweisUndErhaeltAltbestand(){
  var b=new Aktenbestand();var alt=b.Kontakte().Single(k=>k.Id=="DEMO-NORD");
  var f=b.KontaktSpeichern(alt.Id,new(alt.Firma,"Firma",null,alt.Anschrift,"","",alt.Werte.Where(w=>w.Aktiv).Select(w=>new Kontaktwerteingabe(w.Art,w.Wert,w.Primaer)).ToArray(),alt.Version));
  Assert.Equal(alt.Anschrift,f.Anschrift);Assert.Equal("",f.Hausnummer);
  var e=new Kontakteingabe(null,"Person",f.Id,"","","Büro",[new("Telefon","+49 30 23125 093",true)],Strasse:"Fiktivsteg",Hausnummer:"3",Plz:"00000",Ort:"Musterhafen",Vorname:"Maya",Nachname:"Meier");
  var p=b.KontaktSpeichern(null,e);var warn=Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Werte=[new("Telefon","+49 30 23125 094",true)]}));
  Assert.Contains(warn.Hinweise,h=>h.Id==p.Id&&h.Gruende.Any(g=>g.Contains("Anschrift")));
  var andere=b.KontaktSpeichern(null,e with{DublettenBestaetigt=true,Hausnummer="9",Werte=[new("Telefon","+49 30 23125 094",true)]});Assert.NotEqual(p.Id,andere.Id);
 }

 [Fact] public void StammdatenDokumentvorschlagErzeugtKeineUnvollstaendigePersonUndKeineFalscheAdresse(){
  var b=new Aktenbestand();var k=b.Kontakte().Single(c=>c.Id=="DEMO-VERS");
  k=b.KontaktSpeichern(k.Id,new(k.Firma,"Firma",null,"","","",k.Werte.Where(w=>w.Aktiv).Select(w=>new Kontaktwerteingabe(w.Art,w.Wert,w.Primaer)).ToArray(),k.Version,Strasse:"Fiktivring",Hausnummer:"7",Plz:"00000",Ort:"Musterhafen",Land:"Deutschland"));
  b.Hinzufuegen(Encoding.UTF8.GetBytes("Ansprechpartner: Maya Fennmeier\nAnschrift: Neuer Fiktivsteg 9, 00000 Beispielhafen"),"Kontaktbrief.txt","text/plain","versicherung-2026");
  var v=b.Lesen("versicherung-2026").Kontaktvorschlaege.Single(v=>v.Art=="Ansprechpartner");
  Assert.Throws<Arbeitsfehler>(()=>b.KontaktEntscheiden(v.Id,new("Übernehmen",v.Kontaktversion)));
  Assert.DoesNotContain(b.Kontakte(),c=>c.Firma=="Maya Fennmeier");
  var a=b.Lesen("versicherung-2026").Kontaktvorschlaege.Single(v=>v.Art=="Anschrift");
  var neu=b.KontaktEntscheiden(a.Id,new("Übernehmen",a.Kontaktversion));
  Assert.Equal(a.Neu,neu.Anschrift);Assert.Equal("",neu.Strasse);Assert.Equal("",neu.Hausnummer);Assert.Equal("",neu.Plz);
  Assert.Contains(neu.Historie,h=>h.Vorher.Contains("Fiktivring 7"));
 }

 [Theory]
 [InlineData("Rechnung Maya","Maya Maier Rechnung",true)]
 [InlineData("Maya Rechnung","Rechnung Maya Maier",true)]
 [InlineData("Maya Mai","Maya Maier",true)]
 [InlineData("Muller","Müller",true)]
 [InlineData("Meirr","Meier",true)]
 [InlineData("xyz","Maya Maier",false)]
 [InlineData("  ","Maya",true)]
 public void MVP015ToleranteWortsuche(string q,string text,bool trifft)=>Assert.Equal(trifft,Suchvergleich.Passt(q,text));
 [Fact] public void MVP015UmzuordnungIstVersioniertOhneKopieMitHistorie(){
  var b=new Aktenbestand();var d=b.Hinzufuegen(Encoding.UTF8.GetBytes("Brief Sohn"),"Brief-Sohn.txt","text/plain",null);
  var a=b.Zuordnen(d.Id,new("versicherung-2026",null,"versicherung",null,d.Version));
  var neu=b.Umordnen(d.Id,new("versicherung-2026","demo-nord-rechnung-002",a.Version));
  Assert.Equal(d.Id,neu.Id);Assert.Single(neu.Vorgaenge);Assert.Equal("demo-nord-rechnung-002",neu.Vorgaenge[0]);
  Assert.Empty(b.Lesen("versicherung-2026").Dokumente);Assert.Contains(neu.Aktivitaeten!,e=>e.VorherigerVorgangId=="versicherung-2026"&&e.VorgangId=="demo-nord-rechnung-002");
  Assert.Throws<Arbeitsfehler>(()=>b.Umordnen(d.Id,new("versicherung-2026","demo-nord-rechnung-002",a.Version)));
  Assert.Throws<Arbeitsfehler>(()=>b.Umordnen(d.Id,new("demo-nord-rechnung-002","unbekannt",neu.Version)));
  Assert.Throws<Arbeitsfehler>(()=>b.Umordnen(d.Id,new("demo-nord-rechnung-002","demo-nord-rechnung-002",neu.Version)));
  var zurueck=b.Umordnen(d.Id,new("demo-nord-rechnung-002","versicherung-2026",neu.Version));Assert.Equal("versicherung-2026",zurueck.Vorgaenge.Single());
  var c=b.VorgangAnlegen(new("Weitere Vertragsakte","versicherung","DEMO-VERS",true));
  var nochmal=b.Umordnen(d.Id,new("versicherung-2026","demo-nord-rechnung-002",zurueck.Version));
  var dritte=b.Umordnen(d.Id,new("demo-nord-rechnung-002",c.Id,nochmal.Version));
  Assert.Equal(c.Id,dritte.Vorgaenge.Single());Assert.Equal(d.Id,dritte.Id);
  Assert.Single(b.Lesen(c.Id).Dokumente);Assert.DoesNotContain(b.Lesen("versicherung-2026").Dokumente,x=>x.Id==d.Id);Assert.DoesNotContain(b.Lesen("demo-nord-rechnung-002").Dokumente,x=>x.Id==d.Id);
  var h=dritte.Aktivitaeten!.Where(x=>x.Text=="Zuordnung geändert").ToArray();Assert.Equal(4,h.Length);Assert.Equal("demo-nord-rechnung-002",h[^1].VorherigerVorgangId);Assert.Equal(c.Id,h[^1].VorgangId);

 }
 [Fact] public void AbnahmedeltaUmzuordnungKannAtomarNeueAkteAnlegen(){
  var b=new Aktenbestand();var d=b.Hinzufuegen(Encoding.UTF8.GetBytes("Neues Thema"),"neu.txt","text/plain","versicherung-2026");
  var anzahl=b.Vorgaenge().Count;
  Assert.Throws<Arbeitsfehler>(()=>b.Umordnen(d.Id,new("versicherung-2026",null,d.Version-1,"Neue Vertragsakte","versicherung","DEMO-VERS")));
  Assert.Throws<Arbeitsfehler>(()=>b.Umordnen(d.Id,new("versicherung-2026",null,d.Version,"  ","versicherung","DEMO-VERS")));
  Assert.Throws<Arbeitsfehler>(()=>b.Umordnen(d.Id,new("versicherung-2026",null,d.Version,"Neue Vertragsakte","versicherung","unbekannt")));
  Assert.Equal(anzahl,b.Vorgaenge().Count);Assert.Equal("versicherung-2026",b.Dokument(d.Id).Vorgaenge.Single());
  var neu=b.Umordnen(d.Id,new("versicherung-2026",null,d.Version,"Neue Vertragsakte","versicherung","DEMO-VERS"));
  Assert.Equal(anzahl+1,b.Vorgaenge().Count);Assert.Equal(d.Id,neu.Id);var ziel=neu.Vorgaenge.Single();Assert.NotEqual("versicherung-2026",ziel);
  Assert.Equal("Neue Vertragsakte",b.Lesen(ziel).Vorgang.Titel);Assert.Single(b.Lesen(ziel).Dokumente);
  Assert.Contains(neu.Aktivitaeten!,a=>a.VorherigerVorgangId=="versicherung-2026"&&a.VorgangId==ziel);
  var alt=b.Umordnen(d.Id,new(ziel,"versicherung-2026",neu.Version));Assert.Equal("versicherung-2026",alt.Vorgaenge.Single());Assert.Empty(b.Lesen(ziel).Dokumente);
 }
 [Fact] public void MVP015AktensucheFindetVerknuepfteInhalteUndRechteBleibenWirksam(){
  var b=new Aktenbestand();b.Hinzufuegen(Encoding.UTF8.GetBytes("Briefinhalt"),"Sohn-Vertrag.txt","text/plain","versicherung-2026");
  var s=new AktenSuche(b,()=>DemoDaten.UebersichtErstellen(),new(),new LokalerDemozugriff());
  Assert.Contains(s.FindeAkten("Sohn"),v=>v.Id=="versicherung-2026");Assert.Contains(s.Finden("Sohn Vertrag"),t=>t.Art=="Dokument");
  Assert.Empty(s.Finden("!@#$"));Assert.Throws<Arbeitsfehler>(()=>s.Finden(new string('x',121)));
 }

 [Theory][InlineData(" ","Meier")][InlineData("Maya"," ")][InlineData("040 123456","Meier")][InlineData("<script>","Meier")]
 public void MVP015PersonenNegativvalidierung(string vor,string nach){var b=new Aktenbestand();Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,new(null,"Person",null,"","","",[new("Telefon","+49 30 23125 099",true)],Vorname:vor,Nachname:nach)));}
 [Theory][InlineData("Mäya-Luise","O’Fenn")][InlineData("Zoë","D'Fenn")]
 public void MVP015ZulaessigeUnicodeNamen(string vor,string nach){var b=new Aktenbestand();var k=b.KontaktSpeichern(null,new(null,"Person",null,"","","",[new("Telefon","+49 30 23125 099",true)],Vorname:vor,Nachname:nach));Assert.Equal(vor,k.Vorname);}
 [Fact] public void MVP015KommunikationVerlangtExterneSemantik(){var b=new Aktenbestand();var e=new Korrespondenzeingabe("Brief","Inhalt","Brief");Assert.Throws<Arbeitsfehler>(()=>b.KorrespondenzSpeichern("versicherung-2026",e));var k=b.KorrespondenzSpeichern("versicherung-2026",e with{Richtung="Empfangen",KontaktId="DEMO-VERS",KommuniziertAm=DateTimeOffset.UtcNow});Assert.Equal("Empfangen",k.Richtung);Assert.Throws<Arbeitsfehler>(()=>b.KorrespondenzSpeichern("versicherung-2026",e with{Richtung="Gesendet",KontaktId="falsch",KommuniziertAm=DateTimeOffset.UtcNow}));}
 sealed class MVP015DokumentVerboten:ILesezugriff{public bool Erlaubt(string art,string id)=>art!="Dokument";}
 [Fact] public void MVP015VerboteneDokumenteLeckenNichtInAktensuche(){var b=new Aktenbestand();b.Hinzufuegen(Encoding.UTF8.GetBytes("X"),"Geheimwort-Sohn.txt","text/plain","versicherung-2026");Assert.Empty(new AktenSuche(b,()=>DemoDaten.UebersichtErstellen(),new(),new MVP015DokumentVerboten()).FindeAkten("Geheimwort"));}
 [Fact] public async Task MVP015HttpSicherheitsgrenzen(){
  await using var app=ApiAnwendung.Erstellen([],0);await app.StartAsync();try{
   using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};
   var pfad="/api/akten/versicherung-2026/notizen";
   Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(pfad,new Notizeingabe("Titel","Inhalt"))).StatusCode);
   c.DefaultRequestHeaders.Add("Origin","https://fremd.invalid");c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(pfad,new Notizeingabe("Titel","Inhalt"))).StatusCode);
   c.DefaultRequestHeaders.Remove("Origin");c.DefaultRequestHeaders.Add("Origin",c.BaseAddress.GetLeftPart(UriPartial.Authority));
   Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(pfad,new{betreff="Titel",inhalt="Inhalt",autor="Fremder Actor"})).StatusCode);
   Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync(pfad,new StringContent("{",Encoding.UTF8,"application/json"))).StatusCode);
   Assert.Equal(HttpStatusCode.UnsupportedMediaType,(await c.PostAsync(pfad,new StringContent("kein json",Encoding.UTF8,"text/plain"))).StatusCode);
   var gross=await c.PostAsync(pfad,new StringContent(new string('x',140000),Encoding.UTF8,"application/json"));Assert.Equal(HttpStatusCode.RequestEntityTooLarge,gross.StatusCode);
   var text=await gross.Content.ReadAsStringAsync();Assert.DoesNotContain("Exception",text);Assert.DoesNotContain("C:\\",text);
   Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/dokumente/unbekannt/text")).StatusCode);
   var seite=await c.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/akten?seite=0&sort=titel");Assert.InRange(seite.GetProperty("eintraege").GetArrayLength(),1,20);
  }finally{await app.StopAsync();}
 }
 [Fact] public async Task MVP015SchreibaktionenVerlangenVerwaltungsrecht(){await using var app=ApiAnwendung.Erstellen([],0,arbeitsrechte:new KeineVerwaltung());await app.StartAsync();try{using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};c.DefaultRequestHeaders.Add("Origin",c.BaseAddress.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/akten/versicherung-2026/notizen",new Notizeingabe("Titel","Inhalt"))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/dokumente/original-002-v1/umordnen",new Umzuordnung("demo-nord-rechnung-002","versicherung-2026",1))).StatusCode);}finally{await app.StopAsync();}}

 [Fact] public void MVP015KategorienNegativeUndAktivitaetszeit(){var k=new Kategorienverwaltung();foreach(var n in new[]{""," ",new string('x',81)})Assert.Throws<Arbeitsfehler>(()=>k.Speichern(null,new(n,"",null,0,0)));var a=k.Speichern(null,new("Objektakte","",null,0,0));Assert.Throws<Arbeitsfehler>(()=>k.Speichern(a.Id,new(a.Name,"",a.Id,0,a.Version)));var sub=k.Speichern(null,new("Glasakte","",a.Id,0,0));Assert.Throws<Arbeitsfehler>(()=>k.Speichern(a.Id,new(a.Name,"",sub.Id,0,a.Version)));k.Archivieren(sub.Id,sub.Version);Assert.Throws<Arbeitsfehler>(()=>k.Finden(sub.Id));
  var b=new Aktenbestand();var d=b.Hinzufuegen(Encoding.UTF8.GetBytes("Zeitprobe"),"Zeit.txt","text/plain",null);var vorher=DateTimeOffset.UtcNow;var z=b.Zuordnen(d.Id,new("versicherung-2026",null,"versicherung",null,d.Version));Assert.InRange(z.GeaendertAm!.Value,vorher,DateTimeOffset.UtcNow);Assert.Equal(TimeSpan.Zero,z.GeaendertAm.Value.Offset);Assert.Equal(d.ErfasstAm,z.ErfasstAm);
 }
 [Fact] public void AbnahmedeltaFirmenzahlenZaehlenMetadatenUndBeachtenRechte(){
  var b=new Aktenbestand();var eins=b.Hinzufuegen(Encoding.UTF8.GetBytes("Erster Brief"),"eins.txt","text/plain","versicherung-2026");
  var zwei=b.Hinzufuegen(Encoding.UTF8.GetBytes("Zweiter Brief"),"zwei.txt","text/plain","versicherung-2026");
  var n=b.NotizSpeichern("versicherung-2026",null,new("Rückfrage","Intern"));
  var z=b.Firmenzahlen("DEMO-VERS",_=>true,_=>true);Assert.Equal(1,z["akten"]);Assert.Equal(2,z["dokumente"]);Assert.Equal(1,z["notizen"]);Assert.Equal(0,z["korrespondenz"]);
  Assert.Equal(1,b.Firmenzahlen("DEMO-VERS",_=>true,id=>id==eins.Id)["dokumente"]);
  Assert.All(b.Firmenzahlen("DEMO-VERS",_=>false,_=>true).Values,v=>Assert.Equal(0,v));
  b.NotizEntfernen("versicherung-2026",n.Id,n.Version);Assert.Equal(0,b.Firmenzahlen("DEMO-VERS",_=>true,_=>true)["notizen"]);
 }
 [Fact] public async Task AbnahmedeltaFirmenzahlenApiEnthaeltNurCountsUndIstKonsistent(){
  await using var app=ApiAnwendung.Erstellen([],0);await app.StartAsync();try{using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};
   var z=await c.GetFromJsonAsync<Dictionary<string,int>>("/api/kontakte/DEMO-VERS/arbeitszahlen");Assert.Equal(5,z!.Count);
   foreach(var key in z.Keys){var liste=await c.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/kontakte/DEMO-VERS/arbeit?bereich="+key+"&seite=0");Assert.Equal(liste.GetProperty("anzahl").GetInt32(),z[key]);}
   Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/kontakte/unbekannt/arbeitszahlen")).StatusCode);
  }finally{await app.StopAsync();}
 }
 [Fact] public async Task MVP015FirmenarbeitIstBegrenztUndZeigtZentraleReferenzen(){await using var app=ApiAnwendung.Erstellen([],0);await app.StartAsync();try{using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};var d=await c.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/kontakte/DEMO-VERS/arbeit?bereich=dokumente&seite=0");Assert.InRange(d.GetProperty("eintraege").GetArrayLength(),1,20);Assert.Equal("dokument",d.GetProperty("eintraege")[0].GetProperty("ziel").GetString());Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/kontakte/DEMO-VERS/arbeit?bereich=unbekannt&seite=0")).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/kontakte/DEMO-VERS/arbeit?bereich=akten&seite=-1")).StatusCode);}finally{await app.StopAsync();}}

 [Fact] public void MVP015BeispielzeitenUndSuchintention(){var b=new Aktenbestand(beispielszenario:true);Assert.All(b.Kontakte(),k=>{Assert.Equal(k.ErstelltAm,k.GeaendertAm);Assert.Equal(2026,k.GeaendertAm.Year);});var s=new AktenSuche(b,()=>DemoDaten.UebersichtErstellen(),new(),new LokalerDemozugriff());Assert.Equal("Rechnung",s.Finden("Rechnung Joris").First().Art);Assert.Contains(s.FindeAkten("Fjordfalter 1048"),v=>v.Id=="demo-nord-rechnung-002");}

 sealed class MVP015FirmaVerboten:ILesezugriff{public bool Erlaubt(string art,string id)=>art!="Firma"||id!="DEMO-VERS";}
 [Fact] public async Task MVP015ZielakteBeachtetAuchFirmenrecht(){await using var app=ApiAnwendung.Erstellen([],0,leserechte:new MVP015FirmaVerboten());await app.StartAsync();try{using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};c.DefaultRequestHeaders.Add("Origin",c.BaseAddress.GetLeftPart(UriPartial.Authority));c.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/akten/versicherung-2026")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/dokumente/original-002-v1/umordnen",new Umzuordnung("demo-nord-rechnung-002","versicherung-2026",1))).StatusCode);}finally{await app.StopAsync();}}

 [Fact] public void MVP015DateienUndKontaktwerteBleibenKonservativ(){var p=new Dokumentpruefung();foreach(var name in new[]{"../Brief.txt","..\\Brief.txt","C:/Brief.txt","Brief.xlsm","Brief.docm","Brief.exe"})Assert.Throws<Arbeitsfehler>(()=>p.Pruefen(Encoding.UTF8.GetBytes("Inhalt"),name,"text/plain"));var b=new Aktenbestand();var e=new Kontakteingabe("Fennfalter Büro","Firma",null,"","","",[new("Telefon","+49 30 23125 099",true),new("E-Mail","buero@fennfalter.invalid",true)]);Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Name=" "}));Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Name=new string('x',161)}));Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Werte=[new("Telefon","keine Telefonnummer",true),new("E-Mail","buero@fennfalter.invalid",true)]}));Assert.Throws<Arbeitsfehler>(()=>b.KontaktSpeichern(null,e with{Werte=[new("Telefon","+49 30 23125 099",true),new("E-Mail","ungueltig@",true)]}));}
}
