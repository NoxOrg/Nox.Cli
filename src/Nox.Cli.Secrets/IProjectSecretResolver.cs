using Nox.Core.Interfaces;
using Nox.Core.Interfaces.Configuration;

namespace Nox.Cli.Secrets;

public interface IProjectSecretResolver
{
    Task Resolve(IDictionary<string, object?> variables, IProjectConfiguration config);
}