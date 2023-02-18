namespace Nox.Cli.Abstractions.Configuration;

public interface IWorkflowConfiguration
{
    string Name { get; set; }
    ICliConfiguration Cli { get; set; }
    Dictionary<string, IStepConfiguration> Jobs { get; set; }
}