using System.Reflection;
using CodingSeb.ExpressionEvaluator;
using Microsoft.Graph.Models;
using Microsoft.PowerShell.Commands;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;

namespace Nox.Cli.Actions;

public class NoxJob: INoxJob
{
    public int Sequence { get; set; }
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? If { get; set; }
    public object? ForEach { get; set; }

    public NoxJobDisplayMessage? Display { get; set; } = new();
    public IDictionary<string, INoxAction> Steps { get; set; } = new Dictionary<string, INoxAction>();
    
    public bool EvaluateIf()
    {
        if (string.IsNullOrEmpty(If)) return true;

        var evaluator = new ExpressionEvaluator();

        return (bool)evaluator.Evaluate(If);
    }
}