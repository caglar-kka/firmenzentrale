using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Persistenz;
public sealed partial class FirmenDbContext
{
    public DbSet<Dokumentmuster> Dokumentmuster=>Set<Dokumentmuster>();
    private void Dokumentmustermodell(ModelBuilder b){
        var m=Versioniert<Dokumentmuster>(b,"Dokumentmuster");m.Property(x=>x.Feld).HasMaxLength(40);m.Property(x=>x.Anker).HasMaxLength(160);m.Property(x=>x.Strategie).HasMaxLength(30);m.Property(x=>x.LayoutHash).HasMaxLength(64);m.Property(x=>x.Dokumentart).HasMaxLength(40);m.Property(x=>x.Status).HasMaxLength(20);m.Property(x=>x.AutomatikAktion).HasMaxLength(30);
        Fk<Dokumentmuster,KontaktDatensatz>(m,nameof(Betriebsplattform.Web.Akten.Dokumentmuster.OrganisationId));Fk<Dokumentmuster,KontaktDatensatz>(m,nameof(Betriebsplattform.Web.Akten.Dokumentmuster.AbsenderId));Fk<Dokumentmuster,DokumentDatensatz>(m,nameof(Betriebsplattform.Web.Akten.Dokumentmuster.Quelle1Id));Fk<Dokumentmuster,DokumentDatensatz>(m,nameof(Betriebsplattform.Web.Akten.Dokumentmuster.Quelle2Id));Fk<Dokumentmuster,Benutzerkonto>(m,nameof(Betriebsplattform.Web.Akten.Dokumentmuster.FreigabeVonId));
        m.HasIndex(x=>new{x.OrganisationId,x.AbsenderId,x.Feld,x.MusterVersion}).IsUnique();Check(m,"Muster_Status","\"Status\" IN ('Entwurf','Aktiv','Pausiert')");Check(m,"Muster_Aktion","\"AutomatikAktion\" IS NULL OR \"AutomatikAktion\" = 'Klassifizieren'");Check(m,"Muster_Version","\"MusterVersion\" > 0");
        Fk<Dokumentmuster,Eingangsworkflow>(m,nameof(Betriebsplattform.Web.Akten.Dokumentmuster.Quelle1EingangId));Fk<Dokumentmuster,Eingangsworkflow>(m,nameof(Betriebsplattform.Web.Akten.Dokumentmuster.Quelle2EingangId));
    }
}
