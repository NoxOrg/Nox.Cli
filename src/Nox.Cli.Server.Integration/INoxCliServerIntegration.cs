using Nox.Cli.Abstractions;
using Nox.Cli.Shared.DTO.Health;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Integration;

public interface INoxCliServerIntegration
{
    string Endpoint { get; }
    Task<EchoHealthResponse?> EchoHealth();
    Task<ExecuteTaskResult> ExecuteTask(Guid workflowId, INoxAction? action);
    Task<TaskStateResponse> GetTaskState(Guid taskExecutorId);
}