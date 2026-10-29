using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;

namespace Betriebsplattform.Web.Persistenz;

public sealed partial class FirmenDbContext
{
    public DbSet<Rechtevorlagenversion> Rechtevorlagenversionen=>Set<Rechtevorlagenversion>();
    public DbSet<BenutzerRechteprofil> BenutzerRechteprofile=>Set<BenutzerRechteprofil>();
    public DbSet<Berechtigungsaenderung> Berechtigungsaenderungen=>Set<Berechtigungsaenderung>();
    private void Berechtigungsmodell(ModelBuilder b)
    {
        var v=b.Entity<Rechtevorlagenversion>();v.ToTable("Rechtevorlagenversionen");v.HasKey(x=>x.Id);v.Property(x=>x.Id).HasMaxLength(64);v.Property(x=>x.Bereich).HasMaxLength(64);v.Property(x=>x.Name).HasMaxLength(100);v.Property(x=>x.RechteJson).HasMaxLength(2000);v.HasData(Kontaktberechtigungen.Standardvorlagen());
        var p=b.Entity<BenutzerRechteprofil>();p.ToTable("BenutzerRechteprofile");p.HasKey(x=>new{x.BenutzerId,x.Bereich});p.Property(x=>x.BenutzerId).HasMaxLength(64);p.Property(x=>x.Bereich).HasMaxLength(64);p.Property(x=>x.VorlageId).HasMaxLength(64);p.Property(x=>x.Modus).HasMaxLength(20);p.Property(x=>x.AbweichungenJson).HasMaxLength(2000);p.Property(x=>x.Version).IsConcurrencyToken();
        p.HasOne<Benutzerkonto>().WithMany().HasForeignKey(x=>x.BenutzerId).OnDelete(DeleteBehavior.Restrict);p.HasOne<Rechtevorlagenversion>().WithMany().HasForeignKey(x=>x.VorlageId).OnDelete(DeleteBehavior.Restrict);
        p.ToTable(t=>{t.HasCheckConstraint("CK_Rechteprofil_Modus","\"Modus\" IN ('Bestand','Explizit')");t.HasCheckConstraint("CK_Rechteprofil_Version","\"Version\" > 0");});
        var a=b.Entity<Berechtigungsaenderung>();a.ToTable("Berechtigungsaenderungen");a.HasKey(x=>x.Id);a.Property(x=>x.Id).HasMaxLength(64);a.Property(x=>x.AkteurId).HasMaxLength(64);a.Property(x=>x.ZielId).HasMaxLength(64);a.Property(x=>x.Bereich).HasMaxLength(64);a.Property(x=>x.Vorher).HasMaxLength(16000);a.Property(x=>x.Nachher).HasMaxLength(16000);
        a.HasOne<Benutzerkonto>().WithMany().HasForeignKey(x=>x.AkteurId).OnDelete(DeleteBehavior.Restrict);a.HasOne<Benutzerkonto>().WithMany().HasForeignKey(x=>x.ZielId).OnDelete(DeleteBehavior.Restrict);
    }
    public async Task<bool> KontaktaktionErlaubt(string aktion,CancellationToken ct=default)=>sicherheit?.Aktiv!=true||await Kontaktberechtigungen.Erlaubt(this,AktuellerBenutzer,aktion,ct);
    public async Task KontaktaktionPruefen(string aktion,CancellationToken ct=default){if(!await KontaktaktionErlaubt(aktion,ct))throw new Betriebsplattform.Web.Akten.Arbeitsfehler("Für diese Kontaktaktion fehlt die Berechtigung.",403);}
}
