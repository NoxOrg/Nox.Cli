using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Caching;

namespace Nox.Cli.Variables;

public static class NoxCacheVariableResolver
{
    public static void ResolveNoxCacheVariables(this IDictionary<string, object?> variables, INoxCliCache? cache)
    {
        if (cache == null) return;
        var keys = variables
            .Where(kv => kv.Value == null)
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("cache.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[6..])
            .ToArray();

        
        
        foreach (var key in keys)
        {
            var value = ResolveCacheValue(key, cache);
            if (value != null) variables[$"cache.{key}"] = value;
        }
    }
    
    private static object? ResolveCacheValue(string runnerKey, INoxCliCache cache)
    {
        return runnerKey.ToLower() switch
        {
            "upn" => cache.UserPrincipalName,
            "username" => cache.Username,
            "aztoken" => CredentialHelper.GetAzureDevOpsAccessToken().Result,
           _ => null
        };
    }
}