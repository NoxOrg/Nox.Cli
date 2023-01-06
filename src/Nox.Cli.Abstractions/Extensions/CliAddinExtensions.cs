using Nox.Cli.Actions;

namespace Nox.Cli.Abstractions.Extensions;

public static class CliAddinExtensions
{
    public static void ApplyDefaults(this IDictionary<string, object> inputs, INoxCliAddin addin)
    {
        var metadata = addin.Discover();
        foreach (var metaInput in metadata.Inputs)
        {
            inputs[metaInput.Key] = metaInput.Value.Default;
        }
    }

    public static object DefaultValue(this INoxCliAddin addin, string key)
    {
        var metadata = addin.Discover();
        return metadata.Inputs[key];
    }
}