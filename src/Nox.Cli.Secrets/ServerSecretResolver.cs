using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Core.Interfaces.Configuration;

namespace Nox.Cli.Secrets;

public static class ServerSecretResolver
{
    public static void ResolveServerSecrets(this IDictionary<string, IVariable> variables, IRemoteTaskExecutorConfiguration config)
    {
        var secretKeys = variables
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("server.secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[15..])
            .ToArray();
        
        if (config.Secrets == null) return;
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
            variables[$"server.secrets.{kv.Key}"].Value = kv.Value;
            variables[$"server.secrets.{kv.Key}"].IsSecret = true;
        }
    }
}