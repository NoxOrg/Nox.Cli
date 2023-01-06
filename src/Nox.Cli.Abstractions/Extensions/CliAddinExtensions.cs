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

    public static T? Value<T>(this IDictionary<string, object> inputs, string key)
    {
        var result = default(T);
        if (!inputs.ContainsKey(key)) return result;
        var value = inputs[key];
        try
        {
            result = (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            //Ignore exception result is already default(T);
        }
        return result;
    }
    
    public static T ValueOrDefault<T>(this IDictionary<string, object> inputs, string key, INoxCliAddin addin)
    {
        var result = default(T);
        //Set the default
        var metaValue = addin.Discover().Inputs[key].Default;
        if (metaValue != null)
        {
            result = (T)Convert.ChangeType(metaValue, typeof(T));
        }
        
        if (!inputs.ContainsKey(key)) return result;
        var value = inputs[key];
        try
        {
            result = (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            //Ignore exception result is already defaulted;
        }
        return result;
    }
}