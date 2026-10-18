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
        Assert.Equal("DEMO-ISD", daten.Rechnungen[0].EmpfaengerId);
        Assert.Equal("DEMO-ISD", daten.Rechnungen[1].AusstellerId);
        Assert.Equal(2, daten.Rechnungen[1].Zahlungen.Count);
        Assert.Equal("1.280,00 €", daten.Rechnungen[0].BestaetigtZugeordnet);
        Assert.Equal("500,00 €", daten.Rechnungen[1].BestaetigtZugeordnet);
        Assert.Equal("0,00 €", daten.Rechnungen[2].BestaetigtZugeordnet);
        Assert.Equal("Zur Prüfung", daten.Rechnungen[2].Zahlungen.Single().Status);
        Assert.Equal("0,00 €", daten.Rechnungen[2].Zahlungen.Single().Zugeordnet);
    }

    [Fact]
    public void DemoKontakteFolgenUnternehmenskennungUndVorgangsrolle()
    {
        var daten = DemoDaten.UebersichtErstellen();
        var ausgang = daten.Rechnungen[0];
        var eingang = daten.Rechnungen[1];
        Assert.Equal("buero@fjordfalter.invalid", ausgang.AusstellerKontakt!.Email);
        Assert.Equal("+49 30 23125 001", ausgang.AusstellerKontakt.Telefon);
        Assert.Equal("Lina Fennbrück", ausgang.AusstellerKontakt.Ansprechpartner);
        Assert.Equal(ausgang.AusstellerKontakt, eingang.EmpfaengerKontakt);
        Assert.Equal(ausgang.EmpfaengerKontakt, eingang.AusstellerKontakt);
        Assert.Equal("kontakt@elbpfad.invalid", eingang.AusstellerKontakt!.Email);
        Assert.Null(eingang.AusstellerKontakt.Telefon);
        Assert.Null(eingang.AusstellerKontakt.Ansprechpartner);
        Assert.Null(daten.Rechnungen[2].EmpfaengerKontakt);
        Assert.All(daten.Rechnungen.SelectMany(r => new[] { r.AusstellerKontakt, r.EmpfaengerKontakt }).OfType<Kontaktansicht>(),
            k => Assert.StartsWith("synthetisch/Kontakt-DEMO-", k.Quelle));
        Assert.Equal("910,00 €", daten.OffenerBetrag);
    }

    [Fact]
    public void RechnungsansichtOhneKontakteBleibtKonstruierbarUndSerialisierbar()
    {
        var r = new Rechnungsansicht("SYNTH", "2026-10-17", "A", "B", "100,00 €", "100,00 €", "0,00 €", "Offen", "SYNTH-BELEG", "synthetisch/Test", []);
        Assert.Null(r.AusstellerKontakt);
        Assert.Null(r.EmpfaengerKontakt);
        var gelesen = JsonSerializer.Deserialize<Rechnungsansicht>(JsonSerializer.Serialize(r))!;
        Assert.Equal(r.Nummer, gelesen.Nummer);
        Assert.Null(gelesen.AusstellerKontakt);
        Assert.Empty(gelesen.Zahlungen);
    }

    [Fact]
    public void DokumenteSindEchteUnveraenderlichePdfsMitPassenderZuordnung()
    {
        var daten = DemoDaten.UebersichtErstellen();
        var r = daten.Rechnungen[1];
        Assert.Equal("demo-nord-rechnung-002", r.VorgangId);
        Assert.Equal("Empfänger", r.Perspektive!.Rolle);
        var dokumente = DemoDokumente.Liste(r.VorgangId!)!;
        Assert.Equal(2, dokumente.Dokumente.Count);
        Assert.Single(dokumente.Dokumente, d => d.Art == "Originalrechnung" && d.ZahlungId is null);
        var zahlung = Assert.Single(dokumente.Dokumente, d => d.Art == "Zahlungsbeleg");
        Assert.Equal(r.Zahlungen[0].Id, zahlung.ZahlungId);
        Assert.Empty(DemoDokumente.Liste(daten.Rechnungen[0].VorgangId!)!.Dokumente);
        foreach (var d in dokumente.Dokumente)
        {
            var bytes = DemoDokumente.Datei(r.VorgangId!, d.Id)!;
            Assert.Equal(d.Bytezahl, bytes.Length);
            Assert.Equal(d.DateiHash, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
            var pdf = System.Text.Encoding.ASCII.GetString(bytes);
            Assert.StartsWith("%PDF-1.4", pdf);
            Assert.EndsWith("%%EOF\n", pdf);
            Assert.DoesNotContain("SYNTHETISCH", pdf);
            Assert.Contains("RE-2026-1048", pdf);
            Assert.DoesNotContain("/JavaScript", pdf);
            Assert.DoesNotContain("/URI", pdf);
            var xref = int.Parse(pdf.Split("startxref\n")[1].Split('\n')[0], System.Globalization.CultureInfo.InvariantCulture);
            Assert.StartsWith("xref", pdf[xref..]);
            bytes[0] = 0;
            Assert.Equal((byte)'%', DemoDokumente.Datei(r.VorgangId!, d.Id)![0]);
            Assert.Null(DemoDokumente.Datei("demo-nord-rechnung-001", d.Id));
        }
        Assert.Null(DemoDokumente.Datei("../", "../../etc/passwd"));
        Assert.Null(DemoDokumente.Liste("fremdes-unternehmen"));
        Assert.Equal("910,00 €", DemoDaten.UebersichtErstellen().OffenerBetrag);
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
            var rechnungen = json.GetProperty("rechnungen");
            Assert.Equal("buero@fjordfalter.invalid", rechnungen[0].GetProperty("ausstellerKontakt").GetProperty("email").GetString());
            Assert.Equal("Kontaktakte", rechnungen[1].GetProperty("ausstellerKontakt").GetProperty("quelle").GetString());
            Assert.Equal(JsonValueKind.Null, rechnungen[2].GetProperty("empfaengerKontakt").ValueKind);
            var liste = await client.GetFromJsonAsync<JsonElement>("/api/vorgaenge/demo-nord-rechnung-002/dokumente");
            Assert.Equal(2, liste.GetProperty("dokumente").GetArrayLength());
            var id = liste.GetProperty("dokumente")[0].GetProperty("id").GetString();
            using var pdf = await client.GetAsync($"/api/vorgaenge/demo-nord-rechnung-002/dokumente/{id}");
            Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
            Assert.Equal("inline", pdf.Content.Headers.ContentDisposition!.DispositionType);
            Assert.Equal("nosniff", pdf.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Contains("frame-ancestors 'self'", pdf.Headers.GetValues("Content-Security-Policy").Single());
            Assert.Contains("object-src 'none'", pdf.Headers.GetValues("Content-Security-Policy").Single());
            var pdfBytes = await pdf.Content.ReadAsByteArrayAsync();
            Assert.Equal(liste.GetProperty("dokumente")[0].GetProperty("dateiHash").GetString(), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pdfBytes)));
            foreach (var pfad in new[] { $"/api/vorgaenge/demo-nord-rechnung-001/dokumente/{id}", "/api/vorgaenge/fremd/dokumente", "/api/vorgaenge/demo-nord-rechnung-002/dokumente/unbekannt", "/api/vorgaenge/demo-nord-rechnung-002/dokumente/%2e%2e%2fgeheim" })
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(pfad)).StatusCode);
            Assert.False((await client.PostAsync($"/api/vorgaenge/demo-nord-rechnung-002/dokumente/{id}",new StringContent("SYNTH"))).IsSuccessStatusCode);
            using var fehlend = await client.GetAsync("/api/vorgaenge/demo-nord-rechnung-002/dokumente/fehlt");
            Assert.Equal("text/html", fehlend.Content.Headers.ContentType!.MediaType);
            Assert.Contains("PDF nicht verfügbar", await fehlend.Content.ReadAsStringAsync());
            Assert.DoesNotContain("C:\\", await fehlend.Content.ReadAsStringAsync());
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
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/vorgaenge/demo-nord-rechnung-002/dokumente/{id}")).StatusCode);
            client.DefaultRequestHeaders.Remove("Origin");
            client.DefaultRequestHeaders.Host = "fremder-host.invalid";
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/uebersicht")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/vorgaenge/demo-nord-rechnung-002/dokumente/{id}")).StatusCode);
        }
        finally { await app.StopAsync(); }
    }
}
