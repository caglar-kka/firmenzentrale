namespace Betriebsplattform.Web.Akten;

public sealed record Kategorie(string Id, string Name, string Beschreibung, string? ElternId, int Reihenfolge, int Version, bool Archiviert);
public sealed record Kategorieeingabe(string? Name, string? Beschreibung, string? ElternId, int Reihenfolge, int Version);

// Gemeinsame Geschäftsrubriken; Systemstatus werden hier niemals definiert.
public sealed class Kategorienverwaltung
{
    private readonly Dictionary<string, Kategorie> daten = [];
    private readonly object sperre = new();
    private static readonly string[] Statusnamen = ["Bezahlt", "Teilweise bezahlt", "Teilzahlung", "Offen", "Zur Prüfung", "Fehler", "Archiviert", "Freigegeben", "Neu", "Zugeordnet", "Bearbeitet", "Zuordnung vorgeschlagen", "Bestätigt", "In Bearbeitung", "Wartet auf Rückmeldung", "Erledigt"];
    public Kategorienverwaltung()
    {
        var standard = new[] { ("rechnung","Rechnung"), ("versicherung","Versicherung"), ("krankenkasse","Krankenkasse"), ("sozialversicherung","Sozialversicherung"), ("finanzamt","Finanzamt"), ("behoerde","Behörde"), ("vertrag","Vertrag"), ("mahnung","Mahnung"), ("personal","Personal"), ("fahrzeug","Fahrzeug"), ("lieferant","Lieferant"), ("kunde","Kunde"), ("korrespondenz","Allgemeine Korrespondenz"), ("sonstiges","Sonstiges"), ("ungeklaert","Ungeklärt") };
        foreach (var (id,name) in standard) daten[id]=new(id,name,"",null,daten.Count*10,1,false);
        daten["kfz-versicherung"]=new("kfz-versicherung","Kfz-Versicherung","","versicherung",1,1,false);
        daten["betriebshaftpflicht"]=new("betriebshaftpflicht","Betriebshaftpflicht","","versicherung",2,1,false);
    }
    public IReadOnlyList<Kategorie> Liste(){lock(sperre)return daten.Values.Where(k=>k.ElternId is null).OrderBy(k=>k.Reihenfolge).ThenBy(k=>k.Name).SelectMany(k=>new[]{k}.Concat(daten.Values.Where(s=>s.ElternId==k.Id).OrderBy(s=>s.Reihenfolge).ThenBy(s=>s.Name))).ToArray();}
    public Kategorie Finden(string? idOderName, bool aktiv = true)
    {
        lock(sperre){
            var wert=idOderName switch {"Zur Prüfung"=>"ungeklaert","Allgemeines Schreiben"=>"korrespondenz","Behörde / Finanzamt"=>"behoerde","Krankenkasse / Sozialversicherung"=>"sozialversicherung",_=>idOderName};
            var k=daten.GetValueOrDefault(wert??"")??daten.Values.SingleOrDefault(k=>k.Name==wert)??throw new Arbeitsfehler("Bitte eine vorhandene Kategorie wählen.");
            if(aktiv&&k.Archiviert)throw new Arbeitsfehler("Diese Kategorie ist archiviert. Bitte eine aktive Kategorie wählen.");return k;
        }
    }
    public Kategorie Speichern(string? id,Kategorieeingabe e)
    {
        lock(sperre){
            var name=Arbeitsregeln.Text(e.Name,"Kategoriename",80);
            if(Statusnamen.Contains(name,StringComparer.OrdinalIgnoreCase))throw new Arbeitsfehler("Systemstatus können nicht als Geschäftskategorie angelegt oder geändert werden.");
            if(daten.Values.Any(k=>k.Id!=id&&k.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new Arbeitsfehler("Diese Kategorie ist bereits vorhanden.",409);
            if(e.Reihenfolge is <0 or >10000||e.Beschreibung?.Length>500)throw new Arbeitsfehler("Beschreibung oder Reihenfolge ist zu lang.");
            Kategorie? alt=id is null?null:Finden(id,false);if(alt is not null){Arbeitsregeln.Version(e.Version,alt.Version);if(alt.Archiviert)throw new Arbeitsfehler("Archivierte Kategorie kann nicht geändert werden.",409);}
            if(e.ElternId is not null){var eltern=Finden(e.ElternId);if(eltern.ElternId is not null||eltern.Id==id||(id is not null&&daten.Values.Any(k=>k.ElternId==id)))throw new Arbeitsfehler("Nur eine Unterkategorieebene ist erlaubt; keine Kreise.");}
            if(daten.Count>=100&&id is null)throw new Arbeitsfehler("Die lokale Kategoriengrenze ist erreicht.");
            var key=id??Guid.NewGuid().ToString("N");return daten[key]=new(key,name,e.Beschreibung?.Trim()??"",e.ElternId,e.Reihenfolge,(alt?.Version??0)+1,false);
        }
    }
    public Kategorie Archivieren(string id,int version)
    {
        lock(sperre){var k=Finden(id,false);Arbeitsregeln.Version(version,k.Version);if(id is "ungeklaert" or "sonstiges")throw new Arbeitsfehler("Die Auffangkategorien bleiben verfügbar.");if(daten.Values.Any(x=>x.ElternId==id&&!x.Archiviert))throw new Arbeitsfehler("Zuerst aktive Unterkategorien archivieren.");return daten[id]=k with {Archiviert=true,Version=k.Version+1};}
    }
    public Kategorie Verschieben(string id,Kategorieverschiebung e)
    {
        lock(sperre){var k=Finden(id);Arbeitsregeln.Version(e.Version,k.Version);if(e.Richtung is not ("hoch" or "runter" or "position"))throw new Arbeitsfehler("Bitte nach oben oder unten verschieben.");
            var geschwister=Liste().Where(s=>s.ElternId==k.ElternId&&!s.Archiviert).ToList();var i=geschwister.FindIndex(s=>s.Id==id);var ziel=i+(e.Richtung=="hoch"?-1:1);
            if(e.Richtung=="position"){var zielKategorie=Finden(e.ZielId);Arbeitsregeln.Version(e.ZielVersion,zielKategorie.Version);if(zielKategorie.ElternId!=k.ElternId)throw new Arbeitsfehler("Nur innerhalb derselben Kategoriegruppe verschieben.");ziel=geschwister.FindIndex(s=>s.Id==zielKategorie.Id);}
            if(ziel<0||ziel>=geschwister.Count)return k;
            var verschoben=geschwister[i];geschwister.RemoveAt(i);geschwister.Insert(ziel,verschoben);
            for(int n=0;n<geschwister.Count;n++){var s=geschwister[n];if(s.Reihenfolge!=n*10)daten[s.Id]=s with{Reihenfolge=n*10,Version=s.Version+1};}
            return daten[id];}
    }
}
