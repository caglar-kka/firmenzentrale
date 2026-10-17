using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Betriebsplattform.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Betriebsplattform.Rechnungen.Tests;

public sealed class WebPrototypTests
{
    [Fact]
    public void AnzeigeBerechnetSaldenImKernUndZeigtIsdMitVerschiedenenRollen()
    {
        var daten = DemoDaten.UebersichtErstellen();
        Assert.True(daten.Synthetisch);
        Assert.Equal("910,00 €", daten.OffenerBetrag);
        Assert.Equal("0,00 €", daten.Rechnungen[0].OffenerBetrag);
        Assert.Equal("460,00 €", daten.Rechnungen[1].OffenerBetrag);
        Assert.Equal("450,00 €", daten.Rechnungen[2].OffenerBetrag);
        Assert.Contains("ISD", daten.Rechnungen[0].Empfaenger);
        Assert.Contains("ISD", daten.Rechnungen[1].Aussteller);
        Assert.Equal(2, daten.Rechnungen[1].Zahlungen.Count);
        Assert.Equal("1.280,00 €", daten.Rechnungen[0].BestaetigtZugeordnet);
        Assert.Equal("500,00 €", daten.Rechnungen[1].BestaetigtZugeordnet);
        Assert.Equal("0,00 €", daten.Rechnungen[2].BestaetigtZugeordnet);
        Assert.Equal("Zur Prüfung", daten.Rechnungen[2].Zahlungen.Single().Status);
        Assert.Equal("0,00 €", daten.Rechnungen[2].Zahlungen.Single().Zugeordnet);
    }

    [Theory]
    [InlineData("gueltig", false, 2)]
    [InlineData("fehler", true, 3)]
    [InlineData("dublette", true, 3)]
    public void DemoVerwendetTatsaechlicheXlsxImportlogik(string fall, bool fehler, int zeilen)
    {
        var ansicht = DemoDaten.Importvorschau(fall);
        Assert.Equal(fehler, ansicht.HatFehler);
        Assert.Equal(zeilen, ansicht.Zeilenanzahl);
        Assert.Equal(64, ansicht.DateiHash!.Length);
        Assert.All(ansicht.Zeilen.SelectMany(z => z.Zellen), z => Assert.Equal(ansicht.DateiHash, z.Quelle.DateiHash));
        if (fall == "dublette") Assert.Equal(2, ansicht.Fehlerzeilen);
        Assert.Equal("100,25 €", ansicht.Zeilen[0].Betrag);
    }

    [Fact]
    public async Task LokalerHttpVertragUndSicherheitsgrenzenWerdenTatsaechlichGeprueft()
    {
        await using var app = ApiAnwendung.Erstellen(["--urls", "http://0.0.0.0:9"], 0, Path.Combine(Path.GetTempPath(), "SYNTH-NICHT-VORHANDEN"));
        await app.StartAsync();
        try
        {
            var adresse = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            Assert.StartsWith("http://127.0.0.1:", adresse);
            using var client = new HttpClient { BaseAddress = new Uri(adresse), Timeout = TimeSpan.FromSeconds(10) };
            using var antwort = await client.GetAsync("/api/uebersicht");
            Assert.Equal(HttpStatusCode.OK, antwort.StatusCode);
            Assert.Contains("no-store", antwort.Headers.CacheControl!.ToString());
            Assert.Contains("frame-ancestors 'none'", antwort.Headers.GetValues("Content-Security-Policy").Single());
            var json = await antwort.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("910,00 €", json.GetProperty("offenerBetrag").GetString());
            var vorschau = await client.GetFromJsonAsync<JsonElement>("/api/importvorschau/fehler");
            Assert.True(vorschau.GetProperty("hatFehler").GetBoolean());
            Assert.Equal("Warnung", vorschau.GetProperty("meldungen")[0].GetProperty("stufe").GetString());
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/importvorschau/unbekannt")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/fehlt")).StatusCode);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/")).StatusCode);
            using var schreiben = await client.PostAsync("/api/uebersicht", new StringContent("SYNTH"));
            Assert.False(schreiben.IsSuccessStatusCode);
            client.DefaultRequestHeaders.Add("Origin", "https://example.invalid");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/uebersicht")).StatusCode);
            client.DefaultRequestHeaders.Remove("Origin");
            client.DefaultRequestHeaders.Host = "fremder-host.invalid";
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/uebersicht")).StatusCode);
        }
        finally { await app.StopAsync(); }
    }
}
