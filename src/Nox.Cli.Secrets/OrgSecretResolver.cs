using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Secrets;

public class OrgSecretResolver: IOrgSecretResolver
{
    private readonly IPersistedSecretStore _store;

    public OrgSecretResolver(IPersistedSecretStore store)
    {
        _store = store;
    }

    
    public async Task Resolve(IDictionary<string, object?> variables, ILocalTaskExecutorConfiguration? config)
    {
        var secretKeys = variables
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("org.secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[12..])
            .ToList();

        if (config?.Secrets == null) return;
        
        var secrets = new List<KeyValuePair<string, string>>();
        var onlineSecretKeys = new List<string>();
        foreach (var key in secretKeys)
        {
            var secretTtl = TimeSpan.FromHours(4);
            if (config is { Secrets.ValidFor: { } })
            {
                secretTtl = new TimeSpan(config.Secrets.ValidFor.Days ?? 0, config.Secrets.ValidFor.Hours ?? 0, config.Secrets.ValidFor.Minutes ?? 0, config.Secrets.ValidFor.Seconds ?? 0);
            } 
            var storedSecret = await _store.LoadAsync($"org.{key}", secretTtl);
            if (storedSecret != null)
            {
                secrets.Add(new KeyValuePair<string, string>(key, storedSecret));
            }
            else
            {
                onlineSecretKeys.Add(key);
            }
        }

        if (onlineSecretKeys.Any() && config.Secrets.Providers != null)
        {
            foreach (var vault in config.Secrets.Providers)
            {
                switch (vault.Provider.ToLower())
                {
                    case "azure-keyvault":
                        var azureVault = new AzureSecretProvider(vault.Url);
                        var azureSecrets = azureVault.GetSecretsFromVault(onlineSecretKeys.ToArray()).Result;
                        if (azureSecrets != null)
                        {
                            if (azureSecrets.Any()) secrets.AddRange(azureSecrets);
                            foreach (var azureSecret in azureSecrets)
                            {
                                await _store.SaveAsync($"org.{azureSecret.Key}", azureSecret.Value);
                            }
                        }

                        break;
                }
            }
        }

        if (!secrets.Any()) return;

        foreach (var kv in secrets)
        {
            variables[$"org.secrets.{kv.Key}"] = kv.Value;
        }
    }
}