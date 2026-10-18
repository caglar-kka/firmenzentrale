using System.Security.Cryptography;

namespace Betriebsplattform.Web.Akten;

// Eine lokale Arbeitsumgebung; Produktbetrieb benötigt persistente Ressourcen-/Mandantenrechte.
public sealed partial class Aktenbestand
{
    private sealed class Datei(Aktendokument ansicht,byte[] bytes,string hash,string quelle,string text,string methode)
    {
        internal Aktendokument Ansicht {get;set;}=ansicht;
        internal byte[] Bytes {get;}=bytes;
        internal string Hash {get;}=hash;
        internal string Quelle {get;}=quelle;
        internal string Text {get;}=text;
        internal string Methode {get;}=methode;
    }
    private readonly object sperre=new();
    private readonly Dictionary<string,Vorgang> akten=[];
    private readonly Dictionary<string,Datei> dateien=[];
    private readonly Dictionary<string,Korrespondenz> post=[];
    private readonly Notizablage notizen=new();
    private readonly Kontaktpflege kontakte=new();
    public Kategorienverwaltung Kategorien {get;}=new();
    public IReadOnlyDictionary<string,int> Firmenzahlen(string firmaId,Func<Vorgang,bool> akteErlaubt,Func<string,bool> dokumentErlaubt)
    {
        lock(sperre){
            var ids=akten.Values.Where(v=>v.FirmaId==firmaId&&akteErlaubt(v)).Select(v=>v.Id).ToHashSet();
            return new Dictionary<string,int>{["akten"]=ids.Count,
                ["dokumente"]=dateien.Values.Where(d=>dokumentErlaubt(d.Ansicht.Id)).Sum(d=>d.Ansicht.Vorgaenge.Count(ids.Contains)),
                ["notizen"]=notizen.Anzahl(ids),["korrespondenz"]=post.Values.Count(k=>ids.Contains(k.VorgangId))};
        }
    }
    private readonly IDocumentTextExtractor pruefung;
    public Aktenbestand(IDocumentTextExtractor? pruefung=null,bool beispielszenario=false)
    {
        this.pruefung=pruefung??new Dokumentpruefung();
        foreach(var r in DemoDaten.UebersichtErstellen().Rechnungen)Registrieren(r);
        akten["versicherung-2026"]=new("versicherung-2026","Betriebshaftpflicht 2026","Versicherung","DEMO-VERS",KategorieId:"versicherung");
        foreach(var d in DemoDokumente.Liste("demo-nord-rechnung-002")!.Dokumente){
            var bytes=DemoDokumente.Datei(d.VorgangId,d.Id)!;var gelesen=this.pruefung.Pruefen(bytes,d.Dateiname,"application/pdf");
            dateien[d.Id]=new(new(d.Id,d.Art,d.Dateiname,d.Art,"Rechnung","Bestehender Beleg",DateTimeOffset.Parse("2026-10-17T12:00:00Z"),1,"application/pdf","pdf","Begrenzter PDF-Text",[d.VorgangId],KategorieId:"rechnung",Status:"Zugeordnet",Pruefbedarf:false),bytes,d.DateiHash,d.Quelle,gelesen.Text,gelesen.Textmethode);
        }
        if(beispielszenario)BeispieleEinrichten();
    }
    public void Registrieren(Rechnungsansicht r)
    {
        if(r.VorgangId is null)return;lock(sperre){
            var partner=r.Perspektive?.Rolle=="Aussteller"?r.Empfaenger:r.Aussteller;
            var firma=r.Perspektive?.Rolle=="Aussteller"?r.EmpfaengerId:r.AusstellerId;firma??=DemoDaten.Namen.FirstOrDefault(p=>p.Value==partner).Key;
            akten.TryAdd(r.VorgangId,new(r.VorgangId,r.Nummer,"Rechnung",firma,r.Nummer,"rechnung"));
        }
    }
    private Vorgang Akte(string id)=>akten.TryGetValue(id,out var v)?Anreichern(v):throw new Arbeitsfehler("Vorgang nicht gefunden.",404);
    private Aktendokument Kopie(Aktendokument d)=>d with {GeaendertAm=d.Aktivitaeten?.LastOrDefault()?.Zeitpunkt??d.ErfasstAm,LetzterEingangAm=d.Aktivitaeten?.LastOrDefault(a=>a.Kanal is not null)?.Zeitpunkt,Kanal=d.Aktivitaeten?.LastOrDefault(a=>a.Kanal is not null)?.Kanal??(d.Kanal is "Dateiauswahl" or "Drag & Drop" or "Bestehender Beleg"?"Upload":d.Kanal),Kategorie=Kategorien.Finden(d.KategorieId??d.Kategorie,false).Name,Absender=kontakte.Finden(d.AbsenderId)?.Firma??d.Absender,Vorgaenge=Array.AsReadOnly(d.Vorgaenge.ToArray())};
    public Vorgang VorgangAnlegen(Vorgangseingabe e)
    {
        lock(sperre){var titel=Arbeitsregeln.Text(e.Titel,"Vorgangstitel",160);var k=Kategorien.Finden(e.Kategorie);
            if(!kontakte.HatFirma(e.FirmaId))throw new Arbeitsfehler("Bitte eine aktive Firma wählen.");
            var hinweise=akten.Values.Where(v=>Datenvergleich.NameAehnlich(v.Titel,titel)).Take(10).Select(v=>new Dublettenhinweis(v.Id,v.Titel,"Akte",Kontaktname(v.FirmaId,"Nicht zugeordnet"),[v.Kategorie],v.FirmaId==e.FirmaId?["Ähnlicher Betreff","Gleiche Firma"]:["Ähnlicher Betreff – ein anderer Geschäftsfall ist möglich"])).ToArray();
            if(hinweise.Length>0&&!e.DublettenBestaetigt)throw new Arbeitsfehler("Mögliche vorhandene Akte gefunden. Bitte vergleichen oder bewusst eine separate Akte anlegen.",409,hinweise);
            if(akten.Count>=1000)throw new Arbeitsfehler("Die lokale Vorgangsgrenze ist erreicht.");
            var v=new Vorgang(Guid.NewGuid().ToString("N"),titel,k.Name,e.FirmaId,KategorieId:k.Id,LetztesEreignis:DateTimeOffset.UtcNow);akten[v.Id]=v;return Anreichern(v);}
    }
    public Vorgang BearbeitungsstatusSetzen(string id,Bearbeitungsentscheidung e)
    {
        lock(sperre){var v=Akte(id);Arbeitsregeln.Version(e.Version,v.Version);if(e.Status is null||!Vorgangsbearbeitung.Werte.Contains(e.Status))throw new Arbeitsfehler("Bitte einen gültigen Bearbeitungsstatus wählen.");
            return akten[id]=v with {Status=e.Status,Version=v.Version+1,LetztesEreignis=DateTimeOffset.UtcNow};}
    }
    public IReadOnlyList<Vorgang> Vorgaenge(){lock(sperre)return akten.Values.Select(Anreichern).ToArray();}
    public Aktenansicht Lesen(string id){lock(sperre){var v=Akte(id);var docs=dateien.Values.Select(d=>Kopie(d.Ansicht)).Where(d=>d.Vorgaenge.Contains(id)).OrderByDescending(d=>d.ErfasstAm).ToArray();
        return new(v,docs,notizen.Liste(id),post.Values.Where(p=>p.VorgangId==id).OrderByDescending(p=>p.ErfasstAm).ToArray(),kontakte.Vorschlaege(docs.Select(d=>d.Id)).Select(v=>v with{Quelle=DateiFinden(v.DokumentId).Ansicht.Titel}).ToArray());}}
    public IReadOnlyList<Aktendokument> Eingang(bool alle=false){lock(sperre)return dateien.Values.Select(d=>Kopie(d.Ansicht)).Where(d=>alle||d.Vorgaenge.Count==0).OrderByDescending(d=>d.ErfasstAm).ToArray();}
    public Aktendokument Hinzufuegen(byte[] bytes,string name,string mime,string? vorgang,string kanal="Dateiauswahl")
    {
        if(kanal is not ("Dateiauswahl" or "Drag & Drop" or "Scan-Ordner"))throw new Arbeitsfehler("Unbekannter Eingangsweg.");
        var ergebnis=pruefung.Pruefen(bytes,name,mime);var hash=Convert.ToHexString(SHA256.HashData(bytes));
        lock(sperre){
            if(vorgang is not null)_=Akte(vorgang);
            var vorhanden=dateien.Values.SingleOrDefault(d=>d.Hash==hash);
            if(vorhanden is not null){
                if((vorhanden.Ansicht.Aktivitaeten?.Count??0)>=200)throw new Arbeitsfehler("Die lokale Eingangshistorie ist voll. Es wurde nichts geändert.");
                vorhanden.Ansicht=vorhanden.Ansicht with {Version=vorhanden.Ansicht.Version+1,Aktivitaeten=(vorhanden.Ansicht.Aktivitaeten??[]).Append(new Eingangsaktivitaet("Identische Datei erneut eingegangen · "+(kanal=="Scan-Ordner"?"Scan-Ordner":"Upload"),DateTimeOffset.UtcNow,Arbeitskontext.Benutzer,vorgang,kanal=="Scan-Ordner"?"Scan-Ordner":"Upload")).ToArray()};
                if(vorgang is not null&&!vorhanden.Ansicht.Vorgaenge.Contains(vorgang)){
                    vorhanden.Ansicht=vorhanden.Ansicht with {Vorgaenge=vorhanden.Ansicht.Vorgaenge.Append(vorgang).ToArray()};Erkennen(vorhanden,vorgang);
                }
                return Kopie(vorhanden.Ansicht);
            }
            if(dateien.Count>=512||dateien.Values.Sum(d=>(long)d.Bytes.Length)+bytes.Length>64L*1024*1024)throw new Arbeitsfehler("Der lokale Dokumentenspeicher ist voll. Es wurde nichts abgelegt.");
            var id=Guid.NewGuid().ToString("N");var refs=vorgang is null?Array.Empty<string>():[vorgang];
            var a=new Aktendokument(id,Path.GetFileNameWithoutExtension(name).Replace('_',' '),name,"Anhang","Ungeklärt",kanal,DateTimeOffset.UtcNow,1,ergebnis.Inhaltstyp,ergebnis.Vorschau,
                ergebnis.Text.Length>0?"Text vorhanden":"Keine Texterkennung verfügbar",refs,KategorieId:"ungeklaert",Aktivitaeten:[new("Dokument eingegangen",DateTimeOffset.UtcNow,Arbeitskontext.Benutzer,vorgang,kanal=="Scan-Ordner"?"Scan-Ordner":"Upload")]);
            var neu=new Datei(a,bytes.ToArray(),hash,kanal,ergebnis.Text,ergebnis.Textmethode);dateien[id]=neu;
            if(vorgang is not null)Erkennen(neu,vorgang);return Kopie(a);
        }
    }
    private void Erkennen(Datei d,string vorgang){var v=Akte(vorgang);if(v.FirmaId is not null)kontakte.Erkennen(v.FirmaId,d.Ansicht.Id,d.Ansicht.Titel,d.Text);}
    public Aktendokument Zuordnen(string id,Zuordnung wahl)
    {
        lock(sperre){var d=DateiFinden(id);Arbeitsregeln.Version(wahl.Version,d.Ansicht.Version);
            if(d.Ansicht.Vorgaenge.Count>0)throw new Arbeitsfehler("Dieses Dokument ist bereits zugeordnet.",409);
            var kategorie=Kategorien.Finden(wahl.Kategorie);
            string akte;
            if(wahl.VorgangId is not null){_=Akte(wahl.VorgangId);akte=wahl.VorgangId;}
            else{
                akte=VorgangAnlegen(new(wahl.NeuerTitel,kategorie.Id,wahl.FirmaId,wahl.DublettenBestaetigt)).Id;
            }
            d.Ansicht=d.Ansicht with {Kategorie=kategorie.Name,KategorieId=kategorie.Id,Status="Zugeordnet",Pruefbedarf=kategorie.Id=="ungeklaert",Aktivitaeten=(d.Ansicht.Aktivitaeten??[]).Append(new Eingangsaktivitaet("Zuordnung bestätigt",DateTimeOffset.UtcNow,Arbeitskontext.Benutzer,akte)).ToArray(),Vorgaenge=[akte],Version=d.Ansicht.Version+1};Erkennen(d,akte);return Kopie(d.Ansicht);
        }
    }
    public Aktendokument Umordnen(string id,Umzuordnung e){
        lock(sperre){var d=DateiFinden(id);Arbeitsregeln.Version(e.Version,d.Ansicht.Version);
            if(e.BisherigerVorgangId is null||!d.Ansicht.Vorgaenge.Contains(e.BisherigerVorgangId))throw new Arbeitsfehler("Die bisherige Zuordnung besteht nicht mehr. Bitte neu laden.",409);
            if((d.Ansicht.Aktivitaeten?.Count??0)>=200)throw new Arbeitsfehler("Die lokale Zuordnungshistorie ist voll.");
            var kategorie=e.Kategorie is null?null:Kategorien.Finden(e.Kategorie);
            Vorgang ziel;
            if(e.NeuerVorgangId is null){
                ziel=VorgangAnlegen(new(e.NeuerTitel,kategorie?.Id??d.Ansicht.KategorieId,e.FirmaId,e.DublettenBestaetigt));
            }else{
                if(string.IsNullOrWhiteSpace(e.NeuerVorgangId)||e.NeuerVorgangId==e.BisherigerVorgangId||d.Ansicht.Vorgaenge.Contains(e.NeuerVorgangId))throw new Arbeitsfehler("Bitte eine andere Zielakte wählen.");
                ziel=Akte(e.NeuerVorgangId);if(ziel.Status=="Archiviert")throw new Arbeitsfehler("Bitte eine aktive Zielakte wählen.");
            }
            var jetzt=DateTimeOffset.UtcNow;akten[e.BisherigerVorgangId]=Akte(e.BisherigerVorgangId) with{LetztesEreignis=jetzt};akten[ziel.Id]=ziel with{LetztesEreignis=jetzt};
            d.Ansicht=d.Ansicht with{Vorgaenge=[ziel.Id],Kategorie=kategorie?.Name??d.Ansicht.Kategorie,KategorieId=kategorie?.Id??d.Ansicht.KategorieId,Version=d.Ansicht.Version+1,Aktivitaeten=(d.Ansicht.Aktivitaeten??[]).Append(new Eingangsaktivitaet("Zuordnung geändert",jetzt,Arbeitskontext.Benutzer,ziel.Id,VorherigerVorgangId:e.BisherigerVorgangId)).ToArray()};Erkennen(d,ziel.Id);return Kopie(d.Ansicht);
        }
    }
    public Aktennotiz NotizSpeichern(string akte,string? id,Notizeingabe e){lock(sperre){_=Akte(akte);return notizen.Speichern(akte,id,e);}}
    public Aktennotiz NotizEntfernen(string akte,string id,int version,bool wiederherstellen=false){lock(sperre){_=Akte(akte);return notizen.Entfernen(akte,id,version,wiederherstellen);}}
    public Korrespondenz KorrespondenzSpeichern(string akte,Korrespondenzeingabe e)
    {
        lock(sperre){_=Akte(akte);var betreff=Arbeitsregeln.Text(e.Betreff,"Betreff",160);var inhalt=Arbeitsregeln.Text(e.Inhalt,"Inhalt",10000);
            if(e.Art is not ("Brief" or "E-Mail" or "Entwurf" or "Antwortschreiben" or "Anfrage" or "Erinnerung" or "Gesprächsvermerk" or "Sonstige Korrespondenz"))throw new Arbeitsfehler("Bitte Brief, E-Mail oder Entwurf wählen.");
            if(e.Richtung is not ("Empfangen" or "Gesendet" or "Entwurf"))throw new Arbeitsfehler("Bitte empfangen, gesendet oder Entwurf wählen.");
            if(e.Art=="Entwurf"&&e.Richtung!="Entwurf")throw new Arbeitsfehler("Ein Entwurf ist noch keine versandte Kommunikation.");
            var partner=kontakte.Finden(e.KontaktId);if(partner is null||partner.Archiviert)throw new Arbeitsfehler("Bitte eine externe Partei aus den Kontakten wählen.");
            if(e.KommuniziertAm is null||e.KommuniziertAm>DateTimeOffset.UtcNow.AddMinutes(5))throw new Arbeitsfehler("Bitte den tatsächlichen Kommunikationszeitpunkt prüfen.");
            var ids=(e.DokumentIds??(e.DokumentId is null?[]:new[]{e.DokumentId})).Distinct().ToArray();if(ids.Length>80||ids.Any(id=>!DateiFinden(id).Ansicht.Vorgaenge.Contains(akte)))throw new Arbeitsfehler("Dokumentverweise müssen zur aktuellen Akte gehören.");
            if(post.Values.Count(p=>p.VorgangId==akte)>=250)throw new Arbeitsfehler("Die lokale Korrespondenzgrenze ist erreicht.");
            if(post.Values.Sum(p=>(long)p.Inhalt.Length+p.Betreff.Length)+inhalt.Length+betreff.Length>8*1024*1024)throw new Arbeitsfehler("Der lokale Korrespondenzspeicher ist voll.");
            var p=new Korrespondenz(Guid.NewGuid().ToString("N"),akte,betreff,inhalt,e.Art,Arbeitskontext.Benutzer,DateTimeOffset.UtcNow,ids.FirstOrDefault(),Array.AsReadOnly(ids),e.Richtung,e.KontaktId,e.KommuniziertAm);post[p.Id]=p;return p;
        }
    }
    private Datei DateiFinden(string id)=>dateien.TryGetValue(id,out var d)?d:throw new Arbeitsfehler("Dokument nicht gefunden.",404);
    public Aktendokument Dokument(string id){lock(sperre)return Kopie(DateiFinden(id).Ansicht);}
    public Dokumenttechnik Technik(string id){lock(sperre){var d=DateiFinden(id);return new(id,d.Hash,d.Bytes.Length,d.Quelle,d.Methode,d.Ansicht.Vorgaenge.ToArray());}}
    public byte[] Bytes(string id,string? akte=null){lock(sperre){var d=DateiFinden(id);if(akte is not null&&!d.Ansicht.Vorgaenge.Contains(akte))throw new Arbeitsfehler("Dokument gehört nicht zu diesem Vorgang.",404);return d.Bytes.ToArray();}}
    public string Text(string id){lock(sperre)return DateiFinden(id).Text;}
    public IReadOnlyList<Firmenkontakt> Kontakte(){lock(sperre)return kontakte.Liste();}
    public Firmenkontakt KontaktSpeichern(string? id,Kontakteingabe e){lock(sperre)return kontakte.Speichern(id,e);}
    public Firmenkontakt KontaktArchivieren(string id,int version){lock(sperre)return kontakte.Archivieren(id,version);}
    public Firmenkontakt KontaktEntscheiden(string id,Kontaktfreigabe e){lock(sperre)return kontakte.Entscheiden(id,e);}
    public Uebersicht KontakteAnwenden(Uebersicht u){lock(sperre)return u with {Rechnungen=u.Rechnungen.Select(r=>r with {Aussteller=Kontaktname(r.AusstellerId,r.Aussteller),Empfaenger=Kontaktname(r.EmpfaengerId,r.Empfaenger),AusstellerKontakt=kontakte.Ansicht(r.AusstellerId??r.Aussteller),EmpfaengerKontakt=kontakte.Ansicht(r.EmpfaengerId??r.Empfaenger),Perspektive=r.Perspektive is null?null:r.Perspektive with {Unternehmen=r.Perspektive.Rolle=="Aussteller"?Kontaktname(r.AusstellerId,r.Aussteller):Kontaktname(r.EmpfaengerId,r.Empfaenger)},Zahlungen=r.Zahlungen.Select(z=>z with {Zahler=Kontaktname(DemoDaten.Namen.FirstOrDefault(p=>p.Value==z.Zahler).Key,z.Zahler),Empfaenger=Kontaktname(DemoDaten.Namen.FirstOrDefault(p=>p.Value==z.Empfaenger).Key,z.Empfaenger)}).ToArray()}).ToArray()};}
    public Dokumentenliste Dokumentenliste(string akte){lock(sperre){_=Akte(akte);return new(true,akte,Lesen(akte).Dokumente.Select(d=>new Dokumentansicht(d.Id,"DEMO-NORD",akte,d.Art,d.Dateiname,d.ErfasstAm.ToString("yyyy-MM-dd"),d.Version,Technik(d.Id).Bytezahl,Technik(d.Id).DateiHash,Technik(d.Id).Quelle,
        d.Id=="zahlung-002-v1"?"demo-zahlung-002":null,d.Inhaltstyp,d.Vorschau,d.Titel)).ToArray());}}
}
