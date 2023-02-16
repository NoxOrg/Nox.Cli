using Nox.Cli.Abstractions;

namespace Nox.Cli.Server.Abstractions;

public interface IServerCache
{
    INoxWorkflowContext GetContext(Guid workflowId);
    void SaveContext(Guid workflowId, INoxWorkflowContext context);
}