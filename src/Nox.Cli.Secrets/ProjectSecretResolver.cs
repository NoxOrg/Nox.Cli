using Nox.Cli.Abstractions;
using Nox.Core.Configuration;
using Nox.Core.Interfaces.Configuration;

namespace Nox.Cli.Secrets;

public class ProjectSecretResolver: IProjectSecretResolver
{
    private readonly IPersistedSecretStore _store;

    public ProjectSecretResolver(IPersistedSecretStore store)
    {
        _store = store;
    }
    
    public async Task Resolve(IDictionary<string, object?> variables, IProjectConfiguration config)
    {
        var secretKeys = variables
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("project.secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[16..])
            .ToList();
        
        if (config.Secrets == null) return;
        var secrets = new List<KeyValuePair<string, string>>();
        var onlineSecretKeys = new List<string>();
        foreach (var key in secretKeys)
        {
            //Todo change this in Nox.Core to a configurable setting
            var storedSecret = await _store.LoadAsync($"{config.Name}.{key}", TimeSpan.FromHours(1));
            if (storedSecret != null)
            {
                secrets.Add(new KeyValuePair<string, string>(key, storedSecret));
            }
            else
            {
                onlineSecretKeys.Add(key);
            }
        }

        if (onlineSecretKeys.Any())
        {
            foreach (var vault in config.Secrets)
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
                                await _store.SaveAsync($"{config.Name}.{azureSecret.Key}", azureSecret.Value);
                            }
                        }

                        break;
                }
            }
        }

        if (!secrets.Any()) return;

        foreach (var kv in secrets)
        {
            variables[$"project.secrets.{kv.Key}"] = kv.Value;
        }
    }
}