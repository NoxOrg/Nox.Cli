using System.ComponentModel;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Shared.DTO.Workflow;
using Nox.Cli.Variables;

namespace Nox.Cli.Server.Services;

public class WorkflowContext: INoxWorkflowContext
{
    private Guid _instanceId;
    private Guid _workflowId;
    private string? _errorMessage;
    private ActionState _state;
    private readonly ServerVariableProvider _varProvider;

    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+secret[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public WorkflowContext(Guid workflowId, IManifestConfiguration manifest)
    {
        _instanceId = Guid.NewGuid();
        _workflowId = workflowId;
        _varProvider = new ServerVariableProvider(manifest);
    }

    public Guid InstanceId => _instanceId;
    public Guid WorkflowId => _workflowId;
    public ActionState State => _state;

    public async Task<ExecuteTaskResult> ExecuteTask(INoxAction action)
    {
        var result = new ExecuteTaskResult
        {
            WorkflowId = _workflowId,
        };

        //Extract the inputs from the action & add them to the varProvider variables
        var inputVars = _varProvider.ResolveInputs(action);
        var unresolved = _varProvider.GetUnresolvedVariables();
        if (unresolved.Any())
        {
            var err = "Some variables are unresolved. Did you misspell or forget to define them? Is the service.nox.yaml available? ";
            foreach (var item in unresolved)
            {
                err += $"\r\n{item.Key}: {item.Value}";
            }
            SetErrorMessage(err);
            SetState(ActionState.Error);
            result.SetState(ActionState.Error, err);
            return result;
        }
        
        //Resolve the action
        var actionType = NoxWorkflowContextHelpers.ResolveActionProviderTypeFromUses(action.Uses);
        
        if (actionType == null)
        {
            var err = $"{action.Name} uses {action.Uses} which was not found";
            SetErrorMessage(err);
            SetState(ActionState.Error);
            result.SetState(ActionState.Error, err);
            return result;
        }
        else
        {
            try
            {
                var addin = (INoxCliAddin)Activator.CreateInstance(actionType)!;
                await addin.BeginAsync(inputVars);
                var outputs = await addin.ProcessAsync(this);
                _varProvider.SaveOutputs(action.Id, outputs);
                result.Outputs = outputs;
                await addin.EndAsync();
                result.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                result.SetState(ActionState.Error, ex.Message);
            }
                
        }
        return result;
    }
    
    public string? ErrorMessage => _errorMessage;
    
    public void AddToVariables(string key, object value)
    {
        throw new NotImplementedException();
    }

    public void SetErrorMessage(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    public void SetState(ActionState state)
    {
        _state = state;
    }
}