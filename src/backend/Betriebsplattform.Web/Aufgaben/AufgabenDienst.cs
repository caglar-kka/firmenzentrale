using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Aufgaben;

public sealed class AufgabenDienst(FirmenDbContext db,TimeProvider clock,TimeZoneInfo zone)
{
    public static readonly string[] Statuswerte=["Offen","In Bearbeitung","Erledigt","Abgebrochen"];
    public static readonly string[] Prioritaeten=["Niedrig","Normal","Hoch","Dringend"];
    public string Zeitzone=>zone.Id;
    public async Task<IQueryable<string>> DokumentIds(string actor)=>Rechte(await Konto(actor)).Dokumente;
    public async Task<IQueryable<string>> AktenIds(string actor)=>Rechte(await Konto(actor)).Akten;
    public async Task<IQueryable<string>> KontaktIds(string actor)=>Rechte(await Konto(actor)).Kontakte;
    public async Task<bool> RessourceErlaubt(string actor,string art,string id)=>art is not ("Dokument" or "Akte" or "Kontakt")?throw new Arbeitsfehler("Unbekannter Bezug."):await BezugErlaubt(await Konto(actor),new Aufgabe{DokumentId=art=="Dokument"?id:null,AkteId=art=="Akte"?id:null,KontaktId=art=="Kontakt"?id:null});
    private async Task<Benutzerkonto> Konto(string id)=>await db.Benutzer.AsNoTracking().SingleOrDefaultAsync(u=>u.Id==id&&u.Aktiv&&u.Eingerichtet)??throw new Arbeitsfehler("Bitte erneut anmelden.",401);
    private sealed record Bereich(IQueryable<string> Kontakte,IQueryable<string> Akten,IQueryable<string> Rechnungen,IQueryable<string> Dokumente);
    private Bereich Rechte(Benutzerkonto u)
    {
        var admin=u.Rolle=="Administrator";var g=db.Freigaben.Where(g=>g.BenutzerId==u.Id);
        var k=db.Kontakte.IgnoreQueryFilters().Where(k=>admin||g.Any(g=>g.KontaktId==k.Id||(k.FirmaId!=null&&g.KontaktId==k.FirmaId))).Select(k=>k.Id);
        var a=db.Akten.IgnoreQueryFilters().Where(a=>admin||g.Any(g=>g.AkteId==a.Id||(a.FirmaId!=null&&g.KontaktId==a.FirmaId))).Select(a=>a.Id);
        var r=db.Rechnungen.IgnoreQueryFilters().Where(r=>admin||(a.Contains(r.AkteId)&&k.Contains(r.AusstellerId)&&k.Contains(r.EmpfaengerId))).Select(r=>r.Id);
        var z=db.Dokumentzuordnungen.IgnoreQueryFilters();
        var d=db.Dokumente.IgnoreQueryFilters().Where(d=>admin||((g.Any(g=>g.DokumentId==d.Id)||z.Any(z=>z.DokumentId==d.Id&&a.Contains(z.AkteId)))&&!z.Any(z=>z.DokumentId==d.Id&&!a.Contains(z.AkteId))&&(d.AbsenderId==null||k.Contains(d.AbsenderId)))).Select(d=>d.Id);
        return new(k,a,r,d);
    }
    private IQueryable<Aufgabe> ImBereich(IQueryable<Aufgabe> q,Benutzerkonto u)
    {var b=Rechte(u);return q.Where(a=>(a.FirmaId==null||b.Kontakte.Contains(a.FirmaId))&&(a.RechnungId==null||b.Rechnungen.Contains(a.RechnungId))&&(a.AkteId==null||b.Akten.Contains(a.AkteId))&&(a.KontaktId==null||b.Kontakte.Contains(a.KontaktId))&&(a.DokumentId==null||b.Dokumente.Contains(a.DokumentId)));}
    public async Task<IQueryable<Aufgabe>> Sichtbar(string actor)
    {var u=await Konto(actor);return ImBereich(db.Aufgaben,u).Where(a=>u.Rolle=="Administrator"||a.ErstellerId==actor||a.ZustaendigId==actor);}
    public async Task<Aufgabe> Lesen(string actor,string id)=>await (await Sichtbar(actor)).AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id)??throw new Arbeitsfehler("Aufgabe nicht verfügbar.",404);
    private async Task<bool> BezugErlaubt(Benutzerkonto u,Aufgabe a)
    {
        var b=Rechte(u);
        return (a.FirmaId is null||await b.Kontakte.AnyAsync(id=>id==a.FirmaId))&&(a.RechnungId is null||await b.Rechnungen.AnyAsync(id=>id==a.RechnungId))&&(a.AkteId is null||await b.Akten.AnyAsync(id=>id==a.AkteId))&&(a.KontaktId is null||await b.Kontakte.AnyAsync(id=>id==a.KontaktId))&&(a.DokumentId is null||await b.Dokumente.AnyAsync(id=>id==a.DokumentId));
    }
    public async Task<Aufgabe> Speichern(string actor,string? id,Aufgabeneingabe e)
    {
        var u=await Konto(actor);var a=id is null?new Aufgabe{ErstellerId=actor}:await (await Sichtbar(actor)).SingleOrDefaultAsync(a=>a.Id==id)??throw new Arbeitsfehler("Aufgabe nicht verfügbar.",404);
        if(a.Version!=(id is null?1:e.Version)||id is null&&e.Version!=0)throw new Arbeitsfehler("Die Aufgabe wurde inzwischen geändert. Bitte den aktuellen Stand prüfen.",409);
        if(a.Archiviert)throw new Arbeitsfehler("Die Aufgabe ist archiviert.",409);
        if(!Statuswerte.Contains(e.Status)||!Prioritaeten.Contains(e.Prioritaet)||e.Beschreibung is null||e.Beschreibung.Length>4000||e.FaelligUm is not null&&e.FaelligAm is null)throw new Arbeitsfehler("Status, Priorität und Fälligkeit prüfen.");
        var vorher=id is null?"":Stand(a,await db.Benutzer.Where(u=>u.Id==a.ZustaendigId).Select(u=>u.Anzeigename).SingleAsync());
        a.Betreff=Arbeitsregeln.Text(e.Betreff,"Betreff",160);a.Beschreibung=e.Beschreibung.Trim();a.Status=e.Status;a.Prioritaet=e.Prioritaet;a.FaelligAm=e.FaelligAm;a.FaelligUm=e.FaelligUm;a.FirmaId=e.FirmaId;
        a.RechnungId=e.BezugArt=="Rechnung"?e.BezugId:null;a.AkteId=e.BezugArt=="Akte"?e.BezugId:null;a.KontaktId=e.BezugArt=="Kontakt"?e.BezugId:null;a.DokumentId=e.BezugArt=="Dokument"?e.BezugId:null;
        if((e.BezugArt is null)!=(e.BezugId is null)||e.BezugArt is not (null or "Rechnung" or "Akte" or "Kontakt" or "Dokument"))throw new Arbeitsfehler("Vorgangsbezug prüfen.");
        if(a.FirmaId is not null&&!await db.Kontakte.IgnoreQueryFilters().AnyAsync(k=>k.Id==a.FirmaId&&k.Art=="Firma"))throw new Arbeitsfehler("Firma nicht verfügbar.");
        if(!await BezugErlaubt(u,a))throw new Arbeitsfehler("Vorgangsbezug nicht verfügbar.",403);
        var ziel=await Konto(e.ZustaendigId);
        if(!await BezugErlaubt(ziel,a)||(ziel.Id!=actor&&u.Rolle!="Administrator"&&a.FirmaId is null&&e.BezugId is null))throw new Arbeitsfehler("Diese Zuweisung ist nicht zulässig. Für Teamaufgaben einen freigegebenen Vorgang oder eine Firma wählen.",403);
        a.ZustaendigId=ziel.Id;a.Archiviert=e.Archiviert;
        if(a.FaelligAm is not null&&a.FaelligUm is not null&&zone.IsInvalidTime(a.FaelligAm.Value.ToDateTime(a.FaelligUm.Value)))throw new Arbeitsfehler("Diese Uhrzeit entfällt durch die Zeitumstellung. Bitte eine andere Uhrzeit wählen.");
        a.FaelligUtc=null;
        if(a.FaelligAm is not null&&a.FaelligUm is not null){var local=DateTime.SpecifyKind(a.FaelligAm.Value.ToDateTime(a.FaelligUm.Value),DateTimeKind.Unspecified);a.FaelligUtc=zone.IsAmbiguousTime(local)?new DateTimeOffset(local,zone.GetAmbiguousTimeOffsets(local).Min()).ToUniversalTime():new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local,zone));}
        a.GeaendertAm=clock.GetUtcNow();if(id is null){a.ErstelltAm=a.GeaendertAm;db.Aufgaben.Add(a);}
        else db.Entry(a).Property(x=>x.Version).IsModified=true; // Even an unchanged stale draft must perform the version check.
        var nachher=Stand(a,ziel.Anzeigename);
        db.Aufgabenverlauf.Add(new(){AufgabeId=a.Id,BenutzerId=actor,Zeitpunkt=clock.GetUtcNow(),Vorher=vorher,Nachher=nachher});
        db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion=id is null?"Aufgabe erstellt":"Aufgabe geändert",Ressource=a.Id});
        try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){throw new Arbeitsfehler("Die Aufgabe wurde inzwischen geändert. Bitte neu laden; Ihre Eingaben bleiben erhalten.",409);}return a;
    }
    private static string Stand(Aufgabe a,string name)=>JsonSerializer.Serialize(new{a.Betreff,a.Beschreibung,a.ZustaendigId,ZustaendigName=name,a.Status,a.Prioritaet,a.FaelligAm,a.FaelligUm,a.FirmaId,a.RechnungId,a.AkteId,a.KontaktId,a.DokumentId,a.Archiviert});
    public DateOnly Heute=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),zone).DateTime);
    public bool Ueberfaellig(Aufgabe a)
    {
        if(a.Archiviert||a.Status is "Erledigt" or "Abgebrochen"||a.FaelligAm is null)return false;
        if(a.FaelligUm is null)return a.FaelligAm<Heute;
        var local=DateTime.SpecifyKind(a.FaelligAm.Value.ToDateTime(a.FaelligUm.Value),DateTimeKind.Unspecified);
        var utc=zone.IsAmbiguousTime(local)?new DateTimeOffset(local,zone.GetAmbiguousTimeOffsets(local).Min()).UtcDateTime:TimeZoneInfo.ConvertTimeToUtc(local,zone);
        return utc<clock.GetUtcNow().UtcDateTime;
    }
    private async Task<string> GeschuetzterStand(Benutzerkonto u,Aufgabe aktuell,string json)
    {
        if(json.Length==0)return "";
        try{
            var alt=JsonSerializer.Deserialize<Aufgabe>(json);
            // Historical membership AND all historical resource grants are required, not just today's reference.
            if(alt is null||string.IsNullOrEmpty(alt.ZustaendigId)||(u.Rolle!="Administrator"&&aktuell.ErstellerId!=u.Id&&alt.ZustaendigId!=u.Id)||!await BezugErlaubt(u,alt))return "";
            return json;
        }catch(JsonException){return "";} // Unknown/legacy snapshots fail closed.
    }
    public async Task<object> Detail(string actor,string id,int verlaufSeite=0)
    {
        var a=await Lesen(actor,id);var u=await Konto(actor);var q=db.Aufgabenverlauf.AsNoTracking().Where(v=>v.AufgabeId==id);var historieAnzahl=await q.CountAsync();
        var daten=await q.OrderByDescending(v=>v.Zeitpunkt).ThenBy(v=>v.Id).Skip(FirmenDaten.Seite(verlaufSeite)*20).Take(20).Select(v=>new{v.BenutzerId,autor=db.Benutzer.Where(u=>u.Id==v.BenutzerId).Select(u=>u.Anzeigename).Single(),v.Zeitpunkt,v.Vorher,v.Nachher}).ToArrayAsync();
        var historie=new List<object>();foreach(var v in daten){var vorher=await GeschuetzterStand(u,a,v.Vorher);var nachher=await GeschuetzterStand(u,a,v.Nachher);var geschuetzt=(v.Vorher.Length>0&&vorher.Length==0)||(v.Nachher.Length>0&&nachher.Length==0);historie.Add(new{benutzerId=geschuetzt?null:v.BenutzerId,autor=geschuetzt?null:v.autor,v.Zeitpunkt,Vorher=vorher,Nachher=nachher,geschuetzt});}
        var quelle=await db.Eingangsworkflows.AsNoTracking().SingleOrDefaultAsync(w=>w.AufgabeId==id);var quellrecht=quelle is not null&&await BezugErlaubt(u,new Aufgabe{DokumentId=a.DokumentId,AkteId=quelle.AkteId,KontaktId=quelle.AbsenderId});
        return new{eingangId=quellrecht?quelle!.Id:null,eingangsakteId=quellrecht?quelle!.AkteId:null,aufgabe=a,ersteller=await db.Benutzer.Where(u=>u.Id==a.ErstellerId).Select(u=>u.Anzeigename).SingleAsync(),zustaendig=await db.Benutzer.Where(u=>u.Id==a.ZustaendigId).Select(u=>u.Anzeigename).SingleAsync(),historie,historieAnzahl,verlaufSeite,ueberfaellig=Ueberfaellig(a),zeitzone=zone.Id};
    }
    public async Task<object[]> Mitarbeiter(string actor,string? art,string? bezug,string? firma,string text="",int seite=0)
    {
        if((art is null)!=(bezug is null)||art is not (null or "Rechnung" or "Akte" or "Kontakt" or "Dokument"))throw new Arbeitsfehler("Vorgangsbezug prüfen.");
        var u=await Konto(actor);var a=new Aufgabe{FirmaId=firma,RechnungId=art=="Rechnung"?bezug:null,AkteId=art=="Akte"?bezug:null,KontaktId=art=="Kontakt"?bezug:null,DokumentId=art=="Dokument"?bezug:null};
        if(!await BezugErlaubt(u,a))throw new Arbeitsfehler("Vorgangsbezug nicht verfügbar.",403);
        text=text.Trim().ToLowerInvariant();if(text.Length>100)throw new Arbeitsfehler("Bitte höchstens 100 Suchzeichen eingeben.");
        var users=db.Benutzer.AsNoTracking().Where(x=>x.Aktiv&&x.Eingerichtet&&(text==""||x.Anzeigename.ToLower().Contains(text))).OrderBy(x=>x.Anzeigename).ThenBy(x=>x.Id);
        // Read bounded batches, but apply the page boundary only AFTER permission filtering.
        // Materialize each batch before further DB queries (Npgsql permits one active reader).
        var result=new List<object>();var eligible=0;var skip=FirmenDaten.Seite(seite)*20;
        for(var offset=0;;offset+=100){var batch=await users.Skip(offset).Take(100).ToArrayAsync();foreach(var x in batch)if((u.Rolle=="Administrator"||x.Id==actor||firma is not null||bezug is not null)&&await BezugErlaubt(x,a)){if(eligible++<skip)continue;result.Add(new{x.Id,x.Anzeigename});if(result.Count==21)return result.ToArray();}if(batch.Length<100)break;}
        return result.ToArray();
    }
}
