using Nox.Cli.Abstractions.Configuration;
using Nox.Yaml;

namespace Nox.Cli.Configuration;

public class WorkflowConfiguration: YamlConfigNode<WorkflowConfiguration>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CliConfiguration Cli { get; set; } = null!;
    public List<JobConfiguration> Jobs { get; set; } = new();
}

