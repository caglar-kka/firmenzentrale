using System.Security.Cryptography;
using Betriebsplattform.Web.Akten;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Betriebsplattform.Web.Persistenz;

public sealed partial class FirmenDaten
{
    public async Task<DokumentDatensatz> DokumentHinzufuegen(byte[] bytes,string name,string mime,string? akte,IDokumentdateien dateien,CancellationToken ct=default)
    {
        var gelesen=new Dokumentpruefung().Pruefen(bytes,name,mime);var hash=Convert.ToHexString(SHA256.HashData(bytes));
        if(pilot?.Aktiv==true)await (scanner??new Betriebsplattform.Web.Sicherheit.NichtKonfigurierterScanner()).Freigeben(bytes,ct);
        if(akte is not null){var a=await Finden<AkteDatensatz>(akte,ct);if(a.Status=="Archiviert")throw new Arbeitsfehler("Bitte eine aktive Akte wählen.");}
        var alt=await db.Dokumente.SingleOrDefaultAsync(d=>d.Hash==hash,ct);
        if(alt is not null){
            var bisher=await db.Dokumentzuordnungen.Where(z=>z.DokumentId==alt.Id).Select(z=>z.AkteId).SingleOrDefaultAsync(ct);
            if(akte is not null&&bisher!=akte)throw new Arbeitsfehler("Diese Datei ist bereits vorhanden. Bitte die bestehende Zuordnung prüfen oder ändern.",409);
            db.Dokumentaktivitaeten.Add(new(){DokumentId=alt.Id,AkteId=bisher,Text="Identische Datei erneut eingegangen",Kanal="Upload",Benutzer=Arbeitskontext.Benutzer});alt.GeaendertAm=DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);return alt;
        }
        var d=new DokumentDatensatz{Titel=Path.GetFileNameWithoutExtension(name).Replace('_',' '),Dateiname=name,StorageKey=Guid.NewGuid().ToString("N"),Inhaltstyp=gelesen.Inhaltstyp,Groesse=bytes.Length,Hash=hash,Vorschau=gelesen.Vorschau,Text=gelesen.Text,Textmethode=gelesen.Textmethode,Status=akte is null?"Zur Prüfung":"Zugeordnet"};
        d.Suchtext=Normal(d.Titel+" "+d.Dateiname);if(d.Suchtext.Length>1000)d.Suchtext=d.Suchtext[..1000];
        d.Titel=Arbeitsregeln.Text(d.Titel,"Dokumenttitel",160);await dateien.Speichern(d.StorageKey,bytes,ct);
        var commitBegonnen=false;
        try{
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        db.Dokumente.Add(d);if(akte is not null)db.Dokumentzuordnungen.Add(new(){DokumentId=d.Id,AkteId=akte});
        db.Dokumentaktivitaeten.Add(new(){DokumentId=d.Id,AkteId=akte,Text="Dokument eingegangen",Kanal="Upload",Benutzer=Arbeitskontext.Benutzer});
        await db.SaveChangesAsync(ct);if(akte is not null){await KontaktangabenErkennen(d,akte,ct);await db.SaveChangesAsync(ct);}commitBegonnen=true;await tx.CommitAsync(ct);return d;
        }catch{
            // Transaction disposal/rollback precedes compensation, including cancellation.
            if(commitBegonnen){
                // A lost commit acknowledgement must never delete a potentially committed original.
                await using var pruefung=new FirmenDbContext((DbContextOptions<FirmenDbContext>)db.GetService<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions>());
                try{if(await pruefung.Dokumente.AsNoTracking().AnyAsync(x=>x.StorageKey==d.StorageKey,CancellationToken.None))throw new Arbeitsfehler("Bitte den Dokumenteingang aktualisieren und den gespeicherten Stand prüfen.",409);}
                catch(Arbeitsfehler){throw;}
                catch(Exception){throw new Arbeitsfehler("Der Speicherabschluss konnte nicht bestätigt werden. Bitte die Dokumentablage prüfen, bevor Sie die Datei erneut hochladen.",503);}
            }
            try{await dateien.Entfernen(d.StorageKey,CancellationToken.None);}
            catch(Exception){throw new Arbeitsfehler("Das Dokument wurde nicht übernommen. Die Dokumentablage muss vor einem erneuten Versuch geprüft werden.",503);}
            throw;
        }
    }
    public async Task<DokumentDatensatz> Erstzuordnen(string id,Zuordnung e,CancellationToken ct=default)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);var d=await Finden<DokumentDatensatz>(id,ct);Version(d,e.Version);
        if(await db.Dokumentzuordnungen.AnyAsync(z=>z.DokumentId==id,ct))throw new Arbeitsfehler("Dieses Dokument ist bereits zugeordnet.",409);
        await AktiveKategorie(e.Kategorie??"",ct);string akte;
        if(e.VorgangId is null)akte=(await AkteAnlegen(new(e.NeuerTitel,e.Kategorie,e.FirmaId,e.DublettenBestaetigt),ct)).Id;
        else{var a=await Finden<AkteDatensatz>(e.VorgangId,ct);if(a.Status=="Archiviert")throw new Arbeitsfehler("Bitte eine aktive Akte wählen.");akte=a.Id;}
        db.Dokumentzuordnungen.Add(new(){DokumentId=id,AkteId=akte});d.Status="Zugeordnet";d.KategorieId=e.Kategorie;d.Pruefbedarf=e.Kategorie=="ungeklaert";d.GeaendertAm=DateTimeOffset.UtcNow;
        db.Dokumentaktivitaeten.Add(new(){DokumentId=id,AkteId=akte,Text="Zuordnung bestätigt",Benutzer=Arbeitskontext.Benutzer});await db.SaveChangesAsync(ct);await KontaktangabenErkennen(d,akte,ct);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return d;
    }
    // List projection intentionally excludes extracted text and all binary contents.
    public IQueryable<DokumentDatensatz> DokumentMetadaten()=>db.Dokumente.AsNoTracking().Select(d=>new DokumentDatensatz{Id=d.Id,Titel=d.Titel,Suchtext=d.Suchtext,Dateiname=d.Dateiname,StorageKey=d.StorageKey,Inhaltstyp=d.Inhaltstyp,Groesse=d.Groesse,Hash=d.Hash,Art=d.Art,KategorieId=d.KategorieId,AbsenderId=d.AbsenderId,Kanal=d.Kanal,Quelle=d.Quelle,Status=d.Status,Pruefbedarf=d.Pruefbedarf,Vorschau=d.Vorschau,Textmethode=d.Textmethode,Version=d.Version,ErstelltAm=d.ErstelltAm,GeaendertAm=d.GeaendertAm});
    public async Task<Aktendokument[]> DokumentAnsichten(DokumentDatensatz[] daten,CancellationToken ct=default,bool mitVerlauf=true)
    {
        var ids=daten.Select(d=>d.Id).ToArray();var zuordnungen=await db.Dokumentzuordnungen.AsNoTracking().Where(z=>ids.Contains(z.DokumentId)).ToArrayAsync(ct);
        var katIds=daten.Select(d=>d.KategorieId).ToArray();var kategorien=await db.Kategorien.AsNoTracking().Where(k=>katIds.Contains(k.Id)).ToDictionaryAsync(k=>k.Id,k=>k.Name,ct);
        var kontaktIds=daten.Select(d=>d.AbsenderId).ToArray();var kontakte=await db.Kontakte.AsNoTracking().Where(k=>kontaktIds.Contains(k.Id)).ToDictionaryAsync(k=>k.Id,k=>k.Name,ct);
        var eingangszeiten=await db.Dokumentaktivitaeten.AsNoTracking().Where(h=>ids.Contains(h.DokumentId)&&h.Kanal!=null).GroupBy(h=>h.DokumentId).Select(g=>new{Id=g.Key,Zeit=g.Max(h=>h.Zeitpunkt)}).ToDictionaryAsync(h=>h.Id,h=>h.Zeit,ct);
        var verlauf=mitVerlauf?await db.Dokumentaktivitaeten.AsNoTracking().Where(h=>ids.Contains(h.DokumentId)).OrderByDescending(h=>h.Zeitpunkt).Take(2000).ToArrayAsync(ct):[];
        return daten.Select(d=>new Aktendokument(d.Id,d.Titel,d.Dateiname,d.Art,d.KategorieId is null?"Ungeklärt":kategorien.GetValueOrDefault(d.KategorieId,"Ungeklärt"),d.Kanal,d.ErstelltAm,d.Version,d.Inhaltstyp,d.Vorschau,d.Textmethode,zuordnungen.Where(z=>z.DokumentId==d.Id).Select(z=>z.AkteId).ToArray(),d.KategorieId,d.Status,d.AbsenderId,d.AbsenderId is null?"Nicht angegeben":kontakte.GetValueOrDefault(d.AbsenderId,"Nicht angegeben"),Pruefbedarf:d.Pruefbedarf,Aktivitaeten:verlauf.Where(h=>h.DokumentId==d.Id).OrderBy(h=>h.Zeitpunkt).Select(h=>new Eingangsaktivitaet(h.Text,h.Zeitpunkt,h.Benutzer,h.AkteId,h.Kanal,h.VorherigeAkteId)).ToArray(),GeaendertAm:d.GeaendertAm,LetzterEingangAm:eingangszeiten.TryGetValue(d.Id,out var eingang)?eingang:null)).ToArray();
    }
    public async Task<Aktenansicht> AkteLesen(string id,CancellationToken ct=default)
    {
        var a=await Finden<AkteDatensatz>(id,ct);
        var docIds=db.Dokumentzuordnungen.Where(z=>z.AkteId==id).Select(z=>z.DokumentId);
        var docs=await DokumentMetadaten().Where(d=>docIds.Contains(d.Id)).OrderByDescending(d=>d.ErstelltAm).Take(200).ToArrayAsync(ct);
        var notizen=await db.Notizen.AsNoTracking().Where(n=>n.AkteId==id).OrderByDescending(n=>n.GeaendertAm).Take(250).ToArrayAsync(ct);
        var notizIds=notizen.Select(n=>n.Id).ToArray();var revisionen=await db.Notizrevisionen.AsNoTracking().Where(h=>notizIds.Contains(h.NotizId)).OrderByDescending(h=>h.Zeitpunkt).Take(1000).ToArrayAsync(ct);
        var post=await db.Korrespondenz.AsNoTracking().Where(k=>k.AkteId==id).OrderByDescending(k=>k.KommuniziertAm).Take(200).ToArrayAsync(ct);
        var postIds=post.Select(k=>k.Id).ToArray();var refs=await db.Anhaenge.AsNoTracking().Where(h=>postIds.Contains(h.KorrespondenzId)).Take(2000).ToArrayAsync(ct);
        var vorschlaege=await db.Kontaktvorschlaege.AsNoTracking().Where(v=>docIds.Contains(v.DokumentId)).OrderBy(v=>v.Id).Take(100).ToArrayAsync(ct);
        var kontaktIds=vorschlaege.Select(v=>v.KontaktId).Distinct().ToArray();
        var aktuelleKontakte=await db.Kontakte.AsNoTracking().Where(k=>kontaktIds.Contains(k.Id)).ToDictionaryAsync(k=>k.Id,ct);
        var aktuelleWerte=await db.Kontaktwerte.AsNoTracking().Where(w=>kontaktIds.Contains(w.KontaktId)&&w.Aktiv).Take(3000).ToArrayAsync(ct);
        return new(await AkteAnsicht(a,ct),await DokumentAnsichten(docs,ct),notizen.Select(n=>new Aktennotiz(n.Id,n.AkteId,n.Betreff,n.Inhalt,n.Autor,n.ErstelltAm,n.GeaendertAm,n.Version,n.Archiviert,revisionen.Where(h=>h.NotizId==n.Id).Select(h=>new Notizrevision(h.Version,h.Betreff,h.Inhalt,h.Autor,h.Zeitpunkt,h.Aktion)).ToArray())).ToArray(),post.Select(k=>new Akten.Korrespondenz(k.Id,k.AkteId,k.Betreff,k.Inhalt,k.Art,k.Autor,k.ErstelltAm,null,refs.Where(h=>h.KorrespondenzId==k.Id).Select(h=>h.DokumentId).ToArray(),k.Richtung,k.KontaktId,k.KommuniziertAm)).ToArray(),vorschlaege.Select(v=>new Kontaktvorschlag(v.Id,v.KontaktId,v.DokumentId,v.Art,v.Status=="Zur Prüfung"?(v.Art=="Anschrift"?aktuelleKontakte[v.KontaktId].Anschrift:v.Art=="Abteilung"?aktuelleKontakte[v.KontaktId].Abteilung:string.Join(" · ",aktuelleWerte.Where(w=>w.KontaktId==v.KontaktId&&w.Art==v.Art).Select(w=>w.Wert))):v.Bisher,v.Neu,"Dokument",v.Status,aktuelleKontakte[v.KontaktId].Version,aktuelleKontakte[v.KontaktId].Name)).ToArray());
    }
    public async Task<Akten.Korrespondenz> KorrespondenzSpeichern(string akte,Korrespondenzeingabe e,CancellationToken ct=default)
    {
        _=await Finden<AkteDatensatz>(akte,ct);_=await Finden<KontaktDatensatz>(e.KontaktId??"",ct);
        var arten=new[]{"Brief","E-Mail","Antwortschreiben","Anfrage","Erinnerung","Gesprächsvermerk","Sonstige Korrespondenz"};
        if(e.Art is null||!arten.Contains(e.Art)||e.Richtung is not ("Empfangen" or "Gesendet" or "Entwurf")||e.KommuniziertAm is null)throw new Arbeitsfehler("Bitte Art, Richtung, externe Partei und Zeitpunkt der Kommunikation angeben.");
        var ids=(e.DokumentIds??(e.DokumentId is null?[]:[e.DokumentId])).Distinct().ToArray();if(ids.Length>30)throw new Arbeitsfehler("Bitte höchstens 30 Anhänge zuordnen.");
        if(await db.Dokumentzuordnungen.CountAsync(z=>z.AkteId==akte&&ids.Contains(z.DokumentId),ct)!=ids.Length)throw new Arbeitsfehler("Ein Anhang gehört nicht zur aktuellen Akte.");
        var k=new KorrespondenzDatensatz{AkteId=akte,KontaktId=e.KontaktId!,Betreff=Arbeitsregeln.Text(e.Betreff,"Betreff",160),Inhalt=Arbeitsregeln.Text(e.Inhalt,"Inhalt",10000),Art=e.Art,Richtung=e.Richtung,Autor=Arbeitskontext.Benutzer,KommuniziertAm=e.KommuniziertAm.Value.ToUniversalTime()};
        db.Korrespondenz.Add(k);db.Anhaenge.AddRange(ids.Select(id=>new KorrespondenzdokumentDatensatz{KorrespondenzId=k.Id,DokumentId=id}));await db.SaveChangesAsync(ct);
        return new(k.Id,akte,k.Betreff,k.Inhalt,k.Art,k.Autor,k.ErstelltAm,null,ids,k.Richtung,k.KontaktId,k.KommuniziertAm);
    }
}
