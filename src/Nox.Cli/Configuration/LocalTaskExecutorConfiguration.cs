using Nox.Cli.Abstractions.Configuration;
using Nox.Core.Constants;

namespace Nox.Cli.Configuration;

public class LocalTaskExecutorConfiguration: ILocalTaskExecutorConfiguration
{
    public List<ISecretsConfiguration>? Secrets { get; set; }
}

