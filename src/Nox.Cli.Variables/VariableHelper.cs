using System.Text.Json;
using Nox.Cli.Abstractions;

namespace Nox.Cli.Variables;

public static class VariableHelper
{
    public static void UpdateVariables(IDictionary<string, IVariable> source, IDictionary<string, IVariable> destination)
    {
        foreach (var sourceVar in source)
        {
            var key = sourceVar.Key;
            //If not in destination then add it
            if (!destination.ContainsKey(key))
            {
                destination.Add(key, new Variable(sourceVar.Value.Value, sourceVar.Value.IsSecret));
            }
            else //If in destination but source has a value then replace it
            {
                var destVal = destination[key];
                if (!sourceVar.Value!.ToString()!.Contains("{{") &&  sourceVar.Value != destVal)
                {
                    destVal.Value = sourceVar.Value;
                }
            }
        }
    }
    
    public static IDictionary<string, Variable>? ExtractSimpleVariables(IDictionary<string, IVariable>? source)
    {
        if (source == null) return null;
        var result = new Dictionary<string, Variable>();
        foreach (var sourceVar in source)
        {
            var key = sourceVar.Key;
            if (sourceVar.Value.Value != null && sourceVar.Value.Value.GetType().IsSimpleType())
            {
                result.Add(key, (Variable)sourceVar.Value);
            }
        }
        return result;
    }

    public static IDictionary<string, Variable> ToConcreteVariables(this IDictionary<string, IVariable> source)
    {
        return source.ToDictionary(item => item.Key, item => new Variable(item.Value.Value, item.Value.IsSecret));
    }
    
    public static IDictionary<string, IVariable> ParseJsonInputs(IDictionary<string, Variable> source)
    {
        var result = new Dictionary<string, IVariable>();
        foreach (var item in source)
        {
            if (item.Value.Value != null)
            {
                if (item.Value.Value is JsonElement element)
                {
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.False:
                        case JsonValueKind.True:
                            result.Add(item.Key, new Variable(element.GetBoolean(), item.Value.IsSecret));
                            break;
                        case JsonValueKind.Array:
                            result.Add(item.Key, new Variable(element.EnumerateArray(), item.Value.IsSecret));
                            break;
                        case JsonValueKind.Null:
                        case JsonValueKind.Object:
                        case JsonValueKind.Undefined:
                            break;
                        case JsonValueKind.Number:
                            if (element.TryGetInt32(out var intVal))
                            {
                                result.Add(item.Key, new Variable(intVal, item.Value.IsSecret));
                            } else if (element.TryGetDouble(out var dblVal))
                            {
                                result.Add(item.Key, new Variable(dblVal, item.Value.IsSecret));
                            }
                            break;
                        default:
                            result.Add(item.Key, new Variable(element.GetString(), item.Value.IsSecret));
                            break;
                    }    
                }
                else
                {
                    result.Add(item.Key, item.Value);
                }
            }
            else
            {
                result.Add(item.Key, null!);
            }
            
        }

        return result;
    }
}