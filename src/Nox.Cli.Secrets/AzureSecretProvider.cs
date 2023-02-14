using System.Runtime.CompilerServices;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Nox.Cli.Abstractions;

namespace Nox.Cli.Secrets;

public class AzureSecretProvider: ISecretProvider
{
    private readonly Uri _vaultUri;

    public AzureSecretProvider(string vaultUrl)
    {
        if (Uri.IsWellFormedUriString(vaultUrl, UriKind.Absolute))
        {
            _vaultUri = new Uri(vaultUrl);
        }
        else
        {
            throw new Exception("VaultUrl is not a well formed Uri!");
        }
    }
    
    public async Task<IList<KeyValuePair<string, string>>?> GetSecretsFromVault(string[] keys)
    {
        var secrets = new List<KeyValuePair<string, string>>();
        var secretClient = new SecretClient(_vaultUri, new DefaultAzureCredential());
        try
        {
            foreach (var key in keys)
            {
                var secret = await secretClient.GetSecretAsync(key.Replace(":", "--").Replace("_", "-"));
                secrets.Add(new KeyValuePair<string, string>(key, secret.Value.Value ?? ""));
            }
        }
        catch (Exception ex)
        {
            string InterpolateError()
            {
                var interpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 2);
                interpolatedStringHandler.AppendLiteral("Error loading secrets from vault at '");
                interpolatedStringHandler.AppendFormatted(_vaultUri);
                interpolatedStringHandler.AppendLiteral("'. (");
                interpolatedStringHandler.AppendFormatted(ex.Message);
                interpolatedStringHandler.AppendLiteral(")");
                return interpolatedStringHandler.ToStringAndClear();
            }

            var errorMessage = InterpolateError();
            throw new Exception(errorMessage);
        }
        return secrets;
    }
}