using System.Security.Claims;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Sicherheit;

public sealed class MfaNotfallfreigabe
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string ZielId {get;set;}="";
    public string ZielKennung {get;set;}="";
    public int ZielVersion {get;set;}
    public string AntragstellerId {get;set;}="";
    public string? GenehmigerId {get;set;}
    public string? GenehmigerKennung {get;set;}
    public string Grund {get;set;}="";
    public DateTimeOffset Bis {get;set;}
    public DateTimeOffset? GenehmigtAm {get;set;}
    public DateTimeOffset? AusgefuehrtAm {get;set;}
    public int Version {get;set;}=1;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped,System.Text.Json.Serialization.JsonIgnore]
    public EinladungAusgabe? Zugang {get;set;}
}
public sealed record MfaNotfallantrag(string ZielId,string Grund,bool IdentitaetGeprueft,int ZielVersion,MfaPruefung Pruefung);
public sealed record MfaNotfallaktion(int Version,MfaPruefung Pruefung);
public static class MfaNotfall
{
    public static async Task<MfaNotfallfreigabe> Antrag(FirmenDbContext db,MfaDienst mfa,IPasswordHasher<Benutzerkonto> h,TimeProvider clock,string actor,MfaNotfallantrag e)
    {
        if(!e.IdentitaetGeprueft||string.IsNullOrWhiteSpace(e.Grund)||e.Grund.Trim().Length is <20 or >400)throw new Arbeitsfehler("Betriebliche Identitätsprüfung und einen nachvollziehbaren Grund bestätigen.");
        var u=await mfa.StarkPruefen(db,h,actor,e.Pruefung);
        var ziel=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==e.ZielId&&x.Aktiv&&x.Eingerichtet)??throw new Arbeitsfehler("Konto nicht verfügbar.",404);
        if(ziel.Id==actor)throw new Arbeitsfehler("Ein eigener Notfallreset ist nicht zulässig.",403);
        if(ziel.Version!=e.ZielVersion)throw new Arbeitsfehler("Konto wurde geändert. Bitte neu laden.",409);
        if(ziel.MfaGeheimnis is null)throw new Arbeitsfehler("Keine aktive Einrichtung vorhanden.",409);
        var a=new MfaNotfallfreigabe{ZielId=ziel.Id,ZielKennung=ziel.Sicherheitskennung,ZielVersion=ziel.Version,AntragstellerId=actor,Grund=e.Grund.Trim(),Bis=clock.GetUtcNow().AddMinutes(10)};db.MfaNotfaelle.Add(a);
        if(ziel.Rolle!="Administrator")Reset(db,ziel,a,actor,clock.GetUtcNow(),h);
        else db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion="MFA-Notfallfreigabe beantragt",Ressource=a.Id});
        u.Version++;await db.SaveChangesAsync();return a;
    }
    public static async Task<MfaNotfallfreigabe> Aktion(FirmenDbContext db,MfaDienst mfa,IPasswordHasher<Benutzerkonto> h,TimeProvider clock,string actor,string id,MfaNotfallaktion e,bool ausfuehren)
    {
        var u=await mfa.StarkPruefen(db,h,actor,e.Pruefung);var now=clock.GetUtcNow();
        var a=await db.MfaNotfaelle.SingleOrDefaultAsync(x=>x.Id==id)??throw new Arbeitsfehler("Freigabe nicht gefunden.",404);
        if(a.Version!=e.Version||a.Bis<=now||a.AusgefuehrtAm is not null)throw new Arbeitsfehler("Freigabe abgelaufen, geändert oder bereits verwendet.",409);
        var ziel=await db.Benutzer.SingleAsync(x=>x.Id==a.ZielId);
        if(!ziel.Aktiv||!ziel.Eingerichtet||ziel.Sicherheitskennung!=a.ZielKennung||ziel.Version!=a.ZielVersion||ziel.Rolle!="Administrator"||ziel.Id==actor)throw new Arbeitsfehler("Kontostand oder Freigabe ist nicht mehr gültig.",409);
        if(!ausfuehren){
            if(actor==a.AntragstellerId||a.GenehmigerId is not null)throw new Arbeitsfehler("Ein unabhängiger zweiter Administrator muss zustimmen.",403);
            a.GenehmigerId=actor;a.GenehmigerKennung=u.Sicherheitskennung;a.GenehmigtAm=now;
            db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion="MFA-Notfallfreigabe genehmigt",Ressource=a.Id});
        }else{
            if(actor!=a.AntragstellerId||a.GenehmigerId is null||a.GenehmigerId==actor||a.GenehmigerId==ziel.Id)throw new Arbeitsfehler("Unabhängige Freigabe fehlt.",403);
            var genehmiger=await db.Benutzer.SingleAsync(x=>x.Id==a.GenehmigerId);
            if(!genehmiger.Aktiv||genehmiger.Rolle!="Administrator"||genehmiger.MfaGeheimnis is null||genehmiger.Sicherheitskennung!=a.GenehmigerKennung)throw new Arbeitsfehler("Freigabe nicht mehr gültig.",409);
            genehmiger.Version++;Reset(db,ziel,a,actor,now,h);
        }
        a.Version++;u.Version++;await db.SaveChangesAsync();return a;
    }
    private static void Reset(FirmenDbContext db,Benutzerkonto ziel,MfaNotfallfreigabe a,string actor,DateTimeOffset now,IPasswordHasher<Benutzerkonto> h)
    {
        ziel.MfaGeheimnis=null;ziel.MfaRecoveryHashes=null;ziel.MfaFehlversuche=0;ziel.Fehlversuche=0;ziel.GesperrtBis=null;ziel.MfaLetztePeriode=-1;ziel.MfaNeueinrichtung=true;MfaDienst.VorbereitungenWiderrufen(ziel);
        ziel.Sicherheitskennung=Guid.NewGuid().ToString("N");ziel.WiederherstellungHash=null;ziel.WiederherstellungBis=null;ziel.GeaendertAm=now;ziel.Version++;a.AusgefuehrtAm=now;
        // A stolen old password must not win the race to enroll a new authenticator after reset.
        ziel.PasswortHash=h.HashPassword(ziel,Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        a.Zugang=Erstzugang.Ausstellen(ziel,true); // Existing one-use recovery, delivered personally; hash only in the DB.
        db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion="MFA-Notfallreset; Neueinrichtung erforderlich",Ressource=ziel.Id});
    }
    public static void Endpunkte(WebApplication app)
    {
        var g=app.MapGroup("/api/admin/mfa/notfaelle").RequireAuthorization(p=>p.RequireRole("Administrator"));
        g.MapGet("/",async(FirmenDbContext db)=>await db.MfaNotfaelle.AsNoTracking().OrderByDescending(x=>x.Bis).Take(50).Select(x=>new{x.Id,x.ZielId,x.AntragstellerId,x.GenehmigerId,x.Grund,x.Bis,x.AusgefuehrtAm,x.Version}).ToArrayAsync());
        g.MapPost("/",async(MfaNotfallantrag e,HttpContext c,FirmenDbContext db,MfaDienst m,IPasswordHasher<Benutzerkonto> h,TimeProvider clock)=>{
            try{var id=c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;var a=await Antrag(db,m,h,clock,id,e);await MfaEndpunkte.Sitzung(c,await db.Benutzer.SingleAsync(x=>x.Id==id),true,true);return Results.Json(new{a.Id,a.AusgefuehrtAm,zugang=a.Zugang});}catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("mfa");
        foreach(var ausfuehren in new[]{false,true})g.MapPost("/{id}/"+(ausfuehren?"ausfuehren":"genehmigen"),async(string id,MfaNotfallaktion e,HttpContext c,FirmenDbContext db,MfaDienst m,IPasswordHasher<Benutzerkonto> h,TimeProvider clock)=>{
            try{var actor=c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;var a=await Aktion(db,m,h,clock,actor,id,e,ausfuehren);await MfaEndpunkte.Sitzung(c,await db.Benutzer.SingleAsync(x=>x.Id==actor),true,true);return Results.Json(new{a.Id,a.AusgefuehrtAm,zugang=a.Zugang});}catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("mfa");
    }
}
