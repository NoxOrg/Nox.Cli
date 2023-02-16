namespace Nox.Cli.Shared.DTO.Workflow;

public class ExecuteTaskRequest
{
    public Guid WorkflowId { get; set; }
    public ServerAction? ActionConfiguration { get; set; }
}