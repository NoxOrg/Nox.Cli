using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Server.Cache;

namespace Nox.Cli.Server.Services;

public class TaskExecutorFactory : ITaskExecutorFactory
{
    private readonly IList<ITaskExecutor> _pool;
    private readonly IWorkflowCache _cache;

    public TaskExecutorFactory(
        IWorkflowCache cache)
    {
        _pool = new List<ITaskExecutor>();
        _cache = cache;
    }

    public ITaskExecutor GetInstance(Guid id)
    {
        var instance = _pool.FirstOrDefault(i => i.Id == id);
        if (instance == null)
        {
            throw new Exception($"Task Executor instance {id} does not exist in pool.");
        }
        else
        {
            return instance;
        }
    }

    public ITaskExecutor NewInstance(Guid workflowId)
    {
        var executor = new TaskExecutor(workflowId, _cache);
        _pool.Add(executor);
        return executor;
    }

    public void DisposeInstance(Guid id)
    {
        var item = _pool.Single(i => i.Id == id);
        _pool.Remove(item);
    }
}