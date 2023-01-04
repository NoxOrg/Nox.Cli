
namespace Nox.Cli.Actions.Configuration;

public class WorkflowConfiguration
{
    public string Name { get; set; } = string.Empty;
    public CliConfiguration Cli { get; set; } = new();
    public Dictionary<string, StepConfiguration> Jobs { get; set; } = new ();
}

