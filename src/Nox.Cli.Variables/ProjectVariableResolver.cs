using Nox.Solution;

namespace Nox.Cli.Variables;

public static class ProjectVariableResolver
{
    public static Task ResolveProjectVariables(this IDictionary<string, object?> variables, NoxSolution config)
    {
        var solutionKeys = variables
            //.Where(pk => pk.Value == null)
            .Select(pk => pk.Key)
            .Where(pk => pk.StartsWith("solution.", StringComparison.OrdinalIgnoreCase))
            .Select(pk => pk[9..])
            .ToArray();

        config.WalkProperties( (name, value) => { if (solutionKeys.Contains(name, StringComparer.OrdinalIgnoreCase)) { variables[$"solution.{name}"] = value; } });

        return Task.CompletedTask;
    }
}