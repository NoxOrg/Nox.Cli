namespace Nox.Cli.Variables;

public static class EnvironmentVariableResolver
{
    public static void ResolveEnvironmentVariables(this IDictionary<string, object?> variables)
    {
        var envKeys = variables
            .Where(kv => kv.Value == null)
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("env.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[4..])
            .ToArray();
        foreach (var envKey in envKeys)
        {
            var value = Environment.GetEnvironmentVariable(envKey.ToUpper());
            if (value != null) variables[$"env.{envKey}"] = value;
        }
    }
}