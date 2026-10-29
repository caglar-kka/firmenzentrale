using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Persistenz;
public sealed partial class FirmenDbContext
{
    public DbSet<Eingangsworkflow> Eingangsworkflows=>Set<Eingangsworkflow>();
    private void Eingangsmodell(ModelBuilder b)
    {
        var w=b.Entity<Eingangsworkflow>();w.ToTable("Eingangsworkflows");w.HasKey(x=>x.Id);w.Property(x=>x.Version).IsConcurrencyToken();w.Property(x=>x.AngabenJson).HasMaxLength(5000);w.Property(x=>x.Rueckfrage).HasMaxLength(1000);w.Property(x=>x.Status).HasMaxLength(30);
        w.HasOne<DokumentaktivitaetDatensatz>().WithOne().HasForeignKey<Eingangsworkflow>(x=>x.Id).OnDelete(DeleteBehavior.Restrict);
        Fk<Eingangsworkflow,Benutzerkonto>(w,nameof(Eingangsworkflow.ZustaendigId));Fk<Eingangsworkflow,Benutzerkonto>(w,nameof(Eingangsworkflow.GeprueftVonId));
        Fk<Eingangsworkflow,KontaktDatensatz>(w,nameof(Eingangsworkflow.AbsenderId));Fk<Eingangsworkflow,AkteDatensatz>(w,nameof(Eingangsworkflow.AkteId));Fk<Eingangsworkflow,Aufgabe>(w,nameof(Eingangsworkflow.AufgabeId));w.HasIndex(x=>x.AufgabeId).IsUnique();
        w.ToTable(t=>t.HasCheckConstraint("CK_Eingang_Status","\"Status\" IN ('Neu','Zur Prüfung','Zugeordnet','Erledigt')"));
        b.Entity<DokumentaktivitaetDatensatz>().HasOne<DokumentaktivitaetDatensatz>().WithMany().HasForeignKey(x=>x.EingangsId).OnDelete(DeleteBehavior.Restrict);
        Fk<DokumentaktivitaetDatensatz,KontaktDatensatz>(b.Entity<DokumentaktivitaetDatensatz>(),nameof(DokumentaktivitaetDatensatz.PruefAbsenderId));
        Fk<DokumentaktivitaetDatensatz,KontaktDatensatz>(b.Entity<DokumentaktivitaetDatensatz>(),nameof(DokumentaktivitaetDatensatz.VorherigerAbsenderId));
    }
}
