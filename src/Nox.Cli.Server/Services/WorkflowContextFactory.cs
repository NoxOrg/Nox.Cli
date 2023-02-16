using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Server.Services;

public class WorkflowContextFactory: IWorkflowContextFactory
{
    private readonly IList<INoxWorkflowContext> _pool;
    private readonly IServerCache _cache;
    private readonly IManifestConfiguration _manifest;

    public WorkflowContextFactory(IServerCache cache, IManifestConfiguration manifest)
    {
        _pool = new List<INoxWorkflowContext>();
        _cache = cache;
        _manifest = manifest;
    }
    
    public INoxWorkflowContext NewInstance(Guid workflowId)
    {
        var instance = new WorkflowContext(workflowId, _manifest);
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