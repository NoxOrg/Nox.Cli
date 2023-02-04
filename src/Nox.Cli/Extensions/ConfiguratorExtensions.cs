﻿using Azure.Identity;
using Nox.Cli.Commands;
using Nox.Core.Constants;
using RestSharp;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Nox.Cli.Services.Caching;
using Nox.Cli.Configuration;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Authentication;

namespace Nox.Cli;

internal static class ConfiguratorExtensions
{
    public static string CachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nox");
    public static string CacheFile => Path.Combine(CachePath, "NoxCliCache.json");
    public static string WorkflowsCachePath => Path.Combine(CachePath, "workflows");

    public static IConfigurator AddNoxCommands(this IConfigurator cliConfig, IServiceProvider serviceProvider)
    {
        var isServerDefined = false;
        var authenticator = serviceProvider.GetRequiredService<IAuthenticator>();
        
        var cachePath = CachePath;

        Directory.CreateDirectory(cachePath);

        var cacheFile = CacheFile;

        var cache = GetOrCreateCache(cacheFile, authenticator);

        Dictionary<string,string> yamlFiles = new(StringComparer.OrdinalIgnoreCase);

        if (cache != null)
        {
            GetOnlineWorkflows(yamlFiles, cache.Tid, cachePath, cache);
        }

        GetLocalWorkflows(yamlFiles);
        
#if DEBUG
        GetLocalWorkflows(yamlFiles, "../../tests/workflows");        
#endif

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithTypeMapping<IActionConfiguration, ActionConfiguration>()
            .WithTypeMapping<ICliConfiguration, CliConfiguration>()
            .WithTypeMapping<IStepConfiguration, StepConfiguration>()
            .Build();

        var manifest = yamlFiles
            .Where(kv => kv.Key.EndsWith(".cli.nox.yaml")) // TODO: define in nox.core constants
            .Select(kv => deserializer.Deserialize<ManifestConfiguration>(kv.Value))
            .FirstOrDefault();

        isServerDefined = manifest is { RemoteTaskProxy: { } };
        if (isServerDefined) authenticator.Configure(manifest!.RemoteTaskProxy!);
        
        var workflowsByBranch = yamlFiles
            .Where(kv => kv.Key.EndsWith(FileExtension.WorflowDefinition.TrimStart('*')))
            .Select(kv => deserializer.Deserialize<WorkflowConfiguration>(kv.Value))
            .OrderBy(w => w.Cli.Branch, StringComparer.OrdinalIgnoreCase)
            .ThenBy(w => w.Cli.Command)
            .GroupBy(w => w.Cli.Branch.ToLower());

        var branchDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (manifest?.Branches != null)
        {
            branchDescriptions = manifest.Branches.ToDictionary( m => m.Name, m => m.Description, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var branch in workflowsByBranch)
        {
            cliConfig.AddBranch(branch.Key, b =>
            {
                if (branchDescriptions.ContainsKey(branch.Key))
                {
                    b.SetDescription(branchDescriptions[branch.Key]);
                }

                foreach(var workflow in branch)
                {
                    var cmdConfigContinuation = b.AddCommand<DynamicCommand>(workflow.Cli.Command)
                        .WithData(workflow)
                        .WithAlias(workflow.Cli.CommandAlias ?? string.Empty)
                        .WithDescription(workflow.Cli.Description ?? string.Empty);
                        
                    foreach (var example in workflow.Cli.Examples!)
                    {
                        cmdConfigContinuation = cmdConfigContinuation.WithExample(example.ToArray());
                    };
                    if (isServerDefined) workflow.Cli.Server = manifest!.RemoteTaskProxy!;
                }

            });
        }

        return cliConfig;
    }

    private static NoxCliCache? GetOrCreateCache(string cacheFile, IAuthenticator authenticator)
    {
        NoxCliCache? cache;

        if (File.Exists(cacheFile))
        {
            cache = NoxCliCache.Load(cacheFile);

            var now = new DateTimeOffset(DateTime.UtcNow);

            if (cache.Expires > now)
            {
                return cache;
            }
        }

        cache = GetCacheInfoFromAzureToken(cacheFile, authenticator).Result;        

        return cache;
    }

    private static void GetLocalWorkflows(Dictionary<string, string> yamlFiles, string searchPath = "")
    {
        var files = FindWorkflows(searchPath);

        var overriddenFiles = new List<string>(files.Length);

        foreach (var file in files)
        {
            var yaml = File.ReadAllText(file);

            var fileInfo = new FileInfo(file);

            if (yamlFiles.ContainsKey(fileInfo.Name))
            {
                var overriddenPath = fileInfo.DirectoryName;
                var overriddenFile = fileInfo.Name;
                var pathMarkup = $"[underline]{overriddenPath.EscapeMarkup()}[/]";
                if (overriddenPath != null && !overriddenFiles.Contains(pathMarkup))
                {
                    overriddenFiles.Add(pathMarkup);
                }
                overriddenFiles.Add(overriddenFile[..^18].EscapeMarkup());
            }

            yamlFiles[fileInfo.Name] = yaml;
        }
        if (overriddenFiles.Count > 0)
        {
            AnsiConsole.MarkupLine($"[bold yellow]Warning: Local workflows {string.Join(',', overriddenFiles.Skip(1).ToArray())} in local folder {overriddenFiles.First()} overrides remote workflow(s) with the same name[/]");
        }

    }

    private static void GetOnlineWorkflows(Dictionary<string, string> yamlFiles, string tid, string cachePath, NoxCliCache cache)
    {
        var client = new RestClient($"https://noxorg.dev/workflows/{tid}/");

        var request = new RestRequest() { Method = Method.Get };

        request.AddHeader("Accept", "application/json");

        // Get list of files on server
        var onlineFilesJson = client.Execute(request);

        if (onlineFilesJson.Content == null) return;

        if (onlineFilesJson.ResponseStatus == ResponseStatus.Error)
        {
            throw new Exception($"GetOnlineWorkflows:-> {onlineFilesJson.ErrorException?.Message}");
        }
        
        var onlineFiles = JsonSerializer.Deserialize<List<RemoteFileInfo>>(onlineFilesJson.Content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Read and cache the entries

        var workflowCachePath = WorkflowsCachePath;

        Directory.CreateDirectory(workflowCachePath);

        var existingCacheList = Directory
            .GetFiles(workflowCachePath,FileExtension.WorflowDefinition)
            .Select(f => (new FileInfo(f)).Name).ToHashSet();

        foreach (var file in onlineFiles!)
        {
            string? yaml = null;

            if (cache.FileInfo == null
                || !cache.FileInfo.Any(i => i.Name == file.Name)
                || !cache.FileInfo.Any(i => i.Name == file.Name && i.ShaChecksum == file.ShaChecksum))
            { 

                request.Resource = file.Name;

                yaml = client.Execute(request).Content;

                if (yaml == null) throw new Exception($"Couldn't download workflow {file.Name}");

                File.WriteAllText(Path.Combine(workflowCachePath, file.Name), yaml);
            }
            else
            {
                yaml = File.ReadAllText(Path.Combine(workflowCachePath, file.Name));
            }

            yamlFiles[file.Name] = yaml;

            if (existingCacheList.Contains(file.Name))
            {
                existingCacheList.Remove(file.Name);
            }
        }

        foreach (var orphanEntry in existingCacheList)
        {
            File.Delete(Path.Combine(workflowCachePath, orphanEntry));
        }

        cache.FileInfo = onlineFiles;
        
        cache.Save();

    }

    private static async Task<NoxCliCache?> GetCacheInfoFromAzureToken(string cacheFile, IAuthenticator authenticator)
    {
        AnsiConsole.MarkupLine($"[bold mediumpurple3_1]Checking your credentials...[/]");

        var accessToken = await authenticator.SignIn();

        if (accessToken == null)
        {
            AnsiConsole.MarkupLine($"{Emoji.Known.ExclamationQuestionMark} Unable to login to Azure AD. Continuing without login.");
            return null;
        }
        
        if (accessToken!.UserPrincipalName == null)
        {
            AnsiConsole.MarkupLine($"{Emoji.Known.ExclamationQuestionMark} User principal name (UPN) not detected. Continuing without login.");
            return null;
        }

        if (accessToken.TenantId == null)
        {
            AnsiConsole.MarkupLine($"{Emoji.Known.ExclamationQuestionMark} Tenant Id (TId) not detected. Continuing without login.");
            return null;
        }

        if (accessToken.UserName == null)
        {
            AnsiConsole.MarkupLine($"{Emoji.Known.ExclamationQuestionMark} User name not detected. Continuing without login.");
            return null;
        }

        var ret = new NoxCliCache(cacheFile) 
        { 
            Name = accessToken.UserName,
            Upn = accessToken.UserPrincipalName, 
            Tid = accessToken.TenantId, 
            Expires = new DateTimeOffset(DateTime.Now.AddDays(7)) 
        };

        ret.Save();

        AnsiConsole.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} Logged in as {accessToken.UserPrincipalName} on tenant {accessToken.TenantId}");
        AnsiConsole.WriteLine();

        return ret;
    }

    private static string[] FindWorkflows(string searchPath = "")
    {
        var path = new DirectoryInfo(Directory.GetCurrentDirectory());
        
        if (!string.IsNullOrEmpty(searchPath))
        {
            path = new DirectoryInfo(searchPath);
        }

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

