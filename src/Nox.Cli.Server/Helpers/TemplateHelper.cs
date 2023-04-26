using System.Text.Json;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Utilities.Configuration;
using RestSharp;

namespace Nox.Cli.Server.Helpers;

public static class TemplateHelper
{
    public static void GetOnlineTemplates(string templateUrl, string? tenantId)
    {
        var cachePath = WellKnownPaths.CachePath;

        Directory.CreateDirectory(cachePath);

        var cacheFile = WellKnownPaths.CacheFile;
        
        var cache = GetOrCreateCache(cacheFile);
        if (string.IsNullOrEmpty(templateUrl))
        {
            if (string.IsNullOrEmpty(tenantId)) throw new NoxCliException("Tenant Id has not been specified in app settings!");
            templateUrl = $"https://noxorg.dev/templates/{tenantId}";
        }
        
        var client = new RestClient($"{templateUrl}/{tenantId}");

        var fileListRequest = new RestRequest() { Method = Method.Get };

        fileListRequest.AddHeader("Accept", "application/json");

        // Get list of files on server
        var onlineFilesJson = client.Execute(fileListRequest);

        if (onlineFilesJson.Content == null) return;

        if (onlineFilesJson.ResponseStatus == ResponseStatus.Error)
        {
            throw new NoxCliException($"GetOnlineTemplates:-> {onlineFilesJson.ErrorException?.Message}");
        }
        
        var onlineFiles = JsonSerializer.Deserialize<System.Collections.Generic.List<RemoteFileInfo>>(onlineFilesJson.Content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Read and cache the entries

        var templateCachePath = WellKnownPaths.TemplatesCachePath;

        Directory.CreateDirectory(templateCachePath);

        var existingCacheList = TraverseDirectory(templateCachePath).ToHashSet();

        ValidateTemplateCache(existingCacheList, cachePath);
        
        foreach (var file in onlineFiles!)
        {
            string? fileContent = null;

            if (cache!.TemplateInfo == null
                || cache.TemplateInfo.All(i => i.Name != Path.Combine(templateCachePath, file.Name)) 
                || !cache.TemplateInfo.Any(i => i.Name == Path.Combine(templateCachePath, file.Name) && i.ShaChecksum == file.ShaChecksum))
            {
                var fileRequest = new RestRequest() { Method = Method.Post };
                fileRequest.AddHeader("Accept", "application/json");
                fileRequest.AddJsonBody($"{{\"FilePath\": \"{file.Name}\"}}");
                    
                fileContent = client.Execute(fileRequest).Content;

                if (fileContent == null) throw new NoxCliException($"Couldn't download template {file.Name}");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(templateCachePath, file.Name))!);
                File.WriteAllText(Path.Combine(templateCachePath, file.Name), fileContent);
            }

            if (existingCacheList.Contains(Path.Combine(templateCachePath, file.Name)))
            {
                existingCacheList.Remove(Path.Combine(templateCachePath, file.Name));
            }
        }

        foreach (var orphanEntry in existingCacheList)
        {
            File.Delete(Path.Combine(templateCachePath, orphanEntry));
        }

        cache!.WorkflowInfo = onlineFiles;
        cache.Save();
    }
    
    private static NoxCliCache? GetOrCreateCache(string cacheFile)
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

        cache = new NoxCliCache(cacheFile) 
        { 
            Expires = new DateTimeOffset(DateTime.Now.AddDays(7)) 
        };
        return cache;
    }
    
    private static List<string> TraverseDirectory(string directory)
    {
        var result = new List<string>();
        foreach (var file in Directory.GetFiles(directory))
        {
            result.Add(file);
        }

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            result.AddRange(TraverseDirectory(subDirectory));
        }

        return result;
    }

    private static void ValidateTemplateCache(HashSet<string> cache, string cachePath)
    {
        foreach (var item in cache)
        {
            if (!File.Exists(Path.Combine(cachePath, item)))
                cache.Remove(item);
        }
    }
}