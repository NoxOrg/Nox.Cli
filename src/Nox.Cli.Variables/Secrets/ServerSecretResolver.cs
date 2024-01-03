using System.Text.RegularExpressions;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Variables.Secrets;

public class ServerSecretResolver: IServerSecretResolver
{
    private static readonly Regex SecretsVariableRegex = new(@"\$\{\{\s*server\.secrets\.(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
 
    private readonly IPersistedSecretStore _store;
    private readonly string _tenantId;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public ServerSecretResolver(IPersistedSecretStore store, string tenantId, string clientId, string clientSecret)
    {
        _store = store;
        _tenantId = tenantId;
        _clientId = clientId;
        _clientSecret = clientSecret;
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
        
        //Default secret ttl to 30 minutes if not set
        var ttl = new TimeSpan(0, 30, 0);
        var validFor = config.Secrets.ValidFor;
        if (validFor != null)
        {
            ttl = new TimeSpan(validFor.Days ?? 0, validFor.Hours ?? 0, validFor.Minutes ?? 0, validFor.Seconds ?? 0);

        }
        if (ttl == TimeSpan.Zero) ttl = new TimeSpan(0, 30, 0);
        
        var resolvedSecrets = new List<KeyValuePair<string, string>>();
        foreach (var item in secretKeys)
        {
            var cachedSecret = await _store.LoadAsync($"srv.{item.Key}", TimeSpan.FromHours(1));
            resolvedSecrets.Add(new KeyValuePair<string, string>(item.Key, cachedSecret ?? ""));
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
                        var clientCredentials = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
                        
                        var secretClient = new SecretClient(new Uri(vault.Url), clientCredentials);
                        var azureSecrets = new Dictionary<string, string>();
                        foreach (var key in unresolvedSecrets.Select(k => k.Key))
                        {
                            var secret = await secretClient.GetSecretAsync(key.ToAzureSecretKey());
                            azureSecrets.Add(key, secret.Value.Value);
                        }

                        if (azureSecrets.Count > 0)
                        {
                            resolvedSecrets.AddRange(azureSecrets);
                            foreach (var azureSecret in azureSecrets)
                            {
                                await _store.SaveAsync($"srv.{azureSecret.Key}", azureSecret.Value);
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
            var varName = secretKeys.Single(k => k.Key == kv.Key).Value;
            var variable = variables.Single(v => v.FullName == varName);
            variable.Value = kv.Value;
        }
    }
}