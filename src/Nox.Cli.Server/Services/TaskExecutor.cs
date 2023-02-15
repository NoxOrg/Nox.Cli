using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Server.Cache;
using Nox.Cli.Shared.DTO.Workflow;
using Nox.Cli.Variables;

namespace Nox.Cli.Server.Services;

public class TaskExecutor: ITaskExecutor
{
    private readonly IWorkflowCache _cache;
    private Guid _workflowId;
    private ActionState _state;
    private IActionConfiguration? _configuration;
    private INoxCliAddin? _addin;

    public TaskExecutor(
        Guid workflowId,
        IWorkflowCache cache)
    {
        _workflowId = workflowId;
        _cache = cache;
        Id = Guid.NewGuid();
        _state = ActionState.NotStarted;
    }

    public Guid Id { get; init; }
    public Guid WorkflowId => _workflowId;
    public ActionState State => _state;

    public async Task<BeginTaskResponse> BeginAsync(Guid workflowId, IActionConfiguration configuration, IDictionary<string, Variable> inputs)
    {
        var result = new BeginTaskResponse { TaskExecutorId = Id };
        _workflowId = workflowId;
        var variables = _cache.GetWorkflow(_workflowId);
        var inputVars = VariableHelper.ParseJsonInputs(inputs);
        VariableHelper.UpdateVariables(inputVars, variables);
        //TODO resolve any unresolved variables
        _cache.SetWorkflow(_workflowId, variables);
        _configuration = configuration;
        //Resolve action
        var actionType = NoxWorkflowContextHelpers.ResolveActionProviderTypeFromUses(configuration.Uses);

        if (actionType == null)
        {
            result.Error = new Exception($"{_configuration.Name} uses {_configuration.Uses} which was not found");
        }
        else
        {
            try
            {
                _addin = (INoxCliAddin)Activator.CreateInstance(actionType)!;
                await _addin.BeginAsync(variables);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = ex;
            }
                
        }
        return result;
    }

    public async Task<ExecuteTaskResponse> ExecuteAsync()
    {
        var result = new ExecuteTaskResponse
        {
            WorkflowId = _workflowId,
        };
        var variables = _cache.GetWorkflow(_workflowId);
        if (variables == null) throw new NoxCliException("Workflow not found in cache, cannot execute!");
        var context = new NoxServerWorkflowContext();
        var outputs = await _addin!.ProcessAsync(context);
        VariableHelper.UpdateVariables(outputs, variables);
        _cache.SetWorkflow(_workflowId, variables);
        result.Outputs = VariableHelper.ExtractSimpleVariables(outputs);
        result.ErrorMessage = context.ErrorMessage;
        _state = context.State;
        result.State = context.State;
        result.StateName = Enum.GetName(_state)!;
        return result;
    }

    


}