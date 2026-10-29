using System.Globalization;
using Betriebsplattform.Web.Akten;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Persistenz;

public sealed partial class FirmenDaten
{
    private async Task<Dictionary<string,string>> Partnertexte(IEnumerable<string?> firmen,ILesezugriff rechte,CancellationToken ct)
    {
        var ids=firmen.Where(x=>x is not null).Distinct().ToArray();
        var kandidaten=await db.Kontakte.AsNoTracking().Where(k=>ids.Contains(k.Id)||ids.Contains(k.FirmaId)).OrderBy(k=>k.Id).Take(1000).ToArrayAsync(ct);
        var kontakte=kandidaten.Where(k=>rechte.Erlaubt("Firma",k.Id)).ToArray();var kontaktIds=kontakte.Select(k=>k.Id).ToArray();
        var werte=await db.Kontaktwerte.AsNoTracking().Where(w=>kontaktIds.Contains(w.KontaktId)&&w.Aktiv).Take(10000).ToArrayAsync(ct);
        return ids.ToDictionary(id=>id!,id=>string.Join(" ",kontakte.Where(k=>k.Id==id||k.FirmaId==id).Select(k=>k.Name+" "+k.Anschrift+" "+k.Ort+" "+k.Rolle+" "+string.Join(" ",werte.Where(w=>w.KontaktId==k.Id).Select(w=>w.Wert)))));
    }
    // Keep memory bounded while counting all fuzzy-search matches before selecting a page.
    private static async Task<Ergebnisseite<T>> Suchseite<T>(IQueryable<T> query,int skip,int take,Func<T[],Task<T[]>> pruefen,CancellationToken ct)
    {
        var seite=new List<T>();var anzahl=0;
        for(var offset=0;;offset+=200){
            ct.ThrowIfCancellationRequested();var batch=await query.Skip(offset).Take(200).ToArrayAsync(ct);
            foreach(var row in await pruefen(batch)){if(anzahl>=skip&&seite.Count<take)seite.Add(row);anzahl++;}
            if(batch.Length<200)break;
        }
        return new(seite.ToArray(),anzahl);
    }
    private static string Geld(decimal wert)=>wert.ToString("N2",CultureInfo.GetCultureInfo("de-DE"))+" €";
    public async Task<object> AktenSuchen(string? text,string? status,string? kategorie,string? sort,bool ab,int seite,bool paginiert,ILesezugriff rechte,CancellationToken ct=default,Arbeitsmodule? module=null)
    {
        if((text?.Length??0)>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");
        Seite(seite);var woerter=Suchvergleich.Woerter(text??"");var stems=woerter.Where(t=>t.Length>1).Select(t=>t[..Math.Min(2,t.Length)]).Distinct().ToArray();
        var docMatches=stems.Length==0?[]:await (from z in db.Dokumentzuordnungen join d in db.Dokumente on z.DokumentId equals d.Id where stems.Any(t=>d.Suchtext.Contains(t)) orderby d.Id select new{z.AkteId,d.Id,d.AbsenderId,d.Suchtext}).Take(1000).ToArrayAsync(ct);
        docMatches=docMatches.Where(d=>rechte.Erlaubt("Dokument",d.Id)&&(d.AbsenderId is null||rechte.Erlaubt("Firma",d.AbsenderId))).ToArray();
        var docAkten=docMatches.Select(x=>x.AkteId).Distinct().ToArray();
        var finanzen=module?.Aktiv("Finanzen")??true;
        var query=from a in db.Akten.AsNoTracking() join k in db.Kategorien on a.KategorieId equals k.Id
                  join f in db.Kontakte on a.FirmaId equals f.Id into firmen from f in firmen.DefaultIfEmpty()
                  where (finanzen||!db.Rechnungen.Any(r=>r.AkteId==a.Id))&&(string.IsNullOrEmpty(status)||a.Status==status)&&(string.IsNullOrEmpty(kategorie)||a.KategorieId==kategorie)
                  select new{a.Id,a.Titel,a.Suchtext,a.KategorieId,Kategorie=k.Name,a.FirmaId,Firma=f==null?"Nicht zugeordnet":f.Name,FirmenSuche=f==null?"":f.Suchname,a.Status,a.Version,a.GeaendertAm,Nummer=db.Rechnungen.Where(r=>r.AkteId==a.Id).Select(r=>r.Nummer).FirstOrDefault(),Dokumentanzahl=db.Dokumentzuordnungen.Count(z=>z.AkteId==a.Id)};
        if(stems.Length>0)query=query.Where(a=>stems.Any(t=>a.Suchtext.Contains(t)||a.FirmenSuche.Contains(t)||a.Kategorie.ToLower().Contains(t))||docAkten.Contains(a.Id)||db.Kontakte.Any(p=>p.FirmaId==a.FirmaId&&stems.Any(t=>p.Suchname.Contains(t))));
        var geordnet=sort switch{"titel"=>ab?query.OrderByDescending(a=>a.Titel):query.OrderBy(a=>a.Titel),"kategorie"=>ab?query.OrderByDescending(a=>a.Kategorie):query.OrderBy(a=>a.Kategorie),"status"=>ab?query.OrderByDescending(a=>a.Status):query.OrderBy(a=>a.Status),_=>ab?query.OrderByDescending(a=>a.GeaendertAm):query.OrderBy(a=>a.GeaendertAm)};
        var gesamt=stems.Length==0?await query.CountAsync(ct):0;
        var kandidaten=woerter.Length>0?[]:await geordnet.ThenBy(a=>a.Id).Skip(seite*20).Take(paginiert?20:100).ToArrayAsync(ct);
        if(woerter.Length>0){var gefunden=await Suchseite(geordnet.ThenBy(a=>a.Id),seite*20,paginiert?20:100,async batch=>{
            var partner=await Partnertexte(batch.Select(a=>a.FirmaId),rechte,ct);
            return batch.Where(a=>rechte.Erlaubt("Vorgang",a.Id)&&(a.FirmaId is null||rechte.Erlaubt("Firma",a.FirmaId))&&Suchvergleich.Passt(text,"Akte "+a.Titel+" "+a.Kategorie+" "+a.Firma+" "+a.Nummer+" "+(a.FirmaId is null?"":partner.GetValueOrDefault(a.FirmaId,""))+" "+string.Join(' ',docMatches.Where(d=>d.AkteId==a.Id).Select(d=>d.Suchtext)))).ToArray();
        },ct);kandidaten=gefunden.Eintraege;gesamt=gefunden.Anzahl;}
        var aktenPartner=await Partnertexte(kandidaten.Select(a=>a.FirmaId),rechte,ct);
        var result=kandidaten.Where(a=>rechte.Erlaubt("Vorgang",a.Id)&&(a.FirmaId is null||rechte.Erlaubt("Firma",a.FirmaId))&&(woerter.Length==0||Suchvergleich.Passt(text,"Akte "+a.Titel+" "+a.Kategorie+" "+a.Firma+" "+a.Nummer+" "+(a.FirmaId is null?"":aktenPartner.GetValueOrDefault(a.FirmaId,""))+" "+string.Join(' ',docMatches.Where(d=>d.AkteId==a.Id).Select(d=>d.Suchtext))))).Select(a=>new Vorgang(a.Id,a.Titel,a.Kategorie,a.FirmaId,a.Nummer,a.KategorieId,a.Firma,a.Status,a.GeaendertAm,a.Dokumentanzahl,"Dokumente und Schriftverkehr",a.Version)).ToArray();
        if(!paginiert)return result;
        if(rechte is not LokalerDemozugriff)throw new Arbeitsfehler("Berechtigte Gesamtzahlen benötigen einen Datenbank-Rechtefilter.",403);
        return new{eintraege=result,anzahl=gesamt};
    }
    public async Task<Uebersicht> Rechnungsuebersicht(int seite,ILesezugriff rechte,CancellationToken ct=default,Listenfilter? filter=null)
    {
        if(rechte is not LokalerDemozugriff)throw new Arbeitsfehler("Berechtigte Finanzsummen benötigen einen Datenbank-Rechtefilter.",403);
        var basis=db.Rechnungen.Select(r=>new{r.Betrag,r.Richtung,Offen=r.Betrag-(db.Zahlungszuordnungen.Where(z=>z.RechnungId==r.Id).Sum(z=>(decimal?)z.Betrag)??0)});
        var sums=await basis.GroupBy(_=>1).Select(g=>new{Gesamt=g.Sum(x=>x.Betrag),Offen=g.Sum(x=>x.Offen),Forderungen=g.Sum(x=>x.Richtung=="Ausgang"?x.Offen:0),Verbindlichkeiten=g.Sum(x=>x.Richtung=="Eingang"?x.Offen:0),Unklar=g.Any(x=>x.Richtung=="Ungeklärt")}).SingleOrDefaultAsync(ct);
        var f=filter??new Listenfilter();if(!new[]{"","Eingang","Ausgang","Forderungen","Verbindlichkeiten"}.Contains(f.Richtung))throw new Arbeitsfehler("Bitte einen angebotenen Rechnungstyp wählen.");if(f.Text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");var suchwoerter=Suchvergleich.Woerter(f.Text);var stems=suchwoerter.Where(t=>t.Length>1).Select(t=>t[..Math.Min(2,t.Length)]).ToArray();
        var query=from r in db.Rechnungen.AsNoTracking() join a in db.Kontakte on r.AusstellerId equals a.Id join e in db.Kontakte on r.EmpfaengerId equals e.Id
                  where (f.Id==""||r.AkteId==f.Id||r.Id==f.Id)&&(f.Status==""||r.Zahlungsstatus==(f.Status=="Teilzahlung"?"Teilweise bezahlt":f.Status))
                    &&(f.Richtung==""||(f.Richtung=="Eingang"||f.Richtung=="Ausgang"?r.Richtung==f.Richtung:r.Zahlungsstatus!="Bezahlt"&&r.Richtung==(f.Richtung=="Forderungen"?"Ausgang":"Eingang")))
                    &&(stems.Length==0||stems.Any(t=>r.Nummer.ToLower().Contains(t)||a.Suchname.Contains(t)||e.Suchname.Contains(t)||r.Beleg.ToLower().Contains(t)||db.Kontakte.Any(p=>(p.FirmaId==r.AusstellerId||p.FirmaId==r.EmpfaengerId)&&p.Suchname.Contains(t))))
                  select new{r,Aussteller=a.Name,Empfaenger=e.Name,Bezahlt=db.Zahlungszuordnungen.Where(z=>z.RechnungId==r.Id).Sum(z=>(decimal?)z.Betrag)??0};
        var anzahl=await query.CountAsync(ct);var bestand=await db.Rechnungen.CountAsync(ct);
        var sortiert=f.Sort switch{"datum"=>f.Ab?query.OrderByDescending(x=>x.r.Datum):query.OrderBy(x=>x.r.Datum),"betrag"=>f.Ab?query.OrderByDescending(x=>x.r.Betrag):query.OrderBy(x=>x.r.Betrag),"aussteller"=>f.Ab?query.OrderByDescending(x=>x.Aussteller):query.OrderBy(x=>x.Aussteller),"empfaenger"=>f.Ab?query.OrderByDescending(x=>x.Empfaenger):query.OrderBy(x=>x.Empfaenger),"status"=>f.Ab?query.OrderByDescending(x=>x.r.Zahlungsstatus):query.OrderBy(x=>x.r.Zahlungsstatus),_=>f.Ab?query.OrderByDescending(x=>x.r.Nummer):query.OrderBy(x=>x.r.Nummer)};
        var daten=suchwoerter.Length>0?[]:await sortiert.ThenBy(x=>x.r.Id).Skip(Seite(seite)*20).Take(20).ToArrayAsync(ct);
        if(suchwoerter.Length>0){var gefunden=await Suchseite(sortiert.ThenBy(x=>x.r.Id),Seite(seite)*20,20,async batch=>{
            var partner=await Partnertexte(batch.SelectMany(x=>new[]{x.r.AusstellerId,x.r.EmpfaengerId}),rechte,ct);
            return batch.Where(x=>Suchvergleich.Passt(f.Text,"Rechnung "+x.r.Nummer+" "+x.Aussteller+" "+x.Empfaenger+" "+x.r.Beleg+" "+partner.GetValueOrDefault(x.r.AusstellerId,"")+" "+partner.GetValueOrDefault(x.r.EmpfaengerId,""))).ToArray();
        },ct);daten=gefunden.Eintraege;anzahl=gefunden.Anzahl;}
        var ids=daten.Select(x=>x.r.Id).ToArray();var zahlungen=await (from z in db.Zahlungszuordnungen join p in db.Zahlungen on z.ZahlungId equals p.Id join a in db.Kontakte on p.ZahlerId equals a.Id into zahler from a in zahler.DefaultIfEmpty() join e in db.Kontakte on p.EmpfaengerId equals e.Id into empfaenger from e in empfaenger.DefaultIfEmpty() where ids.Contains(z.RechnungId) orderby p.Datum,p.Id select new{z.RechnungId,p.Id,p.Datum,Zahler=a==null?"Nicht freigegebener Kontakt":a.Name,Empfaenger=e==null?"Nicht freigegebener Kontakt":e.Name,p.Betrag,Zugeordnet=z.Betrag,p.Beleg,p.Quelle}).Take(2000).ToArrayAsync(ct);
        var kontaktIds=daten.SelectMany(x=>new[]{x.r.AusstellerId,x.r.EmpfaengerId}).Distinct().ToArray();
        var kontaktwerte=await db.Kontaktwerte.AsNoTracking().Where(w=>kontaktIds.Contains(w.KontaktId)&&w.Aktiv).OrderByDescending(w=>w.Primaer).ThenBy(w=>w.Id).Take(1200).ToArrayAsync(ct);
        Kontaktansicht Kontakt(string id){var emails=kontaktwerte.Where(w=>w.KontaktId==id&&w.Art=="E-Mail").Select(w=>w.Wert).ToArray();var telefone=kontaktwerte.Where(w=>w.KontaktId==id&&w.Art=="Telefon").Select(w=>w.Wert).ToArray();return new(null,emails.FirstOrDefault(),telefone.FirstOrDefault(),"Zentrale Kontaktverwaltung",emails,telefone,[]);}
        var rows=daten.Select(x=>{var bezahlt=x.Bezahlt;var offen=x.r.Betrag-bezahlt;return new Rechnungsansicht(x.r.Nummer,x.r.Datum.ToString("yyyy-MM-dd"),x.Aussteller,x.Empfaenger,Geld(x.r.Betrag),Geld(offen),Geld(bezahlt),offen==0?"Bezahlt":bezahlt==0?"Offen":"Teilzahlung",x.r.Beleg,x.r.Quelle,zahlungen.Where(z=>z.RechnungId==x.r.Id).Select(z=>new Zahlungsansicht(z.Datum.ToString("yyyy-MM-dd"),z.Zahler,z.Empfaenger,Geld(z.Betrag),Geld(z.Zugeordnet),"Bestätigt",z.Beleg,z.Quelle,z.Id)).ToArray(),AusstellerKontakt:Kontakt(x.r.AusstellerId),EmpfaengerKontakt:Kontakt(x.r.EmpfaengerId),VorgangId:x.r.AkteId,Perspektive:x.r.Richtung=="Ungeklärt"?null:new(x.r.Richtung=="Ausgang"?x.Aussteller:x.Empfaenger,x.r.Richtung=="Ausgang"?"Aussteller":"Empfänger"),AusstellerId:x.r.AusstellerId,EmpfaengerId:x.r.EmpfaengerId,RechnungId:x.r.Id);}).ToArray();
        return new(true,Geld(sums?.Gesamt??0),Geld(sums?.Offen??0),rows,sums?.Unklar==true?null:Geld(sums?.Forderungen??0),sums?.Unklar==true?null:Geld(sums?.Verbindlichkeiten??0),anzahl,bestand);
    }
    public async Task<Dictionary<string,int>> Arbeitszahlen(string firma,ILesezugriff rechte,Arbeitsmodule module,CancellationToken ct=default)
    {
        if(rechte is not LokalerDemozugriff)throw new Arbeitsfehler("Berechtigte Gesamtzahlen benötigen einen Datenbank-Rechtefilter.",403);
        if(!await db.Kontakte.AnyAsync(k=>k.Id==Id(firma),ct))throw new Arbeitsfehler("Kontakt nicht gefunden.",404);
        var finanzen=module.Aktiv("Finanzen");var ids=db.Akten.Where(a=>a.FirmaId==firma&&(finanzen||!db.Rechnungen.Any(r=>r.AkteId==a.Id))).Select(a=>a.Id);
        return new(){["akten"]=module.Aktiv("Akten")?await ids.CountAsync(ct):0,["rechnungen"]=module.Aktiv("Finanzen")?await db.Rechnungen.CountAsync(r=>r.AusstellerId==firma||r.EmpfaengerId==firma,ct):0,["dokumente"]=module.Aktiv("Dokumente")&&module.Aktiv("Akten")?await db.Dokumentzuordnungen.CountAsync(z=>ids.Contains(z.AkteId),ct):0,["notizen"]=module.Aktiv("Akten")?await db.Notizen.CountAsync(n=>ids.Contains(n.AkteId)&&!n.Archiviert,ct):0,["korrespondenz"]=module.Aktiv("Kommunikation")&&module.Aktiv("Akten")?await db.Korrespondenz.CountAsync(k=>ids.Contains(k.AkteId),ct):0};
    }
    public async Task<object> Firmenarbeit(string firma,string? bereich,int seite,ILesezugriff rechte,Arbeitsmodule module,CancellationToken ct=default)
    {
        var zahlen=await Arbeitszahlen(firma,rechte,module,ct);if(bereich is null||!zahlen.ContainsKey(bereich))throw new Arbeitsfehler("Bitte einen vorhandenen Arbeitsbereich wählen.");
        var finanzen=module.Aktiv("Finanzen");var akten=db.Akten.Where(a=>a.FirmaId==firma&&(finanzen||!db.Rechnungen.Any(r=>r.AkteId==a.Id)));var aids=akten.Select(a=>a.Id);var skip=Seite(seite)*20;Suchtreffer[] rows=[];
        if(zahlen[bereich]>0)rows=bereich switch{
            "akten"=>await akten.OrderBy(a=>a.Titel).ThenBy(a=>a.Id).Skip(skip).Take(20).Select(a=>new Suchtreffer(a.Id,"Akte",a.Titel,a.Status,"vorgang",a.Id)).ToArrayAsync(ct),
            "rechnungen"=>await db.Rechnungen.Where(r=>r.AusstellerId==firma||r.EmpfaengerId==firma).OrderBy(r=>r.Nummer).ThenBy(r=>r.Id).Skip(skip).Take(20).Select(r=>new Suchtreffer(r.AkteId,"Rechnung",r.Nummer,r.Richtung+" · "+r.Zahlungsstatus,"rechnung",r.AkteId)).ToArrayAsync(ct),
            "dokumente"=>await (from z in db.Dokumentzuordnungen join d in db.Dokumente on z.DokumentId equals d.Id where aids.Contains(z.AkteId) orderby d.Titel,d.Id select new Suchtreffer(d.Id,"Dokument",d.Titel,"Dokument der Firma","dokument",z.AkteId)).Skip(skip).Take(20).ToArrayAsync(ct),
            "notizen"=>await db.Notizen.Where(n=>aids.Contains(n.AkteId)&&!n.Archiviert).OrderBy(n=>n.Betreff).ThenBy(n=>n.Id).Skip(skip).Take(20).Select(n=>new Suchtreffer(n.Id,"Interne Notiz",n.Betreff,"Aktennotiz","vorgang",n.AkteId)).ToArrayAsync(ct),
            "korrespondenz"=>await db.Korrespondenz.Where(k=>aids.Contains(k.AkteId)).OrderBy(k=>k.Betreff).ThenBy(k=>k.Id).Skip(skip).Take(20).Select(k=>new Suchtreffer(k.Id,k.Art,k.Betreff,"Korrespondenz","vorgang",k.AkteId)).ToArrayAsync(ct),_=>[]};
        return new{anzahl=zahlen[bereich],eintraege=rows};
    }
    public async Task<Suchtreffer[]> Suchen(string text,ILesezugriff rechte,Arbeitsmodule module,CancellationToken ct=default)
    {
        if(text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");if(text.Trim().Length<2)return[];
        var words=Suchvergleich.Woerter(text);if(words.Length==0)return[];
        var stems=words.Select(t=>t[..Math.Min(2,t.Length)]).ToArray();var treffer=new List<Suchtreffer>();
        var kontakte=!await db.KontaktaktionErlaubt("Lesen",ct)?[]:await db.Kontakte.AsNoTracking().Where(k=>!k.Archiviert&&stems.Any(t=>k.Suchname.Contains(t))).OrderBy(k=>k.Suchname).Take(100).ToArrayAsync(ct);
        treffer.AddRange(kontakte.Where(k=>rechte.Erlaubt("Firma",k.Id)&&(k.FirmaId is null||rechte.Erlaubt("Firma",k.FirmaId))&&Suchvergleich.Passt(text,k.Art+" Kontakt "+k.Name+" "+k.Ort)).Select(k=>new Suchtreffer(k.Id,k.Art,k.Name,k.Art=="Person"?"Ansprechpartner":"Kontaktakte","kontakt")));
        if(module.Aktiv("Akten")){var a=(Vorgang[])await AktenSuchen(text,null,null,"titel",false,0,false,rechte,ct,module);treffer.AddRange(a.Take(10).Select(v=>new Suchtreffer(v.Id,"Akte",v.Titel,v.Kategorie+" · "+v.Firma,"vorgang",v.Id)));}
        if(module.Aktiv("Dokumente")){
            var docs=await (from d in db.Dokumente.AsNoTracking() join z in db.Dokumentzuordnungen on d.Id equals z.DokumentId into zz from z in zz.DefaultIfEmpty() join a in db.Akten on z.AkteId equals a.Id into aa from a in aa.DefaultIfEmpty() where stems.Any(t=>d.Suchtext.Contains(t)) orderby d.Titel,d.Id select new{d.Id,d.Titel,d.Suchtext,d.AbsenderId,Firma=a==null?null:a.FirmaId,Finanzakte=z!=null&&db.Rechnungen.Any(r=>r.AkteId==z.AkteId),Akte=z==null?null:z.AkteId}).Take(100).ToArrayAsync(ct);
            treffer.AddRange(docs.Where(d=>rechte.Erlaubt("Dokument",d.Id)&&(d.AbsenderId is null||rechte.Erlaubt("Firma",d.AbsenderId))&&(d.Firma is null||rechte.Erlaubt("Firma",d.Firma))&&(d.Akte is null||module.Aktiv("Akten")&&(module.Aktiv("Finanzen")||!d.Finanzakte)&&rechte.Erlaubt("Vorgang",d.Akte))&&Suchvergleich.Passt(text,"Dokument "+d.Suchtext)).Select(d=>new Suchtreffer(d.Id,"Dokument",d.Titel,d.Akte is null?"Posteingang":"Akteninhalt",d.Akte is null?"eingang":"dokument",d.Akte)));
        }
        if(module.Aktiv("Finanzen")){
            var rs=await (from r in db.Rechnungen.AsNoTracking() join a in db.Kontakte on r.AusstellerId equals a.Id join e in db.Kontakte on r.EmpfaengerId equals e.Id where stems.Any(t=>r.Nummer.ToLower().Contains(t)||a.Suchname.Contains(t)||e.Suchname.Contains(t)||db.Kontakte.Any(p=>(p.FirmaId==r.AusstellerId||p.FirmaId==r.EmpfaengerId)&&p.Suchname.Contains(t))) orderby r.Nummer,r.Id select new{r.AkteId,r.Nummer,r.AusstellerId,r.EmpfaengerId,Aussteller=a.Name,Empfaenger=e.Name}).Take(100).ToArrayAsync(ct);
            var partner=await Partnertexte(rs.SelectMany(r=>new[]{r.AusstellerId,r.EmpfaengerId}),rechte,ct);
            treffer.AddRange(rs.Where(r=>rechte.Erlaubt("Rechnung",r.AkteId)&&rechte.Erlaubt("Vorgang",r.AkteId)&&rechte.Erlaubt("Firma",r.AusstellerId)&&rechte.Erlaubt("Firma",r.EmpfaengerId)&&Suchvergleich.Passt(text,"Rechnung "+r.Nummer+" "+r.Aussteller+" "+r.Empfaenger+" "+partner.GetValueOrDefault(r.AusstellerId,"")+" "+partner.GetValueOrDefault(r.EmpfaengerId,""))).Select(r=>new Suchtreffer(r.AkteId,"Rechnung",r.Nummer,r.Aussteller+" → "+r.Empfaenger,"rechnung",r.AkteId)));
        }
        return treffer.OrderByDescending(t=>words.Contains(Suchvergleich.Woerter(t.Art).FirstOrDefault())).ThenBy(t=>t.Titel).Take(10).ToArray();
    }
}

// Bounded query contract; HTTP adapters do not accept arbitrary column expressions.
public sealed record Listenfilter(string Text="",string Status="",string Kategorie="",string Firma="",string Kanal="",string Sicht="",string Von="",string Bis="",string Sort="",bool Ab=false,bool Archiv=false,string Art="",string Richtung="",string Id="")
{
    public static Listenfilter Lesen(HttpContext c)
    {
        string Wert(string key,int max=120){var v=c.Request.Query[key].ToString();if(v.Length>max)throw new Arbeitsfehler("Die Filterangabe ist zu lang.");return v;}
        var von=Wert("von",10);var bis=Wert("bis",10);
        foreach(var v in new[]{von,bis})if(v!=""&&!DateOnly.TryParseExact(v,"yyyy-MM-dd",out _))throw new Arbeitsfehler("Bitte einen gültigen Zeitraum wählen.");
        if(von!=""&&bis!=""&&string.CompareOrdinal(von,bis)>0)throw new Arbeitsfehler("Das Enddatum liegt vor dem Anfangsdatum.");
        return new(Wert("text"),Wert("status"),Wert("kategorie",64),Wert("firma",64),Wert("kanal"),Wert("sicht"),von,bis,Wert("sort"),Wert("ab")=="1",Wert("archiv")=="1",Wert("art"),Wert("richtung"),Wert("id",64));
    }
}
public sealed record Ergebnisseite<T>(T[] Eintraege,int Anzahl);

public sealed partial class FirmenDaten
{
    public async Task<Ergebnisseite<Firmenkontakt>> KontakteSuchen(Listenfilter f,int seite,CancellationToken ct=default)
    {
        var woerter=Suchvergleich.Woerter(f.Text);if(f.Text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");
        var stems=woerter.Where(t=>t.Length>1).Select(t=>t[..Math.Min(2,t.Length)]).ToArray();
        var q=db.Kontakte.AsNoTracking().Where(k=>(f.Archiv||!k.Archiviert)&&(f.Art==""||k.Art==f.Art)&&(f.Firma==""||k.FirmaId==f.Firma));
        if(stems.Length>0)q=q.Where(k=>stems.Any(t=>k.Suchname.Contains(t)||k.Ort.ToLower().Contains(t)||k.Strasse.ToLower().Contains(t)||db.Kontaktwerte.Any(w=>w.KontaktId==k.Id&&w.Aktiv&&w.Vergleichswert.Contains(t))));
        var sorted=f.Sort switch{"art"=>f.Ab?q.OrderByDescending(k=>k.Art):q.OrderBy(k=>k.Art),"firma"=>f.Ab?q.OrderByDescending(k=>db.Kontakte.Where(firma=>firma.Id==k.FirmaId).Select(firma=>firma.Name).FirstOrDefault()):q.OrderBy(k=>db.Kontakte.Where(firma=>firma.Id==k.FirmaId).Select(firma=>firma.Name).FirstOrDefault()),_=>f.Ab?q.OrderByDescending(k=>k.Art=="Person"?k.Nachname:k.Name).ThenByDescending(k=>k.Vorname):q.OrderBy(k=>k.Art=="Person"?k.Nachname:k.Name).ThenBy(k=>k.Vorname)};
        var count=stems.Length==0?await q.CountAsync(ct):0;
        var rows=woerter.Length>0?[]:await sorted.ThenBy(k=>k.Id).Skip(Seite(seite)*20).Take(20).ToArrayAsync(ct);
        if(woerter.Length>0){var gefunden=await Suchseite(sorted.ThenBy(k=>k.Id),Seite(seite)*20,20,async batch=>{
            var ids=batch.Select(k=>k.Id).ToArray();var werte=await db.Kontaktwerte.AsNoTracking().Where(w=>ids.Contains(w.KontaktId)&&w.Aktiv).Take(6000).ToArrayAsync(ct);
            return batch.Where(k=>Suchvergleich.Passt(f.Text,k.Art+" "+k.Name+" "+k.Anschrift+" "+k.Strasse+" "+k.Plz+" "+k.Ort+" "+string.Join(' ',werte.Where(w=>w.KontaktId==k.Id).Select(w=>w.Wert)))).ToArray();
        },ct);rows=gefunden.Eintraege;count=gefunden.Anzahl;}
        return new(await KontaktAnsichten(rows,ct,false),count);
    }
    public async Task<Ergebnisseite<Aktendokument>> EingangSuchen(Listenfilter f,int seite,CancellationToken ct=default,Arbeitsmodule? module=null)
    {
        var norm=Normal(f.Text);if(f.Text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");
        var q=db.Dokumente.AsNoTracking().Where(d=>(norm==""||d.Suchtext.Contains(norm))&&(f.Status==""||d.Status==f.Status)&&(f.Kategorie==""||d.KategorieId==f.Kategorie)&&(f.Firma==""||d.AbsenderId==f.Firma)&&(f.Kanal==""||d.Kanal==f.Kanal));
        if(module is not null&&!module.Aktiv("Akten"))q=q.Where(d=>!db.Dokumentzuordnungen.Any(z=>z.DokumentId==d.Id));
        if(module is not null&&!module.Aktiv("Finanzen"))q=q.Where(d=>!db.Dokumentzuordnungen.Any(z=>z.DokumentId==d.Id&&db.Rechnungen.Any(r=>r.AkteId==z.AkteId)));
        if(f.Sicht=="Zu prüfen")q=q.Where(d=>d.Pruefbedarf||d.Status=="Neu"||d.Status=="Zur Prüfung"||d.Status=="Zuordnung vorgeschlagen");
        if(f.Sicht=="Kürzlich verarbeitet")q=q.Where(d=>d.Status=="Zugeordnet"||d.Status=="Erledigt");
        if(f.Von!=""){var von=DateTimeOffset.ParseExact(f.Von,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal);q=q.Where(d=>d.ErstelltAm>=von);}
        if(f.Bis!=""){var bis=DateTimeOffset.ParseExact(f.Bis,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal).AddDays(1);q=q.Where(d=>d.ErstelltAm<bis);}
        var sorted=f.Sort switch{"status"=>f.Ab?q.OrderByDescending(d=>d.Status):q.OrderBy(d=>d.Status),"kategorie"=>f.Ab?q.OrderByDescending(d=>db.Kategorien.Where(k=>k.Id==d.KategorieId).Select(k=>k.Name).FirstOrDefault()):q.OrderBy(d=>db.Kategorien.Where(k=>k.Id==d.KategorieId).Select(k=>k.Name).FirstOrDefault()),"absender"=>f.Ab?q.OrderByDescending(d=>db.Kontakte.Where(k=>k.Id==d.AbsenderId).Select(k=>k.Name).FirstOrDefault()):q.OrderBy(d=>db.Kontakte.Where(k=>k.Id==d.AbsenderId).Select(k=>k.Name).FirstOrDefault()),"bearbeitet"=>f.Ab?q.OrderByDescending(d=>d.GeaendertAm):q.OrderBy(d=>d.GeaendertAm),_=>f.Ab?q.OrderByDescending(d=>d.ErstelltAm):q.OrderBy(d=>d.ErstelltAm)};
        var ids=await sorted.ThenBy(d=>d.Id).Skip(Seite(seite)*20).Take(20).Select(d=>d.Id).ToArrayAsync(ct);var rows=await DokumentMetadaten().Where(d=>ids.Contains(d.Id)).ToArrayAsync(ct);
        return new(await DokumentAnsichten(ids.Select(id=>rows.Single(d=>d.Id==id)).ToArray(),ct,false),await q.CountAsync(ct));
    }
}
