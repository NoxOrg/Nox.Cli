using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Variables;

namespace Nox.Cli.Server.Cache;

public class WorkflowCache: IWorkflowCache
{
    private readonly IMemoryCache _memCache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public WorkflowCache(
        IMemoryCache memCache)
    {
        _memCache = memCache;
        
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
    }
    
    public IDictionary<string, IVariable> GetWorkflow(Guid workflowId)
    {
        if (_memCache.TryGetValue(workflowId, out IDictionary<string, IVariable>? cacheValue))
        {
            return cacheValue!;
        }
        return new Dictionary<string, IVariable>();
    }

    public void SetWorkflow(Guid workflowId, IDictionary<string, IVariable> variables)
    {
        //var cacheValue = GetWorkflow(workflowId);
        //if (cacheValue != null) 
        _memCache.Remove(workflowId);
        _memCache.Set(workflowId, variables, _cacheOptions);
    }
    
    
}