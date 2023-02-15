namespace Nox.Cli.Abstractions.Configuration;

public interface ISecretsConfiguration
{
    string Provider { get; set; }
    string Url { get; set; }
}