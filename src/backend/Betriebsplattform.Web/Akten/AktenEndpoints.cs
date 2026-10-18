using System.Text;

namespace Betriebsplattform.Web.Akten;

public static class AktenEndpoints
{
    public static void Registrieren(WebApplication app,Aktenbestand bestand,Func<Uebersicht> rechnungen,Arbeitsmodule module,ILesezugriff rechte,ScanOrdnerAdapter? scanner,Arbeitsumgebung umgebung,IArbeitsrechte arbeitsrechte)
    {
        bool Darf(string aktion)=>arbeitsrechte.Darf(aktion);
        app.MapGet("/api/arbeitsumgebung",()=>new{beispielkennzeichnung=umgebung.Beispielkennzeichnung,diagnose=umgebung.Diagnose,benutzer=Arbeitskontext.Benutzer,kategorienVerwalten=Darf("Kategorien.Verwalten"),kontakteVerwalten=Darf("Kontakte.Verwalten"),aktenVerwalten=Darf("Akten.Verwalten")});
        var suche=new AktenSuche(bestand,rechnungen,module,rechte);
        IResult Aktion(Func<object> aktion,string? art=null,string? id=null){
            if(art is not null&&id is not null&&!rechte.Erlaubt(art,id))return Results.StatusCode(403);
            try{
                if(art=="Vorgang"&&id is not null&&!module.Aktiv("Finanzen")&&bestand.Lesen(id).Vorgang.Rechnungsnummer is not null)return Results.NotFound();
                return Results.Ok(aktion());
            }catch(Arbeitsfehler e){return Results.Json(new {meldung=e.Message,hinweise=e.Hinweise.Where(h=>rechte.Erlaubt(h.Art=="Akte"?"Vorgang":"Firma",h.Id)&&(h.Art!="Akte"||module.Aktiv("Finanzen")||bestand.Lesen(h.Id).Vorgang.Rechnungsnummer is null)).ToArray()},statusCode:e.Status);}
        }
        void Aktualisieren(){foreach(var r in rechnungen().Rechnungen)bestand.Registrieren(r);}
        app.MapGet("/api/module",()=>new{module=module.Liste,umgebung="Arbeitsumgebung",persistenz="Arbeitsspeicher"});
        app.MapGet("/api/suche",(string? text)=>{Aktualisieren();return Aktion(()=>suche.Finden(text));});
        app.MapGet("/api/akten",()=>{Aktualisieren();return Aktion(()=>bestand.Vorgaenge().Where(v=>rechte.Erlaubt("Vorgang",v.Id)&&(v.Rechnungsnummer is null||module.Aktiv("Finanzen"))).ToArray());});
        app.MapGet("/api/akten-status",()=>Vorgangsbearbeitung.Werte);
        app.MapPost("/api/akten",(Vorgangseingabe e)=>Darf("Akten.Verwalten")?Aktion(()=>bestand.VorgangAnlegen(e)):Results.StatusCode(403));
        app.MapPost("/api/akten/{id}/bearbeitungsstatus",(string id,Bearbeitungsentscheidung e)=>Darf("Akten.Verwalten")?Aktion(()=>bestand.BearbeitungsstatusSetzen(id,e),"Vorgang",id):Results.StatusCode(403));
        app.MapGet("/api/akten/{id}",(string id)=>{Aktualisieren();return Aktion(()=>{
            var a=bestand.Lesen(id);
            return a with {Dokumente=module.Aktiv("Dokumente")?a.Dokumente.Where(d=>rechte.Erlaubt("Dokument",d.Id)).ToArray():[],
                Korrespondenz=module.Aktiv("Kommunikation")?a.Korrespondenz:[],Kontaktvorschlaege=a.Kontaktvorschlaege.Where(v=>rechte.Erlaubt("Firma",v.FirmaId)&&rechte.Erlaubt("Dokument",v.DokumentId)).ToArray()};
        },"Vorgang",id);});
        app.MapPost("/api/akten/{akte}/notizen",(string akte,Notizeingabe e)=>Aktion(()=>bestand.NotizSpeichern(akte,null,e),"Vorgang",akte));
        app.MapPost("/api/akten/{akte}/notizen/{id}",(string akte,string id,Notizeingabe e)=>Aktion(()=>bestand.NotizSpeichern(akte,id,e),"Vorgang",akte));
        app.MapPost("/api/akten/{akte}/notizen/{id}/entfernen",(string akte,string id,Versionsentscheidung e)=>Aktion(()=>bestand.NotizEntfernen(akte,id,e.Version),"Vorgang",akte));
        app.MapPost("/api/akten/{akte}/notizen/{id}/wiederherstellen",(string akte,string id,Versionsentscheidung e)=>Aktion(()=>bestand.NotizEntfernen(akte,id,e.Version,true),"Vorgang",akte));
        app.MapPost("/api/akten/{akte}/korrespondenz",(string akte,Korrespondenzeingabe e)=>module.Aktiv("Kommunikation")?Aktion(()=>bestand.KorrespondenzSpeichern(akte,e),"Vorgang",akte):Results.NotFound());
        app.MapGet("/api/posteingang",()=>Aktion(()=>new{dokumente=bestand.Eingang(true).Where(d=>d.Aktivitaeten?.Any(a=>a.Kanal is not null)==true&&rechte.Erlaubt("Dokument",d.Id)).OrderByDescending(d=>d.LetzterEingangAm).ToArray(),scanner=scanner?.Status??new Scanordnerstatus(false,"Direkte Scannersteuerung vorbereitet; Scan-Ordner nicht aktiviert",0,0,0)}));
        app.MapPost("/api/posteingang/{id}/zuordnen",(string id,Zuordnung e)=>Aktion(()=>{
            if(e.VorgangId is not null&&!rechte.Erlaubt("Vorgang",e.VorgangId))throw new Arbeitsfehler("Kein Zugriff auf diesen Vorgang.",403);
            return bestand.Zuordnen(id,e);
        },"Dokument",id));
        app.MapGet("/api/kategorien",()=>bestand.Kategorien.Liste());
        app.MapPost("/api/kategorien",(Kategorieeingabe e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Speichern(null,e)):Results.StatusCode(403));
        app.MapPost("/api/kategorien/{id}",(string id,Kategorieeingabe e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Speichern(id,e)):Results.StatusCode(403));
        app.MapPost("/api/kategorien/{id}/archivieren",(string id,Versionsentscheidung e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Archivieren(id,e.Version)):Results.StatusCode(403));
        app.MapPost("/api/kategorien/{id}/verschieben",(string id,Kategorieverschiebung e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Verschieben(id,e)):Results.StatusCode(403));
        app.MapPost("/api/kontakte",(Kontakteingabe e)=>Darf("Kontakte.Verwalten")?Aktion(()=>bestand.KontaktSpeichern(null,e)):Results.StatusCode(403));
        app.MapPost("/api/kontakte/{id}",(string id,Kontakteingabe e)=>Darf("Kontakte.Verwalten")?Aktion(()=>bestand.KontaktSpeichern(id,e),"Firma",id):Results.StatusCode(403));
        app.MapPost("/api/kontakte/{id}/archivieren",(string id,Versionsentscheidung e)=>Darf("Kontakte.Verwalten")?Aktion(()=>bestand.KontaktArchivieren(id,e.Version),"Firma",id):Results.StatusCode(403));
        app.MapGet("/api/kontakte",()=>Aktion(()=>bestand.Kontakte().Where(k=>rechte.Erlaubt("Firma",k.Id)).ToArray()));
        app.MapPost("/api/kontaktvorschlaege/{id}",(string id,Kontaktfreigabe e)=>!Darf("Kontakte.Verwalten")?Results.StatusCode(403):Aktion(()=>{
            var v=bestand.Vorgaenge().SelectMany(v=>bestand.Lesen(v.Id).Kontaktvorschlaege).FirstOrDefault(v=>v.Id==id)??throw new Arbeitsfehler("Vorschlag nicht gefunden.",404);
            if(!rechte.Erlaubt("Firma",v.FirmaId)||!rechte.Erlaubt("Dokument",v.DokumentId))throw new Arbeitsfehler("Kein Zugriff auf diesen Kontaktvorschlag.",403);
            return bestand.KontaktEntscheiden(id,e);
        }));
        app.MapGet("/api/dokumente/{id}/technik",(string id)=>umgebung.Diagnose&&Darf("Diagnose.Lesen")?Aktion(()=>bestand.Technik(id),"Dokument",id):Results.NotFound());
        app.MapGet("/api/dokumente/{id}/text",(string id)=>Aktion(()=>new{text=bestand.Text(id)},"Dokument",id));
        app.MapGet("/api/dokumente/{id}/datei",(string id,HttpContext c)=>{
            if(!rechte.Erlaubt("Dokument",id))return Results.StatusCode(403);
            try{
                var d=bestand.Dokument(id);var bytes=bestand.Bytes(id);var inline=d.Vorschau is "pdf" or "bild" or "text";
                c.Response.Headers.ContentSecurityPolicy=inline?"default-src 'none'; sandbox; frame-ancestors 'self'; object-src 'none'":"default-src 'none'; sandbox; frame-ancestors 'none'";
                c.Response.Headers.ContentDisposition=$"{(inline?"inline":"attachment")}; filename*=UTF-8''{Uri.EscapeDataString(d.Dateiname)}";
                return Results.Bytes(bytes,inline?d.Inhaltstyp:"application/octet-stream");
            }catch(Arbeitsfehler e){return Results.Json(new {meldung=e.Message,hinweise=e.Hinweise.Where(h=>rechte.Erlaubt(h.Art=="Akte"?"Vorgang":"Firma",h.Id)).ToArray()},statusCode:e.Status);}
        });
        app.MapPost("/api/dokumenteingang",async(HttpContext c)=>{
            if(c.Request.ContentType!="application/octet-stream")return Results.BadRequest(new ApiFehler("Datei bitte über die Dateiauswahl oder die Dropfläche hinzufügen."));
            try{
                var name=Uri.UnescapeDataString(c.Request.Headers["X-Dateiname"].ToString());
                var mime=c.Request.Headers["X-Dateityp"].ToString();var akte=c.Request.Query["akte"].FirstOrDefault();
                if(akte is not null&&!rechte.Erlaubt("Vorgang",akte))return Results.StatusCode(403);
                Aktualisieren();using var bytes=new MemoryStream();var puffer=new byte[8192];int gelesen;
                while((gelesen=await c.Request.Body.ReadAsync(puffer,c.RequestAborted))>0){if(bytes.Length+gelesen>Dokumentpruefung.MaxBytes)return Results.Json(new ApiFehler("Die Datei überschreitet 20 MB."),statusCode:413);bytes.Write(puffer,0,gelesen);}
                var kanal=c.Request.Headers["X-Eingangsweg"]=="Drag & Drop"?"Drag & Drop":"Dateiauswahl";
                return Aktion(()=>bestand.Hinzufuegen(bytes.ToArray(),name,mime,akte,kanal));
            }catch(BadHttpRequestException){return Results.Json(new ApiFehler("Die Datei überschreitet 20 MB."),statusCode:413);}
            catch(UriFormatException){return Results.BadRequest(new ApiFehler("Ungültiger Dateiname."));}
        });
    }
}
