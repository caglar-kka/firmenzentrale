namespace Betriebsplattform.Web.Akten;

public sealed class AktenSuche(Aktenbestand akten,Func<Uebersicht> rechnungen,Arbeitsmodule module,ILesezugriff zugriff)
{
    public IReadOnlyList<Suchtreffer> Finden(string? eingabe)
    {
        var text=eingabe?.Trim()??"";if(text.Length<2)return [];if(text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");
        var vergleich=Datenvergleich.Normal(text);if(vergleich.Length<2)return [];
        var kandidaten=new List<Suchtreffer>();var firmen=akten.Kontakte().ToDictionary(k=>k.Id);var vorgaenge=akten.Vorgaenge().Where(v=>v.Rechnungsnummer is null||module.Aktiv("Finanzen")).ToDictionary(v=>v.Id);
        if(module.Aktiv("Finanzen"))foreach(var r in rechnungen().Rechnungen)
            kandidaten.Add(new(r.VorgangId??r.Nummer,"Rechnung",r.Nummer,r.Aussteller+" → "+r.Empfaenger,"rechnung",r.VorgangId));
        if(module.Aktiv("Akten"))foreach(var v in vorgaenge.Values)kandidaten.Add(new(v.Id,"Akte",v.Titel,v.Kategorie,"vorgang",v.Id));
        if(module.Aktiv("Dokumente"))foreach(var d in akten.Eingang(true)){
            if(d.Vorgaenge.Count==0)kandidaten.Add(new(d.Id,"Dokument",d.Titel,"Posteingang · "+d.Kategorie,"eingang"));
            else if(module.Aktiv("Akten"))foreach(var id in d.Vorgaenge.Where(vorgaenge.ContainsKey))kandidaten.Add(new(d.Id,"Dokument",d.Titel,vorgaenge[id].Titel,"dokument",id));
        }
        foreach(var k in firmen.Values.Where(k=>!k.Archiviert))kandidaten.Add(new(k.Id,k.Art=="Person"?"Person":"Firma",k.Firma,k.Art=="Person"?(k.FirmaId is not null&&zugriff.Erlaubt("Firma",k.FirmaId)?firmen.GetValueOrDefault(k.FirmaId)?.Firma??"Person":"Person"):"Kontaktakte","kontakt"));
        return kandidaten.Where(t=>zugriff.Erlaubt(t.Art=="Person"?"Firma":t.Art=="Akte"?"Vorgang":t.Art,t.Id)&&(t.VorgangId is null||zugriff.Erlaubt("Vorgang",t.VorgangId)))
            .Where(t=>Datenvergleich.Normal(t.Titel+" "+t.Zusatz).Contains(vergleich,StringComparison.Ordinal)
                ||t.Ziel=="kontakt"&&firmen.TryGetValue(t.Id,out var kontakt)&&(Datenvergleich.Normal(kontakt.Anschrift).Contains(vergleich,StringComparison.Ordinal)||kontakt.Werte.Any(w=>w.Aktiv&&Datenvergleich.Normal(w.Wert).Contains(vergleich,StringComparison.Ordinal))))
            .OrderByDescending(t=>t.Titel.StartsWith(text,StringComparison.OrdinalIgnoreCase)).ThenBy(t=>t.Titel,StringComparer.OrdinalIgnoreCase)
            .DistinctBy(t=>(t.Art,t.Id,t.VorgangId)).Take(10).ToArray();
    }
}
