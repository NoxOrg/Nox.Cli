using System.Net.NetworkInformation;

namespace Nox.Cli.Services;

public static class InternetChecker
{
    public static bool CheckForInternet()
    {
        string host = "noxorg.dev";
        var ping = new Ping();
        try
        {
            var reply = ping.Send(host, 3000);
            if (reply.Status == IPStatus.Success)
            {
                return true;
            }
        }
        catch { 
            // Ignore
        }
        return false;
    }
}
