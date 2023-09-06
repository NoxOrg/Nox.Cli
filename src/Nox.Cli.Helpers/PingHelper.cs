using System.Net.Sockets;

namespace Nox.Cli.Helpers;

public static class PingHelper
{
    public static bool ServicePing(string host)
    {
        var tcpClient = new TcpClient();
        try
        {
            tcpClient.Connect(host, 80);
            return true;
        }
        catch
        {
            return false;
        }
    }
}