using System.ComponentModel;
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
            if (typeof(T).IsClass)
            {
                result = (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                result = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value.ToString()!)!;    
            }
            
        }
        catch
        {
            //Ignore exception result is already default(T);
        }
        return result;
    }
    
    public static T? ValueOrDefault<T>(this IDictionary<string, object> inputs, string key, INoxCliAddin addin)
    {
        var result = default(T);
        //Set the default
        var meta = addin.Discover();
        if (meta.Inputs.ContainsKey(key))
        {
            var metaValue = meta.Inputs[key];
            if (metaValue != null)
            {
                result = (T)Convert.ChangeType(metaValue.Default, typeof(T));
            }    
        }
        
        if (!inputs.ContainsKey(key)) return result;
        var value = inputs[key];
        try
        {
            result = (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            //Ignore exception result is already defaulted;
        }
        return result;
    }
}