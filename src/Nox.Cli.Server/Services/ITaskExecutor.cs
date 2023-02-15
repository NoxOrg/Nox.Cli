using Nox.Cli.Abstractions;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Services;

public interface ITaskExecutor
{
    Guid Id { get; }
    Guid WorkflowId { get; }
    ActionState State { get; }
    Task<BeginTaskResponse> BeginAsync(Guid workflowId, IActionConfiguration configuration, IDictionary<string, object> inputs);
    Task<ExecuteTaskResponse> ExecuteAsync();
}