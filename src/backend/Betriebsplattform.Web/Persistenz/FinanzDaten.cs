using Betriebsplattform.Rechnungen;
using Betriebsplattform.Web.Akten;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Persistenz;

// Persistence adapter replays only the relevant aggregate through the existing financial core.
// No new HTTP payment/confirmation operation is exposed by this milestone.
public sealed class FinanzDaten(FirmenDbContext db)
{
    private static Rechnung Rechnung(RechnungDatensatz r)=>new(Guid.Parse(r.Id),r.Nummer,r.AusstellerId,r.EmpfaengerId,r.Datum,r.Betrag,new(r.Beleg,r.Quelle));
    private static Zahlung Zahlung(ZahlungDatensatz z)=>new(Guid.Parse(z.Id),z.ZahlerId,z.EmpfaengerId,z.Datum,z.Betrag,new(z.Beleg,z.Quelle));
    public async Task RechnungErfassen(Rechnung r,string akte,string richtung,CancellationToken ct=default)
    {
        if(richtung is not ("Eingang" or "Ausgang" or "Ungeklärt"))throw new Arbeitsfehler("Ungültige Rechnungsrichtung.");
        db.Rechnungen.Add(new(){Id=r.Id.ToString("N"),Nummer=r.Nummer,AusstellerId=r.Aussteller,EmpfaengerId=r.Empfaenger,AkteId=akte,Datum=r.Datum,Betrag=r.Betrag,Beleg=r.Nachweis.Beleg,Quelle=r.Nachweis.Quellenort,Richtung=richtung});await db.SaveChangesAsync(ct);
    }
    public async Task ZahlungErfassen(Zahlung z,CancellationToken ct=default)
    {db.Zahlungen.Add(new(){Id=z.Id.ToString("N"),ZahlerId=z.Zahler,EmpfaengerId=z.Empfaenger,Datum=z.Datum,Betrag=z.Betrag,Beleg=z.Nachweis.Beleg,Quelle=z.Nachweis.Quellenort});await db.SaveChangesAsync(ct);}
    public async Task Bestaetigen(string id,int version,Nachweis nachweis,string actor,CancellationToken ct=default)
    {
        var z=await db.Zahlungen.SingleOrDefaultAsync(z=>z.Id==FirmenDaten.Id(id),ct)??throw new Arbeitsfehler("Zahlung nicht gefunden.",404);Arbeitsregeln.Version(version,z.Version);
        var kern=new Rechnungskern();kern.ZahlungErfassen(Zahlung(z));var jetzt=DateTimeOffset.UtcNow;kern.ZahlungBestaetigen(Guid.Parse(z.Id),nachweis,actor,jetzt);
        db.Zahlungsbestaetigungen.Add(new(){ZahlungId=id,Beleg=nachweis.Beleg,Quelle=nachweis.Quellenort,Benutzer=actor,Zeitpunkt=jetzt});z.GeaendertAm=jetzt;await db.SaveChangesAsync(ct);
    }
    public async Task Zuordnen(string rechnung,string zahlung,int rechnungVersion,int zahlungVersion,decimal betrag,Nachweis nachweis,string actor,CancellationToken ct=default)
    {
        var r=await db.Rechnungen.SingleOrDefaultAsync(r=>r.Id==FirmenDaten.Id(rechnung),ct)??throw new Arbeitsfehler("Rechnung nicht gefunden.",404);
        var z=await db.Zahlungen.SingleOrDefaultAsync(z=>z.Id==FirmenDaten.Id(zahlung),ct)??throw new Arbeitsfehler("Zahlung nicht gefunden.",404);Arbeitsregeln.Version(rechnungVersion,r.Version);Arbeitsregeln.Version(zahlungVersion,z.Version);
        var bisher=await db.Zahlungszuordnungen.AsNoTracking().Where(a=>a.RechnungId==rechnung||a.ZahlungId==zahlung).Take(1001).ToArrayAsync(ct);
        if(bisher.Length>1000)throw new Arbeitsfehler("Dieser umfangreiche Zahlungsvorgang benötigt eine gesonderte Prüfung.");
        var rids=bisher.Select(a=>a.RechnungId).Append(rechnung).Distinct().ToArray();var zids=bisher.Select(a=>a.ZahlungId).Append(zahlung).Distinct().ToArray();
        var kern=new Rechnungskern();foreach(var rr in await db.Rechnungen.AsNoTracking().Where(x=>rids.Contains(x.Id)).ToArrayAsync(ct))kern.RechnungErfassen(Rechnung(rr));
        foreach(var zz in await db.Zahlungen.AsNoTracking().Where(x=>zids.Contains(x.Id)).ToArrayAsync(ct))kern.ZahlungErfassen(Zahlung(zz));
        foreach(var b in await db.Zahlungsbestaetigungen.AsNoTracking().Where(x=>zids.Contains(x.ZahlungId)).ToArrayAsync(ct))kern.ZahlungBestaetigen(Guid.Parse(b.ZahlungId),new(b.Beleg,b.Quelle),b.Benutzer,b.Zeitpunkt);
        foreach(var a in bisher)kern.Zuordnen(Guid.Parse(a.Id),Guid.Parse(a.RechnungId),Guid.Parse(a.ZahlungId),a.Betrag,new(a.Beleg,a.Quelle),a.Benutzer,a.Zeitpunkt);
        var jetzt=DateTimeOffset.UtcNow;var neu=kern.Zuordnen(Guid.NewGuid(),Guid.Parse(rechnung),Guid.Parse(zahlung),betrag,nachweis,actor,jetzt);
        db.Zahlungszuordnungen.Add(new(){Id=neu.Id.ToString("N"),RechnungId=rechnung,ZahlungId=zahlung,Betrag=betrag,Beleg=nachweis.Beleg,Quelle=nachweis.Quellenort,Benutzer=actor,Zeitpunkt=jetzt});
        var offen=kern.OffenerBetrag(Guid.Parse(rechnung));r.Zahlungsstatus=offen==0?"Bezahlt":offen==r.Betrag?"Offen":"Teilweise bezahlt";r.GeaendertAm=jetzt;z.GeaendertAm=jetzt;await db.SaveChangesAsync(ct);
    }
}
