using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Secrets;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Server.Services;

public class WorkflowContextFactory: IWorkflowContextFactory
{
    private readonly IList<INoxWorkflowContext> _pool;
    private readonly IServerCache _cache;
    private readonly IServerSecretResolver? _secretResolver;
    private readonly INoxCliCacheManager _cacheManager;

    public WorkflowContextFactory(IServerCache cache, INoxCliCacheManager cacheManager, IServerSecretResolver? secretResolver = null)
    {
        _pool = new List<INoxWorkflowContext>();
        _cache = cache;
        _secretResolver = secretResolver;
        _cacheManager = cacheManager;
    }
    
    public INoxWorkflowContext NewInstance(Guid workflowId)
    {
        var instance = new WorkflowContext(workflowId, _cacheManager, _secretResolver);
        _pool.Add(instance);
        return instance;
    }

    public INoxWorkflowContext? GetInstance(Guid workflowId)
    {
        return _pool.FirstOrDefault(i => i.WorkflowId == workflowId);
    }

    public void DisposeInstance(Guid workflowId)
    {
        var item = _pool.Single(i => i.WorkflowId == workflowId);
        _pool.Remove(item);
    }
}