using Nox.Cli.Abstractions;

namespace Nox.Cli.Server;

public interface ITaskExecutor
{
    Guid WorkflowId { get; }
    INoxAction Action { get; }

    Task ExecuteAsync(Guid workflowId, INoxAction action);
}