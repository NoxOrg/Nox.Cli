using Spectre.Console.Cli;

namespace Nox.Cli.Variables;

public static class ArgumentVariableResolver
{
    public static void ResolveNoxArgumentVariables(this IDictionary<string, object?> variables, IRemainingArguments arguments)
    {
        var keys = variables
            .Where(kv => kv.Value == null)
            .Select(kv => kv.Key)
            .Where(e => e.StartsWith("args.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[5..])
            .ToArray();

        
        
        foreach (var key in keys)
        {
            var value = ResolveArgumentValue(key, arguments);
            variables[$"args.{key}"] = value;
        }
    }
    
    private static bool ResolveArgumentValue(string argument, IRemainingArguments arguments)
    {
        return arguments.Parsed.Any(a => a.Key == "--" + argument);
    }
}