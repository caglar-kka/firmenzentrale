using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Betriebsplattform.Web.Persistenz;

public sealed partial class FirmenDbContext(DbContextOptions<FirmenDbContext> options,Betriebsplattform.Web.Sicherheit.Ressourcenbereich? sicherheit=null) : DbContext(options)
{
    public DbSet<KontaktDatensatz> Kontakte => Set<KontaktDatensatz>();
    public DbSet<KontaktwertDatensatz> Kontaktwerte => Set<KontaktwertDatensatz>();
    public DbSet<KategorieDatensatz> Kategorien => Set<KategorieDatensatz>();
    public DbSet<AkteDatensatz> Akten => Set<AkteDatensatz>();
    public DbSet<DokumentDatensatz> Dokumente => Set<DokumentDatensatz>();
    public DbSet<DokumentzuordnungDatensatz> Dokumentzuordnungen => Set<DokumentzuordnungDatensatz>();
    public DbSet<DokumentaktivitaetDatensatz> Dokumentaktivitaeten => Set<DokumentaktivitaetDatensatz>();
    public DbSet<NotizDatensatz> Notizen => Set<NotizDatensatz>();
    public DbSet<NotizrevisionDatensatz> Notizrevisionen => Set<NotizrevisionDatensatz>();
    public DbSet<KorrespondenzDatensatz> Korrespondenz => Set<KorrespondenzDatensatz>();
    public DbSet<KorrespondenzdokumentDatensatz> Anhaenge => Set<KorrespondenzdokumentDatensatz>();
    public DbSet<KontaktverlaufDatensatz> Kontaktverlauf => Set<KontaktverlaufDatensatz>();
    public DbSet<KontaktvorschlagDatensatz> Kontaktvorschlaege => Set<KontaktvorschlagDatensatz>();
    public DbSet<RechnungDatensatz> Rechnungen => Set<RechnungDatensatz>();
    public DbSet<ZahlungDatensatz> Zahlungen => Set<ZahlungDatensatz>();
    public DbSet<ZahlungsbestaetigungDatensatz> Zahlungsbestaetigungen => Set<ZahlungsbestaetigungDatensatz>();
    public DbSet<ZahlungszuordnungDatensatz> Zahlungszuordnungen => Set<ZahlungszuordnungDatensatz>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var k=Versioniert<KontaktDatensatz>(b,"Kontakte");
        k.Property(x=>x.Name).HasMaxLength(160);k.Property(x=>x.Suchname).HasMaxLength(200);
        k.Property(x=>x.Vorname).HasMaxLength(80);k.Property(x=>x.Nachname).HasMaxLength(80);
        k.Property(x=>x.Anschrift).HasMaxLength(500);k.Property(x=>x.Abteilung).HasMaxLength(100);k.Property(x=>x.Rolle).HasMaxLength(100);
        k.HasIndex(x=>x.Suchname);k.HasIndex(x=>new{x.Nachname,x.Vorname});k.HasIndex(x=>new{x.Plz,x.Ort});
        Fk<KontaktDatensatz,KontaktDatensatz>(k,nameof(KontaktDatensatz.FirmaId));
        Check(k,"Kontakt_Name","length(trim(\"Name\")) > 0");
        Check(k,"Kontakt_Art","\"Art\" IN ('Firma','Person') AND (\"FirmaId\" IS NULL OR \"Art\" = 'Person') AND (\"FirmaId\" IS NULL OR \"FirmaId\" <> \"Id\")");
        Check(k,"Person_Name","\"Art\" <> 'Person' OR (length(trim(\"Vorname\")) > 0 AND length(trim(\"Nachname\")) > 0)");
        var w=Eintrag<KontaktwertDatensatz>(b,"Kontaktwerte");Fk<KontaktwertDatensatz,KontaktDatensatz>(w,"KontaktId");
        w.HasIndex(x=>new{x.Art,x.Vergleichswert});w.HasIndex(x=>new{x.KontaktId,x.Art}).IsUnique().HasFilter("\"Primaer\" AND \"Aktiv\"");
        Check(w,"Kontaktwert_Art","\"Art\" IN ('Telefon','E-Mail','Website') AND length(trim(\"Wert\")) > 0");
        var c=Versioniert<KategorieDatensatz>(b,"Kategorien");c.Property(x=>x.Name).HasMaxLength(80);c.Property(x=>x.Suchname).HasMaxLength(100);c.Property(x=>x.Beschreibung).HasMaxLength(500);
        c.HasIndex(x=>x.Suchname).IsUnique();c.HasIndex(x=>new{x.ElternId,x.Reihenfolge});Fk<KategorieDatensatz,KategorieDatensatz>(c,"ElternId");
        Check(c,"Kategorie_Name","length(trim(\"Name\")) > 0");Check(c,"Kategorie_Parent","\"ElternId\" IS NULL OR \"ElternId\" <> \"Id\"");Check(c,"Kategorie_Reihenfolge","\"Reihenfolge\" >= 0 AND \"Reihenfolge\" <= 10000");
        var a=Versioniert<AkteDatensatz>(b,"Akten");a.Property(x=>x.Titel).HasMaxLength(160);a.Property(x=>x.Suchtext).HasMaxLength(1000);
        Fk<AkteDatensatz,KategorieDatensatz>(a,"KategorieId");Fk<AkteDatensatz,KontaktDatensatz>(a,"FirmaId");
        a.HasIndex(x=>new{x.Status,x.GeaendertAm});a.HasIndex(x=>x.Suchtext);Check(a,"Akte_Titel","length(trim(\"Titel\")) > 0");
        Check(a,"Akte_Status","\"Status\" IN ('Offen','In Bearbeitung','Wartet auf Rückmeldung','Erledigt','Archiviert')");
        var d=Versioniert<DokumentDatensatz>(b,"Dokumente");d.Property(x=>x.Titel).HasMaxLength(160);d.Property(x=>x.Dateiname).HasMaxLength(180);d.Property(x=>x.StorageKey).HasMaxLength(64);d.Property(x=>x.Hash).HasMaxLength(64);d.Property(x=>x.Suchtext).HasMaxLength(1000);d.Property(x=>x.Text).HasMaxLength(64000);
        d.HasIndex(x=>x.Hash).IsUnique();d.HasIndex(x=>x.StorageKey).IsUnique();d.HasIndex(x=>new{x.Status,x.ErstelltAm});d.HasIndex(x=>x.GeaendertAm);
        Fk<DokumentDatensatz,KategorieDatensatz>(d,"KategorieId");Fk<DokumentDatensatz,KontaktDatensatz>(d,"AbsenderId");
        Check(d,"Dokument_Datei","\"Groesse\" > 0 AND \"Groesse\" <= 20000000 AND length(\"Hash\") = 64 AND length(\"StorageKey\") = 32");
        var z=b.Entity<DokumentzuordnungDatensatz>();z.ToTable("Dokumentzuordnungen");z.HasKey(x=>x.DokumentId);Fk<DokumentzuordnungDatensatz,DokumentDatensatz>(z,"DokumentId");Fk<DokumentzuordnungDatensatz,AkteDatensatz>(z,"AkteId");
        var h=Eintrag<DokumentaktivitaetDatensatz>(b,"Dokumentaktivitaeten");Fk<DokumentaktivitaetDatensatz,DokumentDatensatz>(h,"DokumentId");Fk<DokumentaktivitaetDatensatz,AkteDatensatz>(h,"AkteId");Fk<DokumentaktivitaetDatensatz,AkteDatensatz>(h,"VorherigeAkteId");h.Property(x=>x.Text).HasMaxLength(500);h.HasIndex(x=>new{x.DokumentId,x.Zeitpunkt});
        var n=Versioniert<NotizDatensatz>(b,"Notizen");Fk<NotizDatensatz,AkteDatensatz>(n,"AkteId");n.Property(x=>x.Betreff).HasMaxLength(160);n.Property(x=>x.Inhalt).HasMaxLength(10000);n.HasIndex(x=>new{x.AkteId,x.GeaendertAm});Check(n,"Notiz_Text","length(trim(\"Betreff\")) > 0 AND length(trim(\"Inhalt\")) > 0");
        var nr=Eintrag<NotizrevisionDatensatz>(b,"Notizrevisionen");Fk<NotizrevisionDatensatz,NotizDatensatz>(nr,"NotizId");nr.Property(x=>x.Betreff).HasMaxLength(160);nr.Property(x=>x.Inhalt).HasMaxLength(10000);nr.HasIndex(x=>new{x.NotizId,x.Version}).IsUnique();
        var p=Versioniert<KorrespondenzDatensatz>(b,"Korrespondenz");Fk<KorrespondenzDatensatz,AkteDatensatz>(p,"AkteId");Fk<KorrespondenzDatensatz,KontaktDatensatz>(p,"KontaktId");p.Property(x=>x.Betreff).HasMaxLength(160);p.Property(x=>x.Inhalt).HasMaxLength(10000);p.HasIndex(x=>new{x.AkteId,x.KommuniziertAm});Check(p,"Korrespondenz_Richtung","\"Richtung\" IN ('Empfangen','Gesendet','Entwurf')");
        var pd=b.Entity<KorrespondenzdokumentDatensatz>();pd.ToTable("Korrespondenzdokumente");pd.HasKey(x=>new{x.KorrespondenzId,x.DokumentId});Fk<KorrespondenzdokumentDatensatz,KorrespondenzDatensatz>(pd,"KorrespondenzId");Fk<KorrespondenzdokumentDatensatz,DokumentDatensatz>(pd,"DokumentId");
        var kh=Eintrag<KontaktverlaufDatensatz>(b,"Kontaktverlauf");Fk<KontaktverlaufDatensatz,KontaktDatensatz>(kh,"KontaktId");Fk<KontaktverlaufDatensatz,DokumentDatensatz>(kh,"DokumentId");kh.Property(x=>x.Vorher).HasMaxLength(10000);kh.Property(x=>x.Nachher).HasMaxLength(10000);kh.HasIndex(x=>new{x.KontaktId,x.Zeitpunkt});
        var kv=Versioniert<KontaktvorschlagDatensatz>(b,"Kontaktvorschlaege");Fk<KontaktvorschlagDatensatz,KontaktDatensatz>(kv,"KontaktId");Fk<KontaktvorschlagDatensatz,DokumentDatensatz>(kv,"DokumentId");kv.HasIndex(x=>new{x.KontaktId,x.Status});
        var r=Versioniert<RechnungDatensatz>(b,"Rechnungen");Fk<RechnungDatensatz,KontaktDatensatz>(r,"AusstellerId");Fk<RechnungDatensatz,KontaktDatensatz>(r,"EmpfaengerId");Fk<RechnungDatensatz,AkteDatensatz>(r,"AkteId");r.Property(x=>x.Betrag).HasPrecision(18,2);r.Property(x=>x.Nummer).HasMaxLength(160);r.HasIndex(x=>x.AkteId).IsUnique();r.HasIndex(x=>new{x.AusstellerId,x.Nummer,x.Betrag});r.HasIndex(x=>new{x.Zahlungsstatus,x.Datum});Check(r,"Rechnung_Betrag","\"Betrag\" > 0");Check(r,"Rechnung_Status","\"Zahlungsstatus\" IN ('Offen','Teilweise bezahlt','Bezahlt')");Check(r,"Rechnung_Richtung","\"Richtung\" IN ('Eingang','Ausgang','Ungeklärt')");
        var zahlung=Versioniert<ZahlungDatensatz>(b,"Zahlungen");Fk<ZahlungDatensatz,KontaktDatensatz>(zahlung,"ZahlerId");Fk<ZahlungDatensatz,KontaktDatensatz>(zahlung,"EmpfaengerId");zahlung.Property(x=>x.Betrag).HasPrecision(18,2);zahlung.HasIndex(x=>x.Datum);Check(zahlung,"Zahlung_Betrag","\"Betrag\" > 0");
        var zb=b.Entity<ZahlungsbestaetigungDatensatz>();zb.ToTable("Zahlungsbestaetigungen");zb.HasKey(x=>x.ZahlungId);Fk<ZahlungsbestaetigungDatensatz,ZahlungDatensatz>(zb,"ZahlungId");
        var zz=Eintrag<ZahlungszuordnungDatensatz>(b,"Zahlungszuordnungen");Fk<ZahlungszuordnungDatensatz,RechnungDatensatz>(zz,"RechnungId");Fk<ZahlungszuordnungDatensatz,ZahlungsbestaetigungDatensatz>(zz,"ZahlungId");zz.Property(x=>x.Betrag).HasPrecision(18,2);zz.HasIndex(x=>new{x.RechnungId,x.ZahlungId}).IsUnique();Check(zz,"Zahlungszuordnung_Betrag","\"Betrag\" > 0");
        foreach(var e in b.Model.GetEntityTypes())foreach(var prop in e.GetProperties().Where(p=>p.ClrType==typeof(string))){if(prop.GetMaxLength() is null)prop.SetMaxLength(prop.Name.EndsWith("Id",StringComparison.Ordinal)||prop.Name=="Id"?64:200);}
        Pilotmodell(b);
        // SQLite is test-only; its timestamp ordering needs a UTC conversion. PostgreSQL
        // uses native timestamptz and is independently checked through its generated SQL.
        if(Database.ProviderName=="Microsoft.EntityFrameworkCore.Sqlite")
            foreach(var e in b.Model.GetEntityTypes())foreach(var zeitProperty in e.GetProperties().Where(p=>p.ClrType==typeof(DateTimeOffset)))
                zeitProperty.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset,DateTime>(v=>v.UtcDateTime,v=>new DateTimeOffset(DateTime.SpecifyKind(v,DateTimeKind.Utc))));
    }
    private static EntityTypeBuilder<T> Eintrag<T>(ModelBuilder b,string table) where T:class
    {var e=b.Entity<T>();e.HasBaseType((Type?)null);e.ToTable(table);e.HasKey("Id");return e;}
    private static EntityTypeBuilder<T> Versioniert<T>(ModelBuilder b,string table) where T:VersionierterDatensatz
    {var e=Eintrag<T>(b,table);e.Property(x=>x.Version).IsConcurrencyToken();Check(e,table+"_Version","\"Version\" > 0");return e;}
    private static void Check<T>(EntityTypeBuilder<T> e,string name,string sql) where T:class => e.ToTable(t=>t.HasCheckConstraint(name,sql));
    private static void Fk<T,P>(EntityTypeBuilder<T> e,string property) where T:class where P:class => e.HasOne<P>().WithMany().HasForeignKey(property).OnDelete(DeleteBehavior.Restrict);
    private void VersionenAktualisieren()
    {
        NeueFreigaben();
        foreach(var e in ChangeTracker.Entries<VersionierterDatensatz>().Where(x=>x.State==EntityState.Modified))
        {e.Entity.Version=checked(e.Property(x=>x.Version).OriginalValue+1);e.Entity.GeaendertAm=DateTimeOffset.UtcNow;}
        }
    public override int SaveChanges(bool acceptAllChangesOnSuccess){VersionenAktualisieren();return base.SaveChanges(acceptAllChangesOnSuccess);}
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken token=default){VersionenAktualisieren();return base.SaveChangesAsync(acceptAllChangesOnSuccess,token);
    }
}
