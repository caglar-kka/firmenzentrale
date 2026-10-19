namespace Betriebsplattform.Web.Akten;

public sealed record Arbeitsumgebung(bool Beispielkennzeichnung = true, bool Diagnose = false);
public interface IArbeitsrechte { bool Darf(string aktion); }
// Feste lokale Arbeitsidentität, keine Anmeldung und keine Benutzerwahl im Browser.
public sealed class LokaleArbeitsrechte : IArbeitsrechte
{
    public bool Darf(string aktion) => aktion is "Kontakte.Verwalten" or "Kategorien.Verwalten" or "Diagnose.Lesen" or "Akten.Verwalten";
}
public static class Arbeitskontext
{
    private static readonly AsyncLocal<string?> Actor=new();
    public static string Benutzer=>Actor.Value??"Lina Fennbrück";
    public static IDisposable Beginnen(string actor){var vorher=Actor.Value;Actor.Value=actor;return new Ende(()=>Actor.Value=vorher);}
    private sealed class Ende(Action ende):IDisposable{public void Dispose()=>ende();}
}
