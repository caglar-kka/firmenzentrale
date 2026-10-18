namespace Betriebsplattform.Web.Akten;

public sealed record Arbeitsumgebung(bool Beispielkennzeichnung = true, bool Diagnose = false);
public interface IArbeitsrechte { bool Darf(string aktion); }
// Feste lokale Arbeitsidentität, keine Anmeldung und keine Benutzerwahl im Browser.
public sealed class LokaleArbeitsrechte : IArbeitsrechte
{
    public bool Darf(string aktion) => aktion is "Kontakte.Verwalten" or "Kategorien.Verwalten" or "Diagnose.Lesen" or "Akten.Verwalten";
}
public static class Arbeitskontext { public const string Benutzer = "Lina Fennbrück"; }
