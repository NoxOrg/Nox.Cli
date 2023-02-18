using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Abstractions;

public interface INoxWorkflowExecutor
{
    Task<bool> Execute(IWorkflowConfiguration workflow);
}