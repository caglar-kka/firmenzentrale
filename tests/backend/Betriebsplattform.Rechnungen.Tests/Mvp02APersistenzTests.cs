using Betriebsplattform.Web;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class Mvp02APersistenzTests
{
    private sealed class Daten : IAsyncDisposable
    {
        private readonly SqliteConnection connection=new("Data Source=:memory:");
        public FirmenDbContext Context()=>new(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(connection).Options);
        public static async Task<Daten> Neu(){var d=new Daten();await d.connection.OpenAsync();await using var db=d.Context();await db.Database.EnsureCreatedAsync();
            db.Kontakte.Add(new(){Id="firma",Name="Fennfalter Beispiel GmbH",Suchname="fennfalter beispiel gmbh"});db.Kategorien.Add(new(){Id="kat",Name="Vertrag",Suchname="vertrag"});
            db.Akten.AddRange(new AkteDatensatz{Id="a",Titel="Vertrag A",Suchtext="vertrag a",FirmaId="firma",KategorieId="kat"},new AkteDatensatz{Id="b",Titel="Vertrag B",Suchtext="vertrag b",FirmaId="firma",KategorieId="kat"},new AkteDatensatz{Id="c",Titel="Vertrag C",Suchtext="vertrag c",FirmaId="firma",KategorieId="kat"});
            db.Dokumente.Add(new(){Id="doc",Titel="Beispielbrief",Dateiname="Brief.txt",StorageKey=new string('a',32),Hash=new string('A',64),Groesse=8,Inhaltstyp="text/plain",KategorieId="kat"});db.Dokumentzuordnungen.Add(new(){DokumentId="doc",AkteId="a"});await db.SaveChangesAsync();return d;}
        public async ValueTask DisposeAsync()=>await connection.DisposeAsync();
    }
    [Fact] public void Mvp02AProviderIstExplizitUndVerbindungsdatenWerdenNichtAusgegeben(){
        IConfiguration C(params (string,string)[] values)=>new ConfigurationBuilder().AddInMemoryCollection(values.Select(v=>new KeyValuePair<string,string?>(v.Item1,v.Item2))).Build();
        Assert.Equal("Demo",Persistenzkonfiguration.Lesen(C()).Provider);
        Assert.Throws<InvalidOperationException>(()=>Persistenzkonfiguration.Lesen(C(("FirmenZentrale:Persistence:Provider","PostgreSql"))));
        Assert.Throws<InvalidOperationException>(()=>Persistenzkonfiguration.Lesen(C(("FirmenZentrale:Persistence:Provider","unbekannt"))));
        var p=Persistenzkonfiguration.Lesen(C(("FirmenZentrale:Persistence:Provider","PostgreSql"),("ConnectionStrings:FirmenZentrale","Host=127.0.0.1;Database=synthetic")));
        Assert.DoesNotContain("Host",p.ToString());Assert.DoesNotContain("synthetic",p.ToString());
    }
    [Fact] public void Mvp02APostgresModellUndMigrationHabenRelationaleSicherungen(){
        using var db=new FirmenDbFactory().CreateDbContext([]);var modell=db.Model;
        Assert.True(modell.FindEntityType(typeof(KontaktDatensatz))!.FindProperty("Version")!.IsConcurrencyToken);
        Assert.Equal(nameof(DokumentzuordnungDatensatz.DokumentId),modell.FindEntityType(typeof(DokumentzuordnungDatensatz))!.FindPrimaryKey()!.Properties.Single().Name);
        Assert.All(modell.GetEntityTypes().SelectMany(t=>t.GetForeignKeys()),fk=>Assert.Equal(DeleteBehavior.Restrict,fk.DeleteBehavior));
        Assert.DoesNotContain(modell.GetEntityTypes().SelectMany(t=>t.GetProperties()),p=>p.ClrType==typeof(byte[]));
        var sql=db.GetService<IMigrator>().GenerateScript(options:MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE",sql);Assert.Contains("FOREIGN KEY",sql);Assert.Contains("ON DELETE RESTRICT",sql);Assert.Contains("CHECK",sql);Assert.Contains("UNIQUE INDEX",sql);Assert.DoesNotContain("DROP TABLE",sql);
        Assert.False(db.Database.HasPendingModelChanges());
        var query=db.Kontakte.Where(k=>k.Suchname.StartsWith("fenn")).OrderBy(k=>k.Nachname).ThenBy(k=>k.Id).Skip(20).Take(20).ToQueryString();Assert.Contains("LIMIT",query);Assert.Contains("OFFSET",query);
    }
    [Fact] public async Task Mvp02AUmzuordnungABABCCIstAtomarUndBleibtUeberKontexteErhalten(){
        await using var f=await Daten.Neu();int version=1;string von="a";
        foreach(var ziel in new[]{"b","a","b","c"}){await using var db=f.Context();var d=await new FirmenDaten(db).Umordnen("doc",new(von,ziel,version));Assert.Equal(++version,d.Version);Assert.Equal(new string('a',32),d.StorageKey);von=ziel;}
        await using var pruef=f.Context();Assert.Equal("c",(await pruef.Dokumentzuordnungen.SingleAsync()).AkteId);Assert.Equal(1,await pruef.Dokumente.CountAsync());Assert.Equal(4,await pruef.Dokumentaktivitaeten.CountAsync());
        var history=await pruef.Dokumentaktivitaeten.OrderBy(h=>h.Zeitpunkt).ToArrayAsync();Assert.Equal("a",history[0].VorherigeAkteId);Assert.Equal("b",history[0].AkteId);Assert.Equal("b",history[^1].VorherigeAkteId);Assert.Equal("c",history[^1].AkteId);
    }
    [Theory][InlineData("fehlt",1)][InlineData("a",1)][InlineData("b",0)]
    public async Task Mvp02AUngueltigeVerschiebungAendertWederBeziehungNochVerlauf(string ziel,int version){await using var f=await Daten.Neu();await using(var db=f.Context())await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).Umordnen("doc",new("a",ziel,version)));await using var read=f.Context();Assert.Equal("a",(await read.Dokumentzuordnungen.SingleAsync()).AkteId);Assert.Equal(0,await read.Dokumentaktivitaeten.CountAsync());}
    [Fact] public async Task Mvp02ANeueZielakteUndFehlerSindTransaktional(){
        await using var f=await Daten.Neu();await using(var db=f.Context())await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).Umordnen("doc",new("a",null,1,"Neues Thema","fehlt","firma")));
        await using(var db=f.Context()){Assert.Equal(3,await db.Akten.CountAsync());var d=await new FirmenDaten(db).Umordnen("doc",new("a",null,1,"Neues Thema","kat","firma",true));Assert.Equal(2,d.Version);Assert.Equal(4,await db.Akten.CountAsync());}
    }
    [Fact] public async Task Mvp02AEchteEfConcurrencyVerhindertLastWriteWins(){
        await using var f=await Daten.Neu();await using var a=f.Context();await using var b=f.Context();var alt=await a.Akten.SingleAsync(x=>x.Id=="a");
        await new FirmenDaten(b).AkteStatus("a",new("In Bearbeitung",1));alt.Status="Erledigt";await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>a.SaveChangesAsync());
        await using var read=f.Context();Assert.Equal("In Bearbeitung",(await read.Akten.SingleAsync(x=>x.Id=="a")).Status);Assert.Equal(409,Persistenzfehler.Beschreiben(new DbUpdateConcurrencyException()).Status);
    }
    [Fact] public async Task Mvp02AForeignKeysDeleteSchutzUndChecksGreifen(){
        await using var f=await Daten.Neu();await using(var db=f.Context()){db.Akten.Add(new(){Titel="Ungültige Relation",Suchtext="x",FirmaId="fehlt",KategorieId="kat"});await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());}
        await using(var db=f.Context()){db.Kontakte.Remove(await db.Kontakte.SingleAsync());await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());}
        await using(var db=f.Context()){db.Kategorien.Add(new(){Id="selbst",ElternId="selbst",Name="Selbst",Suchname="selbst"});await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());}
        await using(var db=f.Context()){db.Dokumentzuordnungen.Add(new(){DokumentId="doc",AkteId="b"});await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());}
    }
    [Fact] public async Task Mvp02AKategorienUndNotizenBewahrenValidierungArchivUndHistorie(){
        await using var f=await Daten.Neu();await using(var db=f.Context()){var s=new FirmenDaten(db);await Assert.ThrowsAsync<Arbeitsfehler>(()=>s.KategorieSpeichern("kat",new("Vertrag","","kat",0,1)));await Assert.ThrowsAsync<Arbeitsfehler>(()=>s.KategorieSpeichern(null,new("  ","",null,0,0)));}
        string id;await using(var db=f.Context()){var n=await new FirmenDaten(db).NotizSpeichern("a",null,new("Rückruf","Intern prüfen"));id=n.Id;}
        await using(var db=f.Context()){var n=await new FirmenDaten(db).NotizSpeichern("a",id,new("Rückruf geändert","Noch intern",1));Assert.Equal(2,n.Version);}
        await using(var db=f.Context()){var s=new FirmenDaten(db);await Assert.ThrowsAsync<Arbeitsfehler>(()=>s.NotizSpeichern("a",id,new("Veraltet","Text",1)));var n=await s.NotizSpeichern("a",id,new(null,null,2),true);Assert.True(n.Archiviert);Assert.Equal(3,await db.Notizrevisionen.CountAsync());}
    }
    [Fact] public async Task Mvp02AKontakteBleibenStrukturiertUndDublettenWerdenNichtGemergt(){
        await using var f=await Daten.Neu();var eingabe=new Kontakteingabe("Fennquarz Büro","Firma",null,"","","",[new("Telefon","+49 30 23125 099",true),new("E-Mail","buero@fennquarz.invalid",true)],Strasse:"Musterweg",Hausnummer:"8",Plz:"12345",Ort:"Beispielort",Land:"DE");
        string id;await using(var db=f.Context()){var k=await new FirmenDaten(db).KontaktSpeichern(null,eingabe);id=k.Id;Assert.Equal("8",k.Hausnummer);Assert.Equal("Musterweg",k.Strasse);Assert.Equal(2,k.Werte.Count);}
        await using(var db=f.Context())await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).KontaktSpeichern(null,eingabe));
        await using(var db=f.Context()){var k=await new FirmenDaten(db).KontaktSpeichern(null,eingabe with{DublettenBestaetigt=true});Assert.NotEqual(id,k.Id);}
        await using(var db=f.Context())await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).KontaktSpeichern(null,eingabe with{Name=" ",DublettenBestaetigt=true}));
        await using(var db=f.Context()){var k=await new FirmenDaten(db).KontaktArchivieren(id,1);Assert.True(k.Archiviert);Assert.Equal(2,k.Version);}
    }
    [Fact] public async Task Mvp02ATextIstDatenUndListeLaedtKeinenDateiinhalt(){
        await using var f=await Daten.Neu();await using var db=f.Context();var d=await db.Dokumente.SingleAsync();d.Text="<script>alert(1)</script>";await db.SaveChangesAsync();
        var s=new FirmenDaten(db);var meta=await s.DokumentMetadaten().SingleAsync();Assert.Equal("",meta.Text);Assert.Equal("<script>alert(1)</script>",(await db.Dokumente.SingleAsync()).Text);
        Assert.DoesNotContain("\"Text\"",s.DokumentMetadaten().Take(20).ToQueryString());
    }
    [Theory][InlineData("../escape")][InlineData("x/y")][InlineData("x?y")][InlineData("")]
    public void Mvp02AIdsWerdenKontrolliertAbgelehnt(string id)=>Assert.Throws<Arbeitsfehler>(()=>FirmenDaten.Id(id));
    [Fact] public async Task Mvp02ADateispeicherLehntUngeeigneteKeysVorDateizugriffAb(){var f=new LokaleDokumentdateien(null);await Assert.ThrowsAsync<Arbeitsfehler>(()=>f.Lesen("../fremd",default));}

    [Fact] public async Task Mvp02AKontaktwertAenderungErhoehtVersionUndVerhindertVeralteteBearbeitung(){
        await using var f=await Daten.Neu();var e=new Kontakteingabe("Fennwelle Büro","Firma",null,"","","",[new("Telefon","+49 30 23125 077",true),new("E-Mail","post@fennwelle.invalid",true)],Strasse:"Prüfweg",Hausnummer:"9",Ort:"Beispielort");
        string id;await using(var db=f.Context())id=(await new FirmenDaten(db).KontaktSpeichern(null,e)).Id;
        var neu=e with{Version=1,Werte=[new("Telefon","+49 30 23125 078",true),new("E-Mail","post@fennwelle.invalid",true)]};
        await using(var db=f.Context()){var k=await new FirmenDaten(db).KontaktSpeichern(id,neu);Assert.Equal(2,k.Version);Assert.Contains("Prüfweg",k.Anschrift);Assert.Contains(k.Werte,w=>!w.Aktiv&&w.Wert.EndsWith("077"));}
        await using(var db=f.Context())Assert.Equal(409,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).KontaktSpeichern(id,neu))).Status);
    }
    [Fact] public async Task Mvp02ASynchronesSaveVerwendetEbenfallsConcurrency(){
        await using var f=await Daten.Neu();await using var a=f.Context();await using var b=f.Context();var alt=await a.Akten.SingleAsync(x=>x.Id=="a");var neu=await b.Akten.SingleAsync(x=>x.Id=="a");neu.Status="Erledigt";b.SaveChanges();Assert.Equal(2,neu.Version);alt.Status="Archiviert";Assert.Throws<DbUpdateConcurrencyException>(()=>a.SaveChanges());
    }
    [Fact] public async Task Mvp02AKontaktlistenPaginierenSuchenUndZaehlenServerseitig(){
        await using var f=await Daten.Neu();await using var db=f.Context();for(var i=0;i<45;i++)db.Kontakte.Add(new(){Id="kontakt"+i,Name="Prüffirma "+i.ToString("D2"),Suchname="prueffirma "+i.ToString("D2")});await db.SaveChangesAsync();var d=new FirmenDaten(db);
        var erste=await d.KontakteSuchen(new(),0);var zweite=await d.KontakteSuchen(new(),1);Assert.Equal(46,erste.Anzahl);Assert.Equal(20,erste.Eintraege.Length);Assert.Equal(20,zweite.Eintraege.Length);Assert.Empty(erste.Eintraege.Select(k=>k.Id).Intersect(zweite.Eintraege.Select(k=>k.Id)));
        var treffer=await d.KontakteSuchen(new(Text:"Prufirma 07"),0);Assert.Contains(treffer.Eintraege,k=>k.Id=="kontakt7");
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.KontakteSuchen(new(Text:new string('x',121)),0));await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.KontakteSuchen(new(),-1));
    }
    [Fact] public async Task Mvp02AEingangFilterUndMetadatenSindBegrenzt(){
        await using var f=await Daten.Neu();await using var db=f.Context();for(var i=0;i<24;i++)db.Dokumente.Add(new(){Id="d"+i,Titel="Brief "+i,Suchtext="brief "+i,Dateiname="Brief.txt",Hash=i.ToString("X64"),StorageKey=i.ToString("x32"),Groesse=3,Inhaltstyp="text/plain",Status="Zugeordnet",Pruefbedarf=false});await db.SaveChangesAsync();var d=new FirmenDaten(db);
        var a=await d.EingangSuchen(new(),0);var b=await d.EingangSuchen(new(),1);Assert.Equal(25,a.Anzahl);Assert.Equal(20,a.Eintraege.Length);Assert.Equal(5,b.Eintraege.Length);
        var review=await d.EingangSuchen(new(Sicht:"Zu prüfen"),0);Assert.Single(review.Eintraege);Assert.Equal("doc",review.Eintraege[0].Id);Assert.Equal(24,(await d.EingangSuchen(new(Sicht:"Kürzlich verarbeitet"),0)).Anzahl);
        Assert.Single((await d.EingangSuchen(new(Text:"brief 17"),0)).Eintraege);
    }
    private sealed class VerboteneDokumente:ILesezugriff{public bool Erlaubt(string art,string id)=>art!="Dokument";}
    [Fact] public async Task Mvp02AAktensucheNutztZugeordneteInhalteAberKeineGesperrtenDokumente(){
        await using var f=await Daten.Neu();await using var db=f.Context();var doc=await db.Dokumente.SingleAsync();doc.Suchtext="fennwelle mobilfunk";await db.SaveChangesAsync();var d=new FirmenDaten(db);
        var found=(Vorgang[])await d.AktenSuchen("Mobilfunk",null,null,"titel",false,0,false,new LokalerDemozugriff());Assert.Single(found);Assert.Equal("a",found[0].Id);
        Assert.Empty((Vorgang[])await d.AktenSuchen("Mobilfunk",null,null,"titel",false,0,false,new VerboteneDokumente()));
        var zahlen=await d.Arbeitszahlen("firma",new LokalerDemozugriff(),new Arbeitsmodule());Assert.Equal(3,zahlen["akten"]);Assert.Equal(1,zahlen["dokumente"]);Assert.Equal(0,zahlen["notizen"]);
    }
    private sealed class Dateien:IDokumentdateien{public int Gespeichert;public Task Speichern(string key,byte[] bytes,CancellationToken ct){Gespeichert++;return Task.CompletedTask;}public Task<byte[]> Lesen(string key,CancellationToken ct)=>Task.FromResult(Array.Empty<byte>());}
    [Fact] public async Task Mvp02AUploadIstIdempotentUndUngueltigeZieleErzeugenKeineDateikopie(){
        await using var f=await Daten.Neu();var storage=new Dateien();var bytes=System.Text.Encoding.UTF8.GetBytes("Beispieltext für eine eigene Akte.");string id;
        await using(var db=f.Context())id=(await new FirmenDaten(db).DokumentHinzufuegen(bytes,"Beispiel.txt","text/plain","b",storage)).Id;
        await using(var db=f.Context()){var d=await new FirmenDaten(db).DokumentHinzufuegen(bytes,"Nochmals.txt","text/plain","b",storage);Assert.Equal(id,d.Id);Assert.Equal(2,d.Version);}
        await using(var db=f.Context())await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).DokumentHinzufuegen(bytes,"Wieder.txt","text/plain","c",storage));
        await using(var db=f.Context())await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).DokumentHinzufuegen(bytes,"../ausserhalb.txt","text/plain",null,storage));
        Assert.Equal(1,storage.Gespeichert);await using var read=f.Context();Assert.Equal(2,await read.Dokumente.CountAsync());Assert.Equal(2,await read.Dokumentaktivitaeten.CountAsync());
    }
    [Fact] public async Task Mvp02AFinanzadapterErhaeltKernregelnUndPersistenteVersionen(){
        await using var f=await Daten.Neu();var r=Guid.NewGuid();var p=Guid.NewGuid();var nachweis=new Nachweis("Prüfbeleg","Beispielquelle");
        await using(var db=f.Context()){db.Kontakte.Add(new(){Id="kunde",Name="Kieselpfad Beispiel",Suchname="kieselpfad beispiel"});await db.SaveChangesAsync();var s=new FinanzDaten(db);await s.RechnungErfassen(new(r,"RE-2026-77","firma","kunde",new(2026,10,19),100,nachweis),"a","Ausgang");await s.ZahlungErfassen(new(p,"kunde","firma",new(2026,10,19),40,nachweis));}
        await using(var db=f.Context())await Assert.ThrowsAsync<InvalidOperationException>(()=>new FinanzDaten(db).Zuordnen(r.ToString("N"),p.ToString("N"),1,1,40,nachweis,"Beispielbenutzer"));
        await using(var db=f.Context())await new FinanzDaten(db).Bestaetigen(p.ToString("N"),1,nachweis,"Beispielbenutzer");
        await using(var db=f.Context())await new FinanzDaten(db).Zuordnen(r.ToString("N"),p.ToString("N"),1,2,40,nachweis,"Beispielbenutzer");
        await using(var db=f.Context()){Assert.Equal("Teilweise bezahlt",(await db.Rechnungen.SingleAsync()).Zahlungsstatus);Assert.Equal(2,(await db.Rechnungen.SingleAsync()).Version);Assert.Equal(3,(await db.Zahlungen.SingleAsync()).Version);Assert.Equal(40,(await db.Zahlungszuordnungen.SingleAsync()).Betrag);await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FinanzDaten(db).Zuordnen(r.ToString("N"),p.ToString("N"),1,2,40,nachweis,"Beispielbenutzer"));}
    }
    [Fact] public async Task Mvp02AKorrespondenzVerweistAufZentraleDokumenteUndLehntFremdeAnhaengeAb(){
        await using var f=await Daten.Neu();await using var db=f.Context();var d=new FirmenDaten(db);
        var e=new Korrespondenzeingabe("Rückfrage","Tatsächliche externe Rückfrage","Brief",DokumentIds:["doc"],Richtung:"Empfangen",KontaktId:"firma",KommuniziertAm:DateTimeOffset.UtcNow);
        var k=await d.KorrespondenzSpeichern("a",e);Assert.Single(k.DokumentIds!);Assert.Single(await db.Dokumente.ToArrayAsync());Assert.Single(await db.Anhaenge.ToArrayAsync());await Assert.ThrowsAsync<Arbeitsfehler>(()=>d.KorrespondenzSpeichern("b",e));
    }
    [Theory][InlineData("23001",409)][InlineData("23503",409)][InlineData("23505",409)][InlineData("23514",400)][InlineData("22001",400)]
    public void Mvp02AProviderfehlerEnthaltenKeineInternenDetails(string code,int status){var e=new Npgsql.PostgresException("interne Details","ERROR","ERROR",code);var result=Persistenzfehler.Beschreiben(new DbUpdateException("SQL Intern",e));Assert.Equal(status,result.Status);Assert.DoesNotContain("Intern",result.Meldung,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("SQL",result.Meldung);}

    [Fact] public async Task Mvp02ADatenbankausfallStopptOhneFallbackUndOhneVerbindungsdetails(){
        var options=new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=synthetic_unavailable;Timeout=1;Command Timeout=1").Options;
        var factory=new Microsoft.EntityFrameworkCore.Infrastructure.PooledDbContextFactory<FirmenDbContext>(options);var dienst=new DatenbankStartpruefung(factory);
        var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>dienst.StartAsync(default));Assert.Contains("Kein Demo-Fallback",e.Message);Assert.Null(e.InnerException);Assert.DoesNotContain("Host=",e.Message);
    }

    [Fact] public async Task Mvp02APersonBehaeltFirmenbezugAusserhalbDerListenseite(){
        await using var f=await Daten.Neu();await using var db=f.Context();var e=new Kontakteingabe(null,"Person","firma","","Verwaltung","Ansprechpartner",[new("Telefon","+49 30 23125 079",true)],Vorname:"Mäya",Nachname:"O'Prüf-Bogen");
        var k=await new FirmenDaten(db).KontaktSpeichern(null,e);Assert.Equal("Mäya",k.Vorname);Assert.Equal("firma",k.FirmaId);Assert.Equal("Fennfalter Beispiel GmbH",k.FirmaName);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>new FirmenDaten(db).KontaktSpeichern(null,e with{Nachname="040 123456"}));
    }
    [Fact] public async Task Mvp02ARechnungslisteFiltertUndZaehltMitGesamtbestand(){
        await using var f=await Daten.Neu();await using var db=f.Context();db.Kontakte.Add(new(){Id="kunde",Name="Kieselpfad Beispiel",Suchname="kieselpfad beispiel"});
        for(var i=0;i<25;i++){var akte="rechnungakte"+i;db.Akten.Add(new(){Id=akte,Titel="Rechnung "+i,Suchtext="rechnung "+i,KategorieId="kat",FirmaId="kunde"});db.Rechnungen.Add(new(){Id=Guid.NewGuid().ToString("N"),AkteId=akte,AusstellerId="firma",EmpfaengerId="kunde",Nummer="RE-"+i.ToString("D2"),Richtung="Ausgang",Datum=new(2026,10,19),Betrag=100,Beleg="Beispielbeleg",Quelle="Beispielquelle"});}await db.SaveChangesAsync();var d=new FirmenDaten(db);
        var erste=await d.Rechnungsuebersicht(0,new LokalerDemozugriff());var zweite=await d.Rechnungsuebersicht(1,new LokalerDemozugriff());Assert.Equal(25,erste.Bestandsanzahl);Assert.Equal(25,erste.Anzahl);Assert.Equal(20,erste.Rechnungen.Count);Assert.Equal(5,zweite.Rechnungen.Count);Assert.Equal("2.500,00 €",erste.Gesamtbetrag);Assert.Equal("2.500,00 €",erste.OffeneForderungen);
        Assert.Empty((await d.Rechnungsuebersicht(0,new LokalerDemozugriff(),filter:new(Richtung:"Verbindlichkeiten"))).Rechnungen);
        Assert.Single((await d.Rechnungsuebersicht(0,new LokalerDemozugriff(),filter:new(Text:"RE-07"))).Rechnungen);
    }

    [Fact] public async Task Mvp02ABeziehungssucheFindetAnsprechpartnerOhneGesamtbestandZuLaden(){
        await using var f=await Daten.Neu();await using var db=f.Context();db.Kontakte.AddRange(new KontaktDatensatz{Id="kunde",Name="Kieselpfad Büro",Suchname="kieselpfad buro"},new KontaktDatensatz{Id="person",Art="Person",Name="Mäya Maiwind",Suchname="maya maiwind",Vorname="Mäya",Nachname="Maiwind",FirmaId="firma"});db.Rechnungen.Add(new(){Id=Guid.NewGuid().ToString("N"),AkteId="a",AusstellerId="firma",EmpfaengerId="kunde",Nummer="RE-77",Richtung="Ausgang",Datum=new(2026,10,19),Betrag=50,Beleg="Beispielbeleg",Quelle="Beispielquelle"});await db.SaveChangesAsync();var d=new FirmenDaten(db);var rechte=new LokalerDemozugriff();
        foreach(var text in new[]{"Rechnung Maya","Maya Rechnung","Maya Mai"}){var treffer=await d.Suchen(text,rechte,new Arbeitsmodule());Assert.Contains(treffer,t=>t.Art=="Rechnung"&&t.VorgangId=="a");Assert.Single((await d.Rechnungsuebersicht(0,rechte,filter:new(Text:text))).Rechnungen);}
        Assert.Contains((Vorgang[])await d.AktenSuchen("Maya",null,null,"titel",false,0,false,rechte),a=>a.Id=="a");
    }
}

// Opt-in tests use only a separately configured local synthetic PostgreSQL 18 database.
public sealed class PostgreSqlLiveFactAttribute : FactAttribute
{
    public PostgreSqlLiveFactAttribute(){if(Environment.GetEnvironmentVariable("FZ_POSTGRES_LIVE")!="1")Skip="Requires explicit local PostgreSQL live-gate configuration.";}
}

public sealed class PostgreSqlLiveTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private sealed class SqlBeobachter : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public readonly List<string> Sql=[];
        public bool VerlaufFehler;
        public int AktenInserts;
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(System.Data.Common.DbCommand command,Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData data,Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result,CancellationToken ct=default)
        {
            Sql.Add(command.CommandText); // SQL shape only; parameter values are never recorded.
            if(command.CommandText.Contains("INSERT INTO \"Akten\"",StringComparison.Ordinal))AktenInserts++;
            if(VerlaufFehler&&command.CommandText.Contains("INSERT INTO \"Dokumentaktivitaeten\"",StringComparison.Ordinal))throw new VerlaufTestfehler();
            return ValueTask.FromResult(result);
        }
    }
    private sealed class VerlaufTestfehler:Exception;
    private string Verbindung()
    {
        var value=Environment.GetEnvironmentVariable("ConnectionStrings__FirmenZentrale")??throw new InvalidOperationException("Local live-test configuration is missing.");
        var cs=new Npgsql.NpgsqlConnectionStringBuilder(value){IncludeErrorDetail=false,LogParameters=false};
        Assert.Equal("firmenzentrale_dev",cs.Database);Assert.Contains(cs.Host,new[]{"127.0.0.1","localhost"});Assert.Equal(5432,cs.Port);return cs.ConnectionString;
    }
    private FirmenDbContext Context(SqlBeobachter? beobachter=null)
    {
        var b=new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(Verbindung(),p=>p.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false);
        if(beobachter is not null)b.AddInterceptors(beobachter);return new(b.Options);
    }
    private static async Task<string[][]> Lesen(Npgsql.NpgsqlConnection c,string sql)
    {
        using var cmd=new Npgsql.NpgsqlCommand(sql,c);await using var r=await cmd.ExecuteReaderAsync();var rows=new List<string[]>();
        while(await r.ReadAsync()){var row=new string[r.FieldCount];for(int i=0;i<row.Length;i++)row[i]=r.IsDBNull(i)?"":r.GetValue(i).ToString()!;rows.Add(row);}return rows.ToArray();
    }
    private async Task Schema()
    {
        await using var db=Context();Assert.Empty(await db.Database.GetPendingMigrationsAsync());Assert.False(db.Database.HasPendingModelChanges());
        await using var c=new Npgsql.NpgsqlConnection(Verbindung());await c.OpenAsync();var server=int.Parse((await Lesen(c,"SELECT current_setting('server_version_num')"))[0][0]);Assert.InRange(server,180000,189999);
        var modell=db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel();var tables=modell.Tables.ToArray();Assert.Equal(17,tables.Length);
        var actualTables=(await Lesen(c,"SELECT tablename FROM pg_tables WHERE schemaname='public'")).Select(x=>x[0]).ToHashSet();
        Assert.Equal(tables.Select(t=>t.Name).Append("__EFMigrationsHistory").OrderBy(x=>x),actualTables.OrderBy(x=>x));
        var cols=await Lesen(c,"SELECT table_name,column_name,is_nullable,data_type FROM information_schema.columns WHERE table_schema='public'");
        var cons=await Lesen(c,"SELECT t.relname,c.conname,c.contype::text,c.confdeltype::text FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE n.nspname='public'");
        var indexes=await Lesen(c,"SELECT t.relname,i.relname,ix.indisunique::text FROM pg_index ix JOIN pg_class t ON t.oid=ix.indrelid JOIN pg_class i ON i.oid=ix.indexrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE n.nspname='public'");
        foreach(var t in tables){
            Assert.Contains(cons,c=>c[0]==t.Name&&c[1]==t.PrimaryKey!.Name&&c[2]=="p");
            foreach(var col in t.Columns){var actual=Assert.Single(cols,c=>c[0]==t.Name&&c[1]==col.Name);Assert.Equal(col.IsNullable?"YES":"NO",actual[2]);}
            foreach(var fk in t.ForeignKeyConstraints)Assert.Contains(cons,c=>c[0]==t.Name&&c[1]==fk.Name&&c[2]=="f"&&c[3]=="r");
            foreach(var check in t.CheckConstraints)Assert.Contains(cons,c=>c[0]==t.Name&&c[1]==check.Name&&c[2]=="c");
            foreach(var ix in t.Indexes)Assert.Contains(indexes,i=>i[0]==t.Name&&i[1]==ix.Name&&i[2]==(ix.IsUnique?"true":"false"));
        }
        foreach(var e in db.Model.GetEntityTypes().Where(e=>typeof(VersionierterDatensatz).IsAssignableFrom(e.ClrType))){Assert.True(e.FindProperty("Version")!.IsConcurrencyToken);Assert.Contains(cols,c=>c[0]==e.GetTableName()&&c[1]=="Version"&&c[2]=="NO"&&c[3]=="integer");}
        output.WriteLine($"Schema: {tables.Length} tables, {tables.Sum(t=>t.Indexes.Count())} indexes, {tables.Sum(t=>t.ForeignKeyConstraints.Count())} restrictive FKs, {tables.Sum(t=>t.CheckConstraints.Count())} checks; PK/NOT NULL/unique/concurrency metadata matched.");
    }
    private async Task Negativ(string code,Action<FirmenDbContext> a)
    {
        await using var db=Context();a(db);var e=await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());Assert.Equal(code,Assert.IsType<Npgsql.PostgresException>(e.InnerException).SqlState);
    }
    [PostgreSqlLiveFact]
    public async Task PostgreSql18SchemaFachablaeufeKonflikteRollbackUndHostneustart()
    {
        await Schema();var run=Guid.NewGuid().ToString("N");var name="Fennkiesel Liveprüfung "+run[..8];var probe=new SqlBeobachter();
        string firma,person,kat,a,b,c,doc,notiz;var invoice=Guid.NewGuid();var payment=Guid.NewGuid();
        // No seed operation is executed: the host must not change existing row counts at startup.
        await using(var db=Context(probe)){
            var d=new FirmenDaten(db);var k=await d.KontaktSpeichern(null,new(name,"Firma",null,"","","",[new("Telefon","+49 30 23125 071",true),new("E-Mail",run+"@beispiel.invalid",true)],DublettenBestaetigt:true,Strasse:"Prüfbogen",Hausnummer:"8a",Plz:"12345",Ort:"Beispielort",Land:"DE"));firma=k.Id;
            person=(await d.KontaktSpeichern(null,new(null,"Person",firma,"","Büro","Ansprechpartner",[new("Telefon","+49 30 23125 072",true)],DublettenBestaetigt:true,Vorname:"Mäya",Nachname:"Prüf-Fenn"))).Id;
            var cat=await d.KategorieSpeichern(null,new("Liveprüfung "+run[..8],"Synthetischer PostgreSQL-Nachweis",null,0,0));kat=cat.Id;
            a=(await d.AkteAnlegen(new("Liveakte A "+run,kat,firma,true))).Id;b=(await d.AkteAnlegen(new("Liveakte B "+run,kat,firma,true))).Id;c=(await d.AkteAnlegen(new("Liveakte C "+run,kat,firma,true))).Id;
            var note=await d.NotizSpeichern(a,null,new("Interner Livehinweis","Nur synthetischer Prüfinhalt"));notiz=note.Id;
            var gelesen=(await d.KontaktAnsichten([await db.Kontakte.SingleAsync(k=>k.Id==firma)])).Single();Assert.Equal("8a",gelesen.Hausnummer);Assert.Equal("Prüfbogen",gelesen.Strasse);Assert.Equal(2,gelesen.Werte.Count);
            var z=new FinanzDaten(db);var nachweis=new Nachweis("Synthetischer Prüfbeleg","Lokaler PostgreSQL-Livetest");
            await z.RechnungErfassen(new(invoice,"LIVE-"+run[..8],firma,person,new(2026,10,19),120,nachweis),a,"Ausgang");
            await z.ZahlungErfassen(new(payment,person,firma,new(2026,10,19),40,nachweis));await z.Bestaetigen(payment.ToString("N"),1,nachweis,"Synthetischer Prüfbenutzer");await z.Zuordnen(invoice.ToString("N"),payment.ToString("N"),1,2,40,nachweis,"Synthetischer Prüfbenutzer");
            var storage=new LokaleDokumentdateien(Path.Combine(Repo(),"src","backend","private","postgres-live-dateien"));
            var dokument=await d.DokumentHinzufuegen(System.Text.Encoding.UTF8.GetBytes("Livebeleg "+run+"\nTelefon: +49 30 23125 073"),"Livebeleg.txt","text/plain",null,storage);doc=dokument.Id;
            await d.Erstzuordnen(doc,new(a,null,kat,null,1));
            await d.KorrespondenzSpeichern(a,new("Externe Prüfantwort","Synthetische dokumentierte Antwort","Brief",DokumentIds:[doc],Richtung:"Empfangen",KontaktId:person,KommuniziertAm:DateTimeOffset.UtcNow));
        }
        await using(var db=Context()){
            Assert.Equal("Teilweise bezahlt",(await db.Rechnungen.SingleAsync(r=>r.Id==invoice.ToString("N"))).Zahlungsstatus);Assert.Equal(40,(await db.Zahlungszuordnungen.SingleAsync(z=>z.ZahlungId==payment.ToString("N"))).Betrag);
            var d=new FirmenDaten(db);var p=(await d.KontaktAnsichten([await db.Kontakte.SingleAsync(k=>k.Id==person)])).Single();Assert.Equal(firma,p.FirmaId);Assert.Equal(name,p.FirmaName);Assert.Equal("Mäya",p.Vorname);
            var original=await db.Kontakte.SingleAsync(k=>k.Id==firma);var v=original.Version;original.Ort="Anderer Beispielort";await db.SaveChangesAsync();Assert.Equal(v+1,original.Version);
            var n=await d.NotizSpeichern(a,notiz,new("Geänderter Livehinweis","Prüfänderung",1));Assert.Equal(2,n.Version);await d.NotizSpeichern(a,notiz,new(null,null,2),true);Assert.Equal(3,await db.Notizrevisionen.CountAsync(h=>h.NotizId==notiz));
        }
        output.WriteLine("CRUD: company/person/address/contact values/category/cases/invoice/payment/document/note/correspondence saved and reloaded across PostgreSQL contexts.");
        await Negativ("23503",db=>db.Akten.Add(new(){Titel="Ungültige FK",Suchtext="ungueltig",KategorieId=kat,FirmaId="fehlt-"+run}));
        await Negativ("23502",db=>db.Kontakte.Add(new(){Name=null!,Suchname="null"}));
        await Negativ("22001",db=>db.Kontakte.Add(new(){Name=new string('x',161),Suchname="lang"}));
        await Negativ("23514",db=>db.Rechnungen.Add(new(){AkteId=b,AusstellerId=firma,EmpfaengerId=person,Nummer="NEG",Betrag=-1,Datum=new(2026,10,19)}));
        await Negativ("23505",db=>db.Kategorien.Add(new(){Name="Doppelte Prüfkategorie",Suchname=FirmenDaten.Normal("Liveprüfung "+run[..8])}));
        await Negativ("23514",db=>db.Kategorien.Add(new(){Id="selbst-"+run,ElternId="selbst-"+run,Name="Selbst",Suchname=run}));
        await using(var db=Context()){db.Kontakte.Remove(await db.Kontakte.SingleAsync(k=>k.Id==firma));var e=await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());Assert.Equal("23001",Assert.IsType<Npgsql.PostgresException>(e.InnerException).SqlState);}
        await Negativ("23505",db=>db.Dokumentzuordnungen.Add(new(){DokumentId=doc,AkteId=b}));
        await using(var db=Context())Assert.True(await db.Kontakte.AnyAsync(k=>k.Id==firma));
        output.WriteLine("PostgreSQL negatives: FK, NOT NULL, varchar length, amount/self-parent CHECK, unique category/current assignment, restrictive delete all enforced.");
        await using(var x=Context())await using(var y=Context()){
            var alt=await x.Akten.SingleAsync(v=>v.Id==a);var neu=await y.Akten.SingleAsync(v=>v.Id==a);var version=alt.Version;neu.Status="In Bearbeitung";await y.SaveChangesAsync();Assert.Equal(version+1,neu.Version);alt.Status="Erledigt";
            var conflict=await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>x.SaveChangesAsync());Assert.Equal(409,Persistenzfehler.Beschreiben(conflict).Status);
        }
        await using(var db=Context())Assert.Equal("In Bearbeitung",(await db.Akten.SingleAsync(v=>v.Id==a)).Status);
        output.WriteLine("Concurrency: two real PostgreSQL contexts; stale EF update rejected, newer value retained, mapped to 409.");
        var source=a;int versionDoc;string originalHash,originalKey;
        await using(var db=Context()){var d=await db.Dokumente.SingleAsync(d=>d.Id==doc);versionDoc=d.Version;originalHash=d.Hash;originalKey=d.StorageKey;}
        foreach(var ziel in new[]{b,a,b,c}){await using var db=Context();var d=await new FirmenDaten(db).Umordnen(doc,new(source,ziel,versionDoc));Assert.Equal(++versionDoc,d.Version);Assert.Equal(originalHash,d.Hash);Assert.Equal(originalKey,d.StorageKey);Assert.Equal(ziel,(await db.Dokumentzuordnungen.SingleAsync(z=>z.DokumentId==doc)).AkteId);source=ziel;}
        await using(var db=Context()){
            Assert.Equal(1,await db.Dokumente.CountAsync(d=>d.Id==doc));Assert.Equal(1,await db.Dokumentzuordnungen.CountAsync(z=>z.DokumentId==doc));
            var h=await db.Dokumentaktivitaeten.Where(h=>h.DokumentId==doc&&h.Text=="Zuordnung geändert").OrderBy(h=>h.Zeitpunkt).ToArrayAsync();Assert.Equal(4,h.Length);Assert.Equal(new[]{a,b,a,b},h.Select(h=>h.VorherigeAkteId));Assert.Equal(new[]{b,a,b,c},h.Select(h=>h.AkteId));Assert.All(h,t=>Assert.Equal(TimeSpan.Zero,t.Zeitpunkt.Offset));
        }
        var rollbackName="Rollbackakte "+run;var failure=new SqlBeobachter{VerlaufFehler=true};
        await using(var db=Context(failure)){
            await Assert.ThrowsAnyAsync<Exception>(()=>new FirmenDaten(db).Umordnen(doc,new(c,null,versionDoc,rollbackName,kat,firma,true)));Assert.True(failure.AktenInserts>0);
        }
        await using(var db=Context()){Assert.False(await db.Akten.AnyAsync(x=>x.Titel==rollbackName));Assert.Equal(c,(await db.Dokumentzuordnungen.SingleAsync(z=>z.DokumentId==doc)).AkteId);Assert.Equal(versionDoc,(await db.Dokumente.SingleAsync(d=>d.Id==doc)).Version);Assert.Equal(4,await db.Dokumentaktivitaeten.CountAsync(h=>h.DokumentId==doc&&h.Text=="Zuordnung geändert"));}
        output.WriteLine("Reassignment A-B-A-B-C: one relationship, original file unchanged, full history. Injected failure after case INSERT rolls back new case, relationship, version and history.");
        await HostSmoke(firma,a,b,c,doc,notiz,versionDoc);
        await using(var db=Context(probe)){
            var d=new FirmenDaten(db);var erste=await d.KontakteSuchen(new(),0);var zweite=await d.KontakteSuchen(new(),1);Assert.True(erste.Eintraege.Length<=20);Assert.Empty(erste.Eintraege.Select(x=>x.Id).Intersect(zweite.Eintraege.Select(x=>x.Id)));
            await d.EingangSuchen(new(Sort:"kategorie"),0);await d.AktenSuchen("Liveakte",null,null,"titel",false,0,true,new LokalerDemozugriff());await d.Suchen("Maya Rechnung",new LokalerDemozugriff(),new Arbeitsmodule());await d.Rechnungsuebersicht(0,new LokalerDemozugriff(),filter:new(Text:"LIVE-"+run[..8]));
            Assert.Contains(probe.Sql,s=>s.Contains("LIMIT",StringComparison.Ordinal));Assert.Contains(probe.Sql,s=>s.Contains("OFFSET",StringComparison.Ordinal));Assert.Contains(probe.Sql,s=>s.Contains("WHERE",StringComparison.Ordinal)&&s.Contains("ORDER BY",StringComparison.Ordinal));
        }
        var unavailable=new Npgsql.NpgsqlConnectionStringBuilder(Verbindung()){Port=1,Timeout=1,CommandTimeout=1};
        var factory=new PooledDbContextFactory<FirmenDbContext>(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(unavailable.ConnectionString).Options);var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>new DatenbankStartpruefung(factory).StartAsync(default));Assert.Contains("Kein Demo-Fallback",error.Message);Assert.Null(error.InnerException);
        output.WriteLine("Query sanity: bounded SQL LIMIT/OFFSET, WHERE/ORDER BY. Unreachable PostgreSQL startup fails without fallback or connection details.");
    }
    private static string Repo(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"AGENTS.md")))d=d.Parent;return d?.FullName??throw new InvalidOperationException("Repository root not found.");}
    private async Task HostSmoke(string firma,string akte,string b,string c,string doc,string alteNotiz,int docVersion)
    {
        long[] vor;
        await using(var db=Context())vor=[await db.Kontakte.LongCountAsync(),await db.Kategorien.LongCountAsync(),await db.Akten.LongCountAsync(),await db.Dokumente.LongCountAsync()];
        async Task<(Microsoft.AspNetCore.Builder.WebApplication App,HttpClient Client)> Start(){
            var app=ApiAnwendung.Erstellen(["--FirmenZentrale:Persistence:Provider=PostgreSql"],0,Path.Combine(Repo(),"src","frontend","dist"));await app.StartAsync();
            var addresses=Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>(app.Services).Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            var client=new HttpClient{BaseAddress=new Uri(addresses!.Addresses.Single()),Timeout=TimeSpan.FromSeconds(15)};client.DefaultRequestHeaders.Add("Origin",client.BaseAddress.GetLeftPart(UriPartial.Authority));client.DefaultRequestHeaders.Add("X-Arbeitsaktion","1");return(app,client);
        }
        static async Task<System.Text.Json.JsonElement> Json(HttpClient client,string path){using var response=await client.GetAsync("/api/"+path);Assert.True(response.IsSuccessStatusCode,"GET failed: "+path+" HTTP "+(int)response.StatusCode);return System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();}
        static async Task<System.Text.Json.JsonElement> Post(HttpClient client,string path,object body){using var response=await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client,"/api/"+path,body);Assert.True(response.IsSuccessStatusCode,"POST failed: "+path+" HTTP "+(int)response.StatusCode);return System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();}
        string created;
        var first=await Start();
        try{
            var provider=await Json(first.Client,"persistenz");Assert.Equal("PostgreSQL",provider.GetProperty("provider").GetString());Assert.False(provider.GetProperty("fluechtig").GetBoolean());
            await using(var db=Context()){Assert.Equal(vor,new long[]{await db.Kontakte.LongCountAsync(),await db.Kategorien.LongCountAsync(),await db.Akten.LongCountAsync(),await db.Dokumente.LongCountAsync()});}
            await Json(first.Client,"kontakte?seite=0");await Json(first.Client,"kontakte/"+firma);await Json(first.Client,"akten/"+akte);await Json(first.Client,"uebersicht");await Json(first.Client,"posteingang?seite=0");await Json(first.Client,"akten?seite=0");await Json(first.Client,"kontakte/"+firma+"/arbeitszahlen");await Json(first.Client,"kontakte/"+firma+"/arbeit?bereich=akten&seite=0");
            var snapshot=await Json(first.Client,"akten/"+akte);var version=snapshot.GetProperty("vorgang").GetProperty("version").GetInt32();await Post(first.Client,"akten/"+akte+"/bearbeitungsstatus",new{status="Wartet auf Rückmeldung",version});
            using(var stale=await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(first.Client,"/api/akten/"+akte+"/bearbeitungsstatus",new{status="Erledigt",version})){
                Assert.Equal(System.Net.HttpStatusCode.Conflict,stale.StatusCode);var text=await stale.Content.ReadAsStringAsync();Assert.DoesNotContain("Npgsql",text);Assert.DoesNotContain("Host=",text);Assert.DoesNotContain("StackTrace",text);
            }
            var note=await Post(first.Client,"akten/"+akte+"/notizen",new{betreff="Host-Neustart-Nachweis",inhalt="Dieser Datensatz muss nach Hostneustart wieder erscheinen.",version=0});created=note.GetProperty("id").GetString()!;
            await Post(first.Client,"dokumente/"+doc+"/umordnen",new{bisherigerVorgangId=c,neuerVorgangId=b,version=docVersion});await Post(first.Client,"dokumente/"+doc+"/umordnen",new{bisherigerVorgangId=b,neuerVorgangId=c,version=docVersion+1});
            using var html=await first.Client.GetAsync("/");Assert.True(html.IsSuccessStatusCode);var textHtml=await html.Content.ReadAsStringAsync();Assert.Contains("<div id=\"root\">",textHtml);
            var assets=System.Text.RegularExpressions.Regex.Matches(textHtml,"(?:src|href)=\"(/assets/[^\"]+)\"");Assert.NotEmpty(assets);foreach(System.Text.RegularExpressions.Match asset in assets){using var response=await first.Client.GetAsync(asset.Groups[1].Value);Assert.True(response.IsSuccessStatusCode);}
            Assert.Equal(0,(await Json(first.Client,"importe")).GetArrayLength());using var blocked=await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(first.Client,"/api/importe/live-test",new{});Assert.Equal(System.Net.HttpStatusCode.Conflict,blocked.StatusCode);
        }finally{first.Client.Dispose();await first.App.StopAsync();await first.App.DisposeAsync();}
        var second=await Start();
        try{var data=await Json(second.Client,"akten/"+akte);Assert.Contains(data.GetProperty("notizen").EnumerateArray(),n=>n.GetProperty("id").GetString()==created);Assert.Contains(data.GetProperty("notizen").EnumerateArray(),n=>n.GetProperty("id").GetString()==alteNotiz);Assert.Equal("Wartet auf Rückmeldung",data.GetProperty("vorgang").GetProperty("status").GetString());Assert.Equal(firma,(await Json(second.Client,"kontakte/"+firma)).GetProperty("id").GetString());}
        finally{second.Client.Dispose();await second.App.StopAsync();await second.App.DisposeAsync();}
        output.WriteLine("HTTP/API: contacts, cases, invoice, inbox, assignment, reassignment, notes, pages/counts, stale update 409, frontend HTML/assets and disabled demo-import checked. Host stopped/disposed and freshly started; PostgreSQL records and note retained; no startup seed.");
    }
}
