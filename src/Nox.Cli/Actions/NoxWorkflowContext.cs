using Microsoft.Extensions.Configuration;
using Nox.Cli.Configuration;
using Nox.Core.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Actions;

public class NoxWorkflowContext : INoxWorkflowContext
{

    private readonly IConfiguration _appConfig;
    private readonly INoxConfiguration _noxConfig;
    private readonly WorkflowConfiguration _workflow;
    private readonly IDictionary<string, NoxAction> _steps;
    private readonly IDictionary<string, object> _vars;

    private int _currentActionSequence = 0;

    private NoxAction? _previousAction;
    private NoxAction? _currentAction;
    private NoxAction? _nextAction;

    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>secrets.[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public NoxAction? CurrentAction => _currentAction;

    public NoxWorkflowContext(WorkflowConfiguration workflow, INoxConfiguration noxConfig, IConfiguration appConfig)
    {
        _appConfig = appConfig;
        _noxConfig = noxConfig;
        _workflow = workflow;
        _vars = InitializeVariables();
        _steps = ParseSteps();
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


    private Dictionary<string, object> InitializeVariables()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var workflowString = serializer.Serialize(_workflow);

        var matches = _variableRegex.Matches(workflowString);

        var variables = matches.Select(m => m.Groups[1].Value)
            .Distinct()
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

        return variables;
    }

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

    private Dictionary<string, NoxAction> ParseSteps()
    {
        var steps = new Dictionary<string, NoxAction>(StringComparer.OrdinalIgnoreCase);

        foreach (var (jobKey, stepConfiguration) in _workflow.Jobs)
        {
            var sequence = 0;

            foreach (var step in stepConfiguration.Steps)
            {
                sequence++;

                var actionType = ResolveActionProviderTypeFromUses(step.Uses);

                if (actionType == null)
                {
                    throw new Exception($"Step {sequence} ({step.Name}) uses {step.Uses} which was not found.");
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

    private static Type? ResolveActionProviderTypeFromUses(string uses)
    {
        var actionAssemblyName = $"Nox.Cli.Plugin.{uses.Split('/')[0]}";
        var actionClassNameLower = uses.Replace("/", "").Replace("-", "").Replace("@", "_").ToLower();

        var loadedPaths = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Where(a => a.GetName().Name?.Contains(actionAssemblyName, StringComparison.InvariantCultureIgnoreCase) ?? false)
            .Select(a => a.Location)
            .ToArray();

        var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

        var toLoad = referencedPaths
            .Where(r => r.Contains(actionAssemblyName, StringComparison.InvariantCultureIgnoreCase))
            .Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase))
            .ToArray();

        if (toLoad.Length > 0)
        {
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(toLoad[0]));
        }

        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.Contains(actionAssemblyName, StringComparison.InvariantCultureIgnoreCase) ?? false)
            .ToArray();

        Type? actionType = null;
        
        if (assembly.Length > 0)
        {
            actionType = assembly[0].GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.IsAssignableTo(typeof(INoxCliAddin)))
                .Where(t => t.Name.ToLower().Equals(actionClassNameLower))
                .FirstOrDefault();
        }

        return actionType;
    }


    public IDictionary<string, object> GetInputVariables(NoxAction action)
    {
        ResolveAllVariables(action);

        return action.Inputs.ToDictionary(i => i.Key, i => i.Value.Default, StringComparer.OrdinalIgnoreCase);
    }

    public void StoreOutputVariables(NoxAction action, IDictionary<string, object> outputs)
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
    public void SetErrorMessage(NoxAction action, string errorMessage)
    {
        var varKey = $"steps.{action.Id}.error-message";

        _vars[varKey] = errorMessage;

        ResolveAllVariables(action);

    }

    private void ResolveAllVariables(NoxAction action)
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




