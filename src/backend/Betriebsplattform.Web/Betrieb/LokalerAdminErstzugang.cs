using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Betrieb;

// Local operator command only. No HTTP route and no password in CLI/configuration.
public static class LokalerAdminErstzugang {
 internal const string Marker="Lokaler Admin-Erstzugang vorbereitet";
 internal sealed record Einmalzugang([property:JsonIgnore]string Passwort,string BenutzerId,DateTimeOffset GueltigBis){public override string ToString()=>"Lokaler Einmalzugang (Geheimnis ausgeblendet)";}
 public static bool Abgelaufen(Benutzerkonto u,DateTimeOffset now)=>u.Login=="admin"&&u.MfaNeueinrichtung&&u.MfaGeheimnis==null&&u.EinladungBis!=null&&u.EinladungBis<=now;
 internal static async Task<Einmalzugang> Erstellen(FirmenDbContext db,IPasswordHasher<Benutzerkonto> h,TimeProvider clock){
  var pass=Convert.ToHexString(RandomNumberGenerator.GetBytes(24));var now=clock.GetUtcNow();await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  var u=await db.Benutzer.SingleOrDefaultAsync(x=>x.Login=="admin");
  if(u!=null){
   if(u.Rolle!="Administrator"||!u.Aktiv||!u.Eingerichtet||u.MfaGeheimnis!=null||!u.MfaNeueinrichtung||!await db.Sicherheitsereignisse.AnyAsync(x=>x.Ressource==u.Id&&x.Aktion==Marker)||await db.Sicherheitsereignisse.AnyAsync(x=>x.Ressource==u.Id&&x.Aktion=="Zwei-Faktor-Authentifizierung aktiviert"))throw new Arbeitsfehler("Konto admin ist bereits belegt oder fertig eingerichtet. Keine Änderung. Bestehenden kontrollierten Wiederherstellungsweg verwenden.",409);
   u.PasswortHash=h.HashPassword(u,pass);u.Sicherheitskennung=Guid.NewGuid().ToString("N");u.Version++;u.GeaendertAm=now;
  }else u=await Anmeldung.Anlegen(db,h,new("admin","Administration",pass,"Administrator"));
  u.MfaNeueinrichtung=true;u.EinladungBis=now.AddHours(1);u.Fehlversuche=0;u.GesperrtBis=null;MfaDienst.VorbereitungenWiderrufen(u);
  db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Ressource=u.Id,Aktion=Marker,Zeitpunkt=now});await db.SaveChangesAsync();await tx.CommitAsync();return new(pass,u.Id,u.EinladungBis.Value);
 }
 public static async Task<int> AbnahmeEinrichten(){
  try{
   // Operator stdin only: no password argument, source default or output.
   if(!Console.IsInputRedirected)throw new InvalidOperationException("Geschützte Operator-Eingabe erforderlich.");
   var d=await IsolierteArbeitsumgebung.Vorbereiten("private/arbeitsumgebung/ready.json");
   var pass=Console.ReadLine();if(!Anmeldung.PasswortGueltig(pass,8))throw new InvalidOperationException("Kennwort ungültig.");
   await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(d.Verbindung,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
   await db.Database.OpenConnectionAsync();if(db.Database.GetDbConnection().Database!=d.Datenbank)throw new InvalidOperationException("Falsches Ziel.");
   await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
   var u=await db.Benutzer.SingleAsync(x=>x.Login=="admin"&&x.Rolle=="Administrator"&&x.Aktiv&&x.Eingerichtet);
   u.PasswortHash=Anmeldung.Passwortdienst().HashPassword(u,pass!);u.PasswortMfaGebunden=true;u.EinladungBis=null;u.Fehlversuche=0;u.GesperrtBis=null;u.Sicherheitskennung=Guid.NewGuid().ToString("N");u.Version++;u.GeaendertAm=DateTimeOffset.UtcNow;MfaDienst.VorbereitungenWiderrufen(u);
   db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Ressource=u.Id,Aktion="Lokale Admin-Abnahme ausdrücklich aktiviert"});await db.SaveChangesAsync();await tx.CommitAsync();
   var policy=new LokaleAbnahmeregel(true,d.Datenbank,u.Id,DateTimeOffset.UtcNow.AddDays(7));var path=Path.Combine(d.Repo,"private","arbeitsumgebung","lokale-admin-abnahme.json");
   await File.WriteAllTextAsync(path,System.Text.Json.JsonSerializer.Serialize(policy,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
   Console.WriteLine("Lokale Admin-Abnahme eingerichtet; keine bestätigte MFA. Befristete Regel ist separat widerrufbar.");return 0;
  }catch(Exception){Console.Error.WriteLine("Lokale Abnahme nicht eingerichtet. Isoliertes Profil, Berechtigungen und Konto prüfen.");return 1;}
 }
 public static async Task<int> Konsole(bool status=false){
  try{
   if(!status&&(Console.IsInputRedirected||Console.IsOutputRedirected))throw new InvalidOperationException("Eigenes interaktives lokales Konsolenfenster erforderlich; keine Umleitung/CI/Protokollierung.");
   var d=await IsolierteArbeitsumgebung.Vorbereiten("private/arbeitsumgebung/ready.json");await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(d.Verbindung,n=>n.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
   var u=await db.Benutzer.AsNoTracking().SingleOrDefaultAsync(x=>x.Login=="admin");
   if(status){Console.WriteLine(u==null?"admin: nicht vorhanden":u.MfaGeheimnis!=null?"admin: vorhanden, MFA eingerichtet":"admin: vorhanden, persönliche MFA-Einrichtung noch erforderlich");return 0;}
   Console.WriteLine("Lokale Administrator-Einrichtung – ausschließlich isolierter Entwicklungsbestand: "+d.Datenbank);
   if(u!=null){Console.WriteLine("Nur ein noch nicht abgeschlossener lokaler Erstzugang kann erneuert werden. Zum Erneuern EINRICHTEN eingeben, sonst Abbruch:");if(Console.ReadLine()!="EINRICHTEN")return 2;}
   var z=await Erstellen(db,Anmeldung.Passwortdienst(),TimeProvider.System);
   Console.WriteLine("Login: admin");Console.WriteLine("Einmaliges Anfangspasswort (nur hier, gültig bis "+z.GueltigBis.ToLocalTime().ToString("dd.MM.yyyy HH:mm")+"):");Console.WriteLine(z.Passwort);
   Console.WriteLine("In MBOP anmelden und MFA persönlich einrichten. Erst danach ist der Arbeitsplatz freigegeben. Wiederherstellungscodes persönlich aufbewahren. Anschließend eigenes Passwort wählen.");
   Console.WriteLine("Das Passwort wird nicht erneut angezeigt/gespeichert. Dieses Fenster zum Ablesen offen lassen; danach mit Eingabetaste schließen. Keine Screenshots/Transkripte davon erstellen.");Console.ReadLine();return 0;
  }catch(Exception){Console.Error.WriteLine("Admin-Einrichtung nicht abgeschlossen. Isoliertes Profil, Datenbank, Kontozustand und lokale Berechtigung prüfen. Kein Ersatzpasswort und keine Sicherheitsumgehung.");return 1;}
 }
}
