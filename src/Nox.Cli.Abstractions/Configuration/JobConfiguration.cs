using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class JobConfiguration
{
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;

    public string? If { get; set; }
    public string? ForEach { get; set; }

    public NoxJobDisplayMessage? Display { get; set; }
    public List<ActionConfiguration> Steps { get; set; } = new();
}