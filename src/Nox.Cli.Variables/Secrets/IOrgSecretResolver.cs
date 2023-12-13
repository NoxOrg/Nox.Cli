using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Variables.Secrets;

public interface IOrgSecretResolver
{
    Task Resolve(IDictionary<string, object?> variables, ILocalTaskExecutorConfiguration? config);
}