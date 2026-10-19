using System.Security.Claims;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Sicherheit;

public sealed record Pilotoptionen(bool Aktiv,string? Zertifikat,string? Zertifikatpasswort)
{
    public static Pilotoptionen Lesen(IConfiguration c)=>new(c.GetValue<bool>("FirmenZentrale:Security:Enabled"),c["FirmenZentrale:Security:CertificatePath"],c["FirmenZentrale:Security:CertificatePassword"]);
    public override string ToString()=>Aktiv?"Pilot authentication enabled":"Development authentication disabled";
}
public sealed class Ressourcenbereich(IHttpContextAccessor accessor,Pilotoptionen optionen)
{
    public bool Aktiv=>optionen.Aktiv&&accessor.HttpContext is not null;
    public string Benutzer=>accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)??"";
    public bool Admin=>accessor.HttpContext?.User.IsInRole("Administrator")==true;
}
public sealed class Benutzerkonto
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string Login {get;set;}="";
    public string Anzeigename {get;set;}="";
    public string PasswortHash {get;set;}="";
    public string Rolle {get;set;}="Mitarbeiter";
    public bool Aktiv {get;set;}=true;
    public string Sicherheitskennung {get;set;}=Guid.NewGuid().ToString("N");
    public int Fehlversuche {get;set;}
    public DateTimeOffset? GesperrtBis {get;set;}
    public DateTimeOffset ErstelltAm {get;set;}=DateTimeOffset.UtcNow;
    public DateTimeOffset GeaendertAm {get;set;}=DateTimeOffset.UtcNow;
    public int Version {get;set;}=1;
}
public sealed class Ressourcenfreigabe
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string BenutzerId {get;set;}="";
    public string? KontaktId {get;set;}
    public string? AkteId {get;set;}
    public string? DokumentId {get;set;}
}
public sealed class Sicherheitsereignis
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string BenutzerId {get;set;}="";
    public string Aktion {get;set;}="";
    public string Ressource {get;set;}="";
    public DateTimeOffset Zeitpunkt {get;set;}=DateTimeOffset.UtcNow;
}
