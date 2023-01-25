using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server;

public class TaskExecutor: ITaskExecutor
{
    private readonly IMemoryCache _cache;
    private Guid _workflowId;
    private IActionConfiguration _configuration;
    private IDictionary<string, object> _inputs;

    public TaskExecutor(
        IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task BeginAsync(Guid workflowId, IActionConfiguration configuration, IDictionary<string, object> inputs)
    {
        _workflowId = workflowId;
        _inputs = inputs;
        _configuration = configuration;
        //TODO resolve variables
    }

    public async Task<ExecuteTaskResponse> ExecuteAsync()
    {
        var result = new ExecuteTaskResponse();
        IDictionary<string, object> variables;
        if (_cache.TryGetValue(_workflowId, out IDictionary<string, object> cachedVariables))
        {
            variables = cachedVariables;
        }
        else
        {
            variables = new Dictionary<string, object>();
            _cache.Set(_workflowId, variables, DateTimeOffset.Now.AddMinutes(10));
        }

        return result;
    }

    
}