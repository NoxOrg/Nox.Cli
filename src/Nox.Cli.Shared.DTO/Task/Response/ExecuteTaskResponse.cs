using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class ExecuteTaskResponse
{
    public Guid WorkflowId { get; set; }
    public INoxAction Action { get; set; }
}