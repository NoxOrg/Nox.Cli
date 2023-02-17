using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using RestSharp;

namespace Nox.Cli.Variables;

public static class RunnerVariableResolver
{
    public static void ResolveRunnerVariables(this IDictionary<string, object?> variables)
    {
        var runnerKeys = variables.Select(kv => kv.Key)
            .Where(e => e.StartsWith("runner.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[7..])
            .ToArray();

        var curPath = Assembly.GetExecutingAssembly().Location;
        var hostName = Dns.GetHostName();
        var ipEntry = Dns.GetHostEntry(hostName);
        var ipAddrList = ipEntry.AddressList;
        
        foreach (var runnerKey in runnerKeys)
        {
            switch (runnerKey)
            {
                case "current":
                    if (curPath != null) variables[$"runner.{runnerKey}"] = Path.GetDirectoryName(curPath);
                    break;
                case "temp":
                    var tmpPath = Path.GetTempPath();
                    if (tmpPath != null) variables[$"runner.{runnerKey}"] = tmpPath;
                    break;
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
                                variables[$"runner.{runnerKey}"] = isOnline;
                                break;
                            case "ipv4":
                                variables[$"runner.{runnerKey}"] = isOnlineResponse.Content!.Trim();
                                break;
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                    break;
                case "arch":
                    var arch = RuntimeInformation.ProcessArchitecture;
                    variables[$"runner.{runnerKey}"] = Enum.GetName(arch);
                    break;
                case "os":
                    var os = RuntimeInformation.OSDescription;
                    variables[$"runner.{runnerKey}"] = os;
                    break;
                case "ismacos":
                    var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
                    variables[$"runner.{runnerKey}"] = isMac;
                    break;
                case "islinux":
                    var isProperOs = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                    variables[$"runner.{runnerKey}"] = isProperOs;
                    break;
                case "iswindows":
                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    variables[$"runner.{runnerKey}"] = isWindows;
                    break;
                case "availablememory":
                    GC.Collect();
                    var mem = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes - GC.GetGCMemoryInfo().MemoryLoadBytes;
                    
                    variables[$"runner.{runnerKey}"] = mem/1000000000.0;
                    break;
                case "availablediskspace":
                    var driveInfo = DriveInfo.GetDrives().SingleOrDefault(di => di.Name == Path.GetPathRoot(curPath));
                    if (driveInfo != null)
                    {
                        variables[$"runner.{runnerKey}"] = driveInfo.AvailableFreeSpace/1000000000.0;
                    }
                    break;
                case "debug":
                    break;
            }
        }
    }

}