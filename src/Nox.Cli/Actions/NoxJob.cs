using Nox.Cli.Abstractions;

namespace Nox.Cli.Actions;

public class NoxJob: INoxJob
{
    public int Sequence { get; set; }
    public int FirstStepSequence { get; set; } = 0;
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? If { get; set; }
    
    public NoxJobDisplayMessage? Display { get; set; } = new();
    public IDictionary<string, INoxAction> Steps { get; set; } = new Dictionary<string, INoxAction>();
}