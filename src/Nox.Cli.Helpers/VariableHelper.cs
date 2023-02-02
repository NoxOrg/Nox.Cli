namespace Nox.Cli.Server.Helpers;

public static class VariableHelper
{
    public static void CopyVariables(IDictionary<string, object> source, IDictionary<string, object> destination)
    {
        foreach (var sourceVar in source)
        {
            var key = sourceVar.Key;
            //If not in destination then add it
            if (!destination.ContainsKey(key))
            {
                destination.Add(key, sourceVar.Value);
            }
            else //If in destination but source has a value then replace it
            {
                var destVal = destination[key];
                if (!sourceVar.Value.ToString().Contains("{{") &&  sourceVar.Value != destVal)
                {
                    destination.Remove(key);
                    destination.Add(sourceVar);
                }
            }
            
            
        }
    }

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
}