using System.Security.Claims;
using Betriebsplattform.Web.Akten;
using Microsoft.EntityFrameworkCore;
namespace Betriebsplattform.Web.Aufgaben;

public static class Kalender
{
    public static (DateOnly Von,DateOnly Bis) Zeitraum(string? von,string? bis)
    {
        if(!DateOnly.TryParseExact(von,"yyyy-MM-dd",out var v)||!DateOnly.TryParseExact(bis,"yyyy-MM-dd",out var b)||b<=v||b.DayNumber-v.DayNumber>42)throw new Arbeitsfehler("Bitte einen Zeitraum von höchstens 42 Tagen wählen.");
        return(v,b);
    }
    public static async Task<object> Lesen(string actor,string? von,string? bis,AufgabenDienst d,CancellationToken ct)
    {
        var(v,b)=Zeitraum(von,bis);var q=(await d.Sichtbar(actor)).Where(a=>a.ZustaendigId==actor&&!a.Archiviert&&a.FaelligAm>=v&&a.FaelligAm<b&&a.Status!="Abgebrochen");
        var anzahl=await q.CountAsync(ct);var rows=await q.OrderBy(a=>a.FaelligAm).ThenBy(a=>a.Id).Take(200).Select(a=>new{a.Id,a.Betreff,a.FaelligAm,a.FaelligUm,a.Status,a.Prioritaet}).ToArrayAsync(ct);
        return new{eintraege=rows,anzahl,zeitzone=d.Zeitzone,art="Aufgabenfristen"};
    }
    public static void Endpunkte(WebApplication app)=>app.MapGet("/api/kalender",async(HttpContext c,AufgabenDienst d)=>{
        try{return Results.Json(await Lesen(c.User.FindFirstValue(ClaimTypes.NameIdentifier)!,c.Request.Query["von"],c.Request.Query["bis"],d,c.RequestAborted));}
        catch(Arbeitsfehler e){return Results.Json(new ApiFehler(e.Message),statusCode:e.Status);}
    }).RequireAuthorization();
}
