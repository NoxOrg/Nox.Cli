namespace Nox.Cli.Abstractions.Configuration;

public interface IWorkflowConfiguration
{
    string Name { get; set; }
    string Description { get; set; }
    ICliConfiguration Cli { get; set; }
    List<IJobConfiguration> Jobs { get; set; }
}