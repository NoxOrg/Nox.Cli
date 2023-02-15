using Nox.Cli.Abstractions;
using Nox.Core.Configuration;
using Nox.Core.Interfaces.Configuration;

namespace Nox.Cli.Secrets;

public static class ProjectSecretResolver
{
    public static void ResolveProjectSecrets(this IDictionary<string, IVariable> variables, IProjectConfiguration config)
    {
        var secretKeys = variables
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("project.secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[16..])
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
            variables[$"project.secrets.{kv.Key}"].Value = kv.Value;
            variables[$"project.secrets.{kv.Key}"].IsSecret = true;
        }
    }
}