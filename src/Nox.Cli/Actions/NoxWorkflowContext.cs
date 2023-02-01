﻿using Microsoft.Extensions.Configuration;
using Nox.Cli.Configuration;
using Nox.Cli.Services.Caching;
using Nox.Core.Configuration;
using Nox.Core.Interfaces.Configuration;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Helpers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Actions;

public class NoxWorkflowContext : INoxWorkflowContext
{
    private readonly IConfiguration _appConfig;
    private readonly INoxConfiguration _noxConfig;
    private readonly WorkflowConfiguration _workflow;
    private readonly IDictionary<string, INoxAction> _steps;
    private readonly IDictionary<string, object> _vars;

    private int _currentActionSequence = 0;

    private INoxAction? _previousAction;
    private INoxAction? _currentAction;
    private INoxAction? _nextAction;

    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>secrets.[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public INoxAction? CurrentAction => _currentAction;

    public NoxWorkflowContext(WorkflowConfiguration workflow, INoxConfiguration noxConfig, IConfiguration appConfig)
    {
        WorkflowId = Guid.NewGuid();
        _appConfig = appConfig;
        _noxConfig = noxConfig;
        _workflow = workflow;
        _vars = InitializeVariables();
        _steps = ParseSteps();
        ValidateSteps();
        _currentActionSequence = 0;
        NextStep();
    }

    public void NextStep()
    {
        _currentActionSequence++;
        _previousAction = _currentAction;
        _currentAction = _steps.Select(kv => kv.Value).Where(a => a.Sequence == _currentActionSequence).FirstOrDefault();
        _nextAction = _steps.Select(kv => kv.Value).Where(a => a.Sequence == _currentActionSequence + 1).FirstOrDefault();
    }

    public Guid WorkflowId { get; init; }

    public void AddToVariables(string key, object value)
    {
        _vars[$"vars.{key}"] = value;
    }

    public void SetState(ActionState state)
    {
        if (_currentAction != null)
        {
            _currentAction.State = state;
        }
    }

    public void SetErrorMessage(string errorMessage)
    {
        if (_currentAction != null)
        {
            _currentAction.ErrorMessage = errorMessage;
        }
    }
    
    public IDictionary<string, object> GetInputVariables(INoxAction action)
    {
        ResolveAllVariables(action);

        return action.Inputs.ToDictionary(i => i.Key, i => i.Value.Default, StringComparer.OrdinalIgnoreCase);
    }

    public void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs)
    {
        foreach (var output in outputs)
        {
            var varKey = $"steps.{action.Id}.outputs.{output.Key}";
            if (_vars.ContainsKey(varKey))
            {
                _vars[varKey] = output.Value;
            }
        }

        ResolveAllVariables(action);

    }

    public IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action)
    {
        var unresolvedVars = action.Inputs
            .Where(i => _variableRegex.Match(i.Value.Default.ToString()!).Success)
            .ToDictionary(i => i.Key, i => i.Value.Default, StringComparer.OrdinalIgnoreCase);

        return unresolvedVars;
    }

    public void SetErrorMessage(INoxAction action, string errorMessage)
    {
        var varKey = $"steps.{action.Id}.error-message";

        _vars[varKey] = errorMessage;

        ResolveAllVariables(action);

    }
    
    private Dictionary<string, object> InitializeVariables()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var workflowString = serializer.Serialize(_workflow);

        var matches = _variableRegex.Matches(workflowString);

        var variables = matches.Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e)
            .ToDictionary(e => e, e => new object(), StringComparer.OrdinalIgnoreCase);

        var secretKeys = variables.Select(kv => kv.Key)
            .Where(e => e.StartsWith("secrets.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[8..])
            .ToArray();

        var secrets = ConfigurationHelper.GetNoxSecrets(_appConfig, secretKeys).Result;

        if (secrets is null) return variables;

        foreach (var kv in secrets)
        {
            variables[$"secrets.{kv.Key}"] = kv.Value;
        }

        var configKeys = variables.Select(kv => kv.Key)
            .Where(e => e.StartsWith("config.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[7..])
            .ToArray();

        _noxConfig.WalkProperties( (name, value) => { if (configKeys.Contains(name, StringComparer.OrdinalIgnoreCase)) { variables[$"config.{name}"] = value ?? new object(); } });

        var userKeys = variables.Select(kv => kv.Key)
            .Where(e => e.StartsWith("user.", StringComparison.OrdinalIgnoreCase))
            .Select(e => e[5..])
            .ToArray();

        var cache = NoxCliCache.Load(ConfiguratorExtensions.CacheFile);
        cache.WalkProperties((name, value) => { if (userKeys.Contains(name, StringComparer.OrdinalIgnoreCase)) { variables[$"user.{name}"] = value ?? new object(); } });

        return variables;
    }

    private object ReplaceVariables(string value)
    {
        object result = value;

        var match = _variableRegex.Match(result.ToString()!);

        while (match.Success)
        {
            var fullPhrase = match.Groups[0].Value;

            var variable = match.Groups["variable"].Value;

            var resolvedValue = LookupVariableValue(variable);

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

    private object LookupVariableValue(string variable)
    {
        if (_vars.ContainsKey(variable))
        {
            if (_vars[variable] != null)
            {
                return _vars[variable];
            }
        }
        return $"{{{{ {variable} }}}}";
    }

    private Dictionary<string, INoxAction> ParseSteps()
    {
        var steps = new Dictionary<string, INoxAction>(StringComparer.OrdinalIgnoreCase);

        foreach (var (jobKey, stepConfiguration) in _workflow.Jobs)
        {
            var sequence = 0;

            foreach (var step in stepConfiguration.Steps)
            {
                sequence++;

                if (string.IsNullOrWhiteSpace(step.Uses))
                {
                    throw new Exception($"Step {sequence} ({step.Name}) is missing a 'uses' property");
                }

                var actionType = NoxWorkflowContextHelpers.ResolveActionProviderTypeFromUses(step.Uses);

                if (actionType == null)
                {
                    throw new Exception($"Step {sequence} ({step.Name}) uses {step.Uses} which was not found");
                }

                var newAction = new NoxAction()
                {
                    Sequence = sequence,
                    Id = step.Id,
                    Job = jobKey,
                    Name = step.Name,
                    Uses = step.Uses,
                    If = step.If,
                    Validate = step.Validate,
                    Display = step.Display,
                    RunAtServer = step.RunAtServer,
                    ContinueOnError = step.ContinueOnError,
                };
                newAction.ActionProvider = (INoxCliAddin)Activator.CreateInstance(actionType)!;

                foreach (var (withKey, withValue) in step.With)
                {
                    var input = new NoxActionInput
                    {
                        Id = withKey,
                        Default = withValue
                    };

                    newAction.Inputs.Add(withKey, input);
                }

                if (newAction.Display != null)
                {
                    if (newAction.Display.Error != null)
                    {
                        newAction.Display.Error = MaskSecretsInDisplayText(newAction.Display.Error);
                    }

                    if (newAction.Display.Success != null)
                    {
                        newAction.Display.Success = MaskSecretsInDisplayText(newAction.Display.Success);
                    }
                }

                steps[newAction.Id] = newAction;

            }
        }

        return steps;
    }

    private void ValidateSteps()
    {
        if (_steps.Any(s => s.Value.RunAtServer == true) && string.IsNullOrEmpty(_workflow.Cli.ServerUrl))
        {
            throw new Exception("You have set one of the steps in the workflow to run on the cli server, but the server-url has not been defined in the Manifest.cli.nox.yaml file.");
        }
    }

    private string MaskSecretsInDisplayText(string input)
    {
        var output = input;
        var match = _secretsVariableRegex.Match(output);
        while (match.Success)
        {
            var variable = match.Groups["variable"].Value;
            string resolvedValue = LookupVariableValue(variable)?.ToString() ?? "";
            output = _secretsVariableRegex.Replace(input,
                new string('*', Math.Min(20, resolvedValue.Length))
            );
            match = _secretsVariableRegex.Match(output);
        }
        return output;
    }

    private void ResolveAllVariables(INoxAction action)
    {
        foreach (var (_, input) in action.Inputs)
        {
            if (input.Default is string inputValueString)
            {
                input.Default = ReplaceVariables(inputValueString);
            }
            else if (input.Default is List<object> inputValueList)
            {
                for (var i = 0; i < inputValueList.Count; i++)
                {
                    if (inputValueList[i] is string inputValueArrayString)
                    {
                        var index = inputValueList.FindIndex(n => n.Equals(inputValueList[i]));
                        inputValueList[index] = ReplaceVariables((string)inputValueList[i]);
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
                        inputValueDictionary[item.Key] = ReplaceVariables(itemValueString);
                    }
                }
            }
        }

        if (action.Validate != null)
        {
            foreach(var (key, value) in action.Validate)
            {
                action.Validate[key] = ReplaceVariables(value).ToString()!;
            }
        }

        if (!string.IsNullOrWhiteSpace(action.Display?.Success))
        {
            action.Display.Success = ReplaceVariables(action.Display.Success).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.Display?.Error))
        {
            action.Display.Error = ReplaceVariables(action.Display.Error).ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(action.If))
        {
            action.If = ReplaceVariables(action.If).ToString()!;
        }
    }
}




