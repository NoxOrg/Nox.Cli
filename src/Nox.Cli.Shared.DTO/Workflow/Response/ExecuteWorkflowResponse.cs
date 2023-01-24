using Nox.Cli.Shared.DTO.Enumerations;

namespace Nox.Cli.Shared.DTO.Workflow;

public class ExecuteWorkflowResponse
{
    public Guid WorkflowId { get; set; }
    public WorkflowExecutionState State { get; set; }
}