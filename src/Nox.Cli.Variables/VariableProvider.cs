using System.Text.Json;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Secrets;
using Nox.Core.Interfaces.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Variables;

public class VariableProvider: IVariableProvider
{
    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, IVariable> _variables;
    
    public VariableProvider(
        IProjectConfiguration projectConfig,
        IWorkflowConfiguration workflow,
        ILocalTaskExecutorConfiguration? lteConfig = null)
    {
        _variables = new Dictionary<string, IVariable>(StringComparer.OrdinalIgnoreCase);
        Initialize(projectConfig, workflow, lteConfig);
    }

    private void Initialize(IProjectConfiguration projectConfig, IWorkflowConfiguration workflow, ILocalTaskExecutorConfiguration? lteConfig)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var workflowString = serializer.Serialize(workflow);

        var matches = _variableRegex.Matches(workflowString);

        var variablesTemp = matches.Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e);

        foreach (var v in variablesTemp)
        {
            _variables.Add(v, new Variable(null));
        }
        
        _variables.ResolveOrgSecrets(lteConfig);
        _variables.ResolveProjectSecrets(projectConfig);
        _variables.ResolveProjectVariables(projectConfig);
    }
    
    public void SetVariable(string key, object value)
    {
        if (_variables.ContainsKey(key))
        {
            _variables[$"{key}"].Value = value;    
        }
        else
        {
            _variables.Add(key, new Variable(value));
        }
        
    }

    public void SetActionVariable(INoxAction action, string key, object value)
    {
        SetVariable(key, value);
        ResolveAllVariables(action);
    }

    public IDictionary<string, object> GetInputVariables(INoxAction action)
    {
        ResolveAllVariables(action);

        return action.Inputs.ToDictionary(i => i.Key, i => i.Value.Default, StringComparer.OrdinalIgnoreCase);
    }
    
    public IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action)
    {
        var unresolvedVars = action.Inputs
            .Where(i => _variableRegex.Match(i.Value.Default.ToString()!).Success)
            .ToDictionary(i => i.Key, i => i.Value.Default, StringComparer.OrdinalIgnoreCase);

        return unresolvedVars;
    }

    public void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs)
    {
        foreach (var output in outputs)
        {
            var varKey = $"steps.{action.Id}.outputs.{output.Key}";
            if (_variables.ContainsKey(varKey))
            {
                if (output.Value is JsonElement element)
                {
                    _variables[varKey].Value = GetJsonElementValue(element);
                }
                else
                {
                    _variables[varKey].Value = output.Value;    
                }
                
            }
        }

        ResolveAllVariables(action);
    }
    
    private object ReplaceVariable(string value, bool obfuscate = false)
    {
        object result = value;

        var match = _variableRegex.Match(result.ToString()!);

        while (match.Success)
        {
            var fullPhrase = match.Groups[0].Value;

            var variable = match.Groups["variable"].Value;

            var resolvedValue = LookupValue(variable, obfuscate);

            if (resolvedValue == null || resolvedValue.GetType() == typeof(object))
            {
                break;
            }
            else if (resolvedValue.GetType().IsSimpleType())
            {
                result = result.ToString()!.Replace(fullPhrase, resolvedValue.ToString());
            }
            else
            {
                result = resolvedValue;
                break;
            }

            match = _variableRegex.Match(result.ToString()!);
        }

        return result;
    }
    
    public object? LookupValue(string variable, bool obfuscate = false)
    {
        if (_variables.ContainsKey(variable))
        {
            var lookupVar = _variables[variable];
            if (lookupVar.Value == null) return null;
            return obfuscate ? _variables[variable].DisplayValue : _variables[variable].Value;
        }
        return null;
    }
    
    private void ResolveAllVariables(INoxAction action)
    {
        foreach (var (_, input) in action.Inputs)
        {
            if (input.Default is string inputValueString)
            {
                input.Default = ReplaceVariable(inputValueString);
            }
            else if (input.Default is List<object> inputValueList)
            {
                for (var i = 0; i < inputValueList.Count; i++)
                {
                    if (inputValueList[i] is string)
                    {
                        var index = inputValueList.FindIndex(n => n.Equals(inputValueList[i]));
                        inputValueList[index] = ReplaceVariable((string)inputValueList[i]);
                    }
                }
            } 
            else if (input.Default is Dictionary<object, object> inputValueDictionary)
            {
                for (var i = 0; i < inputValueDictionary.Count; i++)
                {
                    var item = inputValueDictionary.ElementAt(i);
                    
                    if (item.Value is string itemValueString)
                    {
                        inputValueDictionary[item.Key] = ReplaceVariable(itemValueString);
                    }
                }
            }
        }

        if (action.Validate != null)
        {
            foreach(var (key, value) in action.Validate)
            {
                action.Validate[key] = ReplaceVariable(value).ToString()!;
            }
        }

        if (!string.IsNullOrWhiteSpace(action.Display?.Success))
        {
            action.Display.Success = ReplaceVariable(action.Display.Success, true).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.Display?.Error))
        {
            action.Display.Error = ReplaceVariable(action.Display.Error, true).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.If))
        {
            action.If = ReplaceVariable(action.If).ToString()!;
        }
    }

    private object GetJsonElementValue(JsonElement element)
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
                return element.GetDouble();
            case JsonValueKind.Undefined:
            case JsonValueKind.String:
            default:
                return element!.GetString()!;
        }   
    }
}