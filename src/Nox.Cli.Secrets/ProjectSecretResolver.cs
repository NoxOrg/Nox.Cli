using Nox.Core.Interfaces.Configuration;
using Nox.Utilities.Secrets;

namespace Nox.Cli.Secrets;

public class ProjectSecretResolver: IProjectSecretResolver
{
    private readonly IPersistedSecretStore _store;

    public ProjectSecretResolver(IPersistedSecretStore store)
    {
        _store = store;
    }
    
    public async Task Resolve(IDictionary<string, object?> variables, IProjectConfiguration projectConfig)
    {
        var secretKeys = variables
            .Where(kv => kv.Value == null)
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("project.secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[16..])
            .ToList();
        
        if (projectConfig.Secrets == null) return;

        //Default secret ttl to 30 minutes if not set
        var validFor = projectConfig.Secrets.ValidFor;
        TimeSpan ttl = TimeSpan.Zero;
        if (validFor != null)
        {
            ttl = new TimeSpan(validFor.Days ?? 0, validFor.Hours ?? 0, validFor.Minutes ?? 0, validFor.Seconds ?? 0);
        }
        if (ttl == TimeSpan.Zero)
        {
            ttl = new TimeSpan(0, 30, 0);
        }
        
        var resolvedSecrets = new List<KeyValuePair<string, string>>();
        foreach (var key in secretKeys)
        {
            var cachedSecret = await _store.LoadAsync($"{projectConfig.Name}.{key}", ttl); 
            resolvedSecrets.Add(new KeyValuePair<string, string>(key, cachedSecret ?? ""));
        }
        
        //Resolve any remaining secrets from the vaults
        var unresolvedSecrets = resolvedSecrets.Where(s => s.Value == "").ToList();
        if (unresolvedSecrets.Any() && projectConfig.Secrets.Providers != null)
        {
            foreach (var vault in projectConfig.Secrets.Providers)
            {
                if (!unresolvedSecrets.Any()) break;
                switch (vault.Provider.ToLower())
                {
                    case "azure-keyvault":
                        var azureVault = new AzureSecretProvider(vault.Url);
                        var azureSecrets = azureVault.GetSecretsAsync(unresolvedSecrets.Select(k => k.Key).ToArray()).Result;
                        if (azureSecrets != null)
                        {
                            if (azureSecrets.Any())
                            {
                                resolvedSecrets.AddRange(azureSecrets);
                            }
                            foreach (var azureSecret in azureSecrets)
                            {
                                await _store.SaveAsync($"{projectConfig.Name}.{azureSecret.Key}", azureSecret.Value);
                            }
                        }
                        break;
                }
                unresolvedSecrets = resolvedSecrets.Where(s => s.Value == "").ToList();
            }
        }

        if (!resolvedSecrets.Any()) return;

        foreach (var kv in resolvedSecrets)
        {
            variables[$"project.secrets.{kv.Key}"] = kv.Value;
        }
    }
}