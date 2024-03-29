﻿namespace Nox.Cli.Services;

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

            var installedVersionNo = Convert.ToInt32(installedVersion.Replace(".", ""));
            var latestVersionNo = Convert.ToInt32(latestVersion.Replace(".", ""));
            
            if (installedVersionNo < latestVersionNo)
                AnsiConsole.MarkupLine(@$"{Environment.NewLine}[bold underline seagreen1]This version of NOX.Cli ({installedVersion}) is older than that of the latest version ({latestVersion}) Update the tools for the latest features and bug fixes (`dotnet tool update -g Nox.Cli`).[/]{Environment.NewLine}");
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
