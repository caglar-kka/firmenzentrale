using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Persistenz;

public sealed partial class FirmenDbContext
{
    public DbSet<Benutzerkonto> Benutzer=>Set<Benutzerkonto>();
    public DbSet<Ressourcenfreigabe> Freigaben=>Set<Ressourcenfreigabe>();
    public DbSet<Sicherheitsereignis> Sicherheitsereignisse=>Set<Sicherheitsereignis>();
    public bool RechteFiltern=>sicherheit?.Aktiv==true&&!sicherheit.Admin;
    public string AktuellerBenutzer=>sicherheit?.Benutzer??"";
    private void Pilotmodell(ModelBuilder b)
    {
        var u=b.Entity<Benutzerkonto>();u.ToTable("Benutzer");u.HasKey(x=>x.Id);u.HasIndex(x=>x.Login).IsUnique();u.Property(x=>x.Login).HasMaxLength(80);u.Property(x=>x.Anzeigename).HasMaxLength(100);u.Property(x=>x.PasswortHash).HasMaxLength(1000);u.Property(x=>x.Version).IsConcurrencyToken();
        u.ToTable(t=>{t.HasCheckConstraint("CK_Benutzer_Rolle","\"Rolle\" IN ('Administrator','Mitarbeiter')");t.HasCheckConstraint("CK_Benutzer_Pflicht","length(trim(\"Login\")) > 0 AND length(trim(\"Anzeigename\")) > 0 AND length(\"PasswortHash\") > 0");});
        var g=b.Entity<Ressourcenfreigabe>();g.ToTable("Ressourcenfreigaben");g.HasKey(x=>x.Id);g.HasOne<Benutzerkonto>().WithMany().HasForeignKey(x=>x.BenutzerId).OnDelete(DeleteBehavior.Restrict);
        g.HasOne<KontaktDatensatz>().WithMany().HasForeignKey(x=>x.KontaktId).OnDelete(DeleteBehavior.Restrict);g.HasOne<AkteDatensatz>().WithMany().HasForeignKey(x=>x.AkteId).OnDelete(DeleteBehavior.Restrict);g.HasOne<DokumentDatensatz>().WithMany().HasForeignKey(x=>x.DokumentId).OnDelete(DeleteBehavior.Restrict);
        g.HasIndex(x=>new{x.BenutzerId,x.KontaktId}).IsUnique();g.HasIndex(x=>new{x.BenutzerId,x.AkteId}).IsUnique();g.HasIndex(x=>new{x.BenutzerId,x.DokumentId}).IsUnique();
        g.ToTable(t=>t.HasCheckConstraint("CK_Freigabe_Ziel","(CASE WHEN \"KontaktId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"AkteId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"DokumentId\" IS NULL THEN 0 ELSE 1 END) = 1"));
        var h=b.Entity<Sicherheitsereignis>();h.ToTable("Sicherheitsereignisse");h.HasKey(x=>x.Id);h.Property(x=>x.Aktion).HasMaxLength(80);h.Property(x=>x.Ressource).HasMaxLength(100);h.HasIndex(x=>x.Zeitpunkt);h.HasOne<Benutzerkonto>().WithMany().HasForeignKey(x=>x.BenutzerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<KontaktDatensatz>().HasQueryFilter(k=>!RechteFiltern||Freigaben.Any(g=>g.BenutzerId==AktuellerBenutzer&&(g.KontaktId==k.Id||(k.FirmaId!=null&&g.KontaktId==k.FirmaId))));
        b.Entity<AkteDatensatz>().HasQueryFilter(a=>!RechteFiltern||Freigaben.Any(g=>g.BenutzerId==AktuellerBenutzer&&(g.AkteId==a.Id||(a.FirmaId!=null&&g.KontaktId==a.FirmaId))));
        b.Entity<DokumentDatensatz>().HasQueryFilter(d=>!RechteFiltern||Freigaben.Any(g=>g.BenutzerId==AktuellerBenutzer&&g.DokumentId==d.Id)||Dokumentzuordnungen.Any(z=>z.DokumentId==d.Id&&Akten.Any(a=>a.Id==z.AkteId)));
        b.Entity<RechnungDatensatz>().HasQueryFilter(r=>!RechteFiltern||(Akten.Any(a=>a.Id==r.AkteId)&&Kontakte.Any(k=>k.Id==r.AusstellerId)&&Kontakte.Any(k=>k.Id==r.EmpfaengerId)));
        // Invoice access includes its allocated payment facts, not every counterparty's contact record.
        b.Entity<ZahlungDatensatz>().HasQueryFilter(p=>!RechteFiltern||(Kontakte.Any(k=>k.Id==p.ZahlerId)&&Kontakte.Any(k=>k.Id==p.EmpfaengerId))||Zahlungszuordnungen.Any(z=>z.ZahlungId==p.Id));
        b.Entity<KontaktwertDatensatz>().HasQueryFilter(w=>!RechteFiltern||Kontakte.Any(k=>k.Id==w.KontaktId));
        b.Entity<DokumentzuordnungDatensatz>().HasQueryFilter(z=>!RechteFiltern||Akten.Any(a=>a.Id==z.AkteId));
        b.Entity<DokumentaktivitaetDatensatz>().HasQueryFilter(h=>!RechteFiltern||Dokumente.Any(d=>d.Id==h.DokumentId));
        b.Entity<NotizDatensatz>().HasQueryFilter(n=>!RechteFiltern||Akten.Any(a=>a.Id==n.AkteId));
        b.Entity<NotizrevisionDatensatz>().HasQueryFilter(n=>!RechteFiltern||Notizen.Any(x=>x.Id==n.NotizId));
        b.Entity<KorrespondenzDatensatz>().HasQueryFilter(k=>!RechteFiltern||Akten.Any(a=>a.Id==k.AkteId));
        b.Entity<KorrespondenzdokumentDatensatz>().HasQueryFilter(k=>!RechteFiltern||(Korrespondenz.Any(p=>p.Id==k.KorrespondenzId)&&Dokumente.Any(d=>d.Id==k.DokumentId)));
        b.Entity<KontaktverlaufDatensatz>().HasQueryFilter(k=>!RechteFiltern||Kontakte.Any(x=>x.Id==k.KontaktId));
        b.Entity<KontaktvorschlagDatensatz>().HasQueryFilter(k=>!RechteFiltern||(Kontakte.Any(x=>x.Id==k.KontaktId)&&Dokumente.Any(d=>d.Id==k.DokumentId)));
        b.Entity<ZahlungsbestaetigungDatensatz>().HasQueryFilter(z=>!RechteFiltern||Zahlungen.Any(p=>p.Id==z.ZahlungId));
        b.Entity<ZahlungszuordnungDatensatz>().HasQueryFilter(z=>!RechteFiltern||Rechnungen.Any(r=>r.Id==z.RechnungId));
    }
    private void NeueFreigaben()
    {
        if(!RechteFiltern)return;
        foreach(var e in ChangeTracker.Entries().Where(e=>e.State==EntityState.Added).ToArray()){
            Ressourcenfreigabe? g=e.Entity switch{KontaktDatensatz k when k.FirmaId is null=>new(){KontaktId=k.Id},AkteDatensatz a=>new(){AkteId=a.Id},DokumentDatensatz d=>new(){DokumentId=d.Id},_=>null};
            if(g is not null){g.BenutzerId=AktuellerBenutzer;Freigaben.Add(g);}
        }
    }
}
