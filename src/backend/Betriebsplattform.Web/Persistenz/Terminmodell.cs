using Betriebsplattform.Web.Aufgaben;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Persistenz;
public sealed partial class FirmenDbContext
{
    public DbSet<Termin> Termine => Set<Termin>();
    public DbSet<Terminverlauf> Terminverlauf => Set<Terminverlauf>();
    private void Terminmodell(ModelBuilder b)
    {
        var t = Versioniert<Termin>(b, "Termine");
        t.Property(x => x.Titel).HasMaxLength(160); t.Property(x => x.Beschreibung).HasMaxLength(4000); t.Property(x => x.Ort).HasMaxLength(200);
        t.Property(x=>x.Strasse).HasMaxLength(160);t.Property(x=>x.Hausnummer).HasMaxLength(30);t.Property(x=>x.Plz).HasMaxLength(20);t.Property(x=>x.Stadt).HasMaxLength(120);t.Property(x=>x.Land).HasMaxLength(80);t.Property(x=>x.Erinnerungskennung).HasMaxLength(32);t.HasIndex(x=>new{x.EigentuemerId,x.ErinnerungFaelligUtc});
        t.Property(x => x.Beginn).HasMaxLength(16); t.Property(x => x.Ende).HasMaxLength(16); t.Property(x => x.AnlageHash).HasMaxLength(64);
        Fk<Termin, Benutzerkonto>(t, nameof(Termin.EigentuemerId)); t.HasIndex(x => new { x.EigentuemerId, x.Entfernt, x.Beginn });
        Check(t, "Termin_Zeit", "\"EndeUtc\" > \"BeginnUtc\""); Check(t, "Termin_Titel", "length(trim(\"Titel\")) > 0");
        var v = Eintrag<Terminverlauf>(b, "Terminverlauf"); v.Property(x => x.Vorher).HasMaxLength(32000); v.Property(x => x.Nachher).HasMaxLength(32000);
        Fk<Terminverlauf, Termin>(v, "TerminId"); Fk<Terminverlauf, Benutzerkonto>(v, "BenutzerId"); v.HasIndex(x => new { x.TerminId, x.Zeitpunkt });
    }
}
