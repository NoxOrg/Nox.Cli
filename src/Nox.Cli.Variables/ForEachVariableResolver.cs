namespace Nox.Cli.Variables;

public static class ForEachVariableResolver
{
    public static void ResolveForEachVariables(this IDictionary<string, object?> variables, object forEachObject)
    {
        var forEachKeys = variables
            .Select(pk => pk.Key)
            .Where(pk => pk.StartsWith("forEach.", StringComparison.OrdinalIgnoreCase))
            .Select(pk => pk[8..])
            .ToArray();

        forEachObject.WalkProperties( (name, value) => { if (forEachKeys.Contains(name, StringComparer.OrdinalIgnoreCase)) { variables[$"forEach.{name}"] = value; } });
    }
}