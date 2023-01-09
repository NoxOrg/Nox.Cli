using Microsoft.Extensions.Configuration;
using Nox.Cli.Configuration;
using Nox.Core.Interfaces.Configuration;

namespace Nox.Workflow;

public class NoxWorkflowParameters
{
    public WorkflowConfiguration WorkflowConfiguration { get; set; } = null!;
    public IConfiguration AppConfiguration { get; set; } = null!;
    public INoxConfiguration NoxConfiguration { get; set; } = null!;
}




