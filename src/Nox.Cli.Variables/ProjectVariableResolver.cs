using Nox.Core.Interfaces;
using Nox.Core.Interfaces.Configuration;

namespace Nox.Cli.Variables;

public static class ProjectVariableResolver
{
    public static Task ResolveProjectVariables(this IDictionary<string, object?> variables, IProjectConfiguration config)
    {
        var projectKeys = variables
            //.Where(pk => pk.Value == null)
            .Select(pk => pk.Key)
            .Where(pk => pk.StartsWith("project.", StringComparison.OrdinalIgnoreCase))
            .Select(pk => pk[8..])
            .ToArray();

        config.WalkProperties( (name, value) => { if (projectKeys.Contains(name, StringComparer.OrdinalIgnoreCase)) { variables[$"project.{name}"] = value; } });

        return Task.CompletedTask;
    }
}