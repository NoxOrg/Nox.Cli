using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Variables.Secrets;

public interface IServerSecretResolver
{
    Task ResolveAsync(List<ServerVariable> variables, IRemoteTaskExecutorConfiguration config);
}