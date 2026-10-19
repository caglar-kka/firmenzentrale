using Betriebsplattform.Web.Persistenz;
using Betriebsplattform.Web.Sicherheit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Betriebsplattform.Web.Betrieb;

public static class Betriebsverwaltung
{
    public static async Task<int> Ausfuehren(string[] args)
    {
        try{
            var c=new ConfigurationBuilder().AddUserSecrets(typeof(Betriebsverwaltung).Assembly,optional:true).AddEnvironmentVariables().AddCommandLine(args).Build();
            var p=Persistenzkonfiguration.Lesen(c);if(p.Provider!="PostgreSql"||p.Dateiablage is null)throw new InvalidOperationException();
            await using var db=new FirmenDbContext(new DbContextOptionsBuilder<FirmenDbContext>().UseNpgsql(p.Verbindung!,o=>o.SetPostgresVersion(18,0)).EnableSensitiveDataLogging(false).Options);
            var tool=new Sicherung(c["FirmenZentrale:Operations:PostgresBin"]??"");
            switch(c["ops"]){
                case "inventory":var r=await Dokumentbestand.Pruefen(db,p.Dateiablage);Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(r));return r.Konsistent?0:2;
                case "backup":await tool.Erstellen(db,p.Verbindung!,p.Dateiablage,c["FirmenZentrale:Operations:BackupRoot"]??"");Console.WriteLine("Sicherung vollständig erstellt und geprüft.");return 0;
                case "restore":await tool.Wiederherstellen(c["FirmenZentrale:Operations:RestoreSource"]??"",p.Verbindung!,c["FirmenZentrale:Operations:RestoreDatabase"]??"",c["FirmenZentrale:Operations:RestoreRoot"]??"");Console.WriteLine("Wiederherstellung abgeschlossen und Dokumentintegrität geprüft.");return 0;
                case "bootstrap":
                    if(Console.IsInputRedirected)throw new InvalidOperationException();
                    Console.Write("Initialer Login: ");var login=Console.ReadLine();Console.Write("Anzeigename: ");var name=Console.ReadLine();Console.Write("Passwort (mindestens 14 Zeichen): ");
                    var chars=new List<char>();ConsoleKeyInfo key;while((key=Console.ReadKey(intercept:true)).Key!=ConsoleKey.Enter){if(key.Key==ConsoleKey.Backspace){if(chars.Count>0)chars.RemoveAt(chars.Count-1);}else if(!char.IsControl(key.KeyChar)&&chars.Count<128)chars.Add(key.KeyChar);}Console.WriteLine();
                    await using(var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)){await Anmeldung.Anlegen(db,Anmeldung.Passwortdienst(),new(login,name,new string(chars.ToArray()),"Administrator"),bootstrap:true);await tx.CommitAsync();}chars.Clear();Console.WriteLine("Initialer Administrator angelegt; kein Standardpasswort verwendet.");return 0;
                case "provision":
                    if(Console.IsInputRedirected)throw new InvalidOperationException();Console.Write("Neue Laufzeitrolle (fz_app_...): ");var rolle=Console.ReadLine()??"";Console.Write("Generiertes Hex-Kennwort (48 bis 128 Zeichen, verdeckt): ");
                    var pass=new List<char>();ConsoleKeyInfo taste;while((taste=Console.ReadKey(intercept:true)).Key!=ConsoleKey.Enter){if(taste.Key==ConsoleKey.Backspace){if(pass.Count>0)pass.RemoveAt(pass.Count-1);}else if(!char.IsControl(taste.KeyChar)&&pass.Count<128)pass.Add(taste.KeyChar);}Console.WriteLine();await Laufzeitrolle(p.Verbindung!,rolle,new string(pass.ToArray()));pass.Clear();Console.WriteLine("Eingeschränkte Laufzeitrolle angelegt. Zugang ausschließlich in sicherer lokaler Laufzeitkonfiguration hinterlegen.");return 0;
                case "runtime-grants":if(Console.IsInputRedirected)throw new InvalidOperationException();Console.Write("Vorhandene Laufzeitrolle: ");await BerechtigungenSetzen(p.Verbindung!,Console.ReadLine()??"");Console.WriteLine("Laufzeitrechte für das bewusst gewählte Schema gesetzt.");return 0;
                default:throw new InvalidOperationException();
            }
        }catch(Exception){Console.Error.WriteLine("Betriebsaktion nicht abgeschlossen. Konfiguration, gestoppte Anwendung, Version, Bestand und lokale Berechtigungen prüfen. Keine automatische Löschung oder Überschreibung.");return 1;}
    }
    // Explicit local administration only. The caller supplies an in-memory password, never CLI arguments.
    public static async Task Laufzeitrolle(string administration,string name,string passwort,CancellationToken ct=default)
    {
        if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"\Afz_app_[a-z0-9_]{1,32}\z")||!System.Text.RegularExpressions.Regex.IsMatch(passwort,@"\A[a-fA-F0-9]{48,128}\z"))throw new InvalidOperationException("Kontrollierten Rollennamen und starkes generiertes Kennwort verwenden.");
        await using var c=new NpgsqlConnection(administration);await c.OpenAsync(ct);var identifier=new NpgsqlCommandBuilder().QuoteIdentifier(name);
        // Only a validated generated hex secret is quoted here; arbitrary text is never interpolated.
        await using(var cmd=new NpgsqlCommand("CREATE ROLE "+identifier+" LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS NOINHERIT PASSWORD '"+passwort+"'",c))await cmd.ExecuteNonQueryAsync(ct);
        await BerechtigungenSetzen(administration,name,ct);
    }
    public static async Task BerechtigungenSetzen(string administration,string name,CancellationToken ct=default)
    {
        if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"\Afz_app_[a-z0-9_]{1,32}\z"))throw new InvalidOperationException();
        await using var c=new NpgsqlConnection(administration);await c.OpenAsync(ct);await using var tx=await c.BeginTransactionAsync(ct);var identifier=new NpgsqlCommandBuilder().QuoteIdentifier(name);var database=new NpgsqlCommandBuilder().QuoteIdentifier(c.Database);
        await using(var cmd=new NpgsqlCommand("REVOKE CREATE ON SCHEMA public FROM PUBLIC; GRANT CONNECT ON DATABASE "+database+" TO "+identifier+"; GRANT USAGE ON SCHEMA public TO "+identifier+"; GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO "+identifier+"; REVOKE DELETE ON ALL TABLES IN SCHEMA public FROM "+identifier+"; GRANT DELETE ON TABLE \"Ressourcenfreigaben\" TO "+identifier+"; REVOKE INSERT, UPDATE, DELETE ON TABLE \"__EFMigrationsHistory\" FROM "+identifier+"; REVOKE UPDATE, DELETE ON TABLE \"Sicherheitsereignisse\" FROM "+identifier+"; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "+identifier,c,tx))await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
}
