using Betriebsplattform.Web.Akten;
using Xunit;
using Xunit.Abstractions;
namespace Betriebsplattform.Rechnungen.Tests;
public sealed class WindowsScannerTests(ITestOutputHelper output)
{
    [Fact]public async Task DirekterScanWirdOhneGeraetefreigabeNichtSimuliert(){IScannerAdapter scanner=new WindowsScanner();var e=await Assert.ThrowsAsync<Arbeitsfehler>(()=>scanner.ScannenAsync("nicht-freigegeben",default));Assert.Equal(503,e.Status);}
    [Theory][InlineData("C:\\", "eingang")][InlineData("\\\\nicht-aufrufen\\freigabe", "eingang")][InlineData("relativ", "eingang")][InlineData("C:\\Scanordner", "../fremd")]
    public async Task ScanquelleVerweigertUngeeigneteWurzelnVorDateizugriff(string pfad,string quelle){var source=new PersistenterScanordner(new(quelle,pfad,"synthetisch"));await Assert.ThrowsAsync<Arbeitsfehler>(()=>source.Pruefen(null!,null!,null!,null!,null!,TimeProvider.System));}
    private sealed class WiaFact:FactAttribute{public WiaFact(){if(Environment.GetEnvironmentVariable("FZ_WIA_TEST")!="1")Skip="Explicit read-only local WIA inventory required.";}}
    [WiaFact]public async Task EchteLokaleWiaGeraeteabfrageOhneScan(){var inventory=await new WindowsScanner().Geraete(default);Assert.True(inventory.Unterstuetzt);Assert.True(inventory.Geraete.Length<=20);output.WriteLine("Real WIA inventory: "+inventory.Geraete.Length+" scanner devices; no scan executed.");}
}
