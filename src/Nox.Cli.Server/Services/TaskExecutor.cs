using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Secrets;
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
    private IDictionary<string, object>? _inputs;
    private INoxCliAddin? _addin;
    private IManifestConfiguration _manifest;

    public TaskExecutor(
        Guid workflowId,
        IWorkflowCache cache,
        IManifestConfiguration manifest)
    {
        _workflowId = workflowId;
        _cache = cache;
        Id = Guid.NewGuid();
        _state = ActionState.NotStarted;
        _manifest = manifest;
    }

    public Guid Id { get; init; }
    public Guid WorkflowId => _workflowId;
    public ActionState State => _state;

    public async Task<BeginTaskResponse> BeginAsync(Guid workflowId, IActionConfiguration configuration, IDictionary<string, object> inputs)
    {
        var result = new BeginTaskResponse { TaskExecutorId = Id };
        _workflowId = workflowId;
        var variables = _cache.GetWorkflow(_workflowId);
        _inputs = ParseInputs(inputs);
        VariableHelper.CopyVariables(_inputs, variables);
        variables.ResolveServerSecrets(_manifest.RemoteTaskExecutor!);
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
                await _addin.BeginAsync(variables.ToDictionary(item => item.Key, item => item.Value.Value));
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
        VariableHelper.CopyVariables(outputs, variables);
        _cache.SetWorkflow(_workflowId, variables);
        result.Outputs = VariableHelper.ExtractSimpleVariables(outputs);
        result.ErrorMessage = context.ErrorMessage;
        _state = context.State;
        result.State = context.State;
        result.StateName = Enum.GetName(_state)!;
        return result;
    }

    private IDictionary<string, object> ParseInputs(IDictionary<string, object> source)
    {
        var result = new Dictionary<string, object>();
        foreach (var item in source)
        {
            if (item.Value != null)
            {
                if (item.Value is JsonElement element)
                {
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.False:
                        case JsonValueKind.True:
                            result.Add(item.Key, element.GetBoolean());
                            break;
                        case JsonValueKind.Array:
                            result.Add(item.Key, element.EnumerateArray());
                            break;
                        case JsonValueKind.Null:
                            result.Add(item.Key, null!);
                            break;
                        case JsonValueKind.Object:
                            result.Add(item.Key, element);
                            break;
                        case JsonValueKind.Undefined:
                        case JsonValueKind.String:
                        case JsonValueKind.Number:
                        default:
                            result.Add(item.Key, element!.GetString()!);
                            break;
                    }    
                }
                else
                {
                    result.Add(item.Key, item.Value);
                }
            }
            else
            {
                result.Add(item.Key, null!);
            }
            
        }

        return result;
    }


}