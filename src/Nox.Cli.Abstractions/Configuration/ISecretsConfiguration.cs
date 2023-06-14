namespace Nox.Cli.Abstractions.Configuration;

public interface ISecretsConfiguration
{
    ISecretsValidForConfiguration? ValidFor { get; set; }
    IList<ISecretProviderConfiguration>? Providers { get; set; }
}