using CodingSeb.ExpressionEvaluator;
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
    
    public bool EvaluateIf()
    {
        if (string.IsNullOrEmpty(If)) return true;

        var evaluator = new ExpressionEvaluator();

        return (bool)evaluator.Evaluate(If);
    }
}