using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Akten;

// Immutable extraction strategy per row. Changes create a new row; old versions can be reactivated.
public sealed class Dokumentmuster : VersionierterDatensatz
{
    public string OrganisationId{get;set;}="";
    public string AbsenderId{get;set;}="";
    public string Feld{get;set;}="";
    public string Anker{get;set;}="";
    public string Strategie{get;set;}="";
    public string LayoutHash{get;set;}="";
    public string Dokumentart{get;set;}="";
    public string Quelle1Id{get;set;}="";
    public string? Quelle2Id{get;set;}
    public string Quelle1EingangId{get;set;}="";
    public string? Quelle2EingangId{get;set;}
    public int Quelle1Version{get;set;}
    public int? Quelle2Version{get;set;}
    public int MusterVersion{get;set;}
    public string Status{get;set;}="Entwurf";
    public string? AutomatikAktion{get;set;}
    public string? FreigabeVonId{get;set;}
}
public sealed record Musterentwurf(string Feld);
public sealed record Musterentscheidung(int Version,string Status,string? ZweiterEingangId,string? Aktion=null);
public sealed record Analysekontext(string? AkteId=null,string? AbsenderId=null);
public sealed class DokumentmusterDienst(FirmenDbContext db,AufgabenDienst rechte,TimeProvider clock)
{
    private async Task Admin(string actor){if(!await db.Benutzer.AnyAsync(u=>u.Id==actor&&u.Aktiv&&u.Rolle=="Administrator"))throw new Arbeitsfehler("Musterfreigaben sind Administratoren vorbehalten.",403);}
    public async Task<(string Organisation,string Absender)> Kontext(string actor,Analysekontext k){
        if(k.AkteId is null||k.AbsenderId is null||!await rechte.RessourceErlaubt(actor,"Akte",k.AkteId)||!await rechte.RessourceErlaubt(actor,"Kontakt",k.AbsenderId))throw new Arbeitsfehler("Akten- und Absenderkontext nicht verfügbar.",403);
        var firma=await db.Akten.IgnoreQueryFilters().Where(a=>a.Id==k.AkteId&&a.Status!="Archiviert").Select(a=>a.FirmaId).SingleOrDefaultAsync();
        if(firma is null||!await rechte.RessourceErlaubt(actor,"Kontakt",firma))throw new Arbeitsfehler("Organisation nicht eindeutig freigegeben.",403);return(firma,k.AbsenderId);
    }
    private async Task<(DokumentDatensatz Dokument,Eingangsworkflow Workflow,Dokumentanalyse Analyse,Eingangsangaben Angaben)> Quelle(string actor,string id){
        var e=await db.Dokumentaktivitaeten.IgnoreQueryFilters().SingleOrDefaultAsync(e=>e.Id==id&&e.Kanal!=null)??throw new Arbeitsfehler("Bestätigter Eingang nicht verfügbar.",404);
        if(!await rechte.RessourceErlaubt(actor,"Dokument",e.DokumentId))throw new Arbeitsfehler("Dokument nicht verfügbar.",404);
        var w=await db.Eingangsworkflows.AsNoTracking().SingleOrDefaultAsync(w=>w.Id==id&&w.GeprueftAm!=null)??throw new Arbeitsfehler("Eingang zuerst anhand des Originals bestätigen.");
        await Kontext(actor,new(w.AkteId,w.AbsenderId));var d=await db.Dokumente.IgnoreQueryFilters().AsNoTracking().SingleAsync(x=>x.Id==e.DokumentId);
        if(d.AnalyseJson is null)throw new Arbeitsfehler("Gespeicherte Analyse fehlt.");var a=JsonSerializer.Deserialize<Dokumentanalyse>(d.AnalyseJson,Dokumentanalysator.Json)!;
        if(a.Hash!=d.Hash)throw new Arbeitsfehler("Analysebezug stimmt nicht mit dem Original überein.",409);
        return(d,w,a,JsonSerializer.Deserialize<Eingangsangaben>(w.AngabenJson)!);
    }
    public static string Layout(Dokumentanalyse a){
        var text=string.Join('\n',a.Abschnitte.Select(s=>s.Seite+":"+Regex.Replace(s.Text.ToLowerInvariant(),@"\d[\d.,/\-]*","#",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100))+":"+(s.Position is null?"?":Math.Round(s.Position.X*10)+"/"+Math.Round(s.Position.Y*10))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(a.Seiten.Length+"/"+text)));
    }
    public static Prueffeld? Anwenden(Dokumentanalyse a,Dokumentmuster regel){
        if(Layout(a)!=regel.LayoutHash)return null;
        var candidates=new List<(Textabschnitt Abschnitt,string Wert,string Anker)>();
        foreach(var s in a.Abschnitte){
            if(regel.Strategie=="Textzeile"&&s.Text==regel.Anker)candidates.Add((s,s.Text,s.Id));
            else if(regel.Strategie=="Label"&&s.Text.StartsWith(regel.Anker+":",StringComparison.OrdinalIgnoreCase))candidates.Add((s,s.Text[(s.Text.IndexOf(':')+1)..].Trim(),s.Id));
            else if(regel.Strategie=="Tabellenzeile"&&s.Text.Trim().TrimEnd(':')==regel.Anker&&s.Position is not null){var row=a.Abschnitte.Where(v=>v.Seite==s.Seite&&v.Position is not null&&v.Position.X>s.Position.X+s.Position.Breite&&Math.Abs(v.Position.Y-s.Position.Y)<.008).ToArray();if(row.Length==1)candidates.Add((row[0],row[0].Text,s.Id));}
        }
        // Duplicate identical headings still need an unambiguous human source selection.
        var distinct=candidates.Select(c=>(c.Abschnitt,wert:Dokumentanalysator.Normalisieren(regel.Feld,c.Wert),c.Anker)).ToArray();
        if(distinct.Length==0||distinct.Select(c=>c.wert).Distinct().Count()!=1)return null;
        var chosen=distinct.OrderBy(c=>c.Abschnitt.Seite).ThenBy(c=>c.Abschnitt.Position?.Y??1).First();
        return new(regel.Feld,chosen.wert,chosen.Abschnitt.Id,"Vorschlag",chosen.Anker);
    }
    public async Task<object> Entwerfen(string actor,string eingang,string feld){
        await Admin(actor);var q=await Quelle(actor,eingang);var k=await Kontext(actor,new(q.Workflow.AkteId,q.Workflow.AbsenderId));
        var f=q.Angaben.Felder?.SingleOrDefault(f=>f.Name==feld&&f.AbschnittId!=null&&f.Status is "Bestätigt" or "Korrigiert")??throw new Arbeitsfehler("Bestätigtes Feld mit belegter Textstelle wählen.");
        var s=q.Analyse.Abschnitte.Single(x=>x.Id==f.AbschnittId);var anchor=f.AnkerAbschnittId is null?s:q.Analyse.Abschnitte.Single(x=>x.Id==f.AnkerAbschnittId);
        var strategie=anchor.Id!=s.Id?"Tabellenzeile":s.Text.Contains(':')?"Label":"Textzeile";
        if(strategie=="Textzeile"&&feld is not ("aussteller" or "empfaenger"))throw new Arbeitsfehler("Veränderliche Werte benötigen einen stabilen Feldanker.");
        var anker=strategie=="Label"?anchor.Text[..anchor.Text.IndexOf(':')].Trim():anchor.Text.Trim().TrimEnd(':');
        if(anker.Length is <2 or >160)throw new Arbeitsfehler("Feldanker nicht geeignet.");
        var regel=new Dokumentmuster{OrganisationId=k.Organisation,AbsenderId=k.Absender,Feld=feld,Anker=anker,Strategie=strategie,LayoutHash=Layout(q.Analyse),Dokumentart=q.Angaben.Art,Quelle1Id=q.Dokument.Id,Quelle1EingangId=q.Workflow.Id,Quelle1Version=q.Workflow.Version,MusterVersion=1+(await db.Dokumentmuster.Where(m=>m.OrganisationId==k.Organisation&&m.AbsenderId==k.Absender&&m.Feld==feld).Select(m=>(int?)m.MusterVersion).MaxAsync()??0)};
        if(Anwenden(q.Analyse,regel)?.Wert!=f.Wert)throw new Arbeitsfehler("Regel erklärt die bestätigte Korrektur nicht.");
        var vorhanden=await db.Dokumentmuster.AsNoTracking().SingleOrDefaultAsync(m=>m.OrganisationId==k.Organisation&&m.AbsenderId==k.Absender&&m.Feld==feld&&m.Quelle1EingangId==q.Workflow.Id&&m.Quelle1Version==q.Workflow.Version&&m.Anker==anker&&m.Strategie==strategie);if(vorhanden is not null)return Projektion(vorhanden);
        db.Dokumentmuster.Add(regel);db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Zeitpunkt=clock.GetUtcNow(),Aktion="Dokumentmuster entworfen",Ressource=regel.Id});await db.SaveChangesAsync();return Projektion(regel);
    }
    private static object Projektion(Dokumentmuster m)=>new{m.Id,m.OrganisationId,m.AbsenderId,m.Feld,m.Anker,m.Strategie,m.MusterVersion,m.Version,m.Status,m.AutomatikAktion};
    public async Task<object> Liste(string actor,Analysekontext kontext,int seite=0){var k=await Kontext(actor,kontext);var q=db.Dokumentmuster.AsNoTracking().Where(m=>m.OrganisationId==k.Organisation&&m.AbsenderId==k.Absender);return new{anzahl=await q.CountAsync(),eintraege=await q.OrderByDescending(m=>m.GeaendertAm).ThenBy(m=>m.Id).Skip(FirmenDaten.Seite(seite)*20).Take(20).Select(m=>new{m.Id,m.Feld,m.Anker,m.Strategie,m.MusterVersion,m.Version,m.Status,m.AutomatikAktion}).ToArrayAsync()};}
    public async Task<object> Entscheiden(string actor,string id,Musterentscheidung p){
        await Admin(actor);var regel=await db.Dokumentmuster.SingleOrDefaultAsync(m=>m.Id==id)??throw new Arbeitsfehler("Muster nicht verfügbar.",404);
        if(regel.Version!=p.Version)throw new Arbeitsfehler("Muster wurde geändert.",409);
        if(p.Status is not ("Aktiv" or "Pausiert")||p.Aktion is not (null or "Klassifizieren"))throw new Arbeitsfehler("Diese Automatisierungsaktion ist nicht freigabefähig.");
        if(p.Status=="Aktiv"){
            var q=await Quelle(actor,p.ZweiterEingangId??throw new Arbeitsfehler("Unabhängigen bestätigten Prüfbeleg auswählen."));var k=await Kontext(actor,new(q.Workflow.AkteId,q.Workflow.AbsenderId));
            var first=await db.Dokumente.IgnoreQueryFilters().AsNoTracking().SingleAsync(d=>d.Id==regel.Quelle1Id);
            var firstWorkflow=await db.Eingangsworkflows.AsNoTracking().SingleAsync(w=>w.Id==regel.Quelle1EingangId);if(firstWorkflow.Version!=regel.Quelle1Version)throw new Arbeitsfehler("Der erste Prüfbeleg wurde geändert. Neues Muster vorbereiten.",409);var firstData=JsonSerializer.Deserialize<Eingangsangaben>(firstWorkflow.AngabenJson)!;
            var corrected=q.Angaben.Felder?.SingleOrDefault(f=>f.Name==regel.Feld&&f.Status is "Bestätigt" or "Korrigiert");
            if(k.Organisation!=regel.OrganisationId||k.Absender!=regel.AbsenderId||first.Hash==q.Dokument.Hash||firstData.Dokumentdatum is null||q.Angaben.Dokumentdatum is null||string.IsNullOrWhiteSpace(firstData.Rechnungsnummer)||string.IsNullOrWhiteSpace(q.Angaben.Rechnungsnummer)||q.Angaben.Dokumentdatum==firstData.Dokumentdatum||q.Angaben.Art!=regel.Dokumentart||q.Angaben.Rechnungsnummer==firstData.Rechnungsnummer||corrected is null||Anwenden(q.Analyse,regel)?.Wert!=corrected.Wert||q.Analyse.Felder.Any(f=>f.Status=="Widerspruch"))throw new Arbeitsfehler("Unabhängiger Prüfbeleg, Layout oder bestätigte Feldwerte passen nicht. Keine Freigabe.");
            if(p.Aktion is not null&&(q.Analyse.Dokumentart!="Eingangsrechnung"||q.Analyse.Seiten.Any(s=>s.Status!="Digitaltext")||q.Analyse.Felder.Where(f=>f.Name is "rechnungsnummer" or "betrag" or "dokumentdatum").Any(f=>f.Status!="Vorschlag")))throw new Arbeitsfehler("Für automatische Klassifizierung fehlen sichere digitale Pflichtangaben.");
            regel.Quelle2Id=q.Dokument.Id;regel.Quelle2EingangId=q.Workflow.Id;regel.Quelle2Version=q.Workflow.Version;regel.FreigabeVonId=actor;
        }
        regel.Status=p.Status;regel.AutomatikAktion=p.Status=="Aktiv"?p.Aktion:null;
        db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Zeitpunkt=clock.GetUtcNow(),Aktion="Dokumentmuster "+p.Status+(p.Aktion is null?"":"; Klassifizieren freigegeben"),Ressource=regel.Id});await db.SaveChangesAsync();return Projektion(regel);
    }
    public async Task<Dokumentanalyse> Vorschlaege(string actor,Dokumentanalyse a,Analysekontext k){
        var kontext=await Kontext(actor,k);var regels=await db.Dokumentmuster.AsNoTracking().Where(m=>m.OrganisationId==kontext.Organisation&&m.AbsenderId==kontext.Absender&&m.Status=="Aktiv").OrderByDescending(m=>m.MusterVersion).ToArrayAsync();
        var felder=(a.BasisFelder??a.Felder).ToArray();var hints=a.Hinweise.Where(h=>!h.StartsWith("Muster:",StringComparison.Ordinal)).ToList();var angewandt=new List<string>();string? automatik=null;
        foreach(var regel in regels.GroupBy(r=>r.Feld).Select(g=>g.FirstOrDefault(r=>r.LayoutHash==Layout(a))??g.First())){
            if(!await db.Eingangsworkflows.AnyAsync(w=>w.Id==regel.Quelle1EingangId&&w.Version==regel.Quelle1Version&&w.GeprueftAm!=null)||!await db.Eingangsworkflows.AnyAsync(w=>w.Id==regel.Quelle2EingangId&&w.Version==regel.Quelle2Version&&w.GeprueftAm!=null)){hints.Add("Muster: Prüfbeleg geändert; Freigabe unwirksam, menschliche Prüfung erforderlich.");continue;}
            var v=Anwenden(a,regel);if(v is null){hints.Add("Muster: Layout oder Feldanker abweichend; menschliche Prüfung erforderlich.");continue;}
            var index=Array.FindIndex(felder,f=>f.Name==regel.Feld);if(index<0)continue;felder[index]=v;angewandt.Add(regel.Id);
            if(regel.AutomatikAktion=="Klassifizieren"&&regel.Dokumentart==a.Dokumentart&&a.Dokumentart=="Eingangsrechnung"&&a.Seiten.Length>0&&a.Seiten.All(s=>s.Status=="Digitaltext")&&!felder.Any(f=>f.Status=="Widerspruch")&&felder.Where(f=>f.Name is "rechnungsnummer" or "betrag" or "dokumentdatum").All(f=>f.Status=="Vorschlag")&&await db.Benutzer.AnyAsync(u=>u.Id==regel.FreigabeVonId&&u.Aktiv&&u.Rolle=="Administrator"))automatik=regel.Id;
        }
        if(hints.Any(h=>h.StartsWith("Muster:",StringComparison.Ordinal)))automatik=null;
        return a with{Felder=felder,BasisFelder=a.BasisFelder??a.Felder,OrganisationId=kontext.Organisation,AbsenderId=kontext.Absender,MusterIds=angewandt.ToArray(),AutomatikMusterId=automatik,Hinweise=hints.ToArray()};
    }
}
