namespace Nox.Cli.Abstractions.Configuration;

public interface ISecretProviderConfiguration
{
    string Provider { get; set; }
    string Url { get; set; }
}