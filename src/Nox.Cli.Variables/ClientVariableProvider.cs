using System.Text.Json;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Secrets;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Variables;

public class ClientVariableProvider: IClientVariableProvider
{
    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    private readonly Dictionary<string, object?> _variables;
    private readonly IOrgSecretResolver _orgSecretResolver;
    private Solution.Solution? _projectConfig;
    private readonly INoxCliCache? _cache;
    private readonly ILocalTaskExecutorConfiguration? _lteConfig;
    
    
    public ClientVariableProvider(
        IWorkflowConfiguration workflow, 
        IOrgSecretResolver orgSecretResolver,
        Solution.Solution? projectConfig = null,
        ILocalTaskExecutorConfiguration? lteConfig = null,
        INoxCliCache? cache = null)
    {
        _variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _orgSecretResolver = orgSecretResolver;
        _projectConfig = projectConfig;
        _lteConfig = lteConfig;
        _cache = cache;
        Initialize(workflow);
    }

    public void SetVariable(string key, object value)
    {
        if (_variables.ContainsKey(key))
        {
            _variables[$"{key}"] = value;    
        }
        else
        {
            _variables.Add(key, value);
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
                    _variables[varKey] = VariableHelper.GetJsonElementValue(element);
                }
                else
                {
                    _variables[varKey] = output.Value;    
                }
                
            }
        }

        ResolveAllVariables(action);
    }

    public void SetProjectConfiguration(Solution.Solution projectConfig)
    {
        _projectConfig = projectConfig;
    }

    public async Task ResolveAll()
    {
        _variables.ResolveRunnerVariables();
        await ResolveForServer();
    }

    public async Task ResolveForServer()
    {
        if (_lteConfig != null)
        {
            await _orgSecretResolver.Resolve(_variables, _lteConfig);
        }

        await ResolveProjectVariables();

        await _variables.ResolveEnvironmentVariables();
        _variables.ResolveNoxCacheVariables(_cache);
    }

    public async Task ResolveProjectVariables()
    {
        if (_projectConfig != null)
        {
            await _variables.ResolveProjectVariables(_projectConfig);    
        }
    }

    private void Initialize(IWorkflowConfiguration workflow)
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
            _variables.Add(v, null);
        }
    }
    
    private object ReplaceVariable(string value)
    {
        object result = value;

        var match = _variableRegex.Match(result.ToString()!);

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

            match = _variableRegex.Match(result.ToString()!);
        }

        return result;
    }
    
    private object? LookupValue(string variable)
    {
        if (_variables.ContainsKey(variable))
        {
            var lookupVar = _variables[variable];
            if (lookupVar == null) return null;
            return _variables[variable];
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
            action.Display.Success = ReplaceVariable(action.Display.Success).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.Display?.Error))
        {
            action.Display.Error = ReplaceVariable(action.Display.Error).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.Display?.IfCondition))
        {
            action.Display.IfCondition = ReplaceVariable(action.Display.IfCondition).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.If))
        {
            action.If = ReplaceVariable(action.If).ToString()!;
        }
    }

    
}