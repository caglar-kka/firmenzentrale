namespace Betriebsplattform.Web.Akten;

// Aufrufe werden vom gemeinsamen Akten-Lock serialisiert. Keine stille Überschreibung.
internal sealed class Notizablage
{
    private readonly Dictionary<string,Aktennotiz> daten=[];
    internal IReadOnlyList<Aktennotiz> Liste(string vorgang)=>daten.Values.Where(n=>n.VorgangId==vorgang).OrderByDescending(n=>n.GeaendertAm).ToArray();
    internal Aktennotiz Speichern(string vorgang,string? id,Notizeingabe eingabe)
    {
        var betreff=Arbeitsregeln.Text(eingabe.Betreff,"Betreff",160);
        var inhalt=Arbeitsregeln.Text(eingabe.Inhalt,"Inhalt",10000);
        var zeit=DateTimeOffset.UtcNow;const string autor=Arbeitskontext.Benutzer;
        if(daten.Values.Sum(n=>n.Historie.Sum(h=>(long)h.Inhalt.Length+h.Betreff.Length))+inhalt.Length+betreff.Length>8*1024*1024)
            throw new Arbeitsfehler("Der lokale Notizspeicher ist voll. Es wurde nichts überschrieben.");
        if(id is null){
            if(Liste(vorgang).Count>=250)throw new Arbeitsfehler("Der lokale Bestand erlaubt höchstens 250 Notizen je Akte.");
            id=Guid.NewGuid().ToString("N");var version=new Notizrevision(1,betreff,inhalt,autor,zeit,"Erstellt");
            return daten[id]=new(id,vorgang,betreff,inhalt,autor,zeit,zeit,1,false,Array.AsReadOnly(new[]{version}));
        }
        var alt=Finden(vorgang,id);Arbeitsregeln.Version(eingabe.Version,alt.Version);
        if(alt.Entfernt)throw new Arbeitsfehler("Archivierte Notiz zuerst wiederherstellen.",409);
        if(alt.Historie.Count>=100)throw new Arbeitsfehler("Die lokale Änderungshistorie ist voll. Es wurde nichts überschrieben.");
        var revision=new Notizrevision(alt.Version+1,betreff,inhalt,autor,zeit,"Bearbeitet");
        return daten[id]=alt with {Betreff=betreff,Inhalt=inhalt,GeaendertAm=zeit,Version=alt.Version+1,Historie=Array.AsReadOnly(alt.Historie.Append(revision).ToArray())};
    }
    internal Aktennotiz Entfernen(string vorgang,string id,int version,bool wiederherstellen)
    {
        var alt=Finden(vorgang,id);Arbeitsregeln.Version(version,alt.Version);
        if(alt.Entfernt!=wiederherstellen)throw new Arbeitsfehler("Der Entfernt-Status wurde bereits geändert.",409);
        if(alt.Historie.Count>=100)throw new Arbeitsfehler("Die lokale Änderungshistorie ist voll.");
        var zeit=DateTimeOffset.UtcNow;
        return daten[id]=alt with {Entfernt=!wiederherstellen,Version=alt.Version+1,GeaendertAm=zeit,
            Historie=Array.AsReadOnly(alt.Historie.Append(new(alt.Version+1,alt.Betreff,alt.Inhalt,Arbeitskontext.Benutzer,zeit,wiederherstellen?"Wiederhergestellt":"Archiviert")).ToArray())};
    }
    private Aktennotiz Finden(string vorgang,string id)=>daten.TryGetValue(id,out var n)&&n.VorgangId==vorgang?n:throw new Arbeitsfehler("Notiz gehört nicht zu dieser Akte oder ist nicht vorhanden.",404);
}
