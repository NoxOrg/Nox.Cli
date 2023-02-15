using System.ComponentModel;

namespace Nox.Cli.Abstractions.Extensions;

public static class CliAddinExtensions
{
    public static T? Value<T>(this IDictionary<string, IVariable> inputs, string key)
    {
        var result = default(T);
        if (!inputs.ContainsKey(key)) return result;
        var value = inputs[key].Value;
        if (value == null) return result;
        try
        {
            if (typeof(T).IsClass)
            {
                if (typeof(T).IsAssignableTo(typeof(IDictionary<string, string>)))
                {
                    var newDictionary = ((IDictionary<object, object>)value).ToDictionary(
                        kv => kv.Key.ToString()!, kv => kv.Value.ToString()!, StringComparer.OrdinalIgnoreCase
                    );
                    result = (T)Convert.ChangeType(newDictionary, typeof(T));
                }
                else if (typeof(T).IsAssignableTo(typeof(IDictionary<string, object>)))
                {
                    var newDictionary = ((IDictionary<object, object>)value).ToDictionary( 
                        kv => kv.Key.ToString()!, kv => kv.Value, StringComparer.OrdinalIgnoreCase
                    );
                    result = (T)Convert.ChangeType(newDictionary, typeof(T));
                }
                else if (typeof(T).IsAssignableTo(typeof(IList<string>)))
                {
                    var stringList = ((IList<object>)value).Select(o=>o.ToString()).ToArray();
                    result = (T)Convert.ChangeType(stringList, typeof(T));
                }
                else
                {
                    result = (T)Convert.ChangeType(value, typeof(T));
                }
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
    
    public static T? ValueOrDefault<T>(this IDictionary<string, IVariable> inputs, string key, INoxCliAddin addin)
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
        return inputs.Value<T>(key);
    }
}