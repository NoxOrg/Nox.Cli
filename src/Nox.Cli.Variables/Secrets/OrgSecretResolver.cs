using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Secrets;
using Nox.Secrets.Abstractions;

namespace Nox.Cli.Variables.Secrets;

public class OrgSecretResolver: IOrgSecretResolver
{
    private readonly IPersistedSecretStoreEx _store;

    public OrgSecretResolver(IPersistedSecretStoreEx store)
    {
        _store = store;
    }
        
    public async Task Resolve(IDictionary<string, object?> variables, ILocalTaskExecutorConfiguration? config)
    {
        if (config?.Secrets == null) return;

        var secretKeys = variables
            .Where(kv => kv.Value== null)
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("org.secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[12..])
            .ToList();

        //Default secret ttl to 30 minutes if not set
        var validFor = config.Secrets.ValidFor;
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
            var cachedSecret = await _store.LoadAsync($"org.{key}", TimeSpan.FromHours(1));
            resolvedSecrets.Add(new KeyValuePair<string, string>(key, cachedSecret ?? ""));
        }
        
        //Resolve any remaining secrets from the vaults
        var unresolvedSecrets = resolvedSecrets.Where(s => s.Value == "").ToList();
        if (unresolvedSecrets.Any() && config.Secrets.Providers != null)
        {
            foreach (var vault in config.Secrets.Providers)
            {
                if (!unresolvedSecrets.Any()) break;
                switch (vault.Provider.ToLower())
                {
                    case "azure-keyvault":
                        var azureVault = new AzureSecretProvider(vault.Url);
                        var azureSecrets = azureVault.GetSecretsAsync(unresolvedSecrets.Select(k => k.Key).ToArray()).Result;
                        if (azureSecrets != null)
                        {
                            if (azureSecrets.Any()) resolvedSecrets.AddRange(azureSecrets);
                            foreach (var azureSecret in azureSecrets)
                            {
                                await _store.SaveAsync($"org.{azureSecret.Key}", azureSecret.Value);
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
            variables[$"org.secrets.{kv.Key}"] = kv.Value;
        }
    }
}