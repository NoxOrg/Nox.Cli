using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class TaskStateResponse
{
    public Guid WorkflowId { get; set; }
    public ActionState State { get; set; }
}