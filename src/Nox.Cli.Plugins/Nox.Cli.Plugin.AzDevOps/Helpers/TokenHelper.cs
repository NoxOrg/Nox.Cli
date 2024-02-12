namespace Nox.Cli.Plugin.AzDevOps.Helpers;

public static class TokenHelper
{
    public static string ToEncoded(this string pat)
    {
        return Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}"));
    }
}