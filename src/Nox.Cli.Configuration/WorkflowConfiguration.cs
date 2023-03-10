using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class WorkflowConfiguration: IWorkflowConfiguration
{
    public string Name { get; set; } = string.Empty;
    public ICliConfiguration Cli { get; set; } = null!;
    public Dictionary<string, IStepConfiguration> Jobs { get; set; } = new();
}

