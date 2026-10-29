using System.Security.Cryptography;
using System.Text;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class MfaTests
{
    internal sealed class Clock:TimeProvider
    {public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;public void Next()=>Now=Now.AddSeconds(30);}
    internal static string Secret()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    internal static byte[] Key(IDataProtectionProvider p,Benutzerkonto u)=>Convert.FromBase64String(p.CreateProtector("FirmenZentrale.MFA.v1",u.Id).Unprotect(u.MfaGeheimnis??u.MfaVorbereitung!));
    internal static string Code(IDataProtectionProvider p,Benutzerkonto u,Clock c)=>Totp.Code(Key(p,u),c.GetUtcNow().ToUnixTimeSeconds()/30);
    internal static string BadCode(IDataProtectionProvider p,Benutzerkonto u,Clock c)
    {var key=Key(p,u);var period=c.GetUtcNow().ToUnixTimeSeconds()/30;var valid=new[]{period-1,period,period+1}.Select(x=>Totp.Code(key,x)).ToArray();return Enumerable.Range(0,10).Select(x=>x.ToString("D6")).First(x=>!valid.Contains(x));}
    [Fact] public async Task KurzesPasswortIstDauerhaftAnMfaGebundenUndNichtDeaktivierbar(){
        using var c=new SqliteConnection("Data Source=:memory:");c.Open();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();var h=Anmeldung.Passwortdienst();var pass=Secret();var u=await Anmeldung.Anlegen(db,h,new("kurz","Mira Weber",pass,"Administrator"),true);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Anmeldung.PasswortAendern(db,h,u.Id,new(pass,"achtacht"),true));
        var clock=new Clock();var p=new EphemeralDataProtectionProvider();var m=new MfaDienst(p,clock);await m.Vorbereiten(db,h,u.Id,new(pass,null));await m.Verwalten(db,h,u.Id,new(pass,Code(p,u,clock)),"aktivieren");
        await Anmeldung.PasswortAendern(db,h,u.Id,new(pass,"achtacht"),true);Assert.True(u.PasswortMfaGebunden);Assert.True(await MfaRichtlinie.Erforderlich(db,u));
        clock.Next();await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new("achtacht",Code(p,u,clock)),"deaktivieren"));Assert.NotNull(u.MfaGeheimnis);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>Anmeldung.PasswortAendern(db,h,u.Id,new("achtacht","andere88"),false));
        await Anmeldung.PasswortAendern(db,h,u.Id,new("achtacht",Secret()),true);Assert.False(u.PasswortMfaGebunden);
    }
    [Theory][InlineData("hauptdatenbank")][InlineData("fz_browser_test_../x")][InlineData("fz_browser_test_123")]
    public void NormalstartVerweigertAndereDatenbanken(string name)=>Assert.Throws<InvalidOperationException>(()=>Betriebsplattform.Web.Betrieb.IsolierteArbeitsumgebung.Datenbankname(name));
    [Theory][InlineData(59L,"94287082")][InlineData(1111111109L,"07081804")][InlineData(1111111111L,"14050471")][InlineData(1234567890L,"89005924")][InlineData(2000000000L,"69279037")][InlineData(20000000000L,"65353130")]
    public void Rfc6238Sha1Vektoren(long seconds,string expected)=>Assert.Equal(expected,Totp.Code(Encoding.ASCII.GetBytes("12345678901234567890"),seconds/30,8));
    [Fact] public async Task EinrichtungReplayRecoveryNeuausstellungResetUndDeaktivierung()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();
        var p=new EphemeralDataProtectionProvider();var clock=new Clock();var m=new MfaDienst(p,clock);var h=Anmeldung.Passwortdienst();var pass=Secret();var u=await Anmeldung.Anlegen(db,h,new("team","Team",pass,"Mitarbeiter"));
        var setup=await m.Vorbereiten(db,h,u.Id,new(pass,null));Assert.Null(u.MfaGeheimnis);Assert.True(u.MfaVorbereitung!=setup.Schluessel);Assert.Equal(32,setup.Schluessel.Length);Assert.True(Totp.Base32(Key(p,u))==setup.Schluessel);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new(pass,BadCode(p,u,clock)),"aktivieren"));
        var activated=await m.Verwalten(db,h,u.Id,new(pass,Code(p,u,clock)),"aktivieren");var codes=activated.Codes!.Codes;Assert.Equal(8,codes.Length);Assert.Null(u.MfaVorbereitung);Assert.All(codes,c=>Assert.DoesNotContain(c.Replace("-",""),u.MfaRecoveryHashes!));
        // A password-only session already in flight at activation must not mint an MFA session via password change.
        Assert.Equal(401,(await Assert.ThrowsAsync<Arbeitsfehler>(()=>Anmeldung.PasswortAendern(db,h,u.Id,new(pass,Secret())))).Status);
        async Task<string> Challenge(){var value=m.AnmeldungVorbereiten(u);u.Version++;await db.SaveChangesAsync();return value;}
        await Assert.ThrowsAsync<Arbeitsfehler>(async()=>await m.Anmelden(db,await Challenge(),new(null,Code(p,u,clock))));
        clock.Next();await m.Anmelden(db,await Challenge(),new(null,Code(p,u,clock)));await Assert.ThrowsAsync<Arbeitsfehler>(async()=>await m.Anmelden(db,await Challenge(),new(null,Code(p,u,clock))));
        var oldStamp=u.Sicherheitskennung;await m.Anmelden(db,await Challenge(),new(null,codes[0],true));Assert.NotEqual(oldStamp,u.Sicherheitskennung);Assert.Equal(7,u.MfaRecoveryHashes!.Split(',').Length);
        await Assert.ThrowsAsync<Arbeitsfehler>(async()=>await m.Anmelden(db,await Challenge(),new(null,codes[0],true)));
        var reset=(await Erstzugang.WiederherstellungAendern(db,u.Id,new(u.Version,false,true),u.Id))!;var nextPass=Secret();var encrypted=u.MfaGeheimnis;await Erstzugang.Einloesen(db,h,new(reset.Code,nextPass),true);pass=nextPass;Assert.Equal(encrypted,u.MfaGeheimnis);Assert.Equal(7,u.MfaRecoveryHashes!.Split(',').Length);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new("falsch",codes[1],true),"codes"));Assert.Equal(7,u.MfaRecoveryHashes!.Split(',').Length);
        var renewed=(await m.Verwalten(db,h,u.Id,new(pass,codes[1],true),"codes")).Codes!.Codes;Assert.Equal(8,u.MfaRecoveryHashes!.Split(',').Length);
        await Assert.ThrowsAsync<Arbeitsfehler>(async()=>await m.Anmelden(db,await Challenge(),new(null,codes[2],true)));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new(pass,BadCode(p,u,clock)),"deaktivieren"));Assert.NotNull(u.MfaGeheimnis);
        await m.Verwalten(db,h,u.Id,new(pass,renewed[0],true),"deaktivieren");Assert.Null(u.MfaGeheimnis);Assert.Null(u.MfaRecoveryHashes);Assert.Null(u.MfaChallengeHash);
    }
    [Fact] public async Task AblaufFensterUndAnhaltendeFehlversuchsgrenze()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();var p=new EphemeralDataProtectionProvider();var c=new Clock();var m=new MfaDienst(p,c);var h=Anmeldung.Passwortdienst();var pass=Secret();var u=await Anmeldung.Anlegen(db,h,new("team","Team",pass,"Mitarbeiter"));await m.Vorbereiten(db,h,u.Id,new(pass,null));
        c.Now=c.Now.AddMinutes(11);await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new(pass,Code(p,u,c)),"aktivieren"));Assert.Null(u.MfaGeheimnis);
        await m.Vorbereiten(db,h,u.Id,new(pass,null));var future=Totp.Code(Key(p,u),c.GetUtcNow().ToUnixTimeSeconds()/30+1);await m.Verwalten(db,h,u.Id,new(pass,future),"aktivieren");
        var expired=m.AnmeldungVorbereiten(u);u.Version++;await db.SaveChangesAsync();c.Now=c.Now.AddMinutes(6);await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Anmelden(db,expired,new(null,Code(p,u,c))));
        for(var n=0;n<5;n++){var v=m.AnmeldungVorbereiten(u);u.Version++;await db.SaveChangesAsync();await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Anmelden(db,v,new(null,BadCode(p,u,c))));}Assert.True(u.GesperrtBis>c.Now);Assert.Null(u.MfaChallengeHash);
    }
    private sealed class BothRead:SaveChangesInterceptor
    {
        private int count;private readonly TaskCompletionSource ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken ct=default){if(Interlocked.Increment(ref count)==2)ready.TrySetResult();await ready.Task.WaitAsync(TimeSpan.FromSeconds(15),ct);return result;}
    }
    [Fact] public async Task LetzterRecoveryCodeErlaubtEinmaligeErneuerungMitFrischemPasswort()
    {
        await using var cn=new SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();var p=new EphemeralDataProtectionProvider();var c=new Clock();var m=new MfaDienst(p,c);var h=Anmeldung.Passwortdienst();var pass=Secret();var u=await Anmeldung.Anlegen(db,h,new("team","Team",pass,"Mitarbeiter"));await m.Vorbereiten(db,h,u.Id,new(pass,null));var codes=(await m.Verwalten(db,h,u.Id,new(pass,Code(p,u,c)),"aktivieren")).Codes!.Codes;
        u.MfaRecoveryHashes=u.MfaRecoveryHashes!.Split(',')[0];var challenge=m.AnmeldungVorbereiten(u);u.Version++;await db.SaveChangesAsync();var result=await m.Anmelden(db,challenge,new(null,codes[0],true));Assert.Empty(u.MfaRecoveryHashes!);Assert.True(m.HatSchritt(u,result.Schritt));
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new("falsch",null),"codes",result.Schritt));
        var renewed=await m.Verwalten(db,h,u.Id,new(pass,null),"codes",result.Schritt);Assert.Equal(8,renewed.Codes!.Codes.Length);Assert.Null(u.MfaStepupHash);
        await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new(pass,null),"deaktivieren",result.Schritt));
        challenge=m.AnmeldungVorbereiten(u);u.Version++;await db.SaveChangesAsync();var expired=await m.Anmelden(db,challenge,new(null,renewed.Codes.Codes[0],true));c.Now=c.Now.AddMinutes(6);Assert.False(m.HatSchritt(u,expired.Schritt));await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Verwalten(db,h,u.Id,new(pass,null),"codes",expired.Schritt));
    }
    internal static async Task ProviderRace(string connection)
    {
        var p=new EphemeralDataProtectionProvider();var c=new Clock();var m=new MfaDienst(p,c);var h=Anmeldung.Passwortdienst();var pass=Secret();
        DbContextOptions<FirmenDbContext> Options(BothRead? gate=null){var b=new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(connection);if(gate is not null)b.AddInterceptors(gate);return b.Options;}
        await using var setup=new FirmenDbContext(Options());var u=await Anmeldung.Anlegen(setup,h,new("mfa-race","MFA Race",pass,"Mitarbeiter"));await m.Vorbereiten(setup,h,u.Id,new(pass,null));var codes=(await m.Verwalten(setup,h,u.Id,new(pass,Code(p,u,c)),"aktivieren")).Codes!.Codes;
        foreach(var recovery in new[]{false,true}){
            c.Next();var value=m.AnmeldungVorbereiten(u);u.Version++;await setup.SaveChangesAsync();var input=new MfaPruefung(null,recovery?codes[0]:Code(p,u,c),recovery);var gate=new BothRead();
            async Task<bool> Redeem(){await using var db=new FirmenDbContext(Options(gate));try{await m.Anmelden(db,value,input);return true;}catch(Arbeitsfehler){return false;}}
            var results=await Task.WhenAll(Task.Run(Redeem),Task.Run(Redeem));Assert.Single(results,x=>x);await setup.Entry(u).ReloadAsync();
            Assert.Equal(1,await setup.Sicherheitsereignisse.CountAsync(x=>x.BenutzerId==u.Id&&x.Aktion==(recovery?"Anmeldung mit Wiederherstellungscode":"Anmeldung mit Authenticator")));
        }
        var challenge=m.AnmeldungVorbereiten(u);u.Version++;await setup.SaveChangesAsync();var invalid=BadCode(p,u,c);
        async Task Fail(){await using var db=new FirmenDbContext(Options());await Assert.ThrowsAsync<Arbeitsfehler>(()=>m.Anmelden(db,challenge,new(null,invalid)));}
        await Task.WhenAll(Enumerable.Range(0,5).Select(_=>Task.Run(Fail)));await setup.Entry(u).ReloadAsync();Assert.True(u.GesperrtBis>c.Now);Assert.Equal(5,await setup.Sicherheitsereignisse.CountAsync(x=>x.BenutzerId==u.Id&&x.Aktion=="Zwei-Faktor-Prüfung abgelehnt"));
    }
}
