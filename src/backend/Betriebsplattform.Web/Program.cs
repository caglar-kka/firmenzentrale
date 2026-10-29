using Betriebsplattform.Web;

if(args.Length==1&&args[0]=="--admin-abnahme-einrichten"){Environment.ExitCode=await Betriebsplattform.Web.Betrieb.LokalerAdminErstzugang.AbnahmeEinrichten();return;}
if(args.Length==1&&args[0] is "--admin-einrichten" or "--admin-status"){Environment.ExitCode=await Betriebsplattform.Web.Betrieb.LokalerAdminErstzugang.Konsole(args[0]=="--admin-status");return;}
if(args.Contains("--ops")){Environment.ExitCode=await Betriebsplattform.Web.Betrieb.Betriebsverwaltung.Ausfuehren(args);return;}
if(args.Contains("--apply-migrations"))
{
    Environment.ExitCode=await Betriebsplattform.Web.Persistenz.Persistenzverwaltung.MigrationAnwenden(args);
    return;
}
if(args.Length==1&&args[0].StartsWith("--isoliert=",StringComparison.Ordinal)){
    try{await Betriebsplattform.Web.Betrieb.IsolierteArbeitsumgebung.Run(args[0]["--isoliert=".Length..]);}catch(Exception){Console.Error.WriteLine("Isolierte Anwendung nicht gestartet. Bestand, Schema, Zertifikat, Ablage und Port prüfen. Keine automatische Migration oder Vorschau.");Environment.ExitCode=1;}return;
}
try{await using var app = ApiAnwendung.Erstellen(Startmodus.Argumente(args));await app.RunAsync();}
catch(Exception){Console.Error.WriteLine("Anwendung nicht gestartet oder angehalten. Lokale Konfiguration, Datenbank, Speicher und Sicherheitsvoraussetzungen prüfen. Kein Ersatzbetrieb.");Environment.ExitCode=1;}
