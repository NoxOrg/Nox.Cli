using Nox.Cli.Abstractions;
using Nox.Cli.Actions;

namespace Nox.Cli.Configuration;

public class ActionConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string? If { get; set; } = null;
    public string Job { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Uses { get; set; } = string.Empty;
    public Dictionary<string, object> With { get; set; } = new();
    public Dictionary<string, string>? Validate { get; set; }
    public NoxActionDisplayMessage? Display { get; set; }
    public bool ContinueOnError { get; set; } = false;
}

