using Betriebsplattform.Web.Persistenz;
namespace Betriebsplattform.Web.Aufgaben;
public sealed class Aufgabe : VersionierterDatensatz
{
    public string Betreff {get;set;}="";
    public string Beschreibung {get;set;}="";
    public string ErstellerId {get;set;}="";
    public string ZustaendigId {get;set;}="";
    public string Prioritaet {get;set;}="Normal";
    public string Status {get;set;}="Offen";
    public DateOnly? FaelligAm {get;set;}
    public TimeOnly? FaelligUm {get;set;}
    public DateTimeOffset? FaelligUtc {get;set;}
    public bool Archiviert {get;set;}
    public string? FirmaId {get;set;}
    public string? RechnungId {get;set;}
    public string? AkteId {get;set;}
    public string? KontaktId {get;set;}
    public string? DokumentId {get;set;}
}
public sealed class Aufgabenverlauf
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string AufgabeId {get;set;}="";
    public string BenutzerId {get;set;}="";
    public DateTimeOffset Zeitpunkt {get;set;}
    public string Vorher {get;set;}="";
    public string Nachher {get;set;}="";
}
public sealed record Aufgabeneingabe(string Betreff,string Beschreibung,string ZustaendigId,string Prioritaet,string Status,DateOnly? FaelligAm,TimeOnly? FaelligUm,int Version=0,bool Archiviert=false,string? FirmaId=null,string? BezugArt=null,string? BezugId=null);
