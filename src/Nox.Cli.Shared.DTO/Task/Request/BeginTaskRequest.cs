using Nox.Cli.Abstractions;
using Nox.Cli.Variables;

namespace Nox.Cli.Shared.DTO.Workflow;

public class BeginTaskRequest
{
    public Guid WorkflowId { get; set; }
    public ActionConfiguration? ActionConfiguration { get; set; }
    public IDictionary<string, Variable>? Inputs { get; set; }
}