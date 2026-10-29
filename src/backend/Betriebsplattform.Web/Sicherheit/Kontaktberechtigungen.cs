using System.Security.Claims;
using System.Text.Json;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Sicherheit;

public static class Berechtigungskatalog
{
    public static readonly IReadOnlyDictionary<string,string[]> Bereiche=new Dictionary<string,string[]>{["Kontakte"]=["Lesen","Erstellen","Bearbeiten","Archivieren"]};
    public static bool Auswerten(string bereich,string aktion,string? basis,string abweichungen)
    {
        if(!Bereiche.TryGetValue(bereich,out var aktionen)||!aktionen.Contains(aktion))return false;
        try{var a=JsonSerializer.Deserialize<Dictionary<string,bool>>(abweichungen);if(a?.TryGetValue(aktion,out var wert)==true)return wert;
            return basis is not null&&JsonSerializer.Deserialize<Dictionary<string,bool>>(basis)?.GetValueOrDefault(aktion)==true;
        }catch(JsonException){return false;}
    }
}
public sealed class Rechtevorlagenversion
{
    public string Id {get;set;}="";
    public string Bereich {get;set;}="Kontakte";
    public string Name {get;set;}="";
    public int Version {get;set;}=1;
    public string RechteJson {get;set;}="{}";
}
public sealed class BenutzerRechteprofil
{
    public string BenutzerId {get;set;}="";
    public string Bereich {get;set;}="Kontakte";
    public string? VorlageId {get;set;}
    public string Modus {get;set;}="Explizit";
    public string AbweichungenJson {get;set;}="{}";
    public int Version {get;set;}=1;
}
public sealed class Berechtigungsaenderung
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string AkteurId {get;set;}="";
    public string ZielId {get;set;}="";
    public string Bereich {get;set;}="Kontakte";
    public DateTimeOffset Zeitpunkt {get;set;}=DateTimeOffset.UtcNow;
    public string Vorher {get;set;}="";
    public string Nachher {get;set;}="";
}
public sealed record KontaktrechteEingabe(int Version,string? VorlageId,Dictionary<string,bool>? Abweichungen,bool BestandErsetzen,MfaPruefung? Pruefung)
{public override string ToString()=>nameof(KontaktrechteEingabe);}
public sealed record Rechteaktion(string Id,string Titel,bool Basis,bool? Abweichung,bool Erlaubt);
public sealed record Vorlagenansicht(string Id,string Name,int Version,Dictionary<string,bool> Rechte);
public sealed record KontaktrechteAnsicht(int Version,string Modus,string? VorlageId,Rechteaktion[] Aktionen,Vorlagenansicht[] Vorlagen);

public static class Kontaktberechtigungen
{
    public static string[] Aktionen=>Berechtigungskatalog.Bereiche["Kontakte"];
    public static Rechtevorlagenversion[] Standardvorlagen()=>[
        new(){Id="buero:1",Name="Büromitarbeiter",RechteJson="{\"Lesen\":true,\"Erstellen\":true,\"Bearbeiten\":true,\"Archivieren\":false}"},
        new(){Id="teamleitung:1",Name="Teamleitung",RechteJson="{\"Lesen\":true,\"Erstellen\":true,\"Bearbeiten\":true,\"Archivieren\":true}"}];
    public static bool Auswerten(string aktion,string? basis,string abweichungen)=>Berechtigungskatalog.Auswerten("Kontakte",aktion,basis,abweichungen);
    public static async Task<bool> Erlaubt(FirmenDbContext db,string actor,string aktion,CancellationToken ct=default)
    {
        if(!Aktionen.Contains(aktion))return false;
        var u=await db.Benutzer.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor&&x.Aktiv&&x.Eingerichtet,ct);
        if(u is null)return false;if(u.Rolle=="Administrator")return true;
        var p=await db.BenutzerRechteprofile.AsNoTracking().SingleOrDefaultAsync(x=>x.BenutzerId==actor&&x.Bereich=="Kontakte",ct);
        if(p is null)return false;
        var basis=p.VorlageId is null?null:await db.Rechtevorlagenversionen.Where(x=>x.Id==p.VorlageId&&x.Bereich=="Kontakte").Select(x=>x.RechteJson).SingleOrDefaultAsync(ct);
        return Auswerten(aktion,basis,p.AbweichungenJson)&&(aktion=="Lesen"||Auswerten("Lesen",basis,p.AbweichungenJson));
    }
    public static async Task Pruefen(FirmenDbContext db,string actor,string aktion,CancellationToken ct=default)
    {if(!await Erlaubt(db,actor,aktion,ct))throw new Arbeitsfehler("Für diese Kontaktaktion fehlt die Berechtigung.",403);}
    public static async Task<bool> Anfrage(HttpContext c,FirmenDbContext db)
    {
        var seg=(c.Request.Path.Value??"").Split('/',StringSplitOptions.RemoveEmptyEntries);
        string? aktion=null;
        if(seg.Length>=2&&seg[0]=="api"&&seg[1]=="kontakte")aktion=c.Request.Method is "GET" or "HEAD"?"Lesen":c.Request.Method=="POST"?seg.Length==2?"Erstellen":seg.Length==3?"Bearbeiten":seg.Length==4&&seg[3]=="archivieren"?"Archivieren":"Unbekannt":"Unbekannt";
        if(c.Request.Path.StartsWithSegments("/api/kontaktvorschlaege"))aktion="Bearbeiten";
        if(aktion is null)return true;
        if(await Erlaubt(db,c.User.FindFirstValue(ClaimTypes.NameIdentifier)??"",aktion,c.RequestAborted))return true;
        c.Response.StatusCode=403;await c.Response.WriteAsJsonAsync(new ApiFehler("Für diese Kontaktaktion fehlt die Berechtigung."));return false;
    }
    public static async Task<KontaktrechteAnsicht> Lesen(FirmenDbContext db,string ziel,CancellationToken ct=default)
    {
        if(!await db.Benutzer.AnyAsync(x=>x.Id==ziel&&x.Rolle=="Mitarbeiter",ct))throw new Arbeitsfehler("Bitte ein Mitarbeiterkonto auswählen.",400);
        var p=await db.BenutzerRechteprofile.AsNoTracking().SingleOrDefaultAsync(x=>x.BenutzerId==ziel&&x.Bereich=="Kontakte",ct);
        var vorlagen=await db.Rechtevorlagenversionen.AsNoTracking().Where(x=>x.Bereich=="Kontakte").OrderBy(x=>x.Name).ThenBy(x=>x.Version).ToArrayAsync(ct);
        var basis=vorlagen.SingleOrDefault(x=>x.Id==p?.VorlageId)?.RechteJson;
        var a=JsonSerializer.Deserialize<Dictionary<string,bool>>(p?.AbweichungenJson??"{}")!;
        return new(p?.Version??0,p?.Modus??"Ohne Freigabe",p?.VorlageId,Aktionen.Select(x=>new Rechteaktion(x,x,x=="Lesen"?Auswerten(x,basis,"{}"):Auswerten(x,basis,"{}")&&Auswerten("Lesen",basis,"{}"),a.TryGetValue(x,out var value)?value:null,Auswerten(x,basis,p?.AbweichungenJson??"{}")&&(x=="Lesen"||Auswerten("Lesen",basis,p?.AbweichungenJson??"{}")))).ToArray(),vorlagen.Select(x=>new Vorlagenansicht(x.Id,x.Name,x.Version,JsonSerializer.Deserialize<Dictionary<string,bool>>(x.RechteJson)!)).ToArray());
    }
    public static async Task<KontaktrechteAnsicht> Speichern(FirmenDbContext db,string actor,string ziel,KontaktrechteEingabe e,MfaDienst mfa,IPasswordHasher<Benutzerkonto> h)
    {
        var admin=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==actor&&x.Aktiv&&x.Eingerichtet&&x.Rolle=="Administrator");
        if(admin is null||actor==ziel)throw new Arbeitsfehler("Administratorberechtigung für ein anderes Mitarbeiterkonto erforderlich.",403);
        var vorher=await Lesen(db,ziel);
        if(vorher.Version!=e.Version)throw new Arbeitsfehler("Berechtigungen wurden geändert. Bitte neu laden.",409);
        if(vorher.Modus=="Bestand"&&!e.BestandErsetzen)throw new Arbeitsfehler("Bitte die bewusste Ablösung der bisherigen Kontaktrechte bestätigen.");
        if(e.Abweichungen is null||e.Abweichungen.Keys.Any(x=>!Aktionen.Contains(x))||e.Pruefung is null)throw new Arbeitsfehler("Bitte unterstützte Kontaktrechte und Sicherheitsbestätigung angeben.");
        var basis=e.VorlageId is null?null:await db.Rechtevorlagenversionen.Where(x=>x.Id==e.VorlageId&&x.Bereich=="Kontakte").Select(x=>x.RechteJson).SingleOrDefaultAsync();
        if(e.VorlageId is not null&&basis is null)throw new Arbeitsfehler("Unbekannte Vorlagenversion.");
        var json=JsonSerializer.Serialize(e.Abweichungen);
        if(!Auswerten("Lesen",basis,json)&&Aktionen.Where(x=>x!="Lesen").Any(x=>Auswerten(x,basis,json)))throw new Arbeitsfehler("Erstellen, Bearbeiten und Archivieren benötigen zusätzlich Lesen.");
        // Preserve committed MFA failure counters and one-use factor consumption even if
        // the following permission transaction conflicts. Never roll back a failed factor.
        await mfa.StarkPruefen(db,h,actor,e.Pruefung);
        await using var tx=await db.Database.BeginTransactionAsync();
        var p=await db.BenutzerRechteprofile.SingleOrDefaultAsync(x=>x.BenutzerId==ziel&&x.Bereich=="Kontakte");
        if((p?.Version??0)!=e.Version)throw new Arbeitsfehler("Berechtigungen wurden geändert. Bitte neu laden.",409);
        if(p is null){p=new(){BenutzerId=ziel};db.BenutzerRechteprofile.Add(p);}else p.Version++;
        p.VorlageId=e.VorlageId;p.AbweichungenJson=json;p.Modus="Explizit";admin.Version++;
        var nachher=new{p.Version,p.Modus,p.VorlageId,Abweichungen=e.Abweichungen,Wirksam=Aktionen.ToDictionary(x=>x,x=>Auswerten(x,basis,json))};
        db.Berechtigungsaenderungen.Add(new(){AkteurId=actor,ZielId=ziel,Vorher=JsonSerializer.Serialize(vorher),Nachher=JsonSerializer.Serialize(nachher)});
        db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion="Kontaktberechtigungen geändert",Ressource=ziel});
        await db.SaveChangesAsync();await tx.CommitAsync();return await Lesen(db,ziel);
    }
    public static void Endpunkte(WebApplication app)
    {
        var g=app.MapGroup("/api/admin/benutzer/{id}/kontaktrechte").RequireAuthorization(p=>p.RequireRole("Administrator"));
        g.MapGet("",async(string id,FirmenDbContext db,CancellationToken ct)=>{try{return Results.Json(await Lesen(db,id,ct));}catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}});
        g.MapPost("",async(string id,KontaktrechteEingabe e,HttpContext c,FirmenDbContext db,MfaDienst mfa,IPasswordHasher<Benutzerkonto> h)=>{
            try{var actor=c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;var r=await Speichern(db,actor,id,e,mfa,h);await MfaEndpunkte.Sitzung(c,await db.Benutzer.SingleAsync(x=>x.Id==actor),true,true);return Results.Json(r);}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}catch(DbUpdateException){return Results.Conflict(new ApiFehler("Berechtigungen wurden gleichzeitig geändert. Bitte neu laden."));}
        }).RequireRateLimiting("mfa");
    }
}
