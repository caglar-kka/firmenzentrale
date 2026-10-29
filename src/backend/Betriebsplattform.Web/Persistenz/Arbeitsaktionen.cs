using Betriebsplattform.Web.Akten;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Betriebsplattform.Web.Persistenz;

public sealed partial class FirmenDaten
{
    public async Task<Kategorie> KategorieVerschieben(string id,Kategorieverschiebung e,CancellationToken ct=default)
    {
        var d=await Finden<KategorieDatensatz>(id,ct);Version(d,e.Version);if(d.Archiviert||e.Richtung is not ("hoch" or "runter" or "position"))throw new Arbeitsfehler("Bitte eine aktive Kategorie und gültige Reihenfolge wählen.");
        var gruppe=await db.Kategorien.Where(k=>k.ElternId==d.ElternId&&!k.Archiviert).OrderBy(k=>k.Reihenfolge).ThenBy(k=>k.Name).Take(101).ToListAsync(ct);
        if(gruppe.Count>100)throw new Arbeitsfehler("Diese Kategoriegruppe benötigt eine gesonderte Prüfung.");
        var index=gruppe.FindIndex(k=>k.Id==id);int ziel=index+(e.Richtung=="hoch"?-1:1);
        if(e.Richtung=="position"){var andere=await Finden<KategorieDatensatz>(e.ZielId??"",ct);Version(andere,e.ZielVersion);if(andere.ElternId!=d.ElternId||andere.Archiviert)throw new Arbeitsfehler("Nur innerhalb derselben Kategoriegruppe verschieben.");ziel=gruppe.FindIndex(k=>k.Id==andere.Id);}
        if(ziel<0||ziel>=gruppe.Count)return KategorieAnsicht(d);gruppe.RemoveAt(index);gruppe.Insert(ziel,d);for(var i=0;i<gruppe.Count;i++)gruppe[i].Reihenfolge=i*10;
        await db.SaveChangesAsync(ct);return KategorieAnsicht(d);
    }
    private async Task KontaktangabenErkennen(DokumentDatensatz dokument,string akte,CancellationToken ct)
    {
        var firma=await db.Akten.Where(a=>a.Id==akte).Select(a=>a.FirmaId).SingleAsync(ct);if(firma is null||dokument.Text.Length==0)return;
        var k=await db.Kontakte.AsNoTracking().SingleAsync(k=>k.Id==firma,ct);var werte=await db.Kontaktwerte.AsNoTracking().Where(w=>w.KontaktId==firma&&w.Aktiv).Take(30).ToArrayAsync(ct);
        foreach(Match m in Regex.Matches(dokument.Text,@"(?im)^\s*(Telefon|E-Mail|Ansprechpartner|Anschrift|Abteilung):\s*([^\r\n]{1,200})\s*$",RegexOptions.CultureInvariant)){
            var art=m.Groups[1].Value;var wert=m.Groups[2].Value.Trim();if(!Kontaktpflege.Gueltig(art,wert))continue;
            var bisher=art=="Anschrift"?k.Anschrift:art=="Abteilung"?k.Abteilung:string.Join(" · ",werte.Where(w=>w.Art==art).Select(w=>w.Wert));
            if(werte.Any(w=>w.Art==art&&Datenvergleich.Wert(art,w.Wert,k.Land)==Datenvergleich.Wert(art,wert,k.Land))||bisher==wert)continue;
            if(await db.Kontaktvorschlaege.AnyAsync(v=>v.KontaktId==firma&&v.DokumentId==dokument.Id&&v.Art==art&&v.Neu==wert,ct))continue;
            db.Kontaktvorschlaege.Add(new(){KontaktId=firma,DokumentId=dokument.Id,Art=art,Bisher=bisher.Length>200?bisher[..200]:bisher,Neu=wert,Kontaktversion=k.Version});
        }
    }
    public async Task<object> KontaktvorschlagEntscheiden(string id,Kontaktfreigabe e,CancellationToken ct=default)
    {
        await db.KontaktaktionPruefen("Bearbeiten",ct);
        await using var tx=await db.Database.BeginTransactionAsync(ct);var v=await Finden<KontaktvorschlagDatensatz>(id,ct);var k=await Finden<KontaktDatensatz>(v.KontaktId,ct);Version(k,e.Version);
        if(k.Archiviert||v.Status!="Zur Prüfung")throw new Arbeitsfehler("Vorschlag oder Kontakt wurde zwischenzeitlich geändert. Bitte neu prüfen.",409);
        if(e.Aktion is not ("Übernehmen" or "Zusätzlich speichern" or "Ignorieren"))throw new Arbeitsfehler("Bitte eine gültige Entscheidung wählen.");
        var aktuelleWerte=await db.Kontaktwerte.Where(w=>w.KontaktId==k.Id&&w.Art==v.Art&&w.Aktiv).OrderByDescending(w=>w.Primaer).Take(30).ToArrayAsync(ct);
        var bisher=v.Art=="Anschrift"?k.Anschrift:v.Art=="Abteilung"?k.Abteilung:string.Join(" · ",aktuelleWerte.Select(w=>w.Wert));v.Bisher=bisher.Length>200?bisher[..200]:bisher;
        if(e.Aktion!="Ignorieren"){
            if(v.Art is "Anschrift" or "Ansprechpartner")throw new Arbeitsfehler("Bitte diese Angabe strukturiert in der zentralen Kontaktverwaltung pflegen. Der Vorschlag bleibt zur Prüfung erhalten.");
            if(v.Art=="Abteilung"){if(e.Aktion=="Zusätzlich speichern")throw new Arbeitsfehler("Bitte die Abteilung in der Kontaktverwaltung prüfen.");k.Abteilung=v.Neu;}
            else{
                var alt=await db.Kontaktwerte.Where(w=>w.KontaktId==k.Id&&w.Art==v.Art&&w.Aktiv).Take(30).ToArrayAsync(ct);
                if(alt.Any(w=>Datenvergleich.Wert(w.Art,w.Wert,k.Land)==Datenvergleich.Wert(v.Art,v.Neu,k.Land)))throw new Arbeitsfehler("Die Kontaktangabe ist bereits vorhanden. Bitte neu laden.",409);
                if(e.Aktion=="Übernehmen")foreach(var w in alt)w.Primaer=false;
                k.GeaendertAm=DateTimeOffset.UtcNow;await db.SaveChangesAsync(ct);
                db.Kontaktwerte.Add(new(){KontaktId=k.Id,Art=v.Art,Wert=v.Neu,Vergleichswert=Datenvergleich.Wert(v.Art,v.Neu,k.Land),Primaer=e.Aktion=="Übernehmen"});
            }
        }
        v.Status=e.Aktion=="Ignorieren"?"Nicht übernommen":"Übernommen";
        db.Kontaktverlauf.Add(new(){KontaktId=k.Id,DokumentId=v.DokumentId,Vorher=v.Bisher,Nachher=v.Neu,Aktion=e.Aktion,Benutzer=Arbeitskontext.Benutzer});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new{status=v.Status};
    }
}
