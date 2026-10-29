using System.Security.Claims;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Sicherheit;

public sealed class MfaRegel
{
    public int Id {get;set;}=1;
    public bool Administrator {get;set;}
    public bool Mitarbeiter {get;set;}
    public int Version {get;set;}=1;
}
public sealed record MfaRegelEingabe(bool Administrator,bool Mitarbeiter,int Version,MfaPruefung Pruefung);
public static class MfaRichtlinie
{
    public static async Task<bool> Erforderlich(FirmenDbContext db,Benutzerkonto u)=>u.PasswortMfaGebunden||u.MfaNeueinrichtung||await db.MfaRegeln.AnyAsync(r=>u.Rolle=="Administrator"?r.Administrator:r.Mitarbeiter);
    public static async Task<bool> Einrichtung(FirmenDbContext db,string id)
    {var u=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==id);return u.MfaGeheimnis is null&&await Erforderlich(db,u);}
    public static async Task<MfaRegel> Aendern(FirmenDbContext db,string actor,MfaRegelEingabe e,MfaDienst dienst,IPasswordHasher<Benutzerkonto> h)
    {
        var handelnder=await dienst.StarkPruefen(db,h,actor,e.Pruefung);
        var r=await db.MfaRegeln.SingleOrDefaultAsync();
        if((r?.Version??0)!=e.Version)throw new Arbeitsfehler("Die Richtlinie wurde geändert. Bitte neu laden.",409);
        if(e.Administrator&&!await db.Benutzer.AnyAsync(u=>u.Id==actor&&u.Aktiv&&u.Rolle=="Administrator"&&u.MfaGeheimnis!=null))throw new Arbeitsfehler("Bitte zuerst den eigenen Authenticator einrichten.",409);
        if(r is null){r=new();db.MfaRegeln.Add(r);}else r.Version++;
        r.Administrator=e.Administrator;r.Mitarbeiter=e.Mitarbeiter;
        db.Sicherheitsereignisse.Add(new(){BenutzerId=actor,Aktion="MFA-Rollenrichtlinie geändert",Ressource="MFA-Richtlinie"});
        handelnder.Version++;await db.SaveChangesAsync();return r;
    }
    public static void Endpunkte(WebApplication app)
    {
        var g=app.MapGroup("/api/admin/mfa").RequireAuthorization(p=>p.RequireRole("Administrator"));
        g.MapGet("/richtlinie",async(FirmenDbContext db)=>Results.Json(await db.MfaRegeln.AsNoTracking().SingleOrDefaultAsync()??new MfaRegel{Version=0}));
        g.MapPost("/richtlinie",async(MfaRegelEingabe e,HttpContext c,FirmenDbContext db,MfaDienst mfa,IPasswordHasher<Benutzerkonto> h)=>{
            try{var id=c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;var r=await Aendern(db,id,e,mfa,h);await MfaEndpunkte.Sitzung(c,await db.Benutzer.SingleAsync(x=>x.Id==id),true,true);return Results.Json(r);}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("mfa");
    }
}
