using Nox.Cli.Abstractions;

namespace Nox.Cli.Server.Abstractions;

public interface IWorkflowContextFactory
{
    INoxWorkflowContext NewInstance(Guid workflowId);
    INoxWorkflowContext? GetInstance(Guid workflowId);
    void DisposeInstance(Guid workflowId);
}