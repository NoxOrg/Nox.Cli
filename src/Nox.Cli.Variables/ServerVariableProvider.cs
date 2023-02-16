using System.Text.Json;
using System.Text.RegularExpressions;
using Hangfire.Dashboard.Resources;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Secrets;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Variables;

public class ServerVariableProvider
{
    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IManifestConfiguration _manifest;
    private readonly List<ServerVariable> _variables;
    
    public ServerVariableProvider(IManifestConfiguration manifest)
    {
        _variables = new List<ServerVariable>();
        _manifest = manifest;
        if (_manifest.RemoteTaskExecutor == null) throw new Exception("Remote Task Executor has not been configured in Manifest!");
    }

    public void SaveOutputs(string actionId, IDictionary<string, object> values)
    {
        foreach (var value in values)
        {
            var varName = $"steps.{actionId}.outputs.{value.Key}";
            SetVariable(varName, value.Key, value.Value);
        }
    }

    public IDictionary<string, object> ResolveInputs(INoxAction action)
    {
        var tempVars = new Dictionary<string, object?>();
        foreach (var (_, item) in action.Inputs)
        {
            var varName = $"steps.{action.Id}.inputs.{item.Id}";
            if (item.Default is JsonElement element)
            {
                var jsonValue = VariableHelper.GetJsonElementValue(element);
                tempVars.Add(varName, jsonValue);
                SetVariable(varName, item.Id, jsonValue);
            }
            else
            {
                tempVars.Add(varName, item.Default);
                SetVariable(varName, item.Id, item.Default);
            }
        }

        ResolveVariables();
        var result = new Dictionary<string, object>();
        foreach (var item in action.Inputs)
        {
            var varName = $"steps.{action.Id}.inputs.{item.Key}";
            var lookupValue = LookupValue(varName);
            if (lookupValue != null) result.Add(item.Key, lookupValue);
        }
        return result;
    }
    
    private void SetVariable(string fullname, string shortName, object? value)
    {
        var variable = _variables.SingleOrDefault(v => string.Equals(v.FullName, fullname, StringComparison.OrdinalIgnoreCase));
        if (variable == null)
        {
            _variables.Add(new ServerVariable
            {
                ShortName = shortName,
                FullName = fullname,
                Value = value
            });
        }
        else
        {
            variable.Value = value;
        }
    }
    
    private void ResolveVariables()
    {
        _variables.ResolveServerSecrets(_manifest.RemoteTaskExecutor!);
        //TODO resolve runner variables
        ResolveServerVariables();
    }
    
    public IDictionary<string, object?> GetUnresolvedVariables()
    {
        var unresolvedVars = _variables
            .Where(i => _variableRegex.Match(i.Value?.ToString()!).Success)
            .ToDictionary(i => i.FullName, i => i.Value, StringComparer.OrdinalIgnoreCase);

        return unresolvedVars;
    }
    
    private object? LookupValue(string key)
    {
        var variable = _variables.SingleOrDefault(v => string.Equals(v.FullName, key, StringComparison.CurrentCultureIgnoreCase));
        return variable?.Value;
    }

    private void ResolveServerVariables()
    {
        foreach (var item in _variables)
        {
            if (item.Value != null)
            {
                var match = _variableRegex.Match(item.Value.ToString()!);
                if (match.Success)
                {
                    //Find the variable value
                    var lookupValue = LookupValue(match.Groups[1].Value);
                    if (lookupValue != null) item.Value = lookupValue;
                }
            }
        }
    }
}