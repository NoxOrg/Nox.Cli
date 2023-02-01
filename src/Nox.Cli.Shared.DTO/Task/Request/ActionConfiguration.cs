using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class ActionConfiguration: IActionConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string? If { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Uses { get; set; } = string.Empty;
    public Dictionary<string, object> With { get; set; } = new();
    public Dictionary<string, string>? Validate { get; set; }
    public NoxActionDisplayMessage? Display { get; set; }
    public bool ContinueOnError { get; set; }
    public bool RunAtServer { get; set; }
}