using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Secrets;

public interface IServerSecretResolver
{
    Task Resolve(List<ServerVariable> variables, IRemoteTaskExecutorConfiguration config);
}