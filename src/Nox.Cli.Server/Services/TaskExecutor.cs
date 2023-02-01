using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Services;

public class TaskExecutor: ITaskExecutor
{
    private readonly IMemoryCache _cache;
    private Guid _workflowId;
    private ActionState _state;
    private IActionConfiguration? _configuration;
    private IDictionary<string, object>? _inputs;
    private INoxCliAddin? _addin;

    public TaskExecutor(
        Guid workflowId,
        IMemoryCache cache)
    {
        _workflowId = workflowId;
        _cache = cache;
        Id = Guid.NewGuid();
        _state = ActionState.NotStarted;
    }

    public Guid Id { get; init; }
    public Guid WorkflowId => _workflowId;
    public ActionState State => _state;

    public async Task<BeginTaskResponse> BeginAsync(Guid workflowId, IActionConfiguration configuration, IDictionary<string, object> inputs)
    {
        var result = new BeginTaskResponse
        {
            TaskExecutorId = Id
        };
        _workflowId = workflowId;
        _inputs = ParseInputs(inputs);
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
                await _addin.BeginAsync(_inputs);
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
        IDictionary<string, object>? variables;
        if (_cache.TryGetValue(_workflowId, out IDictionary<string, object>? cachedVariables))
        {
            variables = cachedVariables;
        }
        else
        {
            variables = new Dictionary<string, object>();
            _cache.Set(_workflowId, variables, DateTimeOffset.Now.AddMinutes(10));
        }

        var context = new NoxServerWorkflowContext();
        result.Outputs = await _addin!.ProcessAsync(context);
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