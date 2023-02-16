using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class LocalTaskExecutorConfiguration: ILocalTaskExecutorConfiguration
{
    public ISecretsConfiguration? Secrets { get; set; }
}

