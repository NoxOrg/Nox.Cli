using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Core.Configuration;
using Nox.Core.Interfaces.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Variables;

public class VariableProvider: IVariableProvider
{
    private readonly Regex _qualifiedVariableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _variableRegex = new(@"\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>secrets.[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly INoxConfiguration _noxConfig;
    private readonly IConfiguration _appConfig;
    private IDictionary<string, object>? _variables;
    
    public VariableProvider(
        INoxConfiguration noxConfig,
        IConfiguration appConfig)
    {
        _noxConfig = noxConfig;
        _appConfig = appConfig;
    }

    public IDictionary<string, object> Variables => _variables;
    
    public void Initialize(IWorkflowConfiguration workflow)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var workflowString = serializer.Serialize(workflow);

        var matches = _qualifiedVariableRegex.Matches(workflowString);

        var variablesTemp = matches.Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e);
            //.ToDictionary(e => e, e , StringComparer.OrdinalIgnoreCase);

        _variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in variablesTemp)
        {
            _variables.Add(v, null);
        }
            
        var projectKeys = _variables.Select(kv => kv.Key)
            .Where(e => e.StartsWith("project.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[8..])
            .ToArray();

        _noxConfig.WalkProperties( (name, value) => { if (projectKeys.Contains(name, StringComparer.OrdinalIgnoreCase)) { _variables[$"project.{name}"] = value; } });

        var secretKeys = _variables
            .Where(kv => kv.Value != null)
            .Select(kv => kv.Key)
            .Select(e => e[7..])
            .ToArray();

        var secrets = ConfigurationHelper.GetNoxSecrets(_appConfig, secretKeys).Result;

        if (secrets is null) return;

        foreach (var kv in secrets)
        {
            _variables[$"secrets.{kv.Key}"] = kv.Value;
        }

        
        // var userKeys = variables.Select(kv => kv.Key)
        //     .Where(e => e.StartsWith("user.", StringComparison.OrdinalIgnoreCase))
        //     .Select(e => e[5..])
        //     .ToArray();

        // var cache = NoxCliCache.Load(ConfiguratorExtensions.CacheFile);
        // cache.WalkProperties((name, value) =>
        // {
        //     if (userKeys.Contains(name, StringComparer.OrdinalIgnoreCase))
        //     {
        //         variables[$"user.{name}"] = value;
        //     }
        // });
    }
    
    public void SetVariable(string key, object value)
    {
        _variables[$"vars.{key}"] = value;
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
                    _variables[varKey] = GetJsonElementValue(element);
                }
                else
                {
                    _variables[varKey] = output.Value;    
                }
                
            }
        }

        ResolveAllVariables(action);
    }
    
    private object ReplaceVariable(string value)
    {
        object result = value;

        var match = _qualifiedVariableRegex.Match(result.ToString()!);

        while (match.Success)
        {
            var fullPhrase = match.Groups[0].Value;

            var variable = match.Groups["variable"].Value;

            var resolvedValue = LookupValue(variable);

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

            match = _qualifiedVariableRegex.Match(result.ToString()!);
        }

        return result;
    }
    
    public object LookupValue(string variable)
    {
        if (_variables.ContainsKey(variable))
        {
            if (_variables[variable] != null)
            {
                return _variables[variable];
            }
        }
        return $"{{{{ {variable} }}}}";
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
                    if (inputValueList[i] is string inputValueArrayString)
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
            action.Display.Success = ReplaceVariable(action.Display.Success).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.Display?.Error))
        {
            action.Display.Error = ReplaceVariable(action.Display.Error).ToString()!;
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