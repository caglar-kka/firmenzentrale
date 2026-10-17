using Betriebsplattform.Web;

await using var app = ApiAnwendung.Erstellen(args);
await app.RunAsync();
