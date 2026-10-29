using Betriebsplattform.Web.Persistenz;

namespace Betriebsplattform.Web.Leistungen;

public sealed class Leistungsobjekt:VersionierterDatensatz {

 public string KundeId{get;set;}="";public string Name{get;set;}="";public string Anschrift{get;set;}="";public string BueroId{get;set;}="";public string? AkteId{get;set;}

 public string AnlageHash{get;set;}="";

 public bool Aktiv{get;set;}=true;public string Bemerkung{get;set;}="";

}

public sealed class Leistungsplan:VersionierterDatensatz {

 public string ObjektId{get;set;}="";public string Name{get;set;}="";public int Nummer{get;set;}public DateOnly GiltAb{get;set;}public string Positionen{get;set;}="[]";public string AnlageHash{get;set;}="";

}

public sealed class Leistungseinsatz:VersionierterDatensatz {

 public string ObjektId{get;set;}="";public string PlanId{get;set;}="";public DateOnly Arbeitstag{get;set;}public string MitarbeiterId{get;set;}="";

 public string Status{get;set;}="Entwurf";public string Daten{get;set;}="";public string AnlageHash{get;set;}="";public string? VorgaengerId{get;set;}public string Aenderungsgrund{get;set;}="";

}

public sealed class Leistungsnachweis {

 public string Id{get;set;}="";public string EinsatzId{get;set;}="";public string FreigegebenDurch{get;set;}="";public DateTimeOffset Zeitpunkt{get;set;}public string StorageKey{get;set;}="";public string Hash{get;set;}="";

}

public sealed class Leistungsaktion {

 public string Id{get;set;}="";public string? ObjektId{get;set;}public string BenutzerId{get;set;}="";public string Art{get;set;}="";public string AnfrageHash{get;set;}="";public string Ergebnis{get;set;}="";public DateTimeOffset Zeitpunkt{get;set;}

}

public sealed record Sollposition(string Leistung,string Bereich,string Turnus,bool Pflicht,string Hinweis,int[]? Wochentage=null,bool Aktiv=true);

public sealed record Istposition(int Nummer,string Status,string Bemerkung);

public sealed record Einsatzstand(string Kunde,string Objekt,string Anschrift,string Plan,int Planversion,Sollposition[] Soll,Istposition[] Ist,string Bemerkung,string ErfasstDurch,string Erfassungsart,string Rueckmeldung="",string Rueckmeldestatus="",int? BeanstandetePosition=null,string? NacharbeitId=null);

public sealed record Objekteingabe(string AnlageId,string KundeId,string Name,string Anschrift,string BueroId,string? AkteId=null);

public sealed record Planeingabe(string AnlageId,int Objektversion,string Name,DateOnly GiltAb,Sollposition[] Positionen);

public sealed record Einsatzeingabe(string AnlageId,string PlanId,DateOnly Arbeitstag,string MitarbeiterId);

public sealed record Leistungsentscheidung(string AnlageId,int Version,string Art,Istposition[]? Positionen=null,string Bemerkung="",string Kundenstatus="",int? Position=null,bool Nacharbeit=false,Sollposition[]? Sonderpositionen=null);



public sealed record Objektaenderung(string AnlageId,int Version,string Name,string Anschrift,bool Aktiv=true,string Bemerkung="");



public sealed class Katalogleistung:VersionierterDatensatz {

 public string Name{get;set;}="";public string Beschreibung{get;set;}="";public string Kategorie{get;set;}="";public int Reihenfolge{get;set;}public bool Aktiv{get;set;}=true;

}

public sealed record Katalogeingabe(string AnlageId,int Version,string Name,string Beschreibung="",string Kategorie="",int Reihenfolge=0,bool Aktiv=true);

