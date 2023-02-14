using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class SecretConfiguration: ISecretConfiguration
{
    public string Provider { get; set; } = "azure-keyvault";

    public string Url { get; set; } = string.Empty;
}