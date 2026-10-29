using System.Security.Cryptography;
using System.Text;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Sicherheit;

public sealed record MfaPruefung(string? Passwort,string? Code,bool Wiederherstellung=false)
{public override string ToString()=>nameof(MfaPruefung);}
public sealed record MfaEinrichtung(string Schluessel,DateTimeOffset GueltigBis)
{public override string ToString()=>nameof(MfaEinrichtung);}
public sealed record MfaCodes(string[] Codes)
{public override string ToString()=>nameof(MfaCodes);}
public sealed record MfaAbschluss(Benutzerkonto Konto,string? Schritt)
{public override string ToString()=>nameof(MfaAbschluss);}

public sealed class MfaDienst(IDataProtectionProvider protection,TimeProvider clock)
{
    public const string Cookie="__Host-FZ-MFA";
    private DateTimeOffset Now=>clock.GetUtcNow();
    private IDataProtector Protector(string id)=>protection.CreateProtector("FirmenZentrale.MFA.v1",id);
    private static Arbeitsfehler Fehler()=>new("Sicherheitsprüfung nicht möglich. Angaben prüfen oder später erneut versuchen.");
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool Gleich(string? a,string b)=>a is not null&&a.Length==b.Length&&CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a),Encoding.ASCII.GetBytes(b));
    private byte[] Schluessel(string id,string value)
    {
        try{return Convert.FromBase64String(Protector(id).Unprotect(value));}
        catch(Exception e) when(e is CryptographicException or FormatException){throw new Arbeitsfehler("Sicherheitsprüfung derzeit nicht verfügbar. Bitte den Administrator informieren.",503);}
    }
    public static void VorbereitungenWiderrufen(Benutzerkonto u){u.MfaVorbereitung=null;u.MfaVorbereitungBis=null;u.MfaChallengeHash=null;u.MfaChallengeBis=null;u.MfaStepupHash=null;u.MfaStepupBis=null;}
    public bool HatSchritt(Benutzerkonto u,string? schritt)=>schritt is not null&&schritt.Length==64&&u.MfaStepupBis>Now&&Gleich(u.MfaStepupHash,Hash(schritt));
    public static void CookieLoeschen(HttpContext c)=>c.Response.Cookies.Delete(Cookie,new CookieOptions{Path="/",Secure=true,HttpOnly=true,SameSite=SameSiteMode.Strict});
    public string AnmeldungVorbereiten(Benutzerkonto u)
    {
        var value=u.Id+"."+u.Sicherheitskennung+"."+Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        u.MfaChallengeHash=Hash(value);u.MfaChallengeBis=Now.AddMinutes(5);return value;
    }
    public static void CookieSetzen(HttpContext c,string value)=>c.Response.Cookies.Append(Cookie,value,new CookieOptions{Path="/",Secure=true,HttpOnly=true,SameSite=SameSiteMode.Strict,MaxAge=TimeSpan.FromMinutes(5),IsEssential=true});
    private async Task<Benutzerkonto> Konto(FirmenDbContext db,string id)
    {
        var u=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==id);
        if(u is null||!u.Aktiv||!u.Eingerichtet||u.GesperrtBis>Now||Betriebsplattform.Web.Betrieb.LokalerAdminErstzugang.Abgelaufen(u,Now))throw Fehler();return u;
    }
    private async Task Ablehnen(FirmenDbContext db,Benutzerkonto u)
    {
        // Count every failed attempt atomically, including attempts racing from different clients.
        var now=Now;var until=now.AddMinutes(15);await using var tx=await db.Database.BeginTransactionAsync();
        var changed=await db.Benutzer.Where(x=>x.Id==u.Id&&x.Aktiv&&x.Sicherheitskennung==u.Sicherheitskennung&&(x.GesperrtBis==null||x.GesperrtBis<=now))
            .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.MfaFehlversuche,x=>x.MfaFehlversuche+1>=5?0:x.MfaFehlversuche+1)
                .SetProperty(x=>x.GesperrtBis,x=>x.MfaFehlversuche+1>=5?(DateTimeOffset?)until:x.GesperrtBis)
                .SetProperty(x=>x.MfaChallengeHash,x=>x.MfaFehlversuche+1>=5?null:x.MfaChallengeHash)
                .SetProperty(x=>x.MfaChallengeBis,x=>x.MfaFehlversuche+1>=5?null:x.MfaChallengeBis)
                .SetProperty(x=>x.Version,x=>x.Version+1));
        if(changed==1){await db.Entry(u).ReloadAsync();db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Aktion="Zwei-Faktor-Prüfung abgelehnt",Ressource=u.Id});await db.SaveChangesAsync();}
        await tx.CommitAsync();throw Fehler();
    }
    private async Task Passwort(FirmenDbContext db,Benutzerkonto u,IPasswordHasher<Benutzerkonto> h,string? value)
    {if(value is not {Length:>0 and <=128}||h.VerifyHashedPassword(u,u.PasswortHash,value)==PasswordVerificationResult.Failed)await Ablehnen(db,u);}
    private static async Task Speichern(FirmenDbContext db)
    {try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){throw new Arbeitsfehler("Sicherheitsangaben wurden geändert oder bereits verwendet. Bitte erneut versuchen.",409);}}
    private void Ereignis(FirmenDbContext db,Benutzerkonto u,string aktion,bool neueKennung=false)
    {u.MfaFehlversuche=0;u.Fehlversuche=0;u.GesperrtBis=null;u.GeaendertAm=Now;u.Version++;if(neueKennung)u.Sicherheitskennung=Guid.NewGuid().ToString("N");db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Aktion=aktion,Ressource=u.Id});}
    private bool AppCode(Benutzerkonto u,string encrypted,string? code)
    {
        if(code is not {Length:6}||!code.All(char.IsAsciiDigit))return false;
        var key=Schluessel(u.Id,encrypted);var current=Now.ToUnixTimeSeconds()/30;
        // Highest matching period wins. A previous or already accepted period can never be reused.
        for(var period=current+1;period>=Math.Max(0,current-1);period--)if(period>u.MfaLetztePeriode&&Gleich(Totp.Code(key,period),code)){u.MfaLetztePeriode=period;return true;}
        return false;
    }
    private bool Faktor(Benutzerkonto u,MfaPruefung e)
    {
        if(u.MfaGeheimnis is null)return false;
        if(!e.Wiederherstellung)return AppCode(u,u.MfaGeheimnis,e.Code);
        var code=(e.Code??"").Replace("-","").Replace(" ","").ToUpperInvariant();
        if(code.Length!=32||!code.All(char.IsAsciiHexDigit))return false;
        var hashes=(u.MfaRecoveryHashes??"").Split(',',StringSplitOptions.RemoveEmptyEntries).ToList();var i=hashes.FindIndex(x=>Gleich(x,Hash(u.Id+"|"+code)));if(i<0)return false;
        hashes.RemoveAt(i);u.MfaRecoveryHashes=string.Join(',',hashes);u.Sicherheitskennung=Guid.NewGuid().ToString("N");return true;
    }
    private static MfaCodes Codes(Benutzerkonto u)
    {
        var unique=new HashSet<string>(StringComparer.Ordinal);while(unique.Count<8)unique.Add(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));var codes=unique.ToArray();
        u.MfaRecoveryHashes=string.Join(',',codes.Select(c=>Hash(u.Id+"|"+c)));
        return new(codes.Select(c=>string.Join('-',Enumerable.Range(0,4).Select(i=>c.Substring(i*8,8)))).ToArray());
    }
    public async Task<MfaEinrichtung> Vorbereiten(FirmenDbContext db,IPasswordHasher<Benutzerkonto> h,string id,MfaPruefung e)
    {
        var u=await Konto(db,id);await Passwort(db,u,h,e.Passwort);if(u.MfaGeheimnis is not null)throw new Arbeitsfehler("Bitte die bestehende Einrichtung zuerst sicher deaktivieren.");
        var key=RandomNumberGenerator.GetBytes(20);u.MfaVorbereitung=Protector(u.Id).Protect(Convert.ToBase64String(key));u.MfaVorbereitungBis=Now.AddMinutes(10);u.MfaLetztePeriode=-1;
        Ereignis(db,u,"Authenticator-Einrichtung begonnen");await Speichern(db);return new(Totp.Base32(key),u.MfaVorbereitungBis.Value);
    }
    public async Task<Benutzerkonto> StarkPruefen(FirmenDbContext db,IPasswordHasher<Benutzerkonto> h,string id,MfaPruefung e)
    {
        var u=await Konto(db,id);if(u.Rolle!="Administrator"||u.MfaGeheimnis is null)throw new Arbeitsfehler("Hierfür zuerst den eigenen Authenticator einrichten.",403);
        await Passwort(db,u,h,e.Passwort);if(!Faktor(u,e))await Ablehnen(db,u);
        Ereignis(db,u,"Administrative Sicherheitsprüfung bestätigt");await Speichern(db);return u;
    }
    public async Task<(Benutzerkonto Konto,MfaCodes? Codes)> Verwalten(FirmenDbContext db,IPasswordHasher<Benutzerkonto> h,string id,MfaPruefung e,string aktion,string? schritt=null)
    {
        if(aktion is not ("aktivieren" or "codes" or "deaktivieren"))throw Fehler();
        var u=await Konto(db,id);await Passwort(db,u,h,e.Passwort);MfaCodes? ausgabe=null;
        if(aktion=="aktivieren"){
            if(u.MfaGeheimnis is not null||u.MfaVorbereitung is null||u.MfaVorbereitungBis<=Now||u.MfaVorbereitungBis is null)throw Fehler();
            if(!AppCode(u,u.MfaVorbereitung,e.Code))await Ablehnen(db,u);
            u.EinladungBis=null;u.MfaGeheimnis=u.MfaVorbereitung;u.MfaVorbereitung=null;u.MfaVorbereitungBis=null;u.MfaNeueinrichtung=false;ausgabe=Codes(u);
        }else{
            if(u.MfaGeheimnis is null)throw Fehler();
            if(!(string.IsNullOrEmpty(e.Code)?HatSchritt(u,schritt):Faktor(u,e)))await Ablehnen(db,u);
            if(aktion=="codes")ausgabe=Codes(u);
            else{if(await MfaRichtlinie.Erforderlich(db,u))throw new Arbeitsfehler("Für dieses Konto ist die Zwei-Faktor-Authentifizierung vorgeschrieben.",409);u.MfaGeheimnis=null;u.MfaRecoveryHashes=null;u.MfaLetztePeriode=-1;}
        }
        VorbereitungenWiderrufen(u);u.WiederherstellungHash=null;u.WiederherstellungBis=null;
        Ereignis(db,u,aktion=="aktivieren"?"Zwei-Faktor-Authentifizierung aktiviert":aktion=="codes"?"Wiederherstellungscodes erneuert":"Zwei-Faktor-Authentifizierung deaktiviert",true);
        await Speichern(db);return(u,ausgabe);
    }
    public async Task<MfaAbschluss> Anmelden(FirmenDbContext db,string? challenge,MfaPruefung e)
    {
        if(challenge is null||challenge.Length!=130)throw Fehler();var parts=challenge.Split('.');if(parts.Length!=3)throw Fehler();
        var u=await Konto(db,parts[0]);if(u.MfaGeheimnis is null||u.Sicherheitskennung!=parts[1]||u.MfaChallengeBis is null||u.MfaChallengeBis<=Now||!Gleich(u.MfaChallengeHash,Hash(challenge)))throw Fehler();
        if(!Faktor(u,e))await Ablehnen(db,u);
        string? schritt=null;if(e.Wiederherstellung){schritt=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));u.MfaStepupHash=Hash(schritt);u.MfaStepupBis=Now.AddMinutes(5);}
        u.MfaChallengeHash=null;u.MfaChallengeBis=null;Ereignis(db,u,e.Wiederherstellung?"Anmeldung mit Wiederherstellungscode":"Anmeldung mit Authenticator");await Speichern(db);return new(u,schritt);
    }
}
