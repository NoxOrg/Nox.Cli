using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;
using Nox.Core.Interfaces.Configuration;

namespace Nox.Cli.Secrets;

public static class ServerSecretResolver
{
    private static readonly Regex SecretsVariableRegex = new(@"\$\{\{\s*server\.secrets\.(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public static void ResolveServerSecrets(this List<ServerVariable> variables, IRemoteTaskExecutorConfiguration config)
    {
        if (config?.Secrets == null) return;
        var secretKeys = new Dictionary<string, string>();
        foreach (var item in variables)
        {
            var match = SecretsVariableRegex.Match(item.Value.ToString());
            if (match.Success)
            {
                var secretKey = match.Groups["variable"].Value;
                secretKeys.Add(secretKey, item.FullName);
            }
        }
        
        var secrets = new List<KeyValuePair<string, string>>();
        foreach (var vault in config.Secrets)
        {
            switch (vault.Provider.ToLower())
            {
                case "azure-keyvault":
                    var azureVault = new AzureSecretProvider(vault.Url);
                    //TODO cache these secrets
                    var azureSecrets = azureVault.GetSecretsFromVault(secretKeys.Keys.ToArray()).Result;
                    if (azureSecrets != null && azureSecrets.Any()) secrets.AddRange(azureSecrets);
                    break;
            }
        }

        if (!secrets.Any()) return;

        foreach (var kv in secrets)
        {
            var varName = secretKeys[kv.Key];
            var variable = variables.Single(v => v.FullName == varName);
            variable.Value = kv.Value;
        }
    }
}