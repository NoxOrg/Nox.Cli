namespace Nox.Cli.Abstractions.Configuration;

public interface IStepConfiguration
{
    List<IActionConfiguration> Steps { get; set; } 
}