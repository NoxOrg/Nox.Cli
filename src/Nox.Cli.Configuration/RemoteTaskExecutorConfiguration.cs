using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class RemoteTaskExecutorConfiguration: IRemoteTaskExecutorConfiguration
{
    public string Url { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public ISecretsConfiguration? Secrets { get; set; } = null;
}