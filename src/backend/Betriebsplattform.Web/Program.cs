using Betriebsplattform.Web;

if(args.Contains("--ops")){Environment.ExitCode=await Betriebsplattform.Web.Betrieb.Betriebsverwaltung.Ausfuehren(args);return;}
if(args.Contains("--apply-migrations"))
{
    Environment.ExitCode=await Betriebsplattform.Web.Persistenz.Persistenzverwaltung.MigrationAnwenden(args);
    return;
}
try{await using var app = ApiAnwendung.Erstellen(args);await app.RunAsync();}
catch(Exception){Console.Error.WriteLine("Anwendung nicht gestartet oder angehalten. Lokale Konfiguration, Datenbank, Speicher und Sicherheitsvoraussetzungen prüfen. Kein Ersatzbetrieb.");Environment.ExitCode=1;}
