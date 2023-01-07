using Nox.Core.Constants;

namespace Nox.Cli.Configuration;

public class SecretsConfiguration
{
    public string Provider { get; set; } = "azure-keyvault";

    public string Url { get; set; } = KeyVault.DefaultKeyVaultUri;
}

