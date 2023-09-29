namespace Nox.Cli.Abstractions.Configuration;

public interface IJobConfiguration
{
    string Id { get; set; }
    string Name { get; set; }
    string? If { get; set; }
    string? ForEach { get; set; }
    NoxJobDisplayMessage? Display { get; set; }
    List<IActionConfiguration> Steps { get; set; } 
}