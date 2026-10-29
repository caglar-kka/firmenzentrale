using System.Security.Claims;
using Betriebsplattform.Web.Akten;
using Betriebsplattform.Web.Persistenz;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Aufgaben;
public static class AufgabenEndpunkte
{
    public static void Registrieren(WebApplication app)
    {
        var g=app.MapGroup("/api/aufgaben").RequireAuthorization();
        g.AddEndpointFilter(async(c,next)=>{try{return await next(c);}catch(Arbeitsfehler e){return Results.Json(new ApiFehler(e.Message),statusCode:e.Status);}catch(DbUpdateConcurrencyException){return Results.Conflict(new ApiFehler("Der Eintrag wurde geändert. Bitte neu laden."));}});
        static string Actor(HttpContext c)=>c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        g.MapGet("/",async(HttpContext c,AufgabenDienst d,FirmenDbContext db)=>{
            var actor=Actor(c);var q=await d.Sichtbar(actor);var query=c.Request.Query;var text=query["text"].ToString().Trim().ToLowerInvariant();if(text.Length>120)throw new Arbeitsfehler("Bitte höchstens 120 Suchzeichen eingeben.");
            var art=query["bezugArt"].ToString();var bezug=query["bezugId"].ToString();if(art.Length>0||bezug.Length>0){if(art!="Akte"||!await d.RessourceErlaubt(actor,"Akte",bezug))throw new Arbeitsfehler("Akte nicht verfügbar.",404);q=q.Where(a=>a.AkteId==bezug||(a.DokumentId!=null&&db.Dokumentzuordnungen.Any(z=>z.DokumentId==a.DokumentId&&z.AkteId==bezug)));}
            var view=query["ansicht"].ToString();if(view is not ("" or "meine" or "alle" or "heute" or "morgen" or "zukuenftig" or "ueberfaellig" or "erledigt" or "archiv"))throw new Arbeitsfehler("Ansicht nicht verfügbar.");
            q=q.Where(a=>a.Archiviert==(view=="archiv"));if(view=="meine")q=q.Where(a=>a.ZustaendigId==actor);
            var heute=d.Heute;var morgen=heute.AddDays(1);var now=app.Services.GetRequiredService<TimeProvider>().GetUtcNow();
            if(view=="heute")q=q.Where(a=>a.FaelligAm==heute&&a.Status!="Erledigt"&&a.Status!="Abgebrochen");
            if(view=="morgen")q=q.Where(a=>a.FaelligAm==morgen&&a.Status!="Erledigt"&&a.Status!="Abgebrochen");
            if(view=="ueberfaellig")q=q.Where(a=>a.Status!="Erledigt"&&a.Status!="Abgebrochen"&&(a.FaelligAm<heute||a.FaelligAm==heute&&a.FaelligUtc<now));
            if(view=="zukuenftig")q=q.Where(a=>a.FaelligAm>heute&&a.Status!="Erledigt"&&a.Status!="Abgebrochen");
            if(view=="erledigt")q=q.Where(a=>a.Status=="Erledigt");
            if(text.Length>0)q=q.Where(a=>a.Betreff.ToLower().Contains(text)||a.Beschreibung.ToLower().Contains(text));
            var status=query["status"].ToString();if(status.Length>0){if(!AufgabenDienst.Statuswerte.Contains(status))throw new Arbeitsfehler("Status prüfen.");q=q.Where(a=>a.Status==status);}
            var prior=query["prioritaet"].ToString();if(prior.Length>0){if(!AufgabenDienst.Prioritaeten.Contains(prior))throw new Arbeitsfehler("Priorität prüfen.");q=q.Where(a=>a.Prioritaet==prior);}
            var user=query["zustaendig"].ToString();if(user.Length>0)q=q.Where(a=>a.ZustaendigId==user);
            var seite=int.TryParse(query["seite"],out var n)?FirmenDaten.Seite(n):0;
            var anzahl=await q.CountAsync();var sort=query["sort"].ToString();if(sort is not ("" or "faellig" or "neu"))throw new Arbeitsfehler("Sortierung prüfen.");
            var ordered=sort=="neu"?q.OrderByDescending(a=>a.GeaendertAm).ThenBy(a=>a.Id):q.OrderBy(a=>a.FaelligAm==null).ThenBy(a=>a.FaelligAm).ThenBy(a=>a.FaelligUm==null).ThenBy(a=>a.FaelligUm).ThenBy(a=>a.Id);
            var eintraege=await ordered.AsNoTracking().Skip(seite*20).Take(20).Select(a=>new{a.Id,a.Betreff,a.ZustaendigId,zustaendig=db.Benutzer.Where(u=>u.Id==a.ZustaendigId).Select(u=>u.Anzeigename).Single(),a.Status,a.Prioritaet,a.FaelligAm,a.FaelligUm,a.Version,a.Archiviert,ueberfaellig=!a.Archiviert&&a.Status!="Erledigt"&&a.Status!="Abgebrochen"&&(a.FaelligAm<heute||a.FaelligAm==heute&&a.FaelligUtc<now),heuteFaellig=a.FaelligAm==heute}).ToArrayAsync();return Results.Json(new{eintraege,anzahl,heute,zeitzone=d.Zeitzone});
        });
        g.MapGet("/mitarbeiter",async(HttpContext c,AufgabenDienst d)=>Results.Json(await d.Mitarbeiter(Actor(c),c.Request.Query["art"].FirstOrDefault(),c.Request.Query["bezug"].FirstOrDefault(),c.Request.Query["firma"].FirstOrDefault(),c.Request.Query["text"].ToString(),int.TryParse(c.Request.Query["seite"],out var n)?n:0)));
        g.MapGet("/zustaendige",async(HttpContext c,AufgabenDienst d,FirmenDbContext db)=>{var q=await d.Sichtbar(Actor(c));return Results.Json(await db.Benutzer.Where(u=>q.Any(a=>a.ZustaendigId==u.Id)).OrderBy(u=>u.Anzeigename).Select(u=>new{u.Id,u.Anzeigename}).ToArrayAsync());});
        g.MapGet("/meine-uebersicht",async(HttpContext c,AufgabenDienst d)=>{
            var actor=Actor(c);var q=(await d.Sichtbar(actor)).Where(a=>a.ZustaendigId==actor&&!a.Archiviert&&a.Status!="Erledigt"&&a.Status!="Abgebrochen");var heute=d.Heute;var now=app.Services.GetRequiredService<TimeProvider>().GetUtcNow();
            return Results.Json(new{offen=await q.CountAsync(),heute=await q.CountAsync(a=>a.FaelligAm==heute),ueberfaellig=await q.CountAsync(a=>a.FaelligAm<heute||a.FaelligAm==heute&&a.FaelligUtc<now)});
        });
        g.MapGet("/{id}",async(string id,HttpContext c,AufgabenDienst d)=>Results.Json(await d.Detail(Actor(c),id,int.TryParse(c.Request.Query["verlaufSeite"],out var n)?n:0)));
        g.MapPost("/",async(Aufgabeneingabe e,HttpContext c,AufgabenDienst d)=>Results.Json(await d.Speichern(Actor(c),null,e)));
        g.MapPost("/{id}",async(string id,Aufgabeneingabe e,HttpContext c,AufgabenDienst d)=>Results.Json(await d.Speichern(Actor(c),id,e)));
    }
}
