using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class ExecuteTaskResponse
{
    public Guid WorkflowId { get; set; }
    public IDictionary<string, Object> Outputs { get; set; }
    public ActionState State { get; set; }
    public string ErrorMessage { get; set; }
}