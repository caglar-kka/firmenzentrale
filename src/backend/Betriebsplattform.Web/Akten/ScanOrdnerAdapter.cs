namespace Betriebsplattform.Web.Akten;

public sealed record Scanordnerstatus(bool Aktiv,string Status,int Uebernommen,int Fehler,int Wartend);

// Kein Treiberzugriff und keine Löschung. Alle Kanäle verwenden Aktenbestand.Hinzufuegen.
public sealed class ScanOrdnerAdapter(string ordner,Aktenbestand bestand)
{
    private readonly string basis=Path.GetFullPath(ordner);
    private readonly Dictionary<string,(long Laenge,DateTime Zeit,bool Fertig)> beobachtet=new(StringComparer.OrdinalIgnoreCase);
    private int uebernommen,fehler,wartend;
    private string meldung="Scan-Ordner aktiv · nur freigegebene lokale Testdateien";
    public Scanordnerstatus Status=>new(true,meldung,uebernommen,fehler,wartend);
    public void Pruefen()
    {
        try { PruefenIntern(); }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or Arbeitsfehler)
        { meldung="Scan-Ordner nicht lesbar oder nicht zulässig. Dateien bleiben erhalten; Konfiguration prüfen."; }
    }
    private void PruefenIntern()
    {
        if(!Directory.Exists(basis))return;
        if((File.GetAttributes(basis)&FileAttributes.ReparsePoint)!=0)throw new Arbeitsfehler("Der Scanordner darf kein verknüpftes Verzeichnis sein.");
        meldung="Scan-Ordner aktiv · nur freigegebene lokale Testdateien";
        wartend=0;
        foreach(var pfad in Directory.EnumerateFiles(basis).Take(1000)){
            if(!Path.GetFullPath(pfad).StartsWith(basis+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))continue;
            try {
                var info=new FileInfo(pfad);if((info.Attributes&FileAttributes.ReparsePoint)!=0)continue;
                var stamp=(info.Length,info.LastWriteTimeUtc);
                if(beobachtet.TryGetValue(pfad,out var vorher)&&vorher.Laenge==stamp.Length&&vorher.Zeit==stamp.LastWriteTimeUtc){
                    if(vorher.Fertig)continue;
                    if(info.Length is 0 or >Dokumentpruefung.MaxBytes){beobachtet[pfad]=(stamp.Length,stamp.LastWriteTimeUtc,true);fehler++;continue;}
                    using var stream=new FileStream(pfad,FileMode.Open,FileAccess.Read,FileShare.Read);
                    var bytes=new byte[checked((int)info.Length)];stream.ReadExactly(bytes);info.Refresh();
                    if(info.Length!=stamp.Length||info.LastWriteTimeUtc!=stamp.LastWriteTimeUtc){beobachtet.Remove(pfad);continue;}
                    bestand.Hinzufuegen(bytes,info.Name,"",null,"Scan-Ordner");
                    beobachtet[pfad]=(stamp.Length,stamp.LastWriteTimeUtc,true);uebernommen++;
                }else{beobachtet[pfad]=(stamp.Length,stamp.LastWriteTimeUtc,false);wartend++;}
            }catch(Arbeitsfehler){beobachtet.Remove(pfad);fehler++;var i=new FileInfo(pfad);if(i.Exists)beobachtet[pfad]=(i.Length,i.LastWriteTimeUtc,true);}
            catch(IOException){wartend++;}catch(UnauthorizedAccessException){wartend++;}
        }
        foreach(var alt in beobachtet.Keys.Where(p=>!File.Exists(p)).ToArray())beobachtet.Remove(alt);
    }
}

internal sealed class ScanOrdnerDienst(ScanOrdnerAdapter adapter):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(2));
        try { do {adapter.Pruefen();}while(await timer.WaitForNextTickAsync(stoppingToken)); }
        catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){}
    }
}
