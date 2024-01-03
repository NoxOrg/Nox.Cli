using System.Text.Json;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Variables.Secrets;
using Nox.Solution;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Variables;

public class ClientVariableProvider: IClientVariableProvider
{
    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>\b(vars|solution|steps|server|env|runner|cache)[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    private readonly Dictionary<string, object?> _variables;
    private readonly IOrgSecretResolver _orgSecretResolver;
    private NoxSolution? _projectConfig;
    private readonly INoxCliCache? _cache;
    private readonly ILocalTaskExecutorConfiguration? _lteConfig;
    
    
    public ClientVariableProvider(
        IWorkflowConfiguration workflow, 
        IOrgSecretResolver orgSecretResolver,
        NoxSolution? projectConfig = null,
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
        var unresolvedVars = new Dictionary<string, object>();

        foreach (var input in action.Inputs)
        {
            if (input.Value.Default is string unresolvedString)
            {
                if (_variableRegex.Match(unresolvedString).Success)
                {
                    unresolvedVars.TryAdd(input.Key, unresolvedString);
                }
            } else if (input.Value.Default is Dictionary<object, object> unresolvedObjectDictionary)
            {
                foreach (var objDictItem in unresolvedObjectDictionary)
                {
                    if (_variableRegex.Match(objDictItem.Value.ToString()!).Success)
                    {
                        unresolvedVars.TryAdd(input.Key, objDictItem.Value.ToString()!);
                    }    
                }
            } else if (input.Value.Default is Dictionary<string, string> unresolvedStringDictionary)
            {
                foreach (var strDictItem in unresolvedStringDictionary)
                {
                    if (_variableRegex.Match(strDictItem.Value).Success)
                    {
                        unresolvedVars.TryAdd(input.Key, strDictItem.Value);
                    }    
                }
            } 
            else if (input.Value.Default is List<string> unresolvedStringList)
            {
                foreach (var stringItem in unresolvedStringList)
                {
                    if (_variableRegex.Match(stringItem).Success)
                    {
                        unresolvedVars.TryAdd(input.Key, stringItem);
                    }    
                }
            }
        }
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

    public void SetProjectConfiguration(NoxSolution projectConfig)
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

    public void ResolveJobVariables(INoxJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.Display?.Success))
        {
            job.Display.Success = ReplaceVariable(job.Display.Success).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(job.Display?.IfCondition))
        {
            job.Display.IfCondition = ReplaceVariable(job.Display.IfCondition).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(job.If))
        {
            job.If = ReplaceVariable(job.If, true).ToString()!;
        }

        if (job.ForEach != null && !string.IsNullOrWhiteSpace(job.ForEach.ToString()))
        {
            job.ForEach = ReplaceVariable(job.ForEach.ToString()!);
        }
    }

    private void Initialize(IWorkflowConfiguration workflow)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var workflowString = serializer.Serialize(workflow);

        var matches = _variableRegex.Matches(workflowString);

        var variablesTemp = matches.Select(m => m.Groups[2].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e);

        foreach (var v in variablesTemp)
        {
            _variables.Add(v, null);
        }
    }
    
    private object ReplaceVariable(object value, bool isIfCondition = false)
    {
        var result = value;

        var match = _variableRegex.Match(result.ToString()!);

        while (match.Success)
        {
            var fullPhrase = match.Groups[0].Value;

            var variable = match.Groups["variable"].Value;

            var resolvedValue = LookupValue(variable);

            if (resolvedValue?.GetType() == typeof(object))
            {
                break;
            }

            if (resolvedValue != null)
            {
                if (resolvedValue.GetType().IsSimpleType())
                {
                    result = result.ToString()!.Replace(fullPhrase, resolvedValue.ToString());
                }
                else
                {
                    if (value.ToString() == fullPhrase)
                    {
                        result = resolvedValue;
                        break;
                    }

                    result = result.ToString()!.Replace(fullPhrase, "NOT-NULL");
                }
            }
            else
            {
                if (value.ToString() == fullPhrase || !isIfCondition)
                {
                    break;
                }
                result = result.ToString()!.Replace(fullPhrase, "NULL");
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
            else if (input.Default is List<object> inputObjectList)
            {
                for (var i = 0; i < inputObjectList.Count; i++)
                {
                    if (inputObjectList[i] is string)
                    {
                        var index = inputObjectList.FindIndex(n => n.Equals(inputObjectList[i]));
                        inputObjectList[index] = ReplaceVariable((string)inputObjectList[i]);
                    }
                }
            }
            else if (input.Default is List<string> inputStringList)
            {
                for (var i = 0; i < inputStringList.Count; i++)
                {
                    var index = inputStringList.FindIndex(n => n.Equals(inputStringList[i]));
                    inputStringList[index] = ReplaceVariable(inputStringList[i]).ToString()!;
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
            action.If = ReplaceVariable(action.If, true).ToString()!;
        }
    }

    
}