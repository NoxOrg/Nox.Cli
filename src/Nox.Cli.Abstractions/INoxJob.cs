namespace Nox.Cli.Abstractions;

public interface INoxJob
{
    int Sequence { get; set; }
    int FirstStepSequence { get; set; }
    string Id { get; set; }
    
    string Name { get; set; }
    string? If { get; set; }
    NoxJobDisplayMessage? Display { get; set; }
    IDictionary<string, INoxAction> Steps { get; set; }
    
    public bool EvaluateIf();
}