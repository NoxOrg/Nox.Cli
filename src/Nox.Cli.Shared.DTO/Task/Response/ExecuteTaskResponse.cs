using Nox.Cli.Abstractions;
using Nox.Cli.Variables;

namespace Nox.Cli.Shared.DTO.Workflow;

public class ExecuteTaskResponse
{
    public Guid WorkflowId { get; set; }
    public IDictionary<string, Variable>? Outputs { get; set; }
    public ActionState State { get; set; }
    public string? StateName { get; set; }
    public string? ErrorMessage { get; set; }
}