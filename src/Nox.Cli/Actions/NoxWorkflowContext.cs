using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Nox.Core.Configuration;
using Nox.Core.Interfaces.Configuration;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Server.Integration;
using Nox.Cli.Variables;

namespace Nox.Cli.Actions;

public class NoxWorkflowContext : INoxWorkflowContext
{
    private readonly IWorkflowConfiguration _workflow;
    private readonly INoxCliServerIntegration _serverIntegration;
    private readonly IDictionary<string, INoxAction> _steps;
    private readonly VariableProvider _varProvider;

    private int _currentActionSequence = 0;

    private INoxAction? _previousAction;
    private INoxAction? _currentAction;
    private INoxAction? _nextAction;

    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>[(secrets)\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public INoxAction? CurrentAction => _currentAction;

    public NoxWorkflowContext(IWorkflowConfiguration workflow, IProjectConfiguration projectConfig, INoxCliServerIntegration serverIntegration, ILocalTaskExecutorConfiguration? lteConfig)
    {
        WorkflowId = Guid.NewGuid();
        _serverIntegration = serverIntegration;
        _workflow = workflow;
        _varProvider = new VariableProvider(projectConfig, workflow, lteConfig);
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

}




