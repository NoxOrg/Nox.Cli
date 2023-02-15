using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Shared.DTO.Health;
using Nox.Cli.Shared.DTO.Workflow;
using Nox.Cli.Variables;

namespace Nox.Cli.Server.Integration;

public interface INoxCliServerIntegration
{
    Task<EchoHealthResponse?> EchoHealth();
    Task<BeginTaskResponse> BeginTask(Guid workflowId, INoxAction action, IDictionary<string, Variable>? inputs);
    Task<ExecuteTaskResponse> ExecuteTask(Guid taskExecutorId);
    Task<TaskStateResponse> GetTaskState(Guid taskExecutorId);
}