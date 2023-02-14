using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Secrets;

public static class OrgSecretResolver
{
    public static void ResolveOrgSecrets(this IDictionary<string, IVariable> variables, ILocalTaskExecutorConfiguration? config)
    {
        var secretKeys = variables
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("org.secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[12..])
            .ToArray();

        if (config?.Secrets == null) return;
        
        var secrets = new List<KeyValuePair<string, string>>();
        foreach (var vault in config.Secrets)
        {
            switch (vault.Provider.ToLower())
            {
                case "azure-keyvault":
                    var azureVault = new AzureSecretProvider(vault.Url);
                    var azureSecrets = azureVault.GetSecretsFromVault(secretKeys).Result;
                    if (azureSecrets != null && azureSecrets.Any()) secrets.AddRange(azureSecrets);
                    break;
            }
        }

        if (!secrets.Any()) return;

        foreach (var kv in secrets)
        {
            variables[$"org.secrets.{kv.Key}"].Value = kv.Value;
            variables[$"org.secrets.{kv.Key}"].IsSecret = true;
        }
    }
}