using System.Collections;
using System.ComponentModel;

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
                if (typeof(T).IsAssignableTo(typeof(IDictionary<string, string>)))
                {
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<string, string>)) || value.GetType().IsAssignableTo(typeof(Dictionary<string, object>)))
                    {
                        result = (T)Convert.ChangeType((T)value, typeof(T));
                    }
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<object, object>)))
                    {
                        var newDict = new Dictionary<string, string>();
                        foreach (var item in (IDictionary<object, object>)value)
                        {
                            newDict.Add(item.Key.ToString()!, item.Value.ToString()!);
                        }
                        result = (T)Convert.ChangeType(newDict, typeof(T));
                    }
                }
                else if (typeof(T).IsAssignableTo(typeof(IDictionary<string, object>)))
                {
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<string, string>)) || value.GetType().IsAssignableTo(typeof(Dictionary<string, object>)))
                    {
                        result = (T)Convert.ChangeType((T)value, typeof(T));
                        
                    }
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<object, object>)))
                    {
                        var newDict = new Dictionary<string, object>();
                        foreach (var item in (IDictionary<object, object>)value)
                        {
                            newDict.Add(item.Key.ToString()!, item.Value);
                        }
                        result = (T)Convert.ChangeType(newDict, typeof(T));
                    }
                }
                else if (typeof(T).IsAssignableTo(typeof(IDictionary<string, int>)))
                {
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<string, int>)) || value.GetType().IsAssignableTo(typeof(Dictionary<string, object>)))
                    {
                        result = (T)Convert.ChangeType((T)value, typeof(T));
                        
                    }
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<object, object>)))
                    {
                        var newDict = new Dictionary<string, int>();
                        foreach (var item in (IDictionary<object, object>)value)
                        {
                            newDict.Add(item.Key.ToString()!, (int)item.Value);
                        }
                        result = (T)Convert.ChangeType(newDict, typeof(T));
                    }
                }
                else if (typeof(T).IsAssignableTo(typeof(IDictionary<string, double>)))
                {
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<string, double>)) || value.GetType().IsAssignableTo(typeof(Dictionary<string, object>)))
                    {
                        result = (T)Convert.ChangeType((T)value, typeof(T));
                        
                    }
                    if (value.GetType().IsAssignableTo(typeof(Dictionary<object, object>)))
                    {
                        var newDict = new Dictionary<string, double>();
                        foreach (var item in (IDictionary<object, object>)value)
                        {
                            newDict.Add(item.Key.ToString()!, (double)item.Value);
                        }
                        result = (T)Convert.ChangeType(newDict, typeof(T));
                    }
                }
                else if (typeof(T).IsAssignableTo(typeof(string[])))
                {
                    var sourceList = (List<object>)value;
                    var stringArray = sourceList.ConvertAll(obj => obj.ToString()).ToArray();
                    result = (T)Convert.ChangeType(stringArray, typeof(T));
                }
                else if (typeof(T).IsAssignableTo(typeof(IList<string>)))
                {
                    if (value.GetType().IsAssignableTo(typeof(List<string>)))
                    {
                        // var sourceList = (List<string>)value;
                        // var stringArray = sourceList.ConvertAll(obj => obj.ToString());
                        result = (T)Convert.ChangeType(value, typeof(T));
                    } else if (value.GetType().IsAssignableTo(typeof(List<object>)))
                    {
                        var sourceList = (List<object>)value;
                        var stringArray = sourceList.ConvertAll(obj => obj.ToString());
                        result = (T)Convert.ChangeType(stringArray, typeof(T));
                    }
                    
                }
                else if (typeof(T).IsAssignableTo(typeof(IList<object>)))
                {
                    var objList = (value as IEnumerable)!.Cast<object>().ToList();
                    result = (T)Convert.ChangeType(objList, typeof(T));
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
        return inputs.Value<T>(key);
    }
}