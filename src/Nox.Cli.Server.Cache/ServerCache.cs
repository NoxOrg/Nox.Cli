using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Server.Cache;

public class ServerCache: IServerCache
{
    private readonly IMemoryCache _memCache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public ServerCache(
        IMemoryCache memCache)
    {
        _memCache = memCache;
        
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
    }
    
    public void SaveContext(Guid workflowId, INoxWorkflowContext context)
    {
        var cacheValue = GetContext(workflowId);
        if (cacheValue != null) _memCache.Remove(workflowId);
        _memCache.Set(workflowId, context, _cacheOptions);
    }

    public INoxWorkflowContext? GetContext(Guid workflowId)
    {
        if (_memCache.TryGetValue(workflowId, out INoxWorkflowContext? cacheValue))
        {
            return cacheValue!;
        }

        return null;
    }
}