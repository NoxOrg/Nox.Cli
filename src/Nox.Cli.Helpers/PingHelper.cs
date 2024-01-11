using System.Net.Sockets;

namespace Nox.Cli.Helpers;

public static class PingHelper
{
    public static bool ServicePing(string host)
    {
        using var tcpClient = new TcpClient();
        try
        {
            if (tcpClient.ConnectAsync(host, 80).Wait(2000))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}