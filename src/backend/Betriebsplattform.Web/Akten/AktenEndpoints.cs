using System.Text;

namespace Betriebsplattform.Web.Akten;

public static class AktenEndpoints
{
    public static void Registrieren(WebApplication app,Aktenbestand bestand,Func<Uebersicht> rechnungen,Arbeitsmodule module,ILesezugriff rechte,ScanOrdnerAdapter? scanner,Arbeitsumgebung umgebung,IArbeitsrechte arbeitsrechte)
    {
        bool Darf(string aktion)=>arbeitsrechte.Darf(aktion);
        app.MapGet("/api/arbeitsumgebung",()=>new{beispielkennzeichnung=umgebung.Beispielkennzeichnung,diagnose=umgebung.Diagnose,benutzer=Arbeitskontext.Benutzer,kategorienVerwalten=Darf("Kategorien.Verwalten"),kontakteVerwalten=Darf("Kontakte.Verwalten"),aktenVerwalten=Darf("Akten.Verwalten")});
        void AktePruefen(string id){
            if(!module.Aktiv("Akten"))throw new Arbeitsfehler("Aktenbereich nicht verfügbar.",404);
            if(!rechte.Erlaubt("Vorgang",id))throw new Arbeitsfehler("Kein Zugriff auf diese Akte.",403);
            var v=bestand.Lesen(id).Vorgang;
            if(v.FirmaId is not null&&!rechte.Erlaubt("Firma",v.FirmaId))throw new Arbeitsfehler("Kein Zugriff auf diese Firma.",403);
            if(!module.Aktiv("Finanzen")&&v.Rechnungsnummer is not null)throw new Arbeitsfehler("Akte nicht verfügbar.",404);
        }
        var suche=new AktenSuche(bestand,rechnungen,module,rechte);
        IResult Aktion(Func<object> aktion,string? art=null,string? id=null){
            if(art is not null&&id is not null&&!rechte.Erlaubt(art,id))return Results.StatusCode(403);
            try{
                if(art=="Vorgang"&&id is not null)AktePruefen(id);
                return Results.Ok(aktion());
            }catch(Arbeitsfehler e){return Results.Json(new {meldung=e.Message,hinweise=e.Hinweise.Where(h=>rechte.Erlaubt(h.Art=="Akte"?"Vorgang":"Firma",h.Id)&&(h.Art!="Akte"||module.Aktiv("Finanzen")||bestand.Lesen(h.Id).Vorgang.Rechnungsnummer is null)).ToArray()},statusCode:e.Status);}
        }
        void Aktualisieren(){foreach(var r in rechnungen().Rechnungen)bestand.Registrieren(r);}
        app.MapGet("/api/module",()=>new{module=module.Liste,umgebung="Arbeitsumgebung",persistenz="Arbeitsspeicher"});
        app.MapGet("/api/suche",(string? text)=>{Aktualisieren();return Aktion(()=>suche.Finden(text));});
        app.MapGet("/api/akten",(HttpContext c)=>{Aktualisieren();return Aktion(()=>{
            var q=c.Request.Query;var daten=suche.FindeAkten(q["text"]).Where(v=>(q["status"].Count==0||v.Status==q["status"])&&(q["kategorie"].Count==0||v.KategorieId==q["kategorie"]));
            string Key(Vorgang v)=>q["sort"].ToString() switch{"titel"=>v.Titel,"kategorie"=>v.Kategorie,"status"=>v.Status,_=>v.LetztesEreignis?.ToString("O")??""};
            daten=q["ab"]=="1"?daten.OrderByDescending(Key).ThenBy(v=>v.Id):daten.OrderBy(Key).ThenBy(v=>v.Id);
            if(!q.ContainsKey("seite"))return (object)(q.ContainsKey("text")?daten.Take(100):daten).ToArray();
            if(!int.TryParse(q["seite"],out var seite)||seite is <0 or >100000)throw new Arbeitsfehler("Ungültige Ergebnisseite.");
            var a=daten.ToArray();return new{eintraege=a.Skip(seite*20).Take(20).ToArray(),anzahl=a.Length};
        });});
        app.MapGet("/api/akten-status",()=>Vorgangsbearbeitung.Werte);
        app.MapPost("/api/akten",(Vorgangseingabe e)=>Darf("Akten.Verwalten")&&(e.FirmaId is null||rechte.Erlaubt("Firma",e.FirmaId))?Aktion(()=>bestand.VorgangAnlegen(e)):Results.StatusCode(403));
        app.MapPost("/api/akten/{id}/bearbeitungsstatus",(string id,Bearbeitungsentscheidung e)=>Darf("Akten.Verwalten")?Aktion(()=>bestand.BearbeitungsstatusSetzen(id,e),"Vorgang",id):Results.StatusCode(403));
        app.MapGet("/api/akten/{id}",(string id)=>{Aktualisieren();return Aktion(()=>{
            var a=bestand.Lesen(id);
            var dokumente=module.Aktiv("Dokumente")?a.Dokumente.Where(d=>rechte.Erlaubt("Dokument",d.Id)).ToArray():[];
            return a with {Vorgang=a.Vorgang with{Dokumentanzahl=dokumente.Length},Dokumente=dokumente,
                Korrespondenz=module.Aktiv("Kommunikation")?a.Korrespondenz:[],Kontaktvorschlaege=a.Kontaktvorschlaege.Where(v=>rechte.Erlaubt("Firma",v.FirmaId)&&rechte.Erlaubt("Dokument",v.DokumentId)).ToArray()};
        },"Vorgang",id);});
        app.MapPost("/api/akten/{akte}/notizen",(string akte,Notizeingabe e)=>Darf("Akten.Verwalten")?Aktion(()=>bestand.NotizSpeichern(akte,null,e),"Vorgang",akte):Results.StatusCode(403));
        app.MapPost("/api/akten/{akte}/notizen/{id}",(string akte,string id,Notizeingabe e)=>Darf("Akten.Verwalten")?Aktion(()=>bestand.NotizSpeichern(akte,id,e),"Vorgang",akte):Results.StatusCode(403));
        app.MapPost("/api/akten/{akte}/notizen/{id}/entfernen",(string akte,string id,Versionsentscheidung e)=>Darf("Akten.Verwalten")?Aktion(()=>bestand.NotizEntfernen(akte,id,e.Version),"Vorgang",akte):Results.StatusCode(403));
        app.MapPost("/api/akten/{akte}/notizen/{id}/wiederherstellen",(string akte,string id,Versionsentscheidung e)=>Darf("Akten.Verwalten")?Aktion(()=>bestand.NotizEntfernen(akte,id,e.Version,true),"Vorgang",akte):Results.StatusCode(403));
        app.MapPost("/api/akten/{akte}/korrespondenz",(string akte,Korrespondenzeingabe e)=>!Darf("Akten.Verwalten")?Results.StatusCode(403):module.Aktiv("Kommunikation")?Aktion(()=>bestand.KorrespondenzSpeichern(akte,e),"Vorgang",akte):Results.NotFound());
        app.MapGet("/api/posteingang",()=>Aktion(()=>new{dokumente=bestand.Eingang(true).Where(d=>d.Aktivitaeten?.Any(a=>a.Kanal is not null)==true&&rechte.Erlaubt("Dokument",d.Id)).OrderByDescending(d=>d.LetzterEingangAm).ToArray(),scanner=scanner?.Status??new Scanordnerstatus(false,"Direkte Scannersteuerung vorbereitet; Scan-Ordner nicht aktiviert",0,0,0)}));
        app.MapPost("/api/posteingang/{id}/zuordnen",(string id,Zuordnung e)=>!Darf("Akten.Verwalten")?Results.StatusCode(403):Aktion(()=>{
            if(e.FirmaId is not null&&!rechte.Erlaubt("Firma",e.FirmaId))throw new Arbeitsfehler("Kein Zugriff auf diese Firma.",403);
            if(!module.Aktiv("Akten"))throw new Arbeitsfehler("Aktenbereich nicht verfügbar.",404);
            if(e.VorgangId is not null)AktePruefen(e.VorgangId);
            return bestand.Zuordnen(id,e);
        },"Dokument",id));
        app.MapPost("/api/dokumente/{id}/umordnen",(string id,Umzuordnung e)=>!Darf("Akten.Verwalten")?Results.StatusCode(403):Aktion(()=>{
            if(!module.Aktiv("Akten"))throw new Arbeitsfehler("Aktenbereich nicht verfügbar.",404);
            if(e.BisherigerVorgangId is null)throw new Arbeitsfehler("Die aktuelle Zuordnung fehlt.");
            AktePruefen(e.BisherigerVorgangId);
            if(e.NeuerVorgangId is not null)AktePruefen(e.NeuerVorgangId);
            if(e.FirmaId is not null&&!rechte.Erlaubt("Firma",e.FirmaId))throw new Arbeitsfehler("Kein Zugriff auf diese Firma.",403);
            return bestand.Umordnen(id,e);
        },"Dokument",id));
        app.MapGet("/api/kontakte/{id}/arbeitszahlen",(string id)=>Aktion(()=>{
            Aktualisieren();
            if(!bestand.Kontakte().Any(k=>k.Id==id))throw new Arbeitsfehler("Kontakt nicht gefunden.",404);
            var zahlen=new Dictionary<string,int>(bestand.Firmenzahlen(id,
                v=>module.Aktiv("Akten")&&rechte.Erlaubt("Vorgang",v.Id)&&(v.Rechnungsnummer is null||module.Aktiv("Finanzen")),
                d=>module.Aktiv("Dokumente")&&rechte.Erlaubt("Dokument",d)));
            if(!module.Aktiv("Kommunikation"))zahlen["korrespondenz"]=0;
            zahlen["rechnungen"]=module.Aktiv("Finanzen")?rechnungen().Rechnungen.Count(r=>(r.AusstellerId==id||r.EmpfaengerId==id)&&rechte.Erlaubt("Rechnung",r.VorgangId??r.Nummer)&&(r.VorgangId is null||rechte.Erlaubt("Vorgang",r.VorgangId))):0;
            return zahlen;
        },"Firma",id));
        app.MapGet("/api/kontakte/{id}/arbeit",(string id,HttpContext c)=>Aktion(()=>{
            var bereich=c.Request.Query["bereich"].ToString();if(!new[]{"akten","rechnungen","dokumente","notizen","korrespondenz"}.Contains(bereich))throw new Arbeitsfehler("Bitte einen Arbeitsbereich wählen.");
            if(!int.TryParse(c.Request.Query["seite"],out var seite)||seite is <0 or >100000)throw new Arbeitsfehler("Ungültige Ergebnisseite.");
            var kontakt=bestand.Kontakte().SingleOrDefault(k=>k.Id==id)??throw new Arbeitsfehler("Kontakt nicht gefunden.",404);
            var a=bestand.Vorgaenge().Where(v=>v.FirmaId==id&&rechte.Erlaubt("Vorgang",v.Id)&&(v.Rechnungsnummer is null||module.Aktiv("Finanzen"))).ToArray();
            var refs=new List<Suchtreffer>();
            if(bereich=="akten"&&module.Aktiv("Akten"))refs.AddRange(a.Select(v=>new Suchtreffer(v.Id,"Akte",v.Titel,v.Status,"vorgang",v.Id)));
            if(bereich=="rechnungen"&&module.Aktiv("Finanzen"))refs.AddRange(rechnungen().Rechnungen.Where(r=>(r.AusstellerId==id||r.EmpfaengerId==id)&&rechte.Erlaubt("Rechnung",r.VorgangId??r.Nummer)&&(r.VorgangId is null||rechte.Erlaubt("Vorgang",r.VorgangId))).Select(r=>new Suchtreffer(r.VorgangId??r.Nummer,"Rechnung",r.Nummer,(r.Perspektive?.Rolle switch{"Aussteller"=>"Ausgangsrechnung","Empfänger"=>"Eingangsrechnung",_=>"Rechnungsrichtung ungeklärt"})+" · "+r.Status+" · Noch offen: "+r.OffenerBetrag,"rechnung",r.VorgangId)));
            if(bereich!="akten"&&bereich!="rechnungen"&&module.Aktiv("Akten"))foreach(var v in a){var akte=bestand.Lesen(v.Id);
                if(bereich=="dokumente"&&module.Aktiv("Dokumente"))refs.AddRange(akte.Dokumente.Where(d=>rechte.Erlaubt("Dokument",d.Id)).Select(d=>new Suchtreffer(d.Id,"Dokument",d.Titel,v.Titel,"dokument",v.Id)));
                if(bereich=="notizen")refs.AddRange(akte.Notizen.Where(n=>!n.Entfernt).Select(n=>new Suchtreffer(n.Id,"Interne Notiz",n.Betreff,v.Titel,"vorgang",v.Id)));
                if(bereich=="korrespondenz"&&module.Aktiv("Kommunikation"))refs.AddRange(akte.Korrespondenz.Select(k=>new Suchtreffer(k.Id,k.Art,k.Betreff,v.Titel,"vorgang",v.Id)));
            }
            var result=refs.DistinctBy(t=>(t.Id,t.VorgangId)).OrderBy(t=>t.Titel).ToArray();return new{anzahl=result.Length,eintraege=result.Skip(seite*20).Take(20).ToArray()};
        },"Firma",id));
        app.MapGet("/api/kategorien",()=>bestand.Kategorien.Liste());
        app.MapPost("/api/kategorien",(Kategorieeingabe e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Speichern(null,e)):Results.StatusCode(403));
        app.MapPost("/api/kategorien/{id}",(string id,Kategorieeingabe e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Speichern(id,e)):Results.StatusCode(403));
        app.MapPost("/api/kategorien/{id}/archivieren",(string id,Versionsentscheidung e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Archivieren(id,e.Version)):Results.StatusCode(403));
        app.MapPost("/api/kategorien/{id}/verschieben",(string id,Kategorieverschiebung e)=>Darf("Kategorien.Verwalten")?Aktion(()=>bestand.Kategorien.Verschieben(id,e)):Results.StatusCode(403));
        app.MapPost("/api/kontakte",(Kontakteingabe e)=>Darf("Kontakte.Verwalten")&&(e.FirmaId is null||rechte.Erlaubt("Firma",e.FirmaId))?Aktion(()=>bestand.KontaktSpeichern(null,e)):Results.StatusCode(403));
        app.MapPost("/api/kontakte/{id}",(string id,Kontakteingabe e)=>Darf("Kontakte.Verwalten")&&(e.FirmaId is null||rechte.Erlaubt("Firma",e.FirmaId))?Aktion(()=>bestand.KontaktSpeichern(id,e),"Firma",id):Results.StatusCode(403));
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
            if(!Darf("Akten.Verwalten"))return Results.StatusCode(403);
            if(c.Request.ContentType!="application/octet-stream")return Results.BadRequest(new ApiFehler("Datei bitte über die Dateiauswahl oder die Dropfläche hinzufügen."));
            try{
                var name=Uri.UnescapeDataString(c.Request.Headers["X-Dateiname"].ToString());
                var mime=c.Request.Headers["X-Dateityp"].ToString();var akte=c.Request.Query["akte"].FirstOrDefault();
                if(akte is not null&&(!module.Aktiv("Akten")||!rechte.Erlaubt("Vorgang",akte)))return Results.StatusCode(403);
                Aktualisieren();if(akte is not null)AktePruefen(akte);
                using var bytes=new MemoryStream();var puffer=new byte[8192];int gelesen;
                while((gelesen=await c.Request.Body.ReadAsync(puffer,c.RequestAborted))>0){if(bytes.Length+gelesen>Dokumentpruefung.MaxBytes)return Results.Json(new ApiFehler("Die Datei überschreitet 20 MB."),statusCode:413);bytes.Write(puffer,0,gelesen);}
                var kanal=c.Request.Headers["X-Eingangsweg"]=="Drag & Drop"?"Drag & Drop":"Dateiauswahl";
                return Aktion(()=>bestand.Hinzufuegen(bytes.ToArray(),name,mime,akte,kanal));
            }catch(BadHttpRequestException){return Results.Json(new ApiFehler("Die Datei überschreitet 20 MB."),statusCode:413);}
            catch(Arbeitsfehler e){return Results.Json(new ApiFehler(e.Message),statusCode:e.Status);}
            catch(UriFormatException){return Results.BadRequest(new ApiFehler("Ungültiger Dateiname."));}
        });
    }
}
