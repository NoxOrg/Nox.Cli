using Azure.Identity;
using Nox.Cli.Commands;
using Nox.Core.Constants;
using RestSharp;
using Spectre.Console.Cli;
using System.IdentityModel.Tokens.Jwt;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json;
using Nox.Cli.Helpers;
using Spectre.Console;

namespace Nox.Cli;

internal static class ConfiguratorExtensions
{
    public static IConfigurator<CommandSettings> AddNoxCommands(this IConfigurator<CommandSettings> cfg)
    {
        var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nox");
        Directory.CreateDirectory(cachePath);
        var cacheFile = Path.Combine(cachePath, "NoxCliCache.json");

        NoxCliCache? cache = GetOrCreateCache(cacheFile);

        Dictionary<string,string> yamlWorkflows = new(StringComparer.OrdinalIgnoreCase);

        GetLocalWorkflows(yamlWorkflows);

        if (cache != null)
        {
            GetOnlineWorkflows(yamlWorkflows, cache.Tid, cachePath);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var (_,yaml) in yamlWorkflows)
        {
            var cmdConfig = deserializer.Deserialize<NoxWorkflowConfiguration>(yaml).Cli;

            var cmdConfigContinuation = cfg.AddCommand<DynamicCommand>(cmdConfig.Command)
                .WithData(yaml)
                .WithAlias(cmdConfig.CommandAlias)
                .WithDescription(cmdConfig.Description);

            foreach (var example in cmdConfig.Examples)
            {
                cmdConfigContinuation = cmdConfigContinuation.WithExample(example.ToArray());
            };
        }

        return cfg;
    }

    private static NoxCliCache? GetOrCreateCache(string cacheFile)
    {
        NoxCliCache? cache;
        if (File.Exists(cacheFile))
        {
            cache = JsonSerializer.Deserialize<NoxCliCache>(File.ReadAllText(cacheFile))!;
        }
        else
        {
            cache = GetCacheInfoFromAzureToken();
            File.WriteAllText(cacheFile, JsonSerializer.Serialize(cache));
        }
        return cache;
    }

    private static void GetLocalWorkflows(Dictionary<string, string> yamlWorkflows)
    {
        var files = FindWorkflows();

        foreach (var file in files)
        {
            var yaml = File.ReadAllText(file);
            yamlWorkflows[(new FileInfo(file)).Name] = yaml;
        }
    }

    private static void GetOnlineWorkflows(Dictionary<string, string> yamlWorkflows, string tid, string cachePath)
    {
        var client = new RestClient($"https://noxorg.dev/workflows/{tid}/");
        var request = new RestRequest() { Method = Method.Get };
        request.AddHeader("Accept", "application/json");

        // Get list of files on server
        var onlineFilesJson = client.Execute(request);

        if (onlineFilesJson.Content == null) return;

        var onlineFiles = JsonSerializer.Deserialize<string[]>(onlineFilesJson.Content);

        // Read and cache the entries

        var workflowCachePath = Path.Combine(cachePath, "workflows");
        Directory.CreateDirectory(workflowCachePath);

        var existingCacheList = Directory
            .GetFiles(workflowCachePath,FileExtension.WorflowDefinition)
            .Select(f => (new FileInfo(f)).Name).ToHashSet();

        foreach (var file in onlineFiles!)
        {
            request.Resource = file;

            var yaml = client.Execute(request).Content;

            if (yaml != null)
            {
                if (yamlWorkflows[file] != null)
                {
                    AnsiConsole.MarkupLine($"[bold olive]WARNING: {$"Local workflow {file} has been replaced by the online version. Local workflows are only applied if they do not exist in the online repository.".EscapeMarkup()}[/]");
                }
                yamlWorkflows[file] = yaml;
                File.WriteAllText(Path.Combine(workflowCachePath, file), yaml);
                if (existingCacheList.Contains(file))
                {
                    existingCacheList.Remove(file);
                }
            }
        }

        foreach (var orphanEntry in existingCacheList)
        {
            File.Delete(Path.Combine(workflowCachePath, orphanEntry));
        }

    }

    private static NoxCliCache? GetCacheInfoFromAzureToken()
    {
        var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
        
        if(credential == null) return null;

        string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

        var task = credential.GetTokenAsync(new Azure.Core.TokenRequestContext(scopes));
        while (!task.IsCompleted)
        {
            Task.Delay(50).Wait();
        }
        var token = task.Result;

        var handler = new JwtSecurityTokenHandler();

        if (handler.ReadToken(token.Token) is not JwtSecurityToken jsonToken) return null;

        var upn = jsonToken.Claims.FirstOrDefault(c => c.Type == "upn");
        if(upn == null) return null;

        var tid = jsonToken.Claims.FirstOrDefault(c => c.Type == "tid");
        if(tid == null) return null;

        return new NoxCliCache { Upn = upn.Value, Tid = tid.Value };
    }

    private static string[] FindWorkflows()
    {
        var path = new DirectoryInfo(Directory.GetCurrentDirectory());

        var files = path.GetFiles(FileExtension.WorflowDefinition);

        var lastChance = false;

        while (!files.Any())
        {
            // root
            if (path!.Parent == null)
            {
                files = Array.Empty<FileInfo>();
                break;
            }

            // Find special NOX folder
            if (Directory.GetDirectories(path.FullName, @".nox").Any())
            {
                if (lastChance) break;
                lastChance = true;
                path = new DirectoryInfo(Path.Combine(path.FullName,".nox"));
                files = path!.GetFiles(FileExtension.WorflowDefinition, SearchOption.AllDirectories);
                continue;
            }

            // Stop in project root, after checking all sub-directories
            if (Directory.GetDirectories(path.FullName, @".git").Any())
            {
                if (lastChance) break;
                lastChance = true;
                files = path!.GetFiles(FileExtension.WorflowDefinition, SearchOption.AllDirectories);
                continue;
            }

            path = path!.Parent;

            files = path!.GetFiles(FileExtension.WorflowDefinition, SearchOption.TopDirectoryOnly);
        }

        return files.Select(f => f.FullName).ToArray();
    }
}

public class NoxWorkflowConfiguration
{
    public NoxCliConfiguration Cli { get; set; } = null!;
}

public class NoxCliConfiguration
{
    public string Branch { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string CommandAlias { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<List<string>> Examples { get; set; } = null!;
}

public class NoxCliCache
{
    public string Upn { get; set; } = null!;
    public string Tid { get; set; } = null!;
}