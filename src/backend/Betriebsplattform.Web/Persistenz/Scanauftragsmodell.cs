using Betriebsplattform.Web.Akten;
namespace Betriebsplattform.Web.Persistenz;
public sealed partial class FirmenDbContext
{
    public Microsoft.EntityFrameworkCore.DbSet<Scanauftrag> Scanauftraege=>Set<Scanauftrag>();
    private void Scanauftragsmodell(Microsoft.EntityFrameworkCore.ModelBuilder b){
        var j=Versioniert<Scanauftrag>(b,"Scanauftraege");j.Property(x=>x.QuelleId).HasMaxLength(64);j.Property(x=>x.Dateiname).HasMaxLength(180);j.Property(x=>x.Hash).HasMaxLength(64);j.Property(x=>x.Status).HasMaxLength(30);j.Property(x=>x.Fehlercode).HasMaxLength(100);Fk<Scanauftrag,DokumentDatensatz>(j,"DokumentId");j.HasIndex(x=>new{x.QuelleId,x.Hash}).IsUnique();
        Check(j,"Scanauftrag_Status","\"Status\" IN ('Wartend','In Bearbeitung','Erledigt','Blockiert','Fehler')");Check(j,"Scanauftrag_Versuche","\"Versuche\" >= 0 AND \"Versuche\" <= 3");
        var h=b.Entity<DokumentaktivitaetDatensatz>();h.HasOne<Scanauftrag>().WithOne().HasForeignKey<DokumentaktivitaetDatensatz>(x=>x.ScanauftragId).OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);h.HasIndex(x=>x.ScanauftragId).IsUnique();
    }
}
