using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;

namespace Nox.Cli.Server;

public class TaskExecutor: ITaskExecutor
{
    private readonly IMemoryCache _cache;

    public Guid WorkflowId { get; private set; }
    public INoxAction Action { get; private set; }
    

    public TaskExecutor(
        IMemoryCache cache)
    {
        _cache = cache;
    }
    
    public async Task ExecuteAsync(Guid workflowId, INoxAction action)
    {
        WorkflowId = workflowId;
        Action = action;
        IDictionary<string, object> variables;
        if (_cache.TryGetValue(workflowId, out IDictionary<string, object> cachedVariables))
        {
            variables = cachedVariables;
        }
        else
        {
            variables = new Dictionary<string, object>();
            _cache.Set(workflowId, variables, DateTimeOffset.Now.AddMinutes(10));
        }
    }

    
}