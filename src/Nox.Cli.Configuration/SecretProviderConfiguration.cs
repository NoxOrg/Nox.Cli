using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class SecretProviderConfiguration: ISecretProviderConfiguration
{
    public string Provider { get; set; } = "azure-keyvault";

    public string Url { get; set; } = string.Empty;
}