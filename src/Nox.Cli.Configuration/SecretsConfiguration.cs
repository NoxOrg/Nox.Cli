using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class SecretsConfiguration: ISecretsConfiguration
{
    public ISecretsValidForConfiguration? ValidFor { get; set; }
    public IList<ISecretProviderConfiguration>? Providers { get; set; }
}