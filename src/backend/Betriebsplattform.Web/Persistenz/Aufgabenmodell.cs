using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Persistenz;
public sealed partial class FirmenDbContext
{
    public DbSet<Aufgabe> Aufgaben=>Set<Aufgabe>();
    public DbSet<Aufgabenverlauf> Aufgabenverlauf=>Set<Aufgabenverlauf>();
    private void Aufgabenmodell(ModelBuilder b)
    {
        var a=Versioniert<Aufgabe>(b,"Aufgaben");a.Property(x=>x.Betreff).HasMaxLength(160);a.Property(x=>x.Beschreibung).HasMaxLength(4000);a.Property(x=>x.Prioritaet).HasMaxLength(20);a.Property(x=>x.Status).HasMaxLength(30);
        Fk<Aufgabe,Benutzerkonto>(a,nameof(Aufgabe.ErstellerId));Fk<Aufgabe,Benutzerkonto>(a,nameof(Aufgabe.ZustaendigId));Fk<Aufgabe,KontaktDatensatz>(a,nameof(Aufgabe.FirmaId));Fk<Aufgabe,KontaktDatensatz>(a,nameof(Aufgabe.KontaktId));Fk<Aufgabe,AkteDatensatz>(a,nameof(Aufgabe.AkteId));Fk<Aufgabe,RechnungDatensatz>(a,nameof(Aufgabe.RechnungId));Fk<Aufgabe,DokumentDatensatz>(a,nameof(Aufgabe.DokumentId));
        a.HasIndex(x=>new{x.ZustaendigId,x.Archiviert,x.FaelligAm});
        Check(a,"CK_Aufgabe_Status","\"Status\" IN ('Offen','In Bearbeitung','Erledigt','Abgebrochen')");Check(a,"CK_Aufgabe_Prioritaet","\"Prioritaet\" IN ('Niedrig','Normal','Hoch','Dringend')");
        Check(a,"CK_Aufgabe_Zeit","\"FaelligUm\" IS NULL OR \"FaelligAm\" IS NOT NULL");
        Check(a,"CK_Aufgabe_Bezug","(CASE WHEN \"RechnungId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"AkteId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"KontaktId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"DokumentId\" IS NULL THEN 0 ELSE 1 END) <= 1");
        var v=b.Entity<Aufgabenverlauf>();v.ToTable("Aufgabenverlauf");v.HasKey(x=>x.Id);v.Property(x=>x.Vorher).HasMaxLength(32000);v.Property(x=>x.Nachher).HasMaxLength(32000);v.HasIndex(x=>new{x.AufgabeId,x.Zeitpunkt});Fk<Aufgabenverlauf,Aufgabe>(v,"AufgabeId");Fk<Aufgabenverlauf,Benutzerkonto>(v,"BenutzerId");
    }
}
