using Nox.Core.Interfaces.Configuration;

namespace Nox.Cli.Variables;

public static class ProjectVariableResolver
{
    public static Task ResolveProjectVariables(this IDictionary<string, object?> variables, IProjectConfiguration config)
    {
        var projectKeys = variables
            .Where(kv => kv.Value == null)
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("project.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[8..])
            .ToArray();

        config.WalkProperties( (name, value) => { if (projectKeys.Contains(name, StringComparer.OrdinalIgnoreCase)) { variables[$"project.{name}"] = value; } });

        return Task.CompletedTask;
    }
}