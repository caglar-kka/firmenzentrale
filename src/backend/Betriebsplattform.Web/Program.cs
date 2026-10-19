using Betriebsplattform.Web;

if(args.Contains("--apply-migrations"))
{
    Environment.ExitCode=await Betriebsplattform.Web.Persistenz.Persistenzverwaltung.MigrationAnwenden(args);
    return;
}
await using var app = ApiAnwendung.Erstellen(args);
await app.RunAsync();
