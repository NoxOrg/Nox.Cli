namespace Nox.Cli.Services;

using System.Reflection;
using RestSharp;
using Spectre.Console;

public static class VersionChecker
{
    public static void CheckForLatestVersion()
    {
        try
        {
            var installedVersion = GetInstalledNoxCliVersion();

            var client = new RestClient("https://github.com/NoxOrg/Nox.Cli/releases/latest");
            var request = new RestRequest() { Method = Method.Get };
            request.AddHeader("Accept", "text/html");
            var todos = client.Execute(request);

            if (todos?.ResponseUri is null) return;

            var latestVersion = todos.ResponseUri.Segments.LastOrDefault();

            if (latestVersion is null) return;

            if (latestVersion.FirstOrDefault() == 'v')
                latestVersion = latestVersion[1..]; // remove the 'v' prefix. equivalent to `latest.Substring(1, latest.Length - 1)`

            if (installedVersion != latestVersion)
                AnsiConsole.MarkupLine(@$"{Environment.NewLine}[bold seagreen2]This Nox cli version '{installedVersion}' is older than that of the runtime '{latestVersion}'. Update the tools for the latest features and bug fixes (`dotnet tool update -g Nox.Cli`).[/]{Environment.NewLine}");
        }
        catch (Exception)
        {
            // fail silently
        }
    }

    public static string GetInstalledNoxCliVersion()
    {
        var installedVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        installedVersion = installedVersion[0..^2]; 

        return installedVersion;
    }
}
