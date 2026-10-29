using System.Security.Claims;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Assistenz;

public sealed record Assistenzanfrage(string Frage,string Modus="aufgaben",string? KontextArt=null,string? KontextId=null,string Suchtext="")
{
    public void Pruefen(){if(Frage is null||Frage.Trim().Length is <2 or >1000||Suchtext is null||Suchtext.Length>120||Modus is not ("aufgaben" or "suche" or "kontext")||(KontextArt is null)!=(KontextId is null)||KontextArt is not (null or "Akte" or "Kontakt" or "Dokument")||KontextId is {Length:>64})throw new Arbeitsfehler("Bitte Frage, Suche und Kontext prüfen.");}
    public override string ToString()=>nameof(Assistenzanfrage);
}

public sealed class Assistenzkontext(FirmenDbContext db,AufgabenDienst aufgaben)
{
    public async Task<object> Dokumentliste(string actor,string text,int seite,Arbeitsmodule module,ILesezugriff rechte,CancellationToken ct)
    {
        if(!module.Aktiv("Dokumente")||!module.Aktiv("Akten"))throw new Arbeitsfehler("Dokumentbereich nicht verfügbar.",404);
        if(text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");FirmenDaten.Seite(seite);
        if(rechte is not LokalerDemozugriff)throw new Arbeitsfehler("Dokumentliste benötigt einen vollständigen Rechtefilter.",403);
        var ids=await aufgaben.DokumentIds(actor);var query=db.Dokumente.AsNoTracking().Where(d=>ids.Contains(d.Id));
        if(!module.Aktiv("Finanzen"))query=query.Where(d=>!db.Dokumentzuordnungen.Any(z=>z.DokumentId==d.Id&&db.Rechnungen.Any(r=>r.AkteId==z.AkteId)));
        if(text.Trim().Length>0){var t=text.Trim().ToLowerInvariant();query=query.Where(d=>d.Titel.ToLower().Contains(t)||d.Dateiname.ToLower().Contains(t));}
        var anzahl=await query.CountAsync(ct);
        var rows=await query.OrderBy(d=>d.Titel).ThenBy(d=>d.Id).Skip(seite*20).Take(20).Select(d=>new{d.Id,d.Titel,d.Dateiname,d.Art,d.Status,d.ErstelltAm,AkteId=db.Dokumentzuordnungen.Where(z=>z.DokumentId==d.Id).Select(z=>z.AkteId).FirstOrDefault()}).ToArrayAsync(ct);
        return new{anzahl,eintraege=rows};
    }
    public async Task<Assistenzquelle[]> Lesen(string actor,Assistenzanfrage e,ILesezugriff rechte,Arbeitsmodule module,CancellationToken ct)
    {
        e.Pruefen();var result=new List<Assistenzquelle>();
        if(e.Modus=="aufgaben"){
            var q=(await aufgaben.Sichtbar(actor)).Where(a=>a.ZustaendigId==actor&&!a.Archiviert&&a.Status!="Erledigt"&&a.Status!="Abgebrochen");
            var rows=await q.OrderBy(a=>a.FaelligAm==null).ThenBy(a=>a.FaelligAm).ThenBy(a=>a.Id).Take(10).ToArrayAsync(ct);
            result.AddRange(rows.Select(a=>new Assistenzquelle("aufgabe:"+a.Id,a.Betreff,$"Status: {a.Status}; Priorität: {a.Prioritaet}; Fällig: {a.FaelligAm?.ToString("yyyy-MM-dd")??"Ohne Datum"} {a.FaelligUm}; Zeitzone: {aufgaben.Zeitzone}. {a.Beschreibung[..Math.Min(300,a.Beschreibung.Length)]}","aufgaben",a.Id)));
        }
        else if(e.Modus=="suche"){
            if(e.Suchtext.Trim().Length<2)throw new Arbeitsfehler("Für die Quellensuche mindestens zwei Zeichen eingeben.");
            var d=new FirmenDaten(db);var treffer=await d.Suchen(e.Suchtext,rechte,module,ct);
            foreach(var t in treffer){
                var art=t.Ziel switch{"kontakt"=>"Kontakt","vorgang" or "rechnung"=>"Akte",_=>"Dokument"};
                if(!await aufgaben.RessourceErlaubt(actor,art,t.Ziel=="rechnung"?t.VorgangId??t.Id:t.Id))continue;
                result.Add(new(t.Ziel+":"+t.Id,t.Titel,t.Zusatz,t.Ziel switch{"kontakt"=>"kontakte","vorgang"=>"vorgaenge","rechnung"=>"rechnungen",_=>t.VorgangId is null?"posteingang":"vorgaenge"},t.Ziel=="dokument"?t.VorgangId??t.Id:t.Id,t.Ziel=="dokument"?t.Id:null));
            }
        }
        else{
            var id=e.KontextId;var art=e.KontextArt;
            if(id is null||art is null||!await aufgaben.RessourceErlaubt(actor,art,id)||!rechte.Erlaubt(art=="Akte"?"Vorgang":art=="Kontakt"?"Firma":"Dokument",id))throw new Arbeitsfehler("Kontext nicht verfügbar.",404);
            if(!module.Aktiv("Akten"))throw new Arbeitsfehler("Kontext nicht verfügbar.",404);
            if(art=="Akte"){
                var a=await db.Akten.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id,ct)??throw new Arbeitsfehler("Kontext nicht verfügbar.",404);
                if(a.FirmaId is not null&&!await aufgaben.RessourceErlaubt(actor,"Kontakt",a.FirmaId))throw new Arbeitsfehler("Kontext nicht verfügbar.",404);
                if(!module.Aktiv("Finanzen")&&await db.Rechnungen.AnyAsync(r=>r.AkteId==id,ct))throw new Arbeitsfehler("Kontext nicht verfügbar.",404);
                result.Add(new("akte:"+id,a.Titel,"Aktenstatus: "+a.Status,"vorgaenge",id));
            }else if(art=="Kontakt"){
                await db.KontaktaktionPruefen("Lesen",ct);
                var k=await db.Kontakte.AsNoTracking().SingleOrDefaultAsync(k=>k.Id==id,ct)??throw new Arbeitsfehler("Kontext nicht verfügbar.",404);
                result.Add(new("kontakt:"+id,k.Name,"Art: "+k.Art,"kontakte",id));
            }
            if(module.Aktiv("Dokumente")){
                var erlaubte=await aufgaben.DokumentIds(actor);
                var q=db.Dokumente.AsNoTracking().Where(d=>erlaubte.Contains(d.Id));
                q=art=="Dokument"?q.Where(d=>d.Id==id):q.Where(d=>db.Dokumentzuordnungen.Any(z=>z.DokumentId==d.Id&&(art=="Akte"?z.AkteId==id:db.Akten.Any(a=>a.Id==z.AkteId&&a.FirmaId==id))));
                var docs=await q.OrderBy(d=>d.Id).Take(5).Select(d=>new{d.Id,d.Titel,d.Text,d.Textmethode,Akte=db.Dokumentzuordnungen.Where(z=>z.DokumentId==d.Id).Select(z=>z.AkteId).FirstOrDefault()}).ToArrayAsync(ct);
                result.AddRange(docs.Where(d=>rechte.Erlaubt("Dokument",d.Id)).Select(d=>new Assistenzquelle("dokument:"+d.Id,d.Titel,"Dokumentauszug (keine verifizierte Seitenposition), "+d.Textmethode+": "+d.Text[..Math.Min(1500,d.Text.Length)],d.Akte is null?"posteingang":"vorgaenge",d.Akte??d.Id,d.Akte is null?null:d.Id)));
            }
            if(module.Aktiv("Finanzen")&&art is "Akte" or "Kontakt"){
                var rs=await db.Rechnungen.AsNoTracking().Where(r=>art=="Akte"?r.AkteId==id:r.AusstellerId==id||r.EmpfaengerId==id).OrderBy(r=>r.Id).Take(5).ToArrayAsync(ct);
                foreach(var r in rs)if(rechte.Erlaubt("Rechnung",r.AkteId)&&await aufgaben.RessourceErlaubt(actor,"Akte",r.AkteId)&&await aufgaben.RessourceErlaubt(actor,"Kontakt",r.AusstellerId)&&await aufgaben.RessourceErlaubt(actor,"Kontakt",r.EmpfaengerId))result.Add(new("rechnung:"+r.Id,r.Nummer,"Rechnungsstatus: "+r.Zahlungsstatus+"; Datum: "+r.Datum.ToString("yyyy-MM-dd"),"rechnungen",r.AkteId));
            }
        }
        return result.Take(20).ToArray();
    }
    public static void Endpunkte(RouteGroupBuilder api,ILesezugriff rechte,Arbeitsmodule module)
    {
        static string Actor(HttpContext c)=>c.User.FindFirstValue(ClaimTypes.NameIdentifier)??throw new Arbeitsfehler("Anmeldung erforderlich.",401);
        api.MapGet("/assistenz/status",(IConfiguration cfg)=>new{konfiguriert=cfg.GetValue<bool>("FirmenZentrale:KI:LokalerBetriebGeprueft")&&!string.IsNullOrWhiteSpace(cfg["FirmenZentrale:KI:Modell"]),modell=cfg["FirmenZentrale:KI:Modell"]??"",runtime="Verbindung wird bei einer ausdrücklichen Modellanfrage geprüft"}).RequireAuthorization();
        api.MapGet("/dokumentenliste",(HttpContext c,Assistenzkontext d,CancellationToken ct)=>d.Dokumentliste(Actor(c),c.Request.Query["text"].ToString(),int.TryParse(c.Request.Query["seite"],out var n)?n:0,module,rechte,ct)).RequireAuthorization();
        api.MapPost("/assistenz/quellen",async(Assistenzanfrage e,HttpContext c,Assistenzkontext d,CancellationToken ct)=>new{quellen=await d.Lesen(Actor(c),e,rechte,module,ct),hinweis="Strukturierte Quellensuche · begrenzte Auswahl, keine KI-Inferenz."}).RequireAuthorization();
        api.MapPost("/assistenz/antwort",async(Assistenzanfrage e,HttpContext c,Assistenzkontext d,ILokaleInferenz ki,CancellationToken ct)=>{
            var actor=Actor(c);var quellen=await d.Lesen(actor,e,rechte,module,ct);var antwort=await ki.Antworten(e.Frage,quellen,ct);
            var aktuell=await d.Lesen(actor,e,rechte,module,ct);
            if(quellen.Any(q=>!aktuell.Contains(q)))throw new Arbeitsfehler("Quellen oder Rechte wurden geändert. Bitte erneut suchen.",409);
            return new{antwort.Text,quellen=quellen.Where(q=>antwort.Quellen.Contains(q.Id)).ToArray(),entwurf=true};
        }).RequireAuthorization();
    }
}
