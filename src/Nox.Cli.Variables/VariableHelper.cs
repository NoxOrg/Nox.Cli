using System.Text.Json;

namespace Nox.Cli.Variables;

public static class VariableHelper
{
    public static IDictionary<string, object>? ExtractSimpleVariables(IDictionary<string, object>? source)
    {
        if (source == null) return null;
        var result = new Dictionary<string, object>();
        foreach (var sourceVar in source)
        {
            var key = sourceVar.Key;
            if (sourceVar.Value.GetType().IsSimpleType())
            {
                result.Add(key, sourceVar.Value);
            }
        }
        return result;
    }
    
    public static object GetJsonElementValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.False:
            case JsonValueKind.True:
                return element.GetBoolean();
            case JsonValueKind.Array:
                return element.EnumerateArray();
            case JsonValueKind.Null:
                return null!;
            case JsonValueKind.Object:
                return element;
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intVal))
                {
                    return element.GetInt32();    
                }

                return element.GetDouble();
            case JsonValueKind.Undefined:
            case JsonValueKind.String:
            default:
                return element!.GetString()!;
        }   
    }
    
}