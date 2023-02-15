namespace Nox.Cli.Abstractions.Configuration;

public interface ILocalTaskExecutorConfiguration
{
    List<ISecretsConfiguration>? Secrets { get; set; }
}