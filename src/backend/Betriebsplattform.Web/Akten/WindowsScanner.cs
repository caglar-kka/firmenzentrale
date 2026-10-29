using System.Runtime.InteropServices;
using System.Runtime.Versioning;
namespace Betriebsplattform.Web.Akten;
public sealed record Scannergeraet(string Id,string Name);
public sealed record Scannerinventar(bool Unterstuetzt,string Status,Scannergeraet[] Geraete,bool Weitere=false);
// Device inventory is real WIA, never a simulated successful scan. No companion port or driver setup.
public sealed class WindowsScanner:IScannerAdapter
{
    private static readonly SemaphoreSlim Platz=new(1,1);
    public async Task<Scannerinventar> Geraete(CancellationToken ct){
        if(!OperatingSystem.IsWindows())return new(false,"WIA ist nur unter Windows verfügbar.",[]);
        if(!await Platz.WaitAsync(0,ct))throw new Arbeitsfehler("Scannerinventar wird bereits geprüft.",503);
        var done=new TaskCompletionSource<Scannerinventar>(TaskCreationOptions.RunContinuationsAsynchronously);
        var t=new Thread(()=>{try{if(OperatingSystem.IsWindows())done.TrySetResult(ListeWindows());else done.TrySetException(new Arbeitsfehler("Windows-WIA nicht verfügbar.",503));}catch(Exception){done.TrySetException(new Arbeitsfehler("Windows-Scannerinventar nicht verfügbar. Treiber und Geräte lokal prüfen.",503));}finally{Platz.Release();}}){IsBackground=true};
        t.SetApartmentState(ApartmentState.STA);t.Start();try{return await done.Task.WaitAsync(TimeSpan.FromSeconds(8),ct);}catch(TimeoutException){throw new Arbeitsfehler("Windows-Scannerinventar antwortet nicht rechtzeitig.",503);}
    }
    [SupportedOSPlatform("windows")]
    private static Scannerinventar ListeWindows(){
        var type=Type.GetTypeFromProgID("WIA.DeviceManager");if(type is null)return new(false,"Windows-WIA ist nicht registriert.",[]);object manager=Activator.CreateInstance(type)!;object? infos=null;
        try{
            infos=((dynamic)manager).DeviceInfos;var count=(int)((dynamic)infos).Count;var rows=new List<Scannergeraet>();
            for(var i=1;i<=Math.Min(count,20);i++){object info=((dynamic)infos)[i];try{if((int)((dynamic)info).Type!=1)continue;object props=((dynamic)info).Properties;try{object name=((dynamic)props)["Name"];try{var text=(string)((dynamic)name).Value;rows.Add(new((string)((dynamic)info).DeviceID,text[..Math.Min(text.Length,120)]));}finally{Marshal.FinalReleaseComObject(name);}}finally{Marshal.FinalReleaseComObject(props);}}finally{Marshal.FinalReleaseComObject(info);}}
            return new(true,rows.Count==0?"Kein WIA-Scanner gefunden. Direkter Hardwaretest nicht durchgeführt.":"WIA-Geräte erkannt. Direkter Scan benötigt eine gesondert geprüfte Geräteanbindung.",rows.ToArray(),count>20);
        }finally{if(infos is not null)Marshal.FinalReleaseComObject(infos);Marshal.FinalReleaseComObject(manager);}
    }
    public async Task<IReadOnlyList<string>> GeraeteAsync(CancellationToken ct)=>(await Geraete(ct)).Geraete.Select(g=>g.Id).ToArray();
    public Task<byte[]> ScannenAsync(string geraetId,CancellationToken ct){ct.ThrowIfCancellationRequested();return Task.FromException<byte[]>(new Arbeitsfehler("Direkter Scan ist ohne geprüfte Hardware und lokale Gerätefreigabe nicht eingerichtet.",503));}
}
public sealed record Scanwiederholung(int Version,bool Bestaetigt);
