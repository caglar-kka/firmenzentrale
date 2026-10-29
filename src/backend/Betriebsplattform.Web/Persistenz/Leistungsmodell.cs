using Betriebsplattform.Web.Leistungen;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Persistenz;
public sealed partial class FirmenDbContext {
 public DbSet<Katalogleistung> Leistungskatalog=>Set<Katalogleistung>();
 public DbSet<Leistungsobjekt> Leistungsobjekte=>Set<Leistungsobjekt>();public DbSet<Leistungsplan> Leistungsplaene=>Set<Leistungsplan>();public DbSet<Leistungseinsatz> Leistungseinsaetze=>Set<Leistungseinsatz>();public DbSet<Leistungsnachweis> Leistungsnachweise=>Set<Leistungsnachweis>();public DbSet<Leistungsaktion> Leistungsaktionen=>Set<Leistungsaktion>();
 // Current model configuration; generated migration snapshots contain their own frozen model.
 public static void Leistungsmodell20261023(ModelBuilder b){
  var k=Versioniert<Katalogleistung>(b,"Leistungskatalog");k.Property(x=>x.Name).HasMaxLength(160);k.Property(x=>x.Beschreibung).HasMaxLength(1000);k.Property(x=>x.Kategorie).HasMaxLength(100);k.HasIndex(x=>new{x.Reihenfolge,x.Name});
  var o=Versioniert<Leistungsobjekt>(b,"Leistungsobjekte");o.Property(x=>x.Name).HasMaxLength(160);o.Property(x=>x.Anschrift).HasMaxLength(500);o.Property(x=>x.AnlageHash).HasMaxLength(64);Fk<Leistungsobjekt,KontaktDatensatz>(o,"KundeId");Fk<Leistungsobjekt,AkteDatensatz>(o,"AkteId");Fk<Leistungsobjekt,Benutzerkonto>(o,"BueroId");o.HasIndex(x=>x.KundeId);o.Property(x=>x.Bemerkung).HasMaxLength(2000);
  var p=Versioniert<Leistungsplan>(b,"Leistungsplaene");Fk<Leistungsplan,Leistungsobjekt>(p,"ObjektId");p.Property(x=>x.Name).HasMaxLength(160);p.Property(x=>x.Positionen).HasMaxLength(64000);p.Property(x=>x.AnlageHash).HasMaxLength(64);p.HasIndex(x=>new{x.ObjektId,x.Nummer}).IsUnique();
  var e=Versioniert<Leistungseinsatz>(b,"Leistungseinsaetze");Fk<Leistungseinsatz,Leistungsobjekt>(e,"ObjektId");Fk<Leistungseinsatz,Leistungsplan>(e,"PlanId");Fk<Leistungseinsatz,Benutzerkonto>(e,"MitarbeiterId");Fk<Leistungseinsatz,Leistungseinsatz>(e,"VorgaengerId");e.Property(x=>x.Status).HasMaxLength(80);e.Property(x=>x.Daten).HasMaxLength(100000);e.Property(x=>x.AnlageHash).HasMaxLength(64);e.Property(x=>x.Aenderungsgrund).HasMaxLength(1000);e.HasIndex(x=>new{x.ObjektId,x.Arbeitstag});
  var n=Eintrag<Leistungsnachweis>(b,"Leistungsnachweise");Fk<Leistungsnachweis,Leistungseinsatz>(n,"EinsatzId");Fk<Leistungsnachweis,Benutzerkonto>(n,"FreigegebenDurch");n.Property(x=>x.FreigegebenDurch).HasMaxLength(64);n.Property(x=>x.StorageKey).HasMaxLength(64);n.Property(x=>x.Hash).HasMaxLength(64);n.HasIndex(x=>x.EinsatzId).IsUnique();
  var a=Eintrag<Leistungsaktion>(b,"Leistungsaktionen");Fk<Leistungsaktion,Leistungsobjekt>(a,"ObjektId");Fk<Leistungsaktion,Benutzerkonto>(a,"BenutzerId");a.Property(x=>x.Art).HasMaxLength(80);a.Property(x=>x.AnfrageHash).HasMaxLength(64);a.Property(x=>x.Ergebnis).HasMaxLength(200000);a.HasIndex(x=>new{x.ObjektId,x.Zeitpunkt});
 }
}
