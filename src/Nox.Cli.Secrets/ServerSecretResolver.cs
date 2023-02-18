using System.Text.RegularExpressions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;
using Nox.Utilities.Secrets;

namespace Nox.Cli.Secrets;

public class ServerSecretResolver: IServerSecretResolver
{
    private static readonly Regex SecretsVariableRegex = new(@"\$\{\{\s*server\.secrets\.(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
 
    private readonly IPersistedSecretStore _store;

    public ServerSecretResolver(IPersistedSecretStore store)
    {
        _store = store;
    }

    
    public async Task ResolveAsync(List<ServerVariable> variables, IRemoteTaskExecutorConfiguration config)
    {
        if (config.Secrets == null) return;
        var secretKeys = new List<KeyValuePair<string, string>>();
        foreach (var item in variables)
        {
            if (item.Value != null)
            {
                var match = SecretsVariableRegex.Match(item.Value.ToString()!);
                if (match.Success)
                {
                    var secretKey = match.Groups["variable"].Value;
                    secretKeys.Add(new KeyValuePair<string, string>(secretKey, item.FullName));
                }    
            }
        }
        
        var secrets = new List<KeyValuePair<string, string>>();
        var onlineSecretKeys = new List<KeyValuePair<string, string>>();
        foreach (var item in secretKeys)
        {
            var secretTtl = TimeSpan.FromHours(8);
            if (config.Secrets is { ValidFor: { } })
            {
                secretTtl = new TimeSpan(config.Secrets.ValidFor.Days ?? 0, config.Secrets.ValidFor.Hours ?? 0, config.Secrets.ValidFor.Minutes ?? 0, config.Secrets.ValidFor.Seconds ?? 0);
            }
            var storedSecret = await _store.LoadAsync($"srv.{item.Key}", secretTtl);
            if (storedSecret != null)
            {
                secrets.Add(new KeyValuePair<string, string>(item.Key, storedSecret));
            }
            else
            {
                onlineSecretKeys.Add(item);
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
                        //TODO cache these secrets
                        var azureSecrets = azureVault.GetSecretsAsync(onlineSecretKeys.Select(k => k.Key).ToArray()).Result;
                        if (azureSecrets != null && azureSecrets.Any())
                        {
                            secrets.AddRange(azureSecrets);
                            foreach (var azureSecret in azureSecrets)
                            {
                                await _store.SaveAsync($"srv.{azureSecret.Key}", azureSecret.Value);
                            }
                        }
                        
                        break;
                }
            }
        }

        if (!secrets.Any()) return;

        foreach (var kv in secrets)
        {
            var varName = secretKeys.Single(k => k.Key == kv.Key).Value;
            var variable = variables.Single(v => v.FullName == varName);
            variable.Value = kv.Value;
        }
    }
}