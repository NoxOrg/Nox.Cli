using System.Text.Json;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;
using Nox.Cli.Variables.Secrets;

namespace Nox.Cli.Variables;

public class ServerVariableProvider: IServerVariableProvider
{
    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>\b(vars|solution|steps|server|env|runner)\b[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    private readonly IManifestConfiguration _manifest;
    private readonly List<ServerVariable> _variables;
    private readonly IServerSecretResolver? _secretResolver;
    
    
    public ServerVariableProvider(
        IManifestConfiguration manifest,
        IServerSecretResolver? secretResolver = null)
    {
        _variables = new List<ServerVariable>();
        _manifest = manifest;
        _secretResolver = secretResolver;
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
    
    public IDictionary<string, object?> GetUnresolvedVariables()
    {
        var unresolvedVars = new Dictionary<string, object?>();

        foreach (var variable in _variables)
        {
            if (variable.Value is string unresolvedString)
            {
                if (_variableRegex.Match(unresolvedString).Success)
                {
                    unresolvedVars.TryAdd(variable.ShortName, unresolvedString);
                }
            } else if (variable.Value is Dictionary<object, object> unresolvedObjectDictionary)
            {
                foreach (var objDictItem in unresolvedObjectDictionary)
                {
                    if (_variableRegex.Match(objDictItem.Value.ToString()!).Success)
                    {
                        unresolvedVars.TryAdd(variable.ShortName, objDictItem.Value.ToString()!);
                    }    
                }
            } else if (variable.Value is Dictionary<string, string> unresolvedStringDictionary)
            {
                foreach (var strDictItem in unresolvedStringDictionary)
                {
                    if (_variableRegex.Match(strDictItem.Value).Success)
                    {
                        unresolvedVars.TryAdd(variable.ShortName, strDictItem.Value);
                    }    
                }
            } 
            else if (variable.Value is List<string> unresolvedStringList)
            {
                foreach (var stringItem in unresolvedStringList)
                {
                    if (_variableRegex.Match(stringItem).Success)
                    {
                        unresolvedVars.TryAdd(variable.ShortName, stringItem);
                    }    
                }
            }
        }
        return unresolvedVars;
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
        //Resolve runner variables
        _variables.ResolveRunnerVariables();
        _secretResolver?.ResolveAsync(_variables, _manifest.RemoteTaskExecutor!).Wait();
        ResolveServerVariables();
        
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
                    var lookupValue = LookupValue(match.Groups[2].Value);
                    if (lookupValue != null)
                    {
                        if (lookupValue is string)
                        {
                            item.Value = item.Value.ToString()!.Replace(match.Value, lookupValue.ToString());
                        }
                        else
                        {
                            item.Value = lookupValue;
                        }
                    }
                }
            }
        }
    }
}