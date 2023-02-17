using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Nox.Cli.Server.Abstractions;
using RestSharp;

namespace Nox.Cli.Variables;

public static class RunnerVariableResolver
{
    private static readonly Regex RunnerVariableRegex = new(@"\$\{\{\s*runner\.(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public static void ResolveRunnerVariables(this IDictionary<string, object?> variables)
    {
        var keys = variables
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("runner.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[7..])
            .ToArray();

        foreach (var key in keys)
        {
            var value = ResolveValue(key);
            if (value != null) variables[$"runner.{key}"] = value;
        }
    }

    public static void ResolveRunnerVariables(this List<ServerVariable> variables)
    {
        foreach (var item in variables)
        {
            if (item.Value != null)
            {
                var match = RunnerVariableRegex.Match(item.Value.ToString()!);
                if (match.Success)
                {
                    var key = match.Groups["variable"].Value;
                    var value = ResolveValue(key);
                    if (value != null) item.Value = value;
                }    
            }
        }
        
    }

    private static object? ResolveValue(string runnerKey)
    {
        var curPath = Assembly.GetExecutingAssembly().Location;
        var hostName = Dns.GetHostName();
        var ipEntry = Dns.GetHostEntry(hostName);

        switch (runnerKey)
        {
            case "current":
                if (curPath != null) return Path.GetDirectoryName(curPath);
                break;
            case "temp":
                var tmpPath = Path.GetTempPath();
                return tmpPath;
            case "isonline":
            case "ipv4":
                var isOnline = false;
                try
                {
                    var isOnlineClient = new RestClient($"https://icanhazip.com/");
                    var isOnlineRequest = new RestRequest
                    {
                        Method = Method.Get,
                        Timeout = 1000
                    };
                    var isOnlineResponse = isOnlineClient.Execute(isOnlineRequest);
                    isOnline = isOnlineResponse.StatusCode == HttpStatusCode.OK;
                    switch (runnerKey)
                    {
                        case "isonline":
                            return isOnline;
                        case "ipv4":
                            return isOnlineResponse.Content!.Trim();
                    }
                }
                catch
                {
                    // ignore
                }

                break;
            case "arch":
                var arch = RuntimeInformation.ProcessArchitecture;
                return Enum.GetName(arch);
                break;
            case "os":
                var os = RuntimeInformation.OSDescription;
                return os;
            case "ismacos":
                var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
                return isMac;
            case "islinux":
                var isProperOs = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                return isProperOs;
            case "iswindows":
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                return isWindows;
            case "availablememory":
                GC.Collect();
                var mem = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes - GC.GetGCMemoryInfo().MemoryLoadBytes;
                return mem / 1000000000.0;
            case "availablediskspace":
                var driveInfo = DriveInfo.GetDrives().SingleOrDefault(di => di.Name == Path.GetPathRoot(curPath));
                if (driveInfo != null)
                {
                    return driveInfo.AvailableFreeSpace / 1000000000.0;
                }

                break;
            case "debug":
                break;
        }

        return null;
    }
}