namespace Nox.Cli.Abstractions.Configuration;

public interface IRemoteTaskExecutorConfiguration
{
    string Url { get; set; }
    string ApplicationId { get; set; }
    ISecretsConfiguration? Secrets { get; set; }
}