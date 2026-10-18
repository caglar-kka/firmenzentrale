using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Betriebsplattform.Web;
using Xunit;
using static Betriebsplattform.Rechnungen.Tests.SynthetischeArbeitsmappe;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class DemoImportTests
{
    private static readonly Dictionary<string,string> Mapping = new() { ["Firma A"]="DEMO-NORD", ["Firma B"]="DEMO-ISD" };
    private static byte[] Datei() {
        var fehler=Zeile(4,id:"SYNTH-FEHLER",betrag:"-5");
        fehler.Elements().Single(e=>(string?)e.Attribute("r")=="C4").ReplaceWith(Text("C4","SYNTH-FEHLER-RE"));
        return Bauen(Teile(Zeile(), Zeile(3, art:"Zahlung", id:"SYNTH-ZA-TEST"), fehler));
    }
    [Fact]
    public void UebernahmeBrauchtMappingUndAuswahlUndBleibtImWirklichenBestand()
    {
        var bestand=new DemoImportbestand();
        var v=bestand.Hochladen(Datei(),"synthetisch.xlsx");
        Assert.Throws<InvalidOperationException>(()=>bestand.Uebernehmen(v.Id,[2],true));
        v=bestand.Zuordnen(v.Id,new("ISD-Testdaten",Mapping,true));
        Assert.Equal("100,25 €",v.Vorschau.Zeilen[0].Betrag);
        Assert.Contains(v.Vorschau.Zeilen[1].Meldungen,m=>m.Code=="ZAHLUNG_UNBESTAETIGT");
        Assert.Throws<InvalidOperationException>(()=>bestand.Uebernehmen(v.Id,[3],true));
        Assert.Throws<InvalidOperationException>(()=>bestand.Uebernehmen(v.Id,[4],true));
        Assert.Throws<InvalidOperationException>(()=>bestand.Uebernehmen(v.Id,[2],false));
        var bericht=bestand.Uebernehmen(v.Id,[2],true);
        Assert.Equal(1,bericht.Uebernommen); Assert.Equal(1,bericht.Fehlerhaft); Assert.Equal(1,bericht.ZurPruefung);
        var r=Assert.Single(bestand.Uebersicht().Rechnungen,r=>r.Nummer=="SYNTH-RE-01");
        Assert.Equal("100,25 €",r.OffenerBetrag); Assert.Equal("0,00 €",r.BestaetigtZugeordnet);
        Assert.Empty(r.Zahlungen); Assert.Equal("Offen",r.Status);
        Assert.Contains("SHA256:",r.Quellenort);
        Assert.Equal("1.010,25 €",bestand.Uebersicht().OffenerBetrag);
        Assert.Equal("910,00 €",new DemoImportbestand().Uebersicht().OffenerBetrag);
        Assert.Equal("100.25",bestand.Lesen(v.Id).Vorschau.Zeilen[0].Zellen[6].Originalwert);
        Assert.NotNull(bestand.Lesen(v.Id).EntschiedenAm);
    }
    [Fact]
    public async Task WiederholungUndParalleleUebernahmeErzeugenKeineDoppeltenRechnungen()
    {
        var b=new DemoImportbestand(); var v=b.Hochladen(Datei(),"synthetisch.xlsx");
        Assert.Equal(v.Id,b.Hochladen(Datei(),"umbenannt.xlsx").Id);
        b.Zuordnen(v.Id,new("ISD-Testdaten",Mapping,true));
        await Task.WhenAll(Enumerable.Range(0,8).Select(_=>Task.Run(()=>b.Uebernehmen(v.Id,[2],true))));
        Assert.Equal(4,b.Uebersicht().Rechnungen.Count);
        var andere=Datei().Concat(new byte[]{0}).ToArray();
        var neu=b.Hochladen(andere,"andere.xlsx");
        neu=b.Zuordnen(neu.Id,new("ISD-Testdaten",Mapping,true));
        Assert.Contains(neu.Vorschau.Zeilen[0].Meldungen,m=>m.Code is "QUELL_DUBLETTE" or "RECHNUNGS_DUBLETTE");
        Assert.Throws<InvalidOperationException>(()=>b.Uebernehmen(neu.Id,[2],true));
    }
    [Fact]
    public void DateienMappingUndSitzungsgrenzenWerdenGeprueft()
    {
        var b=new DemoImportbestand();
        Assert.Throws<InvalidOperationException>(()=>b.Hochladen([1,2,3],"synthetisch.xlsx"));
        Assert.Throws<InvalidOperationException>(()=>b.Hochladen(Datei(),"synthetisch.xlsm"));
        Assert.Throws<InvalidOperationException>(()=>b.Hochladen(new byte[2*1024*1024+1],"synthetisch.xlsx"));
        var v=b.Hochladen(Datei(),"synthetisch.xlsx");
        Assert.Throws<InvalidOperationException>(()=>b.Zuordnen(v.Id,new("fremd",Mapping,true)));
        Assert.Throws<InvalidOperationException>(()=>b.Zuordnen(v.Id,new("ISD-Testdaten",new Dictionary<string,string>{{"Firma A","FREMD"}},true)));
        Assert.Throws<InvalidOperationException>(()=>b.Zuordnen(v.Id,new("ISD-Testdaten",Mapping,false)));
        Assert.Throws<KeyNotFoundException>(()=>b.Lesen("unbekannt"));
    }
    [Fact]
    public async Task HttpDateiMappingUebernahmeUndOriginGrenzenFunktionieren()
    {
        await using var app=ApiAnwendung.Erstellen([],0,Path.GetTempPath()); await app.StartAsync();
        try {
            using var client=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};
            using var unerlaubt=new HttpRequestMessage(HttpMethod.Post,"/api/importe/vorschau"){Content=new ByteArrayContent(Datei())};
            Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(unerlaubt)).StatusCode);
            client.DefaultRequestHeaders.Add("Origin",client.BaseAddress.GetLeftPart(UriPartial.Authority));
            client.DefaultRequestHeaders.Add("X-Demo-Import","1");
            client.DefaultRequestHeaders.Add("X-Dateiname","synthetisch.xlsx");
            using var upload=new ByteArrayContent(Datei()); upload.Headers.ContentType=new("application/octet-stream");
            var response=await client.PostAsync("/api/importe/vorschau",upload); response.EnsureSuccessStatusCode();
            var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
            (await client.PostAsJsonAsync($"/api/importe/{id}/mapping",new Mappingentscheidung("ISD-Testdaten",Mapping,true))).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/api/importe/{id}/uebernehmen",new Uebernahmeentscheidung([2],true))).EnsureSuccessStatusCode();
            var daten=await client.GetFromJsonAsync<Uebersicht>("/api/uebersicht");
            var r=Assert.Single(daten!.Rechnungen,r=>r.Nummer=="SYNTH-RE-01");
            Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/api/vorgaenge/{r.VorgangId}/dokumente")).StatusCode);
            var audit=await client.GetFromJsonAsync<JsonElement>($"/api/importe/{id}");
            Assert.True(audit.GetProperty("mappingBestaetigt").GetBoolean());
            Assert.Equal("Firma A",audit.GetProperty("vorschau").GetProperty("zeilen")[0].GetProperty("zellen")[3].GetProperty("text").GetString());
            client.DefaultRequestHeaders.Remove("Origin");client.DefaultRequestHeaders.Add("Origin","https://example.invalid");
            Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync($"/api/importe/{id}/uebernehmen",new Uebernahmeentscheidung([2],true))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/importe/{id}")).StatusCode);
        } finally { await app.StopAsync(); }
    }
    [Fact]
    public void ImportierteVerbindlichkeitBehaeltRichtungUndRechnungsbetrag()
    {
        var b=new DemoImportbestand();var z=Zeile();
        z.Elements().Single(e=>(string?)e.Attribute("r")=="D2").ReplaceWith(Text("D2","Firma B"));
        z.Elements().Single(e=>(string?)e.Attribute("r")=="E2").ReplaceWith(Text("E2","Firma A"));
        var bytes=Bauen(Teile(z));var v=b.Hochladen(bytes,"synthetisch.xlsx");
        Array.Clear(bytes); // Aufrufer darf gespeicherte Originalbytes nicht verändern.
        b.Zuordnen(v.Id,new("ISD-Testdaten",Mapping,true));b.Uebernehmen(v.Id,[2],true);
        var r=Assert.Single(b.Uebersicht().Rechnungen,r=>r.Nummer=="SYNTH-RE-01");
        Assert.Equal("Empfänger",r.Perspektive!.Rolle);Assert.Equal("Fjordfalter Objektpflege GmbH",r.Empfaenger);
        Assert.Equal("100,25 €",r.OffenerBetrag);Assert.Equal("0,00 €",r.BestaetigtZugeordnet);
    }
    [Fact]
    public void DemoSitzungIstBegrenztUndNeueUebernahmeIstAtomar()
    {
        var b=new DemoImportbestand();
        for(var i=0;i<20;i++)b.Hochladen(Datei().Concat(new byte[]{(byte)i}).ToArray(),"synthetisch.xlsx");
        Assert.Throws<InvalidOperationException>(()=>b.Hochladen(Datei().Concat(new byte[]{99}).ToArray(),"synthetisch.xlsx"));
        Assert.Equal(3,b.Uebersicht().Rechnungen.Count);
        var andere=new DemoImportbestand();var v=andere.Hochladen(Datei(),"synthetisch.xlsx");
        andere.Zuordnen(v.Id,new("ISD-Testdaten",Mapping,true));
        Assert.Throws<InvalidOperationException>(()=>andere.Uebernehmen(v.Id,[2,4],true));
        Assert.Equal(3,andere.Uebersicht().Rechnungen.Count);
        Assert.Null(andere.Lesen(v.Id).Ergebnis);
    }
    [Fact]
    public async Task HttpLehntUebergrosseDateienUndUngueltigeInhalteOhneBestandsaenderungAb()
    {
        await using var app=ApiAnwendung.Erstellen([],0,Path.GetTempPath());await app.StartAsync();
        try {
            using var c=new HttpClient{BaseAddress=new Uri(app.Urls.Single())};
            c.DefaultRequestHeaders.Add("Origin",c.BaseAddress.GetLeftPart(UriPartial.Authority));
            c.DefaultRequestHeaders.Add("X-Demo-Import","1");c.DefaultRequestHeaders.Add("X-Dateiname","synthetisch.xlsx");
            c.DefaultRequestHeaders.ExpectContinue=true;
            using var gross=new ByteArrayContent(new byte[2*1024*1024+1]);gross.Headers.ContentType=new("application/octet-stream");
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await c.PostAsync("/api/importe/vorschau",gross)).StatusCode);
            using var kaputt=new ByteArrayContent([1,2,3]);kaputt.Headers.ContentType=new("application/octet-stream");
            Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/api/importe/vorschau",kaputt)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/api/importe/vorschau",new StringContent("SYNTH"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/importe/unbekannt")).StatusCode);
            Assert.Empty((await c.GetFromJsonAsync<JsonElement[]>("/api/importe"))!);
            Assert.Equal(3,(await c.GetFromJsonAsync<Uebersicht>("/api/uebersicht"))!.Rechnungen.Count);
        } finally {await app.StopAsync();}
    }

}
