using Betriebsplattform.Web.Akten;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Betriebsplattform.Web.Persistenz;

// Request-scoped unit of work. Never caches a full database or survives a request.
public sealed partial class FirmenDaten(FirmenDbContext db)
{
    public FirmenDbContext Db=>db;
    public const int Seitengroesse=20;
    public static string Normal(string s)=>string.Join(' ',Suchvergleich.Woerter(s));
    public static string Id(string id)=>Regex.IsMatch(id,@"\A[A-Za-z0-9_-]{1,64}\z")?id:throw new Arbeitsfehler("Ungültige Eintragskennung.");
    public static int Seite(int seite)=>seite is >=0 and <=100000?seite:throw new Arbeitsfehler("Ungültige Ergebnisseite.");
    private static string Optional(string? s,string feld,int max)=>string.IsNullOrEmpty(s)?"":Arbeitsregeln.Text(s,feld,max);
    private async Task<T> Finden<T>(string id,CancellationToken ct) where T:VersionierterDatensatz {var key=Id(id);return await db.Set<T>().SingleOrDefaultAsync(e=>e.Id==key,ct)??throw new Arbeitsfehler("Eintrag nicht gefunden.",404);}
    private async Task AktiveKategorie(string id,CancellationToken ct){if(!await db.Kategorien.AnyAsync(k=>k.Id==id&&!k.Archiviert,ct))throw new Arbeitsfehler("Bitte eine aktive Kategorie wählen.");}
    private async Task AktiveFirma(string id,CancellationToken ct){if(!await db.Kontakte.AnyAsync(k=>k.Id==id&&k.Art=="Firma"&&!k.Archiviert,ct))throw new Arbeitsfehler("Bitte eine aktive Firma wählen.");}
    private static void Version(VersionierterDatensatz d,int erwartet){Arbeitsregeln.Version(erwartet,d.Version);}

    public async Task<Kategorie> KategorieSpeichern(string? id,Kategorieeingabe e,CancellationToken ct=default)
    {
        var name=Arbeitsregeln.Text(e.Name,"Kategoriename",80);var normal=Normal(name);
        if(Vorgangsbearbeitung.Werte.Concat(new[]{"Bezahlt","Teilweise bezahlt","Zur Prüfung","Fehler","Freigegeben","Neu","Zugeordnet"}).Contains(name,StringComparer.OrdinalIgnoreCase))throw new Arbeitsfehler("Systemstatus sind keine frei änderbaren Geschäftskategorien.");
        if(e.Reihenfolge is <0 or >10000)throw new Arbeitsfehler("Ungültige Kategorienreihenfolge.");
        var d=id is null?new KategorieDatensatz():await Finden<KategorieDatensatz>(id,ct);
        if(id is not null){Version(d,e.Version);if(d.Archiviert)throw new Arbeitsfehler("Archivierte Kategorie kann nicht geändert werden.",409);}
        else if(e.Version!=0)throw new Arbeitsfehler("Neue Kategorie benötigt keine vorhandene Version.");
        if(await db.Kategorien.AnyAsync(k=>k.Id!=d.Id&&k.Suchname==normal,ct))throw new Arbeitsfehler("Diese Kategorie ist bereits vorhanden.",409);
        if(e.ElternId is not null){var parent=await Finden<KategorieDatensatz>(e.ElternId,ct);if(parent.Archiviert||parent.Id==d.Id||parent.ElternId is not null||await db.Kategorien.AnyAsync(k=>k.ElternId==d.Id,ct))throw new Arbeitsfehler("Nur eine Unterkategorieebene ist erlaubt; keine Kreise.");parent.GeaendertAm=DateTimeOffset.UtcNow;}
        d.Name=name;d.Suchname=normal;d.Beschreibung=Optional(e.Beschreibung,"Beschreibung",500);d.ElternId=e.ElternId;d.Reihenfolge=e.Reihenfolge;
        if(id is null)db.Kategorien.Add(d);await db.SaveChangesAsync(ct);return KategorieAnsicht(d);
    }
    public static Kategorie KategorieAnsicht(KategorieDatensatz d)=>new(d.Id,d.Name,d.Beschreibung,d.ElternId,d.Reihenfolge,d.Version,d.Archiviert);
    public async Task<Kategorie> KategorieArchivieren(string id,int version,CancellationToken ct=default)
    {
        var d=await Finden<KategorieDatensatz>(id,ct);Version(d,version);
        if(d.Id is "ungeklaert" or "sonstiges"||await db.Kategorien.AnyAsync(k=>k.ElternId==id&&!k.Archiviert,ct))throw new Arbeitsfehler("Auffangkategorien bleiben verfügbar; aktive Unterkategorien zuerst archivieren.");
        d.Archiviert=true;await db.SaveChangesAsync(ct);return KategorieAnsicht(d);
    }
    public async Task<Vorgang> AkteAnlegen(Vorgangseingabe e,CancellationToken ct=default)
    {
        var titel=Arbeitsregeln.Text(e.Titel,"Aktenbetreff",160);await AktiveKategorie(e.Kategorie??"",ct);await AktiveFirma(e.FirmaId??"",ct);
        var normal=Normal(titel);var prefix=normal[..Math.Min(3,normal.Length)];
        var kandidaten=await db.Akten.AsNoTracking().Where(a=>a.Suchtext.StartsWith(prefix)).OrderBy(a=>a.Id).Take(100).ToArrayAsync(ct);
        var hinweise=kandidaten.Where(a=>Datenvergleich.NameAehnlich(a.Titel,titel)).Take(10).Select(a=>new Dublettenhinweis(a.Id,a.Titel,"Akte","",[a.KategorieId],["Ähnlicher Betreff – kann ein anderer Geschäftsfall sein"])).ToArray();
        if(hinweise.Length>0&&!e.DublettenBestaetigt)throw new Arbeitsfehler("Mögliche vorhandene Akte gefunden. Bitte vergleichen oder bewusst separat anlegen.",409,hinweise);
        var d=new AkteDatensatz{Titel=titel,Suchtext=normal,KategorieId=e.Kategorie!,FirmaId=e.FirmaId};db.Akten.Add(d);await db.SaveChangesAsync(ct);return await AkteAnsicht(d,ct);
    }
    public async Task<Vorgang> AkteStatus(string id,Bearbeitungsentscheidung e,CancellationToken ct=default)
    {
        var d=await Finden<AkteDatensatz>(id,ct);Version(d,e.Version);if(e.Status is null||!Vorgangsbearbeitung.Werte.Contains(e.Status))throw new Arbeitsfehler("Bitte einen gültigen Bearbeitungsstatus wählen.");
        d.Status=e.Status;d.GeaendertAm=DateTimeOffset.UtcNow;await db.SaveChangesAsync(ct);return await AkteAnsicht(d,ct);
    }
    public async Task<Vorgang> AkteAnsicht(AkteDatensatz d,CancellationToken ct=default)=>new(d.Id,d.Titel,
        await db.Kategorien.Where(k=>k.Id==d.KategorieId).Select(k=>k.Name).SingleAsync(ct),d.FirmaId,
        await db.Rechnungen.Where(r=>r.AkteId==d.Id).Select(r=>r.Nummer).SingleOrDefaultAsync(ct),d.KategorieId,
        await db.Kontakte.Where(k=>k.Id==d.FirmaId).Select(k=>k.Name).SingleOrDefaultAsync(ct)??"Nicht zugeordnet",d.Status,d.GeaendertAm,
        await db.Dokumentzuordnungen.CountAsync(z=>z.AkteId==d.Id,ct),"Dokumente und Schriftverkehr",d.Version);

    public async Task<Firmenkontakt> KontaktSpeichern(string? id,Kontakteingabe e,CancellationToken ct=default)
    {
        if(e.Art is not ("Firma" or "Person"))throw new Arbeitsfehler("Bitte Firma oder Person wählen.");
        var d=id is null?new KontaktDatensatz():await Finden<KontaktDatensatz>(id,ct);
        if(id is not null){Version(d,e.Version);if(d.Art!=e.Art||d.Archiviert)throw new Arbeitsfehler("Kontaktart ist unveränderlich; archivierte Kontakte sind schreibgeschützt.",409);}
        else if(e.Version!=0)throw new Arbeitsfehler("Ungültige Version für einen neuen Kontakt.");
        var vor=e.Art=="Person"?Arbeitsregeln.Text(e.Vorname,"Vorname",80):"";var nach=e.Art=="Person"?Arbeitsregeln.Text(e.Nachname,"Nachname",80):"";
        if(e.Art=="Person"&&new[]{vor,nach}.Any(n=>!Regex.IsMatch(n,@"\A[\p{L}\p{M}][\p{L}\p{M} \-’'.]*\z")))throw new Arbeitsfehler("Bitte gültige Vor- und Nachnamen eingeben.");
        var name=e.Art=="Person"?vor+" "+nach:Arbeitsregeln.Text(e.Name,"Firmenname",160);
        if(e.FirmaId is not null){if(e.Art!="Person"||e.FirmaId==d.Id)throw new Arbeitsfehler("Nur Personen können einer Firma zugeordnet werden.");await AktiveFirma(e.FirmaId,ct);}
        if(e.Werte is null||e.Werte.Count is 0 or >30)throw new Arbeitsfehler("Bitte gültige Kontaktdaten angeben.");
        var werte=e.Werte.Select(w=>w with{Wert=Arbeitsregeln.Text(w.Wert,w.Art,200),Typ=Optional(w.Typ,"Kontaktart",40)}).ToArray();
        if(werte.Any(w=>w.Art is not ("Telefon" or "E-Mail" or "Website")||!Kontaktpflege.Gueltig(w.Art,w.Wert))||!werte.Any(w=>w.Art=="Telefon")||e.Art=="Firma"&&!werte.Any(w=>w.Art=="E-Mail"))throw new Arbeitsfehler("Personen benötigen Telefon, Firmen Telefon und gültige E-Mail-Adresse.");
        if(werte.GroupBy(w=>(w.Art,Datenvergleich.Wert(w.Art,w.Wert,e.Land))).Any(g=>g.Count()>1)||werte.GroupBy(w=>w.Art).Any(g=>g.Count(w=>w.Primaer)>1))throw new Arbeitsfehler("Doppelte Kontaktwerte oder mehrere bevorzugte Angaben derselben Art.");
        var normal=Normal(name);var prefix=normal[..Math.Min(3,normal.Length)];var vergleich=werte.Select(w=>Datenvergleich.Wert(w.Art,w.Wert,e.Land)).ToArray();
        var ids=db.Kontaktwerte.Where(w=>vergleich.Contains(w.Vergleichswert)).Select(w=>w.KontaktId);
        var starkeIds=await ids.Distinct().Take(100).ToArrayAsync(ct);
        var kandidaten=(await db.Kontakte.AsNoTracking().Where(k=>k.Id!=d.Id&&k.Art==e.Art&&!k.Archiviert&&(k.Suchname.StartsWith(prefix)||ids.Contains(k.Id))).OrderBy(k=>k.Id).Take(100).ToArrayAsync(ct)).Where(k=>starkeIds.Contains(k.Id)||Datenvergleich.NameAehnlich(k.Name,name)).ToArray();
        if(kandidaten.Length>0&&!e.DublettenBestaetigt)throw new Arbeitsfehler("Möglicher vorhandener Eintrag gefunden. Bitte vergleichen oder bewusst separat anlegen.",409,kandidaten.Take(10).Select(k=>new Dublettenhinweis(k.Id,k.Name,k.Art,k.Anschrift,[k.Ort],["Ähnlicher Name oder übereinstimmende Kontaktdaten – keine automatische Zusammenführung"])).ToArray());
        var alteWerte=await db.Kontaktwerte.Where(w=>w.KontaktId==d.Id&&w.Aktiv).ToArrayAsync(ct);
        var vorher=id is null?"":Kontaktstand(d)+" | "+string.Join("; ",alteWerte.Select(w=>w.Art+": "+w.Wert));
        d.GeaendertAm=DateTimeOffset.UtcNow;d.Art=e.Art;d.Name=name;d.Suchname=normal;d.Vorname=vor;d.Nachname=nach;d.FirmaId=e.FirmaId;
        d.Strasse=Optional(e.Strasse,"Straße",160);d.Hausnummer=Optional(e.Hausnummer,"Hausnummer",30);d.Plz=Optional(e.Plz,"PLZ",20);d.Ort=Optional(e.Ort,"Ort",100);d.Land=Optional(e.Land,"Land",80);d.Anschrift=Optional(e.Anschrift,"Bisherige Anschrift",500);
        d.Abteilung=Optional(e.Abteilung,"Abteilung",100);d.Rolle=Optional(e.Rolle,"Rolle",100);
        if(id is null)db.Kontakte.Add(d);
        foreach(var w in alteWerte){w.Aktiv=false;w.Primaer=false;}
        // Deactivate old primary values before adding replacements, inside one transaction.
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        if(id is not null)await db.SaveChangesAsync(ct);
        db.Kontaktwerte.AddRange(werte.Select(w=>new KontaktwertDatensatz{KontaktId=d.Id,Art=w.Art,Wert=w.Wert,Typ=w.Typ,Primaer=w.Primaer,Vergleichswert=Datenvergleich.Wert(w.Art,w.Wert,d.Land)}));
        db.Kontaktverlauf.Add(new(){KontaktId=d.Id,Vorher=vorher,Nachher=Kontaktstand(d)+" | "+string.Join("; ",werte.Select(w=>w.Art+": "+w.Wert)),Aktion=id is null?"Angelegt":"Bearbeitet",Benutzer=Arbeitskontext.Benutzer});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return (await KontaktAnsichten([d],ct)).Single();
    }
    private static string Kontaktstand(KontaktDatensatz d)=>string.Join(" · ",new[]{d.Name,d.Strasse,d.Hausnummer,d.Plz,d.Ort,d.Land,d.Anschrift,d.Abteilung,d.Rolle}.Where(x=>x.Length>0));
    public async Task<Firmenkontakt[]> KontaktAnsichten(KontaktDatensatz[] liste,CancellationToken ct=default,bool mitHistorie=true)
    {
        var ids=liste.Select(k=>k.Id).ToArray();var werte=await db.Kontaktwerte.AsNoTracking().Where(w=>ids.Contains(w.KontaktId)&&(mitHistorie||w.Aktiv)).OrderByDescending(w=>w.Aktiv).ThenBy(w=>w.Id).Take(3000).ToArrayAsync(ct);
        var historie=mitHistorie?await db.Kontaktverlauf.AsNoTracking().Where(h=>ids.Contains(h.KontaktId)).OrderByDescending(h=>h.Zeitpunkt).Take(500).ToArrayAsync(ct):[];
        var firmenIds=liste.Select(k=>k.FirmaId).Distinct().ToArray();var firmen=await db.Kontakte.AsNoTracking().Where(k=>firmenIds.Contains(k.Id)).ToDictionaryAsync(k=>k.Id,k=>k.Name,ct);
        return liste.Select(k=>new Firmenkontakt(k.Id,k.Name,k.Version,werte.Where(w=>w.KontaktId==k.Id).Select(w=>new Kontaktwert(w.Id,w.Art,w.Wert,w.Aktiv,w.Primaer,w.Typ)).ToArray(),historie.Where(h=>h.KontaktId==k.Id).Select(h=>new Kontaktentscheidung(h.Aktion,h.Vorher,h.Nachher,h.DokumentId??"",h.Benutzer,h.Zeitpunkt)).ToArray(),k.Art,k.FirmaId,string.IsNullOrEmpty(k.Strasse)&&string.IsNullOrEmpty(k.Ort)?k.Anschrift:string.Join(" ",new[]{k.Strasse,k.Hausnummer,k.Plz,k.Ort,k.Land}.Where(x=>x.Length>0)),k.Abteilung,k.Rolle,k.Archiviert,k.ErstelltAm,k.GeaendertAm,k.Strasse,k.Plz,k.Ort,k.Land,k.Hausnummer,k.Vorname,k.Nachname,k.FirmaId is null?null:firmen.GetValueOrDefault(k.FirmaId))).ToArray();
    }
    public async Task<Firmenkontakt> KontaktArchivieren(string id,int version,CancellationToken ct=default)
    {var d=await Finden<KontaktDatensatz>(id,ct);Version(d,version);d.Archiviert=true;db.Kontaktverlauf.Add(new(){KontaktId=id,Vorher="Aktiv",Nachher="Archiviert",Aktion="Archiviert",Benutzer=Arbeitskontext.Benutzer});await db.SaveChangesAsync(ct);return (await KontaktAnsichten([d],ct)).Single();}

    public async Task<NotizDatensatz> NotizSpeichern(string akte,string? id,Notizeingabe e,bool? archivieren=null,CancellationToken ct=default)
    {
        _=await Finden<AkteDatensatz>(akte,ct);var n=id is null?new NotizDatensatz{AkteId=akte,Autor=Arbeitskontext.Benutzer}:await Finden<NotizDatensatz>(id,ct);
        if(n.AkteId!=akte)throw new Arbeitsfehler("Notiz gehört nicht zu dieser Akte.",404);
        if(id is not null)Version(n,e.Version);else if(e.Version!=0)throw new Arbeitsfehler("Ungültige Notizversion.");
        if(archivieren is null){if(n.Archiviert)throw new Arbeitsfehler("Archivierte Notiz zuerst wiederherstellen.",409);n.Betreff=Arbeitsregeln.Text(e.Betreff,"Betreff",160);n.Inhalt=Arbeitsregeln.Text(e.Inhalt,"Inhalt",10000);}else n.Archiviert=archivieren.Value;
        n.GeaendertAm=DateTimeOffset.UtcNow;if(id is null)db.Notizen.Add(n);
        db.Notizrevisionen.Add(new(){NotizId=n.Id,Version=id is null?1:n.Version+1,Betreff=n.Betreff,Inhalt=n.Inhalt,Autor=Arbeitskontext.Benutzer,Zeitpunkt=n.GeaendertAm,Aktion=archivieren is null?(id is null?"Erstellt":"Bearbeitet"):archivieren.Value?"Archiviert":"Wiederhergestellt"});
        await db.SaveChangesAsync(ct);return n;
    }
    public async Task<Aktennotiz> NotizAnsicht(NotizDatensatz n,CancellationToken ct=default)=>new(n.Id,n.AkteId,n.Betreff,n.Inhalt,n.Autor,n.ErstelltAm,n.GeaendertAm,n.Version,n.Archiviert,await db.Notizrevisionen.AsNoTracking().Where(h=>h.NotizId==n.Id).OrderByDescending(h=>h.Version).Take(100).Select(h=>new Notizrevision(h.Version,h.Betreff,h.Inhalt,h.Autor,h.Zeitpunkt,h.Aktion)).ToArrayAsync(ct));

    public async Task<DokumentDatensatz> Umordnen(string id,Umzuordnung e,CancellationToken ct=default)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var d=await Finden<DokumentDatensatz>(id,ct);Version(d,e.Version);
        var zuordnung=await db.Dokumentzuordnungen.SingleOrDefaultAsync(z=>z.DokumentId==id,ct)??throw new Arbeitsfehler("Die aktuelle Zuordnung besteht nicht mehr. Bitte neu laden.",409);
        if(zuordnung.AkteId!=e.BisherigerVorgangId)throw new Arbeitsfehler("Die Zuordnung wurde zwischenzeitlich geändert. Bitte neu laden.",409);
        string ziel;
        if(e.NeuerVorgangId is null)ziel=(await AkteAnlegen(new(e.NeuerTitel,e.Kategorie,e.FirmaId,e.DublettenBestaetigt),ct)).Id;
        else{var a=await Finden<AkteDatensatz>(e.NeuerVorgangId,ct);if(a.Status=="Archiviert"||a.Id==zuordnung.AkteId)throw new Arbeitsfehler("Bitte eine andere aktive Zielakte wählen.");ziel=a.Id;}
        if(e.Kategorie is not null){await AktiveKategorie(e.Kategorie,ct);d.KategorieId=e.Kategorie;}
        db.Dokumentaktivitaeten.Add(new(){DokumentId=id,VorherigeAkteId=zuordnung.AkteId,AkteId=ziel,Text="Zuordnung geändert",Benutzer=Arbeitskontext.Benutzer});
        var vorher=zuordnung.AkteId;zuordnung.AkteId=ziel;d.GeaendertAm=DateTimeOffset.UtcNow;d.Status="Zugeordnet";
        foreach(var a in await db.Akten.Where(a=>a.Id==vorher||a.Id==ziel).ToArrayAsync(ct))a.GeaendertAm=d.GeaendertAm;
        await db.SaveChangesAsync(ct);await KontaktangabenErkennen(d,ziel,ct);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return d;
    }
}
