using System.Net;
using System.Text.Json;
using Betriebsplattform.Web.Persistenz;
using Npgsql;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Sicherheit;

// Explicit, expiring operator exception. Never represents verified MFA.
public sealed record LokaleAbnahmeregel(bool Aktiv,string Datenbank,string BenutzerId,DateTimeOffset GueltigBis);
public sealed class LokaleAdminAbnahme {
 readonly LokaleAbnahmeregel regel; readonly string datei; readonly string umgebung; readonly string database; readonly string host; readonly int port;
 internal LokaleAdminAbnahme(LokaleAbnahmeregel r,string path,string environment,string db,string dbHost,int listenPort){regel=r;datei=path;umgebung=environment;database=db;host=dbHost;port=listenPort;}
 internal static bool Kontext(string environment,string database,string host,int port,LokaleAbnahmeregel r)=>environment=="Development"&&r.Aktiv&&r.GueltigBis>DateTimeOffset.UtcNow&&r.GueltigBis<=DateTimeOffset.UtcNow.AddDays(8)&&System.Text.RegularExpressions.Regex.IsMatch(r.Datenbank,@"\Afz_browser_test_[a-f0-9]{32}\z")&&database==r.Datenbank&&host is "127.0.0.1" or "localhost"&&port==5181&&!string.IsNullOrWhiteSpace(r.BenutzerId);
 public static void Registrieren(WebApplicationBuilder b){
  var path=b.Configuration["FirmenZentrale:LokaleAbnahme:Datei"];if(path is null)return;
  var r=JsonSerializer.Deserialize<LokaleAbnahmeregel>(File.ReadAllText(path))??throw new InvalidOperationException("Abnahmeregel fehlt.");
  var cs=new NpgsqlConnectionStringBuilder(b.Configuration.GetConnectionString("FirmenZentrale"));var p=b.Configuration.GetValue<int?>("FirmenZentrale:Network:Port")??5181;
  if(!Kontext(b.Environment.EnvironmentName,cs.Database??"",cs.Host??"",p,r)||cs.Port!=5432||!b.Configuration.GetValue<bool>("FirmenZentrale:Security:Enabled")||b.Configuration["FirmenZentrale:Network:Address"] is not(null or "127.0.0.1"))throw new InvalidOperationException("Lokale Abnahme außerhalb des geprüften Kontexts verweigert.");
  b.Services.AddSingleton(new LokaleAdminAbnahme(r,path,b.Environment.EnvironmentName,cs.Database!,cs.Host??"",p));
 }
 internal bool Erlaubt(HttpContext c,FirmenDbContext db,Benutzerkonto u){
  try{
   var aktuell=JsonSerializer.Deserialize<LokaleAbnahmeregel>(File.ReadAllText(datei));
   return aktuell==regel&&Kontext(umgebung,database,host,port,regel)&&db.Database.GetDbConnection().Database==database&&u.Id==regel.BenutzerId&&u.Login=="admin"&&u.Rolle=="Administrator"&&u.Aktiv&&u.Eingerichtet&&c.Request.IsHttps&&c.Request.Host.Host=="127.0.0.1"&&c.Request.Host.Port==5181&&c.Connection.LocalPort==5181&&c.Connection.LocalIpAddress is {} local&&IPAddress.IsLoopback(local)&&c.Connection.RemoteIpAddress is {} remote&&IPAddress.IsLoopback(remote);
  }catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){return false;}
 }
 public static bool Gilt(HttpContext c,FirmenDbContext db,Benutzerkonto? u)=>u is not null&&c.RequestServices.GetService<LokaleAdminAbnahme>()?.Erlaubt(c,db,u)==true;
}
