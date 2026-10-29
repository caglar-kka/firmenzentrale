using System.Net;
using System.Text.Json;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Xunit;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class LokaleAdminAbnahmeTests {
 const string Db="fz_browser_test_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
 [Theory][InlineData("Development",Db,"127.0.0.1",5181,true)][InlineData("Production",Db,"127.0.0.1",5181,false)][InlineData("Staging",Db,"127.0.0.1",5181,false)][InlineData("Development","firmenzentrale","127.0.0.1",5181,false)][InlineData("Development","fz_browser_test_bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","127.0.0.1",5181,false)][InlineData("Development",Db,"192.168.1.20",5181,false)][InlineData("Development",Db,"127.0.0.1",5180,false)]
 public void NurExakterLokalerDevelopmentKontext(string env,string db,string host,int port,bool expected)=>Assert.Equal(expected,LokaleAdminAbnahme.Kontext(env,db,host,port,new(true,Db,"id",DateTimeOffset.UtcNow.AddDays(1))));
 [Fact] public void AbgelaufeneOderDeaktivierteRegelErteiltNichts(){Assert.False(LokaleAdminAbnahme.Kontext("Development",Db,"127.0.0.1",5181,new(false,Db,"id",DateTimeOffset.UtcNow.AddDays(1))));Assert.False(LokaleAdminAbnahme.Kontext("Development",Db,"127.0.0.1",5181,new(true,Db,"id",DateTimeOffset.UtcNow.AddDays(-1))));}
 [Fact] public void BenutzerHostTransportUndSofortigerWiderruf(){
 var path=Path.Combine(Path.GetTempPath(),"mbop-local-acceptance-"+Guid.NewGuid().ToString("N")+".json");var rule=new LokaleAbnahmeregel(true,Db,"id",DateTimeOffset.UtcNow.AddDays(1));File.WriteAllText(path,JsonSerializer.Serialize(rule));
 try{using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql("Host=127.0.0.1;Database="+Db).Options);var gate=new LokaleAdminAbnahme(rule,path,"Development",Db,"127.0.0.1",5181);var u=new Benutzerkonto{Id="id",Login="admin",Rolle="Administrator"};var c=new DefaultHttpContext();c.Request.Scheme="https";c.Request.Host=new("127.0.0.1",5181);c.Connection.LocalIpAddress=IPAddress.Loopback;c.Connection.RemoteIpAddress=IPAddress.Loopback;c.Connection.LocalPort=5181;
 Assert.True(gate.Erlaubt(c,db,u));u.Login="anderer";Assert.False(gate.Erlaubt(c,db,u));u.Login="admin";u.Id="fremd";Assert.False(gate.Erlaubt(c,db,u));u.Id="id";u.Rolle="Mitarbeiter";Assert.False(gate.Erlaubt(c,db,u));u.Rolle="Administrator";
 c.Connection.RemoteIpAddress=IPAddress.Parse("192.168.1.2");Assert.False(gate.Erlaubt(c,db,u));c.Connection.RemoteIpAddress=IPAddress.Loopback;c.Connection.LocalIpAddress=IPAddress.Any;Assert.False(gate.Erlaubt(c,db,u));c.Connection.LocalIpAddress=IPAddress.Loopback;c.Request.Scheme="http";Assert.False(gate.Erlaubt(c,db,u));c.Request.Scheme="https";c.Request.Host=new("anderer.example",5181);Assert.False(gate.Erlaubt(c,db,u));c.Request.Host=new("127.0.0.1",5181);
 File.WriteAllText(path,JsonSerializer.Serialize(rule with{Aktiv=false}));Assert.False(gate.Erlaubt(c,db,u));File.WriteAllText(path,"ungültig");Assert.False(gate.Erlaubt(c,db,u));
 }finally{File.Delete(path);}}
}
