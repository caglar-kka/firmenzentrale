using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Akten;

// Supplement to the existing document arrival activity, never a second original-document store.
public sealed class Eingangsworkflow
{
    public string Id {get;set;}="";
    public string Status {get;set;}="Neu";
    public string? ZustaendigId {get;set;}
    public string? AbsenderId {get;set;}
    public string? AkteId {get;set;}
    public string? AufgabeId {get;set;}
    public string AngabenJson {get;set;}="";
    public string Rueckfrage {get;set;}="";
    public string? GeprueftVonId {get;set;}
    public DateTimeOffset? GeprueftAm {get;set;}
    public int Version {get;set;}=1;
}
public sealed record Eingangsangaben(string Betreff,string Art,DateOnly? Dokumentdatum=null,string? Rechnungsnummer=null,decimal? Betrag=null,DateOnly? FaelligAm=null,string? Referenz=null,Prueffeld[]? Felder=null);
public sealed record Eingangspruefung(int Version,string Entscheidung,Eingangsangaben Angaben,string? AbsenderId,string? AkteId,string? ZustaendigId,string Grund,string Rueckfrage,bool Bestaetigt);
public sealed record Eingangsaufgabe(int Version,bool Bestaetigt,bool FristBestaetigt,Aufgabeneingabe Aufgabe,string? Regelbezug=null);
public sealed record Aufgabenregelvorschlag(string Regel,string? Sperrgrund,string? Bezug,int Version,string DokumentHash,string? AkteId,string? Akte,string? AbsenderId,string? ZustaendigId,string? AbschnittId,Aufgabeneingabe? Aufgabe);
public sealed record Analyseantwort(Dokumentanalyse Analyse);
public sealed record Eingangsansicht(string Id,string DokumentId,string Betreff,string Art,string Dateiname,string Absender,string? AbsenderId,string? AkteId,string? Akte,string? ZustaendigId,string? Zustaendig,string Status,string Kanal,DateTimeOffset EingegangenAm,int Version,string? AufgabeId,Eingangsangaben Vorschlag,Eingangsangaben? BestaetigteAngaben,string Quelle,string Rueckfrage,bool IdentischErneut,string Vorschau,DateTimeOffset? GeprueftAm);
public sealed class EingangsDienst(FirmenDbContext db,AufgabenDienst aufgaben,TimeProvider clock)
{
    public static readonly string[] Entscheidungen=["Zuordnen","Bereits vorhanden","Als neuen Eingang behalten","Zur Prüfung zurückstellen","Erledigen"];
    public static readonly string[] Arten=["Eingangsrechnung","Versicherungsschreiben","Behördenbrief","Kundenkorrespondenz","Sonstiges"];
    private async Task<IQueryable<DokumentaktivitaetDatensatz>> Sichtbar(string actor)
    {
        var docs=await aufgaben.DokumentIds(actor);var akten=await aufgaben.AktenIds(actor);var kontakte=await aufgaben.KontaktIds(actor);
        return db.Dokumentaktivitaeten.IgnoreQueryFilters().Where(x=>x.Kanal!=null&&docs.Contains(x.DokumentId)&&(!db.Eingangsworkflows.Any(w=>w.Id==x.Id)||db.Eingangsworkflows.Any(w=>w.Id==x.Id&&(w.AkteId==null||akten.Contains(w.AkteId))&&(w.AbsenderId==null||kontakte.Contains(w.AbsenderId)))));
    }
    private async Task<DokumentaktivitaetDatensatz> Eingang(string actor,string id)=>await (await Sichtbar(actor)).OrderByDescending(x=>x.Zeitpunkt).FirstOrDefaultAsync(x=>x.Id==id||x.DokumentId==id)??throw new Arbeitsfehler("Eingang nicht verfügbar.",404);
    public static Eingangsangaben Vorschlag(DokumentDatensatz d)
    {
        // Deterministic, explicitly unconfirmed labels; no OCR/LLM and no invented missing values.
        string? Wert(string key){var m=Regex.Match(d.Text,"(?m)^"+Regex.Escape(key)+@":\s*([^\r\n]{1,160})$",RegexOptions.CultureInvariant);return m.Success?m.Groups[1].Value.Trim():null;}
        DateOnly? Datum(string key)=>DateOnly.TryParseExact(Wert(key),"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var v)?v:null;
        var art=Wert("Dokumentart");return new(d.Titel,Arten.Contains(art??"")?art!:"Sonstiges",Datum("Dokumentdatum"),Wert("Rechnungsnummer"),decimal.TryParse(Wert("Betrag"),System.Globalization.NumberStyles.AllowDecimalPoint,System.Globalization.CultureInfo.InvariantCulture,out var b)?b:null,Datum("Antwort bis"),Wert("Referenz"));
    }
    public async Task<Eingangsansicht> Ansicht(string actor,DokumentaktivitaetDatensatz e)
    {
        var d=await db.Dokumente.IgnoreQueryFilters().AsNoTracking().SingleAsync(x=>x.Id==e.DokumentId);var w=await db.Eingangsworkflows.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==e.Id);
        var a=w?.AkteId??await db.Dokumentzuordnungen.IgnoreQueryFilters().Where(x=>x.DokumentId==d.Id).Select(x=>x.AkteId).SingleOrDefaultAsync();
        // Document visibility was checked before projection; never include technical hashes or extracted raw text.
        var sender=w?.AbsenderId;var confirmed=w?.GeprueftAm is not null&&w.AngabenJson.Length>0?JsonSerializer.Deserialize<Eingangsangaben>(w.AngabenJson):null;
        return new(e.Id,d.Id,confirmed?.Betreff??d.Titel,confirmed?.Art??(Arten.Contains(d.Art)?d.Art:Vorschlag(d).Art),d.Dateiname,sender is null?"Noch ungeklärt":await db.Kontakte.IgnoreQueryFilters().Where(k=>k.Id==sender).Select(k=>k.Name).SingleAsync(),sender,a,a is null?null:await db.Akten.IgnoreQueryFilters().Where(k=>k.Id==a).Select(k=>k.Titel).SingleAsync(),w?.ZustaendigId,w?.ZustaendigId is null?null:await db.Benutzer.Where(u=>u.Id==w.ZustaendigId).Select(u=>u.Anzeigename).SingleAsync(),w?.Status??"Neu",e.Kanal!,e.Zeitpunkt,w?.Version??0,w?.AufgabeId,Vorschlag(d),confirmed,"Regelvorschlag aus Dokumenttext; keine fachliche Bestätigung",w?.Rueckfrage??"Zuordnung und Angaben prüfen.",await db.Dokumentaktivitaeten.IgnoreQueryFilters().AnyAsync(x=>x.DokumentId==d.Id&&x.Kanal!=null&&x.Id!=e.Id&&x.Zeitpunkt<=e.Zeitpunkt),d.Vorschau,w?.GeprueftAm);
    }
    public async Task<object> Liste(string actor,string sicht,string text,int seite,string? akte=null)
    {
        if(sicht is not ("neu" or "pruefen" or "meine" or "erledigt" or "alle"))throw new Arbeitsfehler("Ansicht nicht verfügbar.");
        text=text.Trim().ToLowerInvariant();if(text.Length>120)throw new Arbeitsfehler("Suchtext ist zu lang.");var q=await Sichtbar(actor);
        if(sicht=="neu")q=q.Where(x=>!db.Eingangsworkflows.Any(w=>w.Id==x.Id)||db.Eingangsworkflows.Any(w=>w.Id==x.Id&&w.Status=="Neu"));
        if(sicht=="pruefen")q=q.Where(x=>!db.Eingangsworkflows.Any(w=>w.Id==x.Id)||db.Eingangsworkflows.Any(w=>w.Id==x.Id&&(w.Status=="Zur Prüfung"||w.Status=="Neu")));
        if(sicht=="meine")q=q.Where(x=>db.Eingangsworkflows.Any(w=>w.Id==x.Id&&w.ZustaendigId==actor&&w.Status!="Erledigt"));
        if(sicht=="erledigt")q=q.Where(x=>db.Eingangsworkflows.Any(w=>w.Id==x.Id&&w.Status=="Erledigt"));
        if(akte is not null)q=q.Where(x=>db.Eingangsworkflows.Any(w=>w.Id==x.Id&&w.AkteId==akte)||db.Dokumentzuordnungen.Any(z=>z.DokumentId==x.DokumentId&&z.AkteId==akte));
        if(text.Length>0)q=q.Where(x=>db.Dokumente.IgnoreQueryFilters().Any(d=>d.Id==x.DokumentId&&(d.Titel.ToLower().Contains(text)||d.Dateiname.ToLower().Contains(text)))||db.Eingangsworkflows.Any(w=>w.Id==x.Id&&w.AngabenJson.ToLower().Contains(text)));
        var anzahl=await q.CountAsync();var rows=await q.OrderByDescending(x=>x.Zeitpunkt).ThenBy(x=>x.Id).Skip(FirmenDaten.Seite(seite)*20).Take(20).AsNoTracking().ToArrayAsync();var daten=new List<Eingangsansicht>();foreach(var row in rows)daten.Add(await Ansicht(actor,row));return new{eintraege=daten,anzahl};
    }
    public async Task<object> Detail(string actor,string id,int verlaufSeite=0)
    {
        var e=await Eingang(actor,id);var a=await Ansicht(actor,e);var akten=await aufgaben.AktenIds(actor);
        var kontakte=await aufgaben.KontaktIds(actor);var q=db.Dokumentaktivitaeten.IgnoreQueryFilters().AsNoTracking().Where(h=>h.EingangsId==e.Id&&(h.AkteId==null||akten.Contains(h.AkteId))&&(h.VorherigeAkteId==null||akten.Contains(h.VorherigeAkteId))&&(h.PruefAbsenderId==null||kontakte.Contains(h.PruefAbsenderId))&&(h.VorherigerAbsenderId==null||kontakte.Contains(h.VorherigerAbsenderId)));
        var historieAnzahl=await q.CountAsync();var historie=await q.OrderByDescending(h=>h.Zeitpunkt).ThenBy(h=>h.Id).Skip(FirmenDaten.Seite(verlaufSeite)*20).Take(20).Select(h=>new{h.Text,h.Zeitpunkt,h.Benutzer}).ToArrayAsync();
        return new{eingang=a,historie,historieAnzahl};
    }
    public async Task<object> Uebersicht(string actor){var q=await Sichtbar(actor);return new{neu=await q.CountAsync(x=>!db.Eingangsworkflows.Any(w=>w.Id==x.Id)||db.Eingangsworkflows.Any(w=>w.Id==x.Id&&w.Status=="Neu")),pruefen=await q.CountAsync(x=>!db.Eingangsworkflows.Any(w=>w.Id==x.Id)||db.Eingangsworkflows.Any(w=>w.Id==x.Id&&(w.Status=="Neu"||w.Status=="Zur Prüfung"))),meine=await q.CountAsync(x=>db.Eingangsworkflows.Any(w=>w.Id==x.Id&&w.ZustaendigId==actor&&w.Status!="Erledigt"))};}
    private async Task<Eingangsworkflow> Stand(string actor,string id,int version)
    {
        var e=await Eingang(actor,id);var w=await db.Eingangsworkflows.SingleOrDefaultAsync(w=>w.Id==e.Id);
        if((w?.Version??0)!=version)throw new Arbeitsfehler("Eingang wurde geändert. Bitte neu laden.",409);
        if(w is null){w=new(){Id=e.Id};db.Eingangsworkflows.Add(w);}else w.Version++;return w;
    }
    public async Task<object> Pruefen(string actor,string id,Eingangspruefung p)
    {
        var e=await Eingang(actor,id);if(!Entscheidungen.Contains(p.Entscheidung)||p.Angaben is null||!p.Bestaetigt||string.IsNullOrWhiteSpace(p.Grund)||p.Grund.Length>400||p.Rueckfrage is null||p.Rueckfrage.Length>1000)throw new Arbeitsfehler("Entscheidung und Begründung ausdrücklich bestätigen.");
        var originalFuerPruefung=await db.Dokumente.IgnoreQueryFilters().AsNoTracking().SingleAsync(x=>x.Id==e.DokumentId);
        if(p.Angaben.Felder is not null){
            if(originalFuerPruefung.AnalyseJson is null)throw new Arbeitsfehler("Zuerst die Dokumentanalyse laden.");
            var analyse=JsonSerializer.Deserialize<Dokumentanalyse>(originalFuerPruefung.AnalyseJson,Dokumentanalysator.Json)!;
            if(analyse.OrganisationId is not null&&!await aufgaben.RessourceErlaubt(actor,"Kontakt",analyse.OrganisationId)||analyse.AbsenderId is not null&&!await aufgaben.RessourceErlaubt(actor,"Kontakt",analyse.AbsenderId))throw new Arbeitsfehler("Analysekontext nicht freigegeben.",403);
            if(p.Angaben.Felder.Any(f=>f is null)||p.Angaben.Felder.Length>16||p.Angaben.Felder.Select(f=>f.Name).Distinct().Count()!=p.Angaben.Felder.Length)throw new Arbeitsfehler("Prüffelder sind nicht eindeutig.");
            foreach(var f in p.Angaben.Felder){
                if(!analyse.Felder.Any(a=>a.Name==f.Name)||f.Wert?.Length>160||f.Status is not ("Bestätigt" or "Korrigiert" or "Manuell")||(f.AbschnittId is not null&&!analyse.Abschnitte.Any(a=>a.Id==f.AbschnittId)))throw new Arbeitsfehler("Prüffeld oder Fundstelle ungültig.");
                if(f.Status=="Manuell"&&(f.AbschnittId is not null||f.AnkerAbschnittId is not null))throw new Arbeitsfehler("Manuelle Angaben ohne Fundstelle kennzeichnen.");
                if(f.Status=="Bestätigt"&&!analyse.Felder.Any(v=>v.Name==f.Name&&v.Wert==f.Wert&&v.AbschnittId==f.AbschnittId&&v.Status=="Vorschlag"))throw new Arbeitsfehler("Bestätigter Vorschlag stimmt nicht mit der Analyse überein.");
                if(f.AnkerAbschnittId is not null&&!analyse.Abschnitte.Any(a=>a.Id==f.AnkerAbschnittId))throw new Arbeitsfehler("Feldanker nicht verfügbar.");
                if(f.Status=="Korrigiert"&&f.AbschnittId is not null){var text=analyse.Abschnitte.Single(a=>a.Id==f.AbschnittId).Text;var wert=text.Contains(':')?text[(text.IndexOf(':')+1)..].Trim():text;if(Dokumentanalysator.Normalisieren(f.Name,wert)!=f.Wert&&!analyse.Felder.Any(v=>v.Name==f.Name&&v.AbschnittId==f.AbschnittId&&v.Wert==f.Wert&&v.Status=="Vorschlag"))throw new Arbeitsfehler("Korrigierter Wert stimmt nicht mit der gewählten Textstelle überein. Bitte als manuelle Eingabe speichern.");}
                var passend=f.Name switch{"rechnungsnummer"=>f.Wert==p.Angaben.Rechnungsnummer,"betrag"=>f.Wert is null?p.Angaben.Betrag is null:Dokumentanalysator.Betrag(f.Wert)==p.Angaben.Betrag,"dokumentdatum"=>f.Wert==(p.Angaben.Dokumentdatum?.ToString("yyyy-MM-dd")),"faelligAm"=>f.Wert==(p.Angaben.FaelligAm?.ToString("yyyy-MM-dd")),"referenz"=>f.Wert==p.Angaben.Referenz,_=>true};
                if(!passend)throw new Arbeitsfehler("Prüffeld und Dokumentangabe widersprechen sich.");
            }
            if(JsonSerializer.Serialize(p.Angaben).Length>5000)throw new Arbeitsfehler("Prüfangaben sind zu umfangreich.");
        }
        if(!Arten.Contains(p.Angaben.Art)||string.IsNullOrWhiteSpace(p.Angaben.Betreff)||p.Angaben.Betreff.Length>160||p.Angaben.Rechnungsnummer?.Length>160||p.Angaben.Referenz?.Length>160||p.Angaben.Betrag is <0 or >9999999999999||p.Angaben.Betrag is decimal geld&&decimal.Round(geld,2)!=geld)throw new Arbeitsfehler("Dokumentangaben prüfen.");
        if(p.AbsenderId is not null&&!await aufgaben.RessourceErlaubt(actor,"Kontakt",p.AbsenderId))throw new Arbeitsfehler("Absender nicht verfügbar.",403);
        if(p.AkteId is not null&&(!await aufgaben.RessourceErlaubt(actor,"Akte",p.AkteId)||!await db.Akten.IgnoreQueryFilters().AnyAsync(a=>a.Id==p.AkteId&&a.Status!="Archiviert")))throw new Arbeitsfehler("Akte nicht verfügbar.",403);
        if(p.ZustaendigId is not null&&(!await aufgaben.RessourceErlaubt(p.ZustaendigId,"Dokument",e.DokumentId)||(p.AkteId is not null&&!await aufgaben.RessourceErlaubt(p.ZustaendigId,"Akte",p.AkteId))||(p.AbsenderId is not null&&!await aufgaben.RessourceErlaubt(p.ZustaendigId,"Kontakt",p.AbsenderId))))throw new Arbeitsfehler("Zuständigkeit ist für diesen Vorgang nicht freigegeben.",403);
        await using var tx=await db.Database.BeginTransactionAsync();var w=await Stand(actor,e.Id,p.Version);var before=w.AkteId;var alterAbsender=w.AbsenderId;
        if(w.AufgabeId is not null&&(w.AkteId!=p.AkteId||w.AbsenderId!=p.AbsenderId))throw new Arbeitsfehler("Bezug einer Aufgabe zuerst im Aufgabenbereich prüfen. Eingang bleibt unverändert.",409);
        if(p.Entscheidung is "Zuordnen" or "Erledigen"&&p.AkteId is null)throw new Arbeitsfehler("Bitte eine vorhandene Akte auswählen. Neue Akten zunächst im Aktenbereich vorbereiten.");
        if(p.Entscheidung is "Zuordnen" or "Erledigen"){
            var z=await db.Dokumentzuordnungen.IgnoreQueryFilters().SingleOrDefaultAsync(z=>z.DokumentId==e.DokumentId);
            if(z is not null&&z.AkteId!=p.AkteId)throw new Arbeitsfehler("Das Original gehört bereits zu einer anderen Akte. Dort die bestehende Zuordnung ausdrücklich ändern.",409);
            if(z is null)db.Dokumentzuordnungen.Add(new(){DokumentId=e.DokumentId,AkteId=p.AkteId!});
            var original=await db.Dokumente.IgnoreQueryFilters().SingleAsync(d=>d.Id==e.DokumentId);original.Status="Zugeordnet";original.Pruefbedarf=false;original.Art=p.Angaben.Art;original.AbsenderId=p.AbsenderId;

        }
        var vorherigeFelder=w.AngabenJson.Length==0?null:JsonSerializer.Deserialize<Eingangsangaben>(w.AngabenJson)?.Felder;
        foreach(var f in p.Angaben.Felder??[]){var vorher=vorherigeFelder?.SingleOrDefault(v=>v.Name==f.Name);if(vorher!=f)db.Dokumentaktivitaeten.Add(new(){DokumentId=e.DokumentId,EingangsId=e.Id,AkteId=p.AkteId,VorherigeAkteId=before,PruefAbsenderId=p.AbsenderId,VorherigerAbsenderId=alterAbsender,Text=$"Prüffeld {f.Name}: {vorher?.Wert??"unbestätigt"} → {f.Wert??"leer"} ({f.Status}; {f.AbschnittId??"ohne Fundstelle"})",Benutzer=(await db.Benutzer.SingleAsync(u=>u.Id==actor)).Anzeigename,Zeitpunkt=clock.GetUtcNow()});}
        w.AkteId=p.AkteId;w.AbsenderId=p.AbsenderId;w.ZustaendigId=p.ZustaendigId;w.AngabenJson=JsonSerializer.Serialize(p.Angaben);w.Rueckfrage=p.Rueckfrage.Trim();w.GeprueftVonId=actor;w.GeprueftAm=clock.GetUtcNow();
        w.Status=p.Entscheidung=="Erledigen"||p.Entscheidung=="Bereits vorhanden"?"Erledigt":p.Entscheidung=="Zuordnen"?"Zugeordnet":"Zur Prüfung";
        db.Dokumentaktivitaeten.Add(new(){DokumentId=e.DokumentId,EingangsId=e.Id,AkteId=p.AkteId,VorherigeAkteId=before,PruefAbsenderId=p.AbsenderId,VorherigerAbsenderId=alterAbsender,Text=p.Entscheidung+": "+p.Grund.Trim(),Benutzer=(await db.Benutzer.SingleAsync(u=>u.Id==actor)).Anzeigename,Zeitpunkt=clock.GetUtcNow()});
        db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion="Eingangsprüfung bestätigt",Ressource=e.Id});
        try{await db.SaveChangesAsync();await tx.CommitAsync();}catch(DbUpdateConcurrencyException){throw new Arbeitsfehler("Eingang wurde gleichzeitig geändert.",409);}return await Detail(actor,e.Id);
    }
    // One explicit, versioned task template. Extraction patterns remain extraction-only.
    // Pure proposal: no writes, no inferred legal deadline and no automatic action.
    public async Task<Aufgabenregelvorschlag> Aufgabenregel(string actor,string id)
    {
        const string regel="versicherung-antwort-v1";
        var e=await Eingang(actor,id);
        var w=await db.Eingangsworkflows.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==e.Id);
        var d=await db.Dokumente.IgnoreQueryFilters().AsNoTracking().SingleAsync(x=>x.Id==e.DokumentId);
        var a=w is null||w.AngabenJson.Length==0?null:JsonSerializer.Deserialize<Eingangsangaben>(w.AngabenJson);
        var analyse=d.AnalyseJson is null?null:JsonSerializer.Deserialize<Dokumentanalyse>(d.AnalyseJson,Dokumentanalysator.Json);
        var feld=a?.Felder?.SingleOrDefault(x=>x.Name=="faelligAm");
        var akte=w?.AkteId is null?null:await db.Akten.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==w.AkteId);
        var absender=w?.AbsenderId is null?null:await db.Kontakte.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==w.AbsenderId&&!x.Archiviert);
        string? sperre=null;
        if(w?.GeprueftAm is null||w.Status is not ("Zugeordnet" or "Erledigt")||a?.Art!="Versicherungsschreiben")sperre="Versicherungsschreiben zuerst am Original prüfen und zuordnen.";
        else if(akte is null||akte.Status=="Archiviert"||absender is null||w.ZustaendigId is null)sperre="Aktive Akte, bestätigter Absender und Zuständigkeit fehlen.";
        else if(analyse?.Hash!=d.Hash||analyse.Felder.Any(x=>x.Status=="Widerspruch")||a.Dokumentdatum is null||a.FaelligAm is null||feld?.AbschnittId is null||feld.Status is not ("Bestätigt" or "Korrigiert")||feld.Wert!=a.FaelligAm.Value.ToString("yyyy-MM-dd")||!analyse.Abschnitte.Any(x=>x.Id==feld.AbschnittId))sperre="Eindeutige Analyse und bestätigte Fälligkeit mit Originalfundstelle fehlen. Keine Frist wird berechnet.";
        else if(!await aufgaben.RessourceErlaubt(w.ZustaendigId,"Dokument",d.Id)||!await aufgaben.RessourceErlaubt(w.ZustaendigId,"Akte",akte.Id)||!await aufgaben.RessourceErlaubt(w.ZustaendigId,"Kontakt",absender.Id))sperre="Zuständige Person ist für die Quelle nicht mehr freigegeben.";
        var bezug=sperre is not null?null:Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{regel,e.Id,w!.Version,d.Hash,Dokumentversion=d.Version,Aktenversion=akte!.Version,Absenderversion=absender!.Version,w.ZustaendigId,feld}))));
        var aufgabe=sperre is null?new Aufgabeneingabe("Versicherungsschreiben prüfen und Antwort vorbereiten","Genannten Termin und benötigte Unterlagen anhand des Originals prüfen. Kein automatischer Versand.",w!.ZustaendigId!,"Normal","Offen",a!.FaelligAm,null):null;
        return new(regel,sperre,bezug,w?.Version??0,d.Hash,w?.AkteId,akte?.Titel,w?.AbsenderId,w?.ZustaendigId,feld?.AbschnittId,aufgabe);
    }
    public async Task<object> Folgeaufgabe(string actor,string id,Eingangsaufgabe p,IDokumentdateien? dateien=null)
    {
        try{
        if(p.Aufgabe is null)throw new Arbeitsfehler("Bitte Angaben zur Folgeaufgabe prüfen.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);var e=await Eingang(actor,id);var existing=await db.Eingangsworkflows.SingleOrDefaultAsync(w=>w.Id==e.Id);
        if(existing?.AkteId is not null&&(!await aufgaben.RessourceErlaubt(actor,"Akte",existing.AkteId)||!await db.Akten.AnyAsync(a=>a.Id==existing.AkteId&&a.Status!="Archiviert")))throw new Arbeitsfehler("Zielakte ist nicht mehr aktiv oder freigegeben.",409);
        if(existing?.AbsenderId is not null&&!await db.Kontakte.AnyAsync(k=>k.Id==existing.AbsenderId&&!k.Archiviert))throw new Arbeitsfehler("Bestätigter Absender ist nicht mehr aktiv.",409);
        if(existing?.AufgabeId is not null){await aufgaben.Lesen(actor,existing.AufgabeId);return new{aufgabeId=existing.AufgabeId};}
        if(!p.Bestaetigt||p.Aufgabe.FaelligAm is not null&&!p.FristBestaetigt||existing?.GeprueftAm is null||existing.Status is not ("Zugeordnet" or "Erledigt"))throw new Arbeitsfehler("Eingang zuerst prüfen. Aufgabe und belegte Fälligkeit ausdrücklich bestätigen.");
        if((existing.AkteId is not null&&!await aufgaben.RessourceErlaubt(p.Aufgabe.ZustaendigId,"Akte",existing.AkteId))||(existing.AbsenderId is not null&&!await aufgaben.RessourceErlaubt(p.Aufgabe.ZustaendigId,"Kontakt",existing.AbsenderId)))throw new Arbeitsfehler("Zuständigkeit nicht freigegeben.",403);
        string? regeltext=null;
        if(p.Regelbezug is not null){
            var v=await Aufgabenregel(actor,e.Id);
            if(v.Sperrgrund is not null||v.Bezug!=p.Regelbezug||v.Version!=p.Version)throw new Arbeitsfehler("Regelvorschlag oder Quelle wurde geändert. Bitte erneut prüfen.",409);
            if(dateien is null)throw new Arbeitsfehler("Originalprüfung nicht verfügbar.",503);
            var original=await db.Dokumente.IgnoreQueryFilters().AsNoTracking().SingleAsync(d=>d.Id==e.DokumentId);await OriginalGeprueft(original,dateien,default);
            regeltext=$"Regel {v.Regel}; Eingang {e.Id}; Original SHA256 {v.DokumentHash}; Fundstelle {v.AbschnittId}; Akte {v.AkteId}.";
        }
        var beschreibung=p.Aufgabe.Beschreibung;
        if(regeltext is not null){if(beschreibung is null||beschreibung.Length>3000)throw new Arbeitsfehler("Hinweise auf höchstens 3000 Zeichen begrenzen.");beschreibung=beschreibung.Trim()+"\n"+regeltext;}
        var w=await Stand(actor,e.Id,p.Version);var task=await aufgaben.Speichern(actor,null,p.Aufgabe with{Beschreibung=beschreibung,BezugArt="Dokument",BezugId=e.DokumentId,FirmaId=null,Version=0,Archiviert=false,Status="Offen"});w.AufgabeId=task.Id;
        db.Dokumentaktivitaeten.Add(new(){DokumentId=e.DokumentId,EingangsId=e.Id,AkteId=w.AkteId,PruefAbsenderId=w.AbsenderId,Text="Folgeaufgabe ausdrücklich bestätigt"+(regeltext is null?"":"; "+regeltext),Benutzer=(await db.Benutzer.SingleAsync(u=>u.Id==actor)).Anzeigename,Zeitpunkt=clock.GetUtcNow()});
        try{await db.SaveChangesAsync();await tx.CommitAsync();}catch(DbUpdateConcurrencyException){throw new Arbeitsfehler("Folgeaufgabe wurde gleichzeitig angelegt. Eingang neu laden.",409);}return new{aufgabeId=task.Id};
        }catch(Exception ex)when(Serialisierungskonflikt(ex)){throw new Arbeitsfehler("Vorgang wurde gleichzeitig geändert. Bitte neu laden und erneut prüfen.",409);}
    }
    private static bool Serialisierungskonflikt(Exception ex)
    {
        // Npgsql's non-retrying execution strategy wraps transient serialization failures.
        for(Exception? cause=ex;cause is not null;cause=cause.InnerException)
            if(cause is Npgsql.PostgresException {SqlState:"40001" or "40P01"})return true;
        return false;
    }
    public static void Endpunkte(WebApplication app,IArbeitsrechte? rechte=null,Arbeitsmodule? module=null)
    {
        var g=app.MapGroup("/api/posteingang").RequireAuthorization();
        g.AddEndpointFilter(async(c,next)=>{try{if(module?.Aktiv("Akten")==false||(c.HttpContext.Request.Method!="GET"&&rechte?.Darf("Akten.Verwalten")==false))throw new Arbeitsfehler("Bearbeitung nicht freigegeben.",403);return await next(c);}catch(Arbeitsfehler e){return Results.Json(new ApiFehler(e.Message),statusCode:e.Status);}catch(DbUpdateException){return Results.Conflict(new ApiFehler("Eingang wurde geändert. Bitte neu laden."));}});
        static string Actor(HttpContext c)=>c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        static async Task ScannerAdmin(HttpContext c,FirmenDbContext db){var actor=Actor(c);if(!await db.Benutzer.AnyAsync(u=>u.Id==actor&&u.Aktiv&&u.Rolle=="Administrator"))throw new Arbeitsfehler("Scanquellen sind Administratoren vorbehalten.",403);}
        g.MapGet("/scanner",async(HttpContext c,FirmenDbContext db)=>{await ScannerAdmin(c,db);return Results.Json(await new WindowsScanner().Geraete(c.RequestAborted));});
        g.MapGet("/scanordner",async(HttpContext c,FirmenDbContext db,IServiceProvider services)=>{await ScannerAdmin(c,db);var source=services.GetService<PersistenterScanordner>();if(source is null)return Results.Json(new{aktiv=false,status="Persistenter Scanordner nicht eingerichtet.",anzahl=0,auftraege=Array.Empty<object>()});var page=FirmenDaten.Seite(int.TryParse(c.Request.Query["seite"],out var n)?n:0);var q=db.Scanauftraege.AsNoTracking().Where(j=>j.QuelleId==source.QuelleId);return Results.Json(new{aktiv=true,status=source.Status,anzahl=await q.CountAsync(),auftraege=await q.OrderByDescending(j=>j.GeaendertAm).ThenBy(j=>j.Id).Skip(page*20).Take(20).Select(j=>new{j.Id,j.Dateiname,j.Status,j.Fehlercode,j.Versuche,j.Version,j.DokumentId}).ToArrayAsync()});});
        g.MapPost("/scanauftraege/{id}/wiederholen",async(string id,Scanwiederholung p,HttpContext c,FirmenDbContext db,IServiceProvider services)=>{
            await ScannerAdmin(c,db);var source=services.GetService<PersistenterScanordner>()??throw new Arbeitsfehler("Scanquelle nicht eingerichtet.",503);var j=await db.Scanauftraege.SingleOrDefaultAsync(j=>j.Id==id&&j.QuelleId==source.QuelleId)??throw new Arbeitsfehler("Auftrag nicht verfügbar.",404);
            if(!p.Bestaetigt||j.Status is not ("Blockiert" or "Fehler"))throw new Arbeitsfehler("Nur fehlgeschlagene Aufträge nach Ursachenprüfung ausdrücklich erneut freigeben.");if(j.Version!=p.Version)throw new Arbeitsfehler("Auftrag wurde geändert.",409);j.Status="Wartend";j.Versuche=0;j.LeasingBis=null;j.LeaseId=null;j.Fehlercode=null;db.Sicherheitsereignisse.Add(new(){BenutzerId=Actor(c),Aktion="Scanauftrag erneut freigegeben",Ressource=j.Id});await db.SaveChangesAsync();return Results.Ok(new{meldung="Wiederholung vorgemerkt. Sicherheitsgate bleibt vorgeschaltet."});
        });
        if(app.Environment.IsDevelopment()){
            g.MapGet("/beispiele",()=>SynthetischeEingaenge.Schluessel);
            g.MapGet("/beispiele/{fall}",(string fall)=>{var f=SynthetischeEingaenge.Datei(fall);return Results.File(f.Bytes,f.Name.EndsWith(".pdf")?"application/pdf":"text/plain",f.Name);});
            g.MapPost("/beispiele/{fall}",async(string fall,FirmenDbContext db,IDokumentdateien dateien)=>{var f=SynthetischeEingaenge.Datei(fall);var d=new FirmenDaten(db,new Pilotoptionen(true,null,null),new SynthetischeEingaenge.NurKatalog());var doc=await d.DokumentHinzufuegen(f.Bytes,f.Name,f.Name.EndsWith(".pdf")?"application/pdf":"text/plain",null,dateien);return Results.Json(new{dokumentId=doc.Id});});
        }
        g.MapGet("/",async(HttpContext c,EingangsDienst d)=>Results.Json(await d.Liste(Actor(c),c.Request.Query["sicht"].FirstOrDefault()??"pruefen",c.Request.Query["text"].ToString(),int.TryParse(c.Request.Query["seite"],out var n)?n:0,c.Request.Query["akte"].FirstOrDefault())));
        g.MapGet("/uebersicht",async(HttpContext c,EingangsDienst d)=>Results.Json(await d.Uebersicht(Actor(c))));
        g.MapGet("/{id}",async(string id,HttpContext c,EingangsDienst d)=>Results.Json(await d.Detail(Actor(c),id,int.TryParse(c.Request.Query["verlaufSeite"],out var n)?n:0)));
        g.MapGet("/{id}/analyse",async(string id,HttpContext c,EingangsDienst d)=>Results.Json(await d.AnalyseLesen(Actor(c),id)));
        g.MapPost("/{id}/analyse",async(string id,Analysekontext kontext,HttpContext c,EingangsDienst d,[Microsoft.AspNetCore.Mvc.FromServices] Dokumentanalysator analyse,[Microsoft.AspNetCore.Mvc.FromServices] IDokumentdateien dateien,CancellationToken ct)=>Results.Json(await d.Analysieren(Actor(c),id,analyse,dateien,ct,kontext)));
        g.MapGet("/muster",async(HttpContext c,[Microsoft.AspNetCore.Mvc.FromServices] DokumentmusterDienst d)=>Results.Json(await d.Liste(Actor(c),new(c.Request.Query["akte"].FirstOrDefault(),c.Request.Query["absender"].FirstOrDefault()),int.TryParse(c.Request.Query["seite"],out var n)?n:0)));
        g.MapPost("/{id}/muster",async(string id,Musterentwurf p,HttpContext c,[Microsoft.AspNetCore.Mvc.FromServices] DokumentmusterDienst d)=>Results.Json(await d.Entwerfen(Actor(c),id,p.Feld)));
        g.MapPost("/muster/{id}",async(string id,Musterentscheidung p,HttpContext c,[Microsoft.AspNetCore.Mvc.FromServices] DokumentmusterDienst d)=>Results.Json(await d.Entscheiden(Actor(c),id,p)));
        g.MapGet("/{id}/seite/{seite:int}",async(string id,int seite,HttpContext c,EingangsDienst d,[Microsoft.AspNetCore.Mvc.FromServices] IDokumentdateien dateien,IServiceProvider services,CancellationToken ct)=>{
            var doc=await d.AnalyseDokument(Actor(c),id);var adapter=services.GetService<IPdfAnalyse>();
            var a=doc.AnalyseJson is null?null:JsonSerializer.Deserialize<Dokumentanalyse>(doc.AnalyseJson,Dokumentanalysator.Json);
            if(adapter is null||a is null||a.Hash!=doc.Hash||!a.Seiten.Any(s=>s.Nummer==seite))throw new Arbeitsfehler("Seite nicht verfügbar.",404);
            var bytes=await d.OriginalGeprueft(doc,dateien,ct);return Results.File(await adapter.Seite(bytes,seite,ct),"image/png");
        });
        g.MapPost("/{id}/pruefen",async(string id,Eingangspruefung p,HttpContext c,EingangsDienst d)=>Results.Json(await d.Pruefen(Actor(c),id,p)));
        g.MapGet("/{id}/aufgabenvorschlag",async(string id,HttpContext c,EingangsDienst d)=>Results.Json(await d.Aufgabenregel(Actor(c),id)));
        g.MapPost("/{id}/aufgabe",async(string id,Eingangsaufgabe p,HttpContext c,EingangsDienst d,[Microsoft.AspNetCore.Mvc.FromServices] IDokumentdateien dateien)=>Results.Json(await d.Folgeaufgabe(Actor(c),id,p,dateien)));
    }
    private async Task<DokumentDatensatz> AnalyseDokument(string actor,string id){var e=await Eingang(actor,id);return await db.Dokumente.IgnoreQueryFilters().SingleAsync(x=>x.Id==e.DokumentId);}
    private async Task<byte[]> OriginalGeprueft(DokumentDatensatz doc,IDokumentdateien dateien,CancellationToken ct){var bytes=await dateien.Lesen(doc.StorageKey,ct);if(bytes.Length!=doc.Groesse||Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))!=doc.Hash)throw new Arbeitsfehler("Originalintegrität konnte nicht bestätigt werden.",409);return bytes;}
    public async Task<object> AnalyseLesen(string actor,string id){var d=await AnalyseDokument(actor,id);var a=d.AnalyseJson is null?null:JsonSerializer.Deserialize<Dokumentanalyse>(d.AnalyseJson,Dokumentanalysator.Json);if(a is not null&&(a.OrganisationId is not null&&!await aufgaben.RessourceErlaubt(actor,"Kontakt",a.OrganisationId)||a.AbsenderId is not null&&!await aufgaben.RessourceErlaubt(actor,"Kontakt",a.AbsenderId)))a=a with{Felder=a.BasisFelder??a.Felder,MusterIds=[],OrganisationId=null,AbsenderId=null,AutomatikMusterId=null};return new{analyse=a};}
    public async Task<Analyseantwort> Analysieren(string actor,string id,Dokumentanalysator analyse,IDokumentdateien dateien,CancellationToken ct,Analysekontext? kontext=null){
        var d=await AnalyseDokument(actor,id);
        var bytes=await OriginalGeprueft(d,dateien,ct);
        var gespeichert=d.AnalyseJson is null?null:JsonSerializer.Deserialize<Dokumentanalyse>(d.AnalyseJson,Dokumentanalysator.Json);
        var basis=gespeichert?.Hash==d.Hash&&gespeichert.Version==analyse.Version?gespeichert:await analyse.Analysieren(bytes,d.Dateiname,d.Inhaltstyp,ct);
        var ergebnis=basis with{Felder=basis.BasisFelder??basis.Felder,MusterIds=[],AutomatikMusterId=null,OrganisationId=null,AbsenderId=null,Hinweise=basis.Hinweise.Where(h=>!h.StartsWith("Muster:",StringComparison.Ordinal)).ToArray()};
        if(kontext?.AkteId is null&&kontext?.AbsenderId is null){var e=await Eingang(actor,id);var w=await db.Eingangsworkflows.AsNoTracking().SingleOrDefaultAsync(w=>w.Id==e.Id);if(w?.AkteId is not null&&w.AbsenderId is not null)kontext=new(w.AkteId,w.AbsenderId);}
        if(kontext?.AkteId is not null||kontext?.AbsenderId is not null)ergebnis=await new DokumentmusterDienst(db,aufgaben,clock).Vorschlaege(actor,ergebnis,kontext);
        if(ergebnis.AutomatikMusterId is not null&&await db.Eingangsworkflows.AnyAsync(w=>w.GeprueftAm!=null&&db.Dokumentaktivitaeten.Any(e=>e.Id==w.Id&&e.DokumentId==d.Id)))ergebnis=ergebnis with{AutomatikMusterId=null,Hinweise=ergebnis.Hinweise.Append("Menschlich bestätigte Dokumente werden nicht automatisch umklassifiziert.").Distinct().ToArray()};
        var json=JsonSerializer.Serialize(ergebnis,Dokumentanalysator.Json);if(json.Length>500000)throw new Arbeitsfehler("Analyse überschreitet die Speichergrenze.",422);
        if(ergebnis.AutomatikMusterId is not null&&ergebnis.AutomatikMusterId!=gespeichert?.AutomatikMusterId){d.Art=ergebnis.Dokumentart;var e=await Eingang(actor,id);db.Dokumentaktivitaeten.Add(new(){DokumentId=d.Id,EingangsId=e.Id,AkteId=kontext!.AkteId,PruefAbsenderId=kontext.AbsenderId,Text="Automatische interne Klassifizierung; freigegebenes Muster "+ergebnis.AutomatikMusterId,Benutzer=(await db.Benutzer.SingleAsync(u=>u.Id==actor)).Anzeigename,Zeitpunkt=clock.GetUtcNow()});db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion="Dokument automatisch klassifiziert",Ressource=d.Id});}
        if(json!=d.AnalyseJson){d.AnalyseJson=json;await db.SaveChangesAsync(ct);}return new(ergebnis);
    }
}
