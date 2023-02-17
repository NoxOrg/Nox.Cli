using Nox.Core.Interfaces.Configuration;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Secrets;
using Nox.Cli.Variables;

namespace Nox.Cli.Actions;

public class NoxWorkflowContext : INoxWorkflowContext
{
    private readonly IWorkflowConfiguration _workflow;
    private readonly IDictionary<string, INoxAction> _steps;
    private readonly ClientVariableProvider _varProvider;

    private int _currentActionSequence = 0;

    private INoxAction? _previousAction;
    private INoxAction? _currentAction;
    private INoxAction? _nextAction;

    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+secret[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public INoxAction? CurrentAction => _currentAction;

    public NoxWorkflowContext(
        IWorkflowConfiguration workflow, 
        IProjectConfiguration projectConfig,
        IProjectSecretResolver projectSecretResolver,
        IOrgSecretResolver orgSecretResolver,
        ILocalTaskExecutorConfiguration? lteConfig)
    {
        WorkflowId = Guid.NewGuid();
        _workflow = workflow;
        _varProvider = new ClientVariableProvider(workflow, projectSecretResolver, orgSecretResolver, projectConfig, lteConfig);
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

    public Guid InstanceId { get; }
    public Guid WorkflowId { get; init; }
    public ActionState State { get; }

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
    
    public void SetErrorMessage(INoxAction action, string errorMessage)
    {
        var varKey = $"steps.{action.Id}.error-message";
        _varProvider.SetActionVariable(action, varKey, errorMessage);
    }

    public Task<ExecuteTaskResult> ExecuteTask(INoxAction action)
    {
        throw new NotImplementedException();
    }

    public void AddToVariables(string key, object value)
    {
        _varProvider.SetVariable($"vars.{key}", value);
    }
    
    public IDictionary<string, object> GetInputVariables(INoxAction action)
    {
        return _varProvider.GetInputVariables(action);
    }

    public void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs)
    {
        _varProvider.StoreOutputVariables(action, outputs);
    }

    public IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action)
    {
        return _varProvider.GetUnresolvedInputVariables(action);
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
                        //newAction.Display.Success = MaskSecretsInDisplayText(newAction.Display.Success);
                    }
                }

                steps[newAction.Id] = newAction;

            }
        }

        return steps;
    }

    private void ValidateSteps()
    {
        // if (_steps.Any(s => s.Value.RunAtServer == true) && _serverIntegration == null || !_serverIntegration.IsConfigured)
        // {
        //     throw new Exception("You have set one of the steps in the workflow to run on the cli server, but the server has not been defined in the Manifest.cli.nox.yaml file.");
        // }
    }
    
    private string MaskSecretsInDisplayText(string input)
    {
        var output = input;
        var match = _secretsVariableRegex.Match(output);
        while (match.Success)
        {
            var variable = match.Groups["variable"].Value;
            output = _secretsVariableRegex.Replace(input,
                new string('*', 20)
            );
            match = _secretsVariableRegex.Match(output);
        }
        return output;
    }
    
}




