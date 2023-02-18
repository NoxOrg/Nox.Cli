namespace Nox.Cli.Abstractions.Configuration;

public interface ILocalTaskExecutorConfiguration
{
    ISecretsConfiguration? Secrets { get; set; }
}