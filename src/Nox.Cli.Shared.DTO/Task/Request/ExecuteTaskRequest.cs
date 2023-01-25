namespace Nox.Cli.Shared.DTO.Workflow;

public class ExecuteTaskRequest
{
    public Guid WorkflowId { get; set; }
    public IDictionary<string, object> Variables { get; set; }
    public string Type { get; set; }
}