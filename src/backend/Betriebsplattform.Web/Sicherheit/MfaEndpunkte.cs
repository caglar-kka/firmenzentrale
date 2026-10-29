using System.Security.Claims;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Sicherheit;

public static class MfaEndpunkte
{
    internal static async Task Sitzung(HttpContext c,Benutzerkonto u,bool erneuern=false,bool mfaGeprueft=false,string? schritt=null)
    {
        var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,u.Id),new(ClaimTypes.Name,u.Anzeigename),new(ClaimTypes.Role,u.Rolle),new("stamp",u.Sicherheitskennung)};
        if(u.MfaGeheimnis is not null){if(!mfaGeprueft)throw new Arbeitsfehler("Bitte mit beiden Faktoren erneut anmelden.",401);claims.Add(new("mfa","true"));}
        if(schritt is not null)claims.Add(new("mfa_schritt",schritt));
        var properties=erneuern?(await c.AuthenticateAsync()).Properties:null;
        await c.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),properties);
    }
    public static void Registrieren(WebApplication app)
    {
        var profil=app.MapGroup("/api/auth/mfa").RequireAuthorization();
        profil.MapGet("/status",async(HttpContext c,FirmenDbContext db,MfaDienst mfa)=>{
            var id=c.User.FindFirstValue(ClaimTypes.NameIdentifier);var u=await db.Benutzer.AsNoTracking().SingleAsync(x=>x.Id==id);
            return Results.Json(new{pflicht=await MfaRichtlinie.Erforderlich(db,u),aktiv=u.MfaGeheimnis is not null,verbleibend=(u.MfaRecoveryHashes??"").Split(',',StringSplitOptions.RemoveEmptyEntries).Length,wiederherstellungGeprueft=mfa.HatSchritt(u,c.User.FindFirstValue("mfa_schritt"))});
        });
        profil.MapPost("/vorbereiten",async(MfaPruefung e,HttpContext c,FirmenDbContext db,IPasswordHasher<Benutzerkonto> h,MfaDienst mfa)=>{
            if(!Erstzugang.SichererTransport(c))return Results.BadRequest();
            try{return Results.Json(await mfa.Vorbereiten(db,h,c.User.FindFirstValue(ClaimTypes.NameIdentifier)!,e));}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("mfa");
        foreach(var aktion in new[]{"aktivieren","codes","deaktivieren"}){
            profil.MapPost("/"+aktion,async(MfaPruefung e,HttpContext c,FirmenDbContext db,IPasswordHasher<Benutzerkonto> h,MfaDienst mfa)=>{
                if(!Erstzugang.SichererTransport(c))return Results.BadRequest();
                try{var result=await mfa.Verwalten(db,h,c.User.FindFirstValue(ClaimTypes.NameIdentifier)!,e,aktion,c.User.FindFirstValue("mfa_schritt"));await Sitzung(c,result.Konto,true,true);return Results.Json(result.Codes);}
                catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
            }).RequireRateLimiting("mfa");
        }
        app.MapPost("/api/auth/mfa-anmelden",async(MfaPruefung e,HttpContext c,FirmenDbContext db,MfaDienst mfa)=>{
            if(!Erstzugang.SichererTransport(c))return Results.BadRequest();
            try{var result=await mfa.Anmelden(db,c.Request.Cookies[MfaDienst.Cookie],e);MfaDienst.CookieLoeschen(c);await Sitzung(c,result.Konto,false,true,result.Schritt);return Results.Ok();}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("mfa");
    }
}
