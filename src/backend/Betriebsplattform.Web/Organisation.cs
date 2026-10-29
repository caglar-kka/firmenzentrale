namespace Betriebsplattform.Web;

// One organization per existing host/database. No cross-company selection or public upload.
public static class Organisation
{
    public static object Lesen(IConfiguration config)
    {
        var name=config["FirmenZentrale:Organisation:Name"]?.Trim()??"";
        if(name.Length>80||name.Any(char.IsControl))throw new InvalidOperationException("Ungültiger Organisationsname.");
        return new{name,logoStatus="Nicht eingerichtet"};
    }
}
