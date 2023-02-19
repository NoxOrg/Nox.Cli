using System.Net;
using System.Net.NetworkInformation;
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
            .Where(kv => kv.Value == null)
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("runner.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[7..])
            .ToArray();

        foreach (var key in keys)
        {
            var value = ResolveRunnerValue(key);
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
                    var value = ResolveRunnerValue(key);
                    if (value != null) item.Value = value;
                }    
            }
        }    
    }

    private static object? ResolveRunnerValue(string runnerKey)
    {
        return runnerKey.ToLower() switch
        {
            "cli" => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location!),
            "current" => Directory.GetCurrentDirectory(),
            "temp" => Path.GetTempPath(),
            "home" => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "machine" => Environment.MachineName,
            "host" => Dns.GetHostName(),
            "isonline" => IsOnline(),
            "ipv4" => PublicIPv4(),
            "arch" => Enum.GetName(RuntimeInformation.ProcessArchitecture),
            "os" => RuntimeInformation.OSDescription,
            "ismacos" => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            "islinux" => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "iswindows" => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "totalmemory" => TotalMemory(),
            "availablememory" => AvailableMemory(),
            "totaldiskspace" => TotalDiskspace(),
            "availablediskspace" => AvailableDiskspace(),
            "debug" => false,
            _ => null
        };
    }

    private const string IP_HOST_NAME = "icanhazip.com";

    private static bool IsOnline()
    {
        try
        {
            return (new Ping()).Send(IP_HOST_NAME, 3000).Status == IPStatus.Success;
        }
        catch { }
        return false;
    }

    private static string? PublicIPv4()
    {
        try
        {
            using var client = new HttpClient();
            return client.GetStringAsync($"http://{IP_HOST_NAME}").Result.TrimEnd();
        }
        catch { }
        return null;
    }

    private static double AvailableMemory()
    {
        GC.Collect();
        return (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes - GC.GetGCMemoryInfo().MemoryLoadBytes) / 1_000_000_000.0;
    }

    private static double AvailableDiskspace()
    {
        var currentDrive = Path.GetPathRoot(Directory.GetCurrentDirectory());
        var driveInfo = DriveInfo.GetDrives().SingleOrDefault(di => di.Name.Equals(currentDrive));
        return (driveInfo?.AvailableFreeSpace ?? 0) / 1_000_000_000.0;
    }

    private static double TotalMemory()
    {
        GC.Collect();
        return (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes) / 1_000_000_000.0;
    }

    private static double TotalDiskspace()
    {
        var currentDrive = Path.GetPathRoot(Directory.GetCurrentDirectory());
        var driveInfo = DriveInfo.GetDrives().SingleOrDefault(di => di.Name.Equals(currentDrive));
        return (driveInfo?.TotalSize ?? 0) / 1_000_000_000.0;
    }
}