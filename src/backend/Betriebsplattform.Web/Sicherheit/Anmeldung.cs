using System.Text;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Betriebsplattform.Web.Sicherheit;

public sealed record LoginEingabe(string? Login,string? Passwort);
public sealed record Passwortwechsel(string? BisherigesPasswort,string? NeuesPasswort)
{
    public override string ToString()=>nameof(Passwortwechsel);
}
public sealed record BenutzerEingabe(string? Login,string? Anzeigename,string? Passwort,string? Rolle);
public sealed record BenutzerAenderung(bool Aktiv,string Rolle,int Version);
public sealed record Freigabeeingabe(string Art,string Id,bool Erlaubt);

public static class Anmeldung
{
    internal static bool PasswortGueltig(string? passwort,int minimum=15)=>passwort is not null&&passwort.EnumerateRunes().Count()>=minimum&&passwort.Length<=128&&!string.IsNullOrWhiteSpace(passwort);
    public static async Task<Benutzerkonto> PasswortAendern(FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher,string benutzerId,Passwortwechsel eingabe,bool mfaSitzung=false)
    {
        if(!PasswortGueltig(eingabe.NeuesPasswort,8)||string.IsNullOrEmpty(eingabe.BisherigesPasswort)||eingabe.BisherigesPasswort.Length>128)
            throw new Arbeitsfehler("Bitte das bisherige Passwort und ein neues Passwort mit 15 bis 128 Zeichen eingeben.");
        var u=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==benutzerId&&x.Aktiv&&x.Eingerichtet);
        if(u is null||u.GesperrtBis>DateTimeOffset.UtcNow)throw new Arbeitsfehler("Bitte erneut anmelden.",401);
        var minimum=u.MfaGeheimnis is not null&&mfaSitzung?8:15;
        if(!PasswortGueltig(eingabe.NeuesPasswort,minimum))throw new Arbeitsfehler($"Das neue Passwort benötigt {minimum} bis 128 Zeichen. Acht Zeichen sind nur mit eingerichtetem und bestätigtem zweiten Faktor möglich.");
        if(u.MfaGeheimnis is not null&&!mfaSitzung)throw new Arbeitsfehler("Bitte mit beiden Faktoren erneut anmelden.",401);
        if(hasher.VerifyHashedPassword(u,u.PasswortHash,eingabe.BisherigesPasswort)==PasswordVerificationResult.Failed)
            throw new Arbeitsfehler("Das bisherige Passwort ist nicht korrekt.");
        if(eingabe.BisherigesPasswort==eingabe.NeuesPasswort)throw new Arbeitsfehler("Bitte ein anderes neues Passwort wählen.");
        u.PasswortHash=hasher.HashPassword(u,eingabe.NeuesPasswort!);u.PasswortMfaGebunden=eingabe.NeuesPasswort!.EnumerateRunes().Count()<15;
        u.WiederherstellungHash=null;u.WiederherstellungBis=null;MfaDienst.VorbereitungenWiderrufen(u);
        u.Sicherheitskennung=Guid.NewGuid().ToString("N");u.GeaendertAm=DateTimeOffset.UtcNow;u.Version++;
        db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Aktion="Eigenes Passwort geändert",Ressource=u.Id});
        await db.SaveChangesAsync();return u;
    }
    public static IPasswordHasher<Benutzerkonto> Passwortdienst()=>new PasswordHasher<Benutzerkonto>(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions{IterationCount=210000}));
    private static readonly string DummyHash=new PasswordHasher<Benutzerkonto>(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions{IterationCount=210000})).HashPassword(new(),Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
    public static void Dienste(WebApplicationBuilder b,Pilotoptionen p,string? root)
    {
        LokaleAdminAbnahme.Registrieren(b);b.Services.AddSingleton(p);b.Services.AddHttpContextAccessor();b.Services.AddSingleton<Ressourcenbereich>();
        if(!p.Aktiv)return;
        // New local host => new login. Production keeps the existing cookie lifetime/keyring behavior.
        var entwicklungsinstanz=b.Environment.IsDevelopment()?Guid.NewGuid().ToString("N"):null;
        b.Services.AddSingleton<MfaDienst>();b.Services.AddSingleton(TimeProvider.System);
        if(root is null)throw new InvalidOperationException("Pilot requires explicit document storage.");
        if(Directory.Exists(Path.Combine(root,"keyring"))&&(File.GetAttributes(Path.Combine(root,"keyring"))&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked key directories are not permitted.");
        b.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddPolicy("bildverwaltung",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));o.AddPolicy("login",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));o.AddPolicy("mfa",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
        b.Services.AddSingleton<IPasswordHasher<Benutzerkonto>,PasswordHasher<Benutzerkonto>>();
        b.Services.Configure<PasswordHasherOptions>(o=>o.IterationCount=210000);
        var dp=b.Services.AddDataProtection().SetApplicationName("FirmenZentrale.Pilot").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root,"keyring")));
        if(OperatingSystem.IsWindows())dp.ProtectKeysWithDpapi();else throw new InvalidOperationException("Pilot key protection requires the prepared Windows host.");
        b.Services.AddAntiforgery(o=>{o.HeaderName="X-CSRF-TOKEN";o.Cookie.Name="__Host-FZ-CSRF";o.Cookie.SecurePolicy=CookieSecurePolicy.Always;o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;});
        b.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
            o.Cookie.Name="__Host-FZ";o.Cookie.Path="/";o.Cookie.HttpOnly=true;o.Cookie.SecurePolicy=CookieSecurePolicy.Always;o.Cookie.SameSite=SameSiteMode.Strict;
            o.ExpireTimeSpan=TimeSpan.FromMinutes(30);o.SlidingExpiration=false;
            o.Events.OnSigningIn=c=>{if(entwicklungsinstanz is not null)c.Properties.Items["mbop-development-instance"]=entwicklungsinstanz;return Task.CompletedTask;};
            o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
            o.Events.OnValidatePrincipal=async c=>{
                if(entwicklungsinstanz is not null&&(!c.Properties.Items.TryGetValue("mbop-development-instance",out var instanz)||instanz!=entwicklungsinstanz)){c.RejectPrincipal();await c.HttpContext.SignOutAsync();return;}
                var id=c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);var stamp=c.Principal?.FindFirstValue("stamp");var db=c.HttpContext.RequestServices.GetRequiredService<FirmenDbContext>();
                var mfa=c.Principal?.HasClaim("mfa","true")==true;var now=DateTimeOffset.UtcNow;var u=await db.Benutzer.SingleOrDefaultAsync(x=>x.Id==id);var lokal=LokaleAdminAbnahme.Gilt(c.HttpContext,db,u);
                if(u is null||!u.Aktiv||!u.Eingerichtet||u.Sicherheitskennung!=stamp||u.GesperrtBis>now||(!lokal&&Betriebsplattform.Web.Betrieb.LokalerAdminErstzugang.Abgelaufen(u,now))||(u.MfaGeheimnis!=null&&!mfa&&!lokal)||(c.Principal?.HasClaim("lokale-abnahme","true")==true&&!lokal)){c.RejectPrincipal();await c.HttpContext.SignOutAsync();}};
        });
        b.Services.AddAuthorization();
    }
    public static void Pipeline(WebApplication app,Pilotoptionen p)
    {
        if(!p.Aktiv)return;
        app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
        app.Use(async(c,next)=>{
            if(c.User.Identity?.IsAuthenticated==true&&c.Request.Path.StartsWithSegments("/api")){
                var db=c.RequestServices.GetRequiredService<FirmenDbContext>();
                var konto=await db.Benutzer.AsNoTracking().SingleAsync(u=>u.Id==c.User.FindFirstValue(ClaimTypes.NameIdentifier));
                var eingeschraenkt=!LokaleAdminAbnahme.Gilt(c,db,konto)&&await MfaRichtlinie.Einrichtung(db,konto.Id);
                c.Items["mfaEinrichten"]=eingeschraenkt;
                if(eingeschraenkt&&!new[]{"/api/auth/session","/api/auth/logout","/api/auth/mfa/status","/api/auth/mfa/vorbereiten","/api/auth/mfa/aktivieren"}.Contains(c.Request.Path.Value)){
                    c.Response.StatusCode=403;await c.Response.WriteAsJsonAsync(new{mfaEinrichten=true,meldung="Bitte zuerst die Zwei-Faktor-Authentifizierung einrichten."});return;
                }
            }

            if(c.Request.Path.StartsWithSegments("/api")&&!c.Request.Path.StartsWithSegments("/api/auth")){
                if(c.User.Identity?.IsAuthenticated!=true){c.Response.StatusCode=401;await c.Response.WriteAsJsonAsync(new ApiFehler("Bitte anmelden."));return;}
                if(c.Request.Path.StartsWithSegments("/api/admin")&&!c.User.IsInRole("Administrator")){c.Response.StatusCode=403;return;}
            }
            if(c.Request.Path.StartsWithSegments("/api")&&c.Request.Method is not ("GET" or "HEAD")){
                try{await c.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(c);}
                catch(AntiforgeryValidationException){c.Response.StatusCode=400;await c.Response.WriteAsJsonAsync(new ApiFehler("Die Sicherheitsbestätigung ist abgelaufen. Bitte die Seite neu laden."));return;}
            }
            using var actor=Arbeitskontext.Beginnen((c.User.Identity?.Name??"Nicht angemeldet")+" ["+(c.User.FindFirstValue(ClaimTypes.NameIdentifier)??"")+"]");
            try{if(!await Kontaktberechtigungen.Anfrage(c,c.RequestServices.GetRequiredService<FirmenDbContext>()))return;await next();}catch(Exception e) when(e is DbUpdateException or Npgsql.NpgsqlException or IOException){if(c.Response.HasStarted)throw;var f=Persistenzfehler.Beschreiben(e);c.Response.StatusCode=f.Status;await c.Response.WriteAsJsonAsync(new ApiFehler(f.Meldung));}
        });
    }
    private static string Login(string? s){s=(s??"").Trim().ToLowerInvariant();if(!System.Text.RegularExpressions.Regex.IsMatch(s,@"\A[a-z0-9._-]{3,80}\z"))throw new Arbeitsfehler("Login benötigt 3 bis 80 Buchstaben, Ziffern oder ._-.");return s;}
    public static async Task<Benutzerkonto> Anlegen(FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher,BenutzerEingabe e,bool bootstrap=false,CancellationToken ct=default)
    {
        if(bootstrap&&await db.Benutzer.AnyAsync(ct))throw new Arbeitsfehler("Initialisierung ist bereits abgeschlossen.",409);
        if(e.Rolle is not ("Administrator" or "Mitarbeiter")||!PasswortGueltig(e.Passwort))throw new Arbeitsfehler("Bitte Rolle und ein Passwort mit 15 bis 128 Zeichen angeben.");
        var u=new Benutzerkonto{Login=Login(e.Login),Anzeigename=Arbeitsregeln.Text(e.Anzeigename,"Anzeigename",100),Rolle=e.Rolle};u.PasswortHash=hasher.HashPassword(u,e.Passwort!);db.Benutzer.Add(u);await db.SaveChangesAsync(ct);return u;
    }
    public static void Endpunkte(WebApplication app,Pilotoptionen p)
    {
        app.MapGet("/api/auth/session",(HttpContext c)=>p.Aktiv?Results.Json(new{aktiv=true,passwortMinimum=c.User.HasClaim("mfa","true")?8:15,benutzerId=c.User.FindFirstValue(ClaimTypes.NameIdentifier),mfaEinrichten=c.Items["mfaEinrichten"] is true,angemeldet=c.User.Identity?.IsAuthenticated==true,benutzer=c.User.Identity?.Name,admin=c.User.IsInRole("Administrator"),csrf=c.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(c).RequestToken}):Results.Json(new{aktiv=false}));
        if(!p.Aktiv)return;
        MfaEndpunkte.Registrieren(app);MfaRichtlinie.Endpunkte(app);Kontaktberechtigungen.Endpunkte(app);MfaNotfall.Endpunkte(app);
        app.MapPost("/api/auth/passwort",async(Passwortwechsel e,HttpContext c,FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher)=>{
            try{
                var u=await PasswortAendern(db,hasher,c.User.FindFirstValue(ClaimTypes.NameIdentifier)!,e,c.User.HasClaim("mfa","true"));
                // Keep this reauthenticated session; every other cookie retains the obsolete stamp.
                await MfaEndpunkte.Sitzung(c,u,true,c.User.HasClaim("mfa","true"));
                return Results.Ok();
            }catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireAuthorization().RequireRateLimiting("login");
        var admins=app.MapGroup("/api/admin").RequireAuthorization(policy=>policy.RequireRole("Administrator"));
        app.MapPost("/api/auth/login",async(LoginEingabe e,HttpContext c,FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher,MfaDienst mfa)=>{
            if(e.Login?.Length>80||e.Passwort?.Length>128||e.Passwort is null)return Results.Unauthorized();
            var login=(e.Login??"").Trim().ToLowerInvariant();var u=await db.Benutzer.SingleOrDefaultAsync(u=>u.Login==login);var now=DateTimeOffset.UtcNow;
            // Work factor also applies to unknown accounts; no user enumeration response.
            var dummy=new Benutzerkonto();var hash=u?.PasswortHash??DummyHash;
            var lokal=LokaleAdminAbnahme.Gilt(c,db,u);var pruefung=hasher.VerifyHashedPassword(u??dummy,hash,e.Passwort);var ok=pruefung!=PasswordVerificationResult.Failed&&(lokal||!(u!=null&&Betriebsplattform.Web.Betrieb.LokalerAdminErstzugang.Abgelaufen(u,now)));
            if(u is null||!u.Aktiv||!u.Eingerichtet||u.GesperrtBis>now||!ok){
                if(u is not null){u.Fehlversuche++;if(u.Fehlversuche>=5){u.GesperrtBis=now.AddMinutes(15);u.Fehlversuche=0;}u.Version++;db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Aktion="Anmeldung abgelehnt",Ressource=u.Id});await db.SaveChangesAsync();}
                return Results.Json(new ApiFehler("Anmeldung nicht möglich. Angaben prüfen oder später erneut versuchen."),statusCode:401);
            }
            if(pruefung==PasswordVerificationResult.SuccessRehashNeeded)u.PasswortHash=hasher.HashPassword(u,e.Passwort);
            u.Fehlversuche=0;u.GesperrtBis=null;u.Version++;
            if(u.MfaGeheimnis is not null&&!lokal){
                var challenge=mfa.AnmeldungVorbereiten(u);db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Aktion="Anmeldung wartet auf zweiten Faktor",Ressource=u.Id});await db.SaveChangesAsync();
                await c.SignOutAsync();MfaDienst.CookieSetzen(c,challenge);return Results.Json(new{mfaErforderlich=true});
            }
            MfaDienst.CookieLoeschen(c);
            db.Sicherheitsereignisse.Add(new(){BenutzerId=u.Id,Aktion="Anmeldung",Ressource=u.Id});await db.SaveChangesAsync();
            var begrenzt=await MfaRichtlinie.Erforderlich(db,u);
            var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,u.Id),new(ClaimTypes.Name,u.Anzeigename),new("stamp",u.Sicherheitskennung)};
            if(!begrenzt||lokal)claims.Add(new(ClaimTypes.Role,u.Rolle));
            if(lokal)claims.Add(new("lokale-abnahme","true"));
            await c.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
            return Results.Ok();
        }).RequireRateLimiting("login");
        app.MapPost("/api/auth/logout",async(HttpContext c,FirmenDbContext db)=>{
            var id=c.User.FindFirstValue(ClaimTypes.NameIdentifier);MfaDienst.CookieLoeschen(c);await c.SignOutAsync();
            if(id is not null)try{db.Sicherheitsereignisse.Add(new(){BenutzerId=id,Aktion="Abmeldung",Ressource=id});await db.SaveChangesAsync();}catch(Exception){c.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("FirmenZentrale.Security").LogWarning("Abmeldung erfolgt; Auditablage momentan nicht verfügbar.");}
            return Results.Ok();
        });
        admins.MapGet("/benutzer",async(FirmenDbContext db)=>await db.Benutzer.AsNoTracking().OrderBy(u=>u.Login).Select(u=>new{u.Id,u.Login,u.Anzeigename,u.Rolle,u.Aktiv,u.Version,u.Eingerichtet,u.EinladungBis,u.WiederherstellungBis,u.MfaNeueinrichtung}).Take(100).ToArrayAsync());
        admins.MapPost("/benutzer",async(EinladungEingabe e,HttpContext c,FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher)=>{
            if(!Erstzugang.SichererTransport(c))return Results.BadRequest(new ApiFehler("Bitte eine vertrauenswürdige HTTPS-Verbindung verwenden."));
            try{return Results.Json(await Erstzugang.Anlegen(db,hasher,e,c.User.FindFirstValue(ClaimTypes.NameIdentifier)!));}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
            catch(DbUpdateException){return Results.Conflict(new ApiFehler("Login ist bereits vergeben oder Angaben wurden geändert."));}
        }).RequireRateLimiting("login");
        admins.MapPost("/benutzer/{id}/einladung",async(string id,EinladungAenderung e,HttpContext c,FirmenDbContext db)=>{
            if(!Erstzugang.SichererTransport(c))return Results.BadRequest(new ApiFehler("Bitte eine vertrauenswürdige HTTPS-Verbindung verwenden."));
            try{return Results.Json(await Erstzugang.Aendern(db,id,e,c.User.FindFirstValue(ClaimTypes.NameIdentifier)!));}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("login");
        app.MapPost("/api/auth/einrichten",async(EinrichtungEingabe e,HttpContext c,FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher)=>{
            if(!Erstzugang.SichererTransport(c))return Results.BadRequest(new ApiFehler("Bitte eine vertrauenswürdige HTTPS-Verbindung verwenden."));
            try{await Erstzugang.Einloesen(db,hasher,e);return Results.Ok();}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("login");
        admins.MapPost("/benutzer/{id}/wiederherstellung",async(string id,WiederherstellungAenderung e,HttpContext c,FirmenDbContext db)=>{
            if(!Erstzugang.SichererTransport(c))return Results.BadRequest(new ApiFehler("Bitte eine vertrauensw\u00fcrdige HTTPS-Verbindung verwenden."));
            try{return Results.Json(await Erstzugang.WiederherstellungAendern(db,id,e,c.User.FindFirstValue(ClaimTypes.NameIdentifier)!));}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("login");
        app.MapPost("/api/auth/wiederherstellen",async(EinrichtungEingabe e,HttpContext c,FirmenDbContext db,IPasswordHasher<Benutzerkonto> hasher)=>{
            if(!Erstzugang.SichererTransport(c))return Results.BadRequest(new ApiFehler("Bitte eine vertrauensw\u00fcrdige HTTPS-Verbindung verwenden."));
            try{await Erstzugang.Einloesen(db,hasher,e,true);return Results.Ok();}
            catch(Arbeitsfehler f){return Results.Json(new ApiFehler(f.Message),statusCode:f.Status);}
        }).RequireRateLimiting("login");
        admins.MapPost("/benutzer/{id}",async(string id,BenutzerAenderung e,HttpContext c,FirmenDbContext db)=>{
            await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var u=await db.Benutzer.SingleOrDefaultAsync(u=>u.Id==id);if(u is null)return Results.NotFound();
            if(u.Version!=e.Version)return Results.Conflict(new ApiFehler("Benutzer wurde zwischenzeitlich geändert."));
            if(e.Rolle is not ("Administrator" or "Mitarbeiter"))return Results.BadRequest();
            if(u.Eingerichtet&&u.Aktiv&&u.Rolle=="Administrator"&&(!e.Aktiv||e.Rolle!="Administrator")&&await db.Benutzer.CountAsync(x=>x.Aktiv&&x.Eingerichtet&&x.Rolle=="Administrator")<=1)return Results.Conflict(new ApiFehler("Mindestens ein aktiver Administrator muss erhalten bleiben."));
            if(!e.Aktiv){MfaDienst.VorbereitungenWiderrufen(u);u.EinladungHash=null;u.EinladungBis=null;u.WiederherstellungHash=null;u.WiederherstellungBis=null;}
            u.Aktiv=e.Aktiv;u.Rolle=e.Rolle;u.Sicherheitskennung=Guid.NewGuid().ToString("N");u.GeaendertAm=DateTimeOffset.UtcNow;u.Version++;
            db.Sicherheitsereignisse.Add(new(){BenutzerId=c.User.FindFirstValue(ClaimTypes.NameIdentifier)!,Aktion="Benutzerrechte geändert",Ressource=id});
            try{await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok();}catch(DbUpdateConcurrencyException){return Results.Conflict();}
        });
        admins.MapPost("/benutzer/{id}/freigaben",async(string id,Freigabeeingabe e,HttpContext c,FirmenDbContext db)=>{
            if(!await db.Benutzer.AnyAsync(u=>u.Id==id))return Results.NotFound();
            var valid=e.Art switch{"Firma"=>await db.Kontakte.AnyAsync(x=>x.Id==e.Id),"Akte"=>await db.Akten.AnyAsync(x=>x.Id==e.Id),"Dokument"=>await db.Dokumente.AnyAsync(x=>x.Id==e.Id),_=>false};if(!valid)return Results.BadRequest(new ApiFehler("Ungültige Ressource."));
            var alt=await db.Freigaben.Where(g=>g.BenutzerId==id&&(e.Art=="Firma"?g.KontaktId==e.Id:e.Art=="Akte"?g.AkteId==e.Id:g.DokumentId==e.Id)).SingleOrDefaultAsync();
            if(e.Erlaubt&&alt is null)db.Freigaben.Add(new(){BenutzerId=id,KontaktId=e.Art=="Firma"?e.Id:null,AkteId=e.Art=="Akte"?e.Id:null,DokumentId=e.Art=="Dokument"?e.Id:null});else if(!e.Erlaubt&&alt is not null)db.Freigaben.Remove(alt);
            db.Sicherheitsereignisse.Add(new(){BenutzerId=c.User.FindFirstValue(ClaimTypes.NameIdentifier)!,Aktion=e.Erlaubt?"Ressource freigegeben":"Freigabe entzogen",Ressource=e.Art+":"+e.Id});await db.SaveChangesAsync();return Results.Ok();
        });
    }
}
