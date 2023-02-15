using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class BeginTaskRequest
{
    public Guid WorkflowId { get; set; }
    public ActionConfiguration? ActionConfiguration { get; set; }
    public IDictionary<string, object>? Inputs { get; set; }
}