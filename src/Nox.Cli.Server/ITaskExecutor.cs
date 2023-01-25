using Nox.Cli.Abstractions;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server;

public interface ITaskExecutor
{
    Task BeginAsync(Guid workflowId, IActionConfiguration configuration, IDictionary<string, object> inputs);
    Task<ExecuteTaskResponse> ExecuteAsync();
}