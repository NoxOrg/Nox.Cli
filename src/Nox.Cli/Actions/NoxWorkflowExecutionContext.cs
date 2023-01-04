using Microsoft.Extensions.Configuration;
using Nox.Core.Configuration;
using Spectre.Console;
using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutionContext
{

    private readonly IConfiguration _appConfig;
    private readonly IDictionary<object, object> _noxConfig;
    private readonly IDictionary<object, object> _workflow;
    private readonly IDictionary<object, object> _jobs;
    private readonly IDictionary<string, NoxAction> _steps;
    private readonly IDictionary<string, object> _vars;
    
    private int _currentActionSequence = 0;
    
    private NoxAction? _previousAction;
    private NoxAction? _currentAction;
    private NoxAction? _nextAction;

    private readonly Regex _variableRegex = new (@"\$\{\{\s*(?<variable>[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled);

    private readonly Regex _secretsVariableRegex = new (@"\$\{\{\s*(?<variable>secrets.[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled);

    public NoxAction? CurrentAction => _currentAction;

    public NoxWorkflowExecutionContext(IDictionary<object, object> workflow, IDictionary<object, object> noxConfig, IConfiguration appConfig)
    {
        _appConfig = appConfig;
        _noxConfig = noxConfig;
        _workflow = workflow;
        _jobs = (IDictionary<object, object>)workflow["jobs"];
        _vars = InitializeVariables();
        _steps = ParseSteps();
        _currentActionSequence = 0;
        Next();
    }

    public void Next()
    {
        _currentActionSequence++;
        _previousAction = _currentAction;
        _currentAction = _steps.Select(kv => kv.Value).Where(a => a.Sequence == _currentActionSequence).FirstOrDefault();
        _nextAction = _steps.Select(kv => kv.Value).Where(a => a.Sequence == _currentActionSequence+1).FirstOrDefault();
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
            .ToDictionary(e => e, e => new object());

        var secretKeys = variables.Select(kv => kv.Key)
            .Where(e => e.StartsWith("secrets."))
            .Select(e => e[8..])
            .ToArray();

        var secrets = ConfigurationHelper.GetNoxSecrets(_appConfig, secretKeys).Result;

        if (secrets is null) return variables;

        foreach (var kv in secrets)
        {
            variables[$"secrets.{kv.Key}"] = kv.Value;
        }

        var configKeys = variables.Select(kv => kv.Key)
            .Where(e => e.StartsWith("config."))
            .Select(e => e[7..])
            .ToArray();


        _noxConfig.WalkDictionary(kv => { if (configKeys.Contains(kv.Key)) { variables[$"config.{kv.Key}"] = kv.Value; } });

        return variables;
    }

    public void AddToVariables(string key, object value)
    {
        _vars[$"vars.{key}"] = value;
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
        var steps = new Dictionary<string, NoxAction>();

        foreach (var (jobKey, jobValueObject) in _jobs)
        {
            var jobValue = jobValueObject as IDictionary<object, object>;

            if (jobValue == null) continue;

            var sequence = 0;

            foreach (var stepObject in (IList<object>)jobValue["steps"])
            {
                sequence++;

                var step = stepObject as IDictionary<object, object>;

                if (step is null) continue;

                var actionType = ResolveActionTypeFromUses((string)step["uses"]);

                if (actionType == null)
                {
                    // throw new Exception($"Step {sequence} ({step["name"]}) uses {step["uses"]} which was not found.");
                    continue;
                }

                var newAction = (NoxAction)Activator.CreateInstance(actionType)!;

                newAction.Sequence = sequence;
                newAction.Id = (string)step["id"];
                newAction.Job = (string)jobKey;
                newAction.Name = (string)step["name"];
                newAction.Uses = (string)step["uses"];

                if (step.ContainsKey("if"))
                {
                    newAction.If = (string)step["if"];
                }

                if (step.ContainsKey("with"))
                {
                    var withs = (IDictionary<object, object>)step["with"];

                    foreach (var (withKey, withValue) in withs)
                    {
                        if (withKey == null) continue;

                        var input = new NoxActionInput
                        {
                            Id = (string)withKey,
                            Default = withValue
                        };

                        newAction.Inputs.Add((string)withKey, input);
                    }
                }

                if (step.ContainsKey("validate"))
                {
                    var validate = (IDictionary<object, object>)step["validate"];

                    foreach (var (validateKey, validateValue) in validate)
                    {
                        if (validateKey == null) continue;

                        newAction.Validate.Add(((string)validateKey, (string)validateValue));
                    }
                }

                if (step.ContainsKey("display"))
                {
                    var display = (IDictionary<object, object>)step["display"];

                    if (display.ContainsKey("error"))
                    {
                        newAction.Display.Error = MaskSecretsInDisplayText((string)display["error"]);
                    }

                    if (display.ContainsKey("success"))
                    {
                        newAction.Display.Success = MaskSecretsInDisplayText((string)display["success"]);
                    }
                }

                if (step.ContainsKey("continue-on-error"))
                {
                    newAction.ContinueOnError = Convert.ToBoolean(step["continue-on-error"]);
                }

                steps[(string)step["id"]] = newAction;

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
                new string('*', Math.Min(20,resolvedValue.Length))
            );
            match = _secretsVariableRegex.Match(output);
        }
        return output;
    }

    private static Type? ResolveActionTypeFromUses(string uses)
    {
        var actionClassNameLower = uses.Replace("/", "").Replace("-", "").Replace("@", "_").ToLower();

        var actionType = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.IsAssignableTo(typeof(INoxAction)))
            .Where(t => t.Name.ToLower().Equals(actionClassNameLower))
            .FirstOrDefault();

        return actionType;
    }

    public IDictionary<string, object> GetInputVariables(NoxAction action)
    {
        ResolveAllVariables(action);

        return action.Inputs.ToDictionary(i => i.Key, i => i.Value.Default);
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
        foreach(var (_,input) in action.Inputs)
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

        for(var i = 0; i < action.Validate.Count; i++)
        {
            var v = action.Validate[i];
            v.Item2 = ReplaceVariables(action.Validate[i].Item2).ToString()!;
            action.Validate[i] = v;
        }

        if (action.Display.Success != string.Empty)
        {
            action.Display.Success = ReplaceVariables(action.Display.Success).ToString()!;
        }

        if (action.Display.Error != string.Empty)
        {
            action.Display.Error = ReplaceVariables(action.Display.Error).ToString()!;
        }

        if (action.If != string.Empty)
        {
            action.If = ReplaceVariables(action.If).ToString()!;
        }
    }
}




