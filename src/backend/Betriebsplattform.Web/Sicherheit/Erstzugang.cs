using System.Security.Cryptography;
using System.Text;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Sicherheit;

public sealed record EinladungEingabe(string? Login,string? Anzeigename,string? Rolle);
public sealed record EinladungAenderung(int Version,bool Widerrufen);
public sealed record WiederherstellungAenderung(int Version,bool Widerrufen,bool IdentitaetGeprueft);
public sealed record EinrichtungEingabe(string? Code,string? Passwort)
{
    public override string ToString()=>nameof(EinrichtungEingabe);
}
public sealed record EinladungAusgabe(string Id,string Code,DateTimeOffset GueltigBis)
{
    public override string ToString()=>nameof(EinladungAusgabe);
}

public static class Erstzugang
{
    private static Arbeitsfehler Ungueltig(bool wiederherstellung=false)=>new(wiederherstellung?"Wiederherstellungscode ungültig oder abgelaufen. Bitte einen neuen Code anfordern.":"Erstzugang ungültig oder abgelaufen. Bitte eine neue Einladung anfordern.");
    public static bool SichererTransport(HttpContext c)=>c.Request.IsHttps ||
        (c.Connection.RemoteIpAddress is {} ip&&System.Net.IPAddress.IsLoopback(ip));
    private static string Hash(string code)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    internal static EinladungAusgabe Ausstellen(Benutzerkonto u,bool wiederherstellung=false)
    {
        var code=u.Id+"."+Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var bis=DateTimeOffset.UtcNow.AddMinutes(wiederherstellung?60:1440);
        if(wiederherstellung){u.WiederherstellungHash=Hash(code);u.WiederherstellungBis=bis;}
        else{u.EinladungHash=Hash(code);u.EinladungBis=bis;u.Sicherheitskennung=Guid.NewGuid().ToString("N");}
        u.GeaendertAm=DateTimeOffset.UtcNow;u.Version++;
        return new(u.Id,code,bis);
    }
    public static async Task<EinladungAusgabe> Anlegen(FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher,EinladungEingabe e,string administrator)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        // Reuse existing validation and hasher. Nobody receives the random placeholder password.
        var u=await Anmeldung.Anlegen(db,hasher,new(e.Login,e.Anzeigename,Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),e.Rolle));
        u.Eingerichtet=false;var ausgabe=Ausstellen(u);
        db.Sicherheitsereignisse.Add(new(){BenutzerId=administrator,Aktion="Benutzer mit Erstzugang angelegt",Ressource=u.Id});
        await db.SaveChangesAsync();await tx.CommitAsync();return ausgabe;
    }
    public static async Task<EinladungAusgabe?> Aendern(FirmenDbContext db,string id,EinladungAenderung e,string administrator)
    {
        var u=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==id);
        if(u is null||!u.Aktiv||u.Eingerichtet)throw new Arbeitsfehler("Erstzugang nur für aktive, noch nicht eingerichtete Konten möglich.");
        if(u.Version!=e.Version)throw new Arbeitsfehler("Benutzer wurde zwischenzeitlich geändert. Bitte neu laden.",409);
        EinladungAusgabe? ausgabe=null;
        if(e.Widerrufen){u.EinladungHash=null;u.EinladungBis=null;u.Version++;u.GeaendertAm=DateTimeOffset.UtcNow;}
        else ausgabe=Ausstellen(u);
        db.Sicherheitsereignisse.Add(new(){BenutzerId=administrator,Aktion=e.Widerrufen?"Erstzugang widerrufen":"Erstzugang neu ausgestellt",Ressource=id});
        await db.SaveChangesAsync();return ausgabe;
    }
    public static async Task<EinladungAusgabe?> WiederherstellungAendern(FirmenDbContext db,string id,WiederherstellungAenderung e,string administrator)
    {
        if(!e.Widerrufen&&!e.IdentitaetGeprueft)throw new Arbeitsfehler("Bitte zuerst die Identität der Person betrieblich prüfen und bestätigen.");
        var u=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==id);
        if(u is null||!u.Aktiv||!u.Eingerichtet)throw new Arbeitsfehler("Wiederherstellung nur für aktive, eingerichtete Konten möglich.");
        if(u.Version!=e.Version)throw new Arbeitsfehler("Benutzer wurde zwischenzeitlich geändert. Bitte neu laden.",409);
        EinladungAusgabe? ausgabe=null;
        if(e.Widerrufen){u.WiederherstellungHash=null;u.WiederherstellungBis=null;u.Version++;u.GeaendertAm=DateTimeOffset.UtcNow;}
        else ausgabe=Ausstellen(u,true);
        db.Sicherheitsereignisse.Add(new(){BenutzerId=administrator,Aktion=e.Widerrufen?"Passwortwiederherstellung widerrufen":"Passwortwiederherstellung nach Identitätsprüfung freigegeben",Ressource=id});
        await db.SaveChangesAsync();return ausgabe;
    }
    public static async Task Einloesen(FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher,EinrichtungEingabe e,bool wiederherstellung=false)
    {
        if(e.Code is not {Length:97}||e.Code[32]!='.'||!e.Code[..32].All(char.IsAsciiHexDigit)||!e.Code[33..].All(char.IsAsciiHexDigit))throw Ungueltig(wiederherstellung);
        if(!Anmeldung.PasswortGueltig(e.Passwort))throw new Arbeitsfehler("Bitte ein Passwort mit 15 bis 128 Zeichen eingeben.");
        var id=e.Code[..32];var u=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==id);
        var hash=wiederherstellung?u?.WiederherstellungHash:u?.EinladungHash;var bis=wiederherstellung?u?.WiederherstellungBis:u?.EinladungBis;
        if(u is null||!u.Aktiv||u.Eingerichtet!=wiederherstellung||bis is null||bis<=DateTimeOffset.UtcNow||hash is null||
            !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hash),Encoding.ASCII.GetBytes(Hash(e.Code))))throw Ungueltig(wiederherstellung);
        u.PasswortHash=hasher.HashPassword(u,e.Passwort!);u.PasswortMfaGebunden=false;u.Eingerichtet=true;u.EinladungHash=null;u.EinladungBis=null;
        u.WiederherstellungHash=null;u.WiederherstellungBis=null;MfaDienst.VorbereitungenWiderrufen(u);
        u.Fehlversuche=0;u.GesperrtBis=null;u.Sicherheitskennung=Guid.NewGuid().ToString("N");u.Version++;u.GeaendertAm=DateTimeOffset.UtcNow;
        db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Aktion=wiederherstellung?"Passwort wiederhergestellt":"Erstzugang eingerichtet",Ressource=u.Id});
        // EF's version predicate makes password, consumption and audit one atomic SaveChanges.
        // Two contexts using the same version cannot both commit, including reissue/deactivation races.
        try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){throw Ungueltig(wiederherstellung);}
    }
}
