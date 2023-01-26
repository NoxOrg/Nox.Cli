using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class TaskStateResponse
{
    public Guid TaskExecutorId { get; set; }
    public Guid WorkflowId { get; set; }
    public ActionState State { get; set; }
    public string? StateName { get; set; }
}