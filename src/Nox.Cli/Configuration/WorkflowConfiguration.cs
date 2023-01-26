namespace Nox.Cli.Configuration;

public class WorkflowConfiguration
{
    public string Name { get; set; } = string.Empty;
    public Uri ServerUri { get; set; } = new("");
    public CliConfiguration Cli { get; set; } = new();
    public Dictionary<string, StepConfiguration> Jobs { get; set; } = new();
}

