using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;

namespace Nox.Cli.Server.Services;

public class TaskExecutorFactory : ITaskExecutorFactory
{
    private readonly IList<ITaskExecutor> _pool;
    private readonly IMemoryCache _cache;

    public TaskExecutorFactory(
        IMemoryCache cache)
    {
        _pool = new List<ITaskExecutor>();
        _cache = cache;
    }

    public ITaskExecutor GetInstance(Guid? id = null)
    {
        if (id == null) return CreateNewInstance();
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

    public void DisposeInstance(Guid id)
    {
        var item = _pool.Single(i => i.Id == id);
        _pool.Remove(item);
    }


    private ITaskExecutor CreateNewInstance()
    {
        var executor = new TaskExecutor(_cache);
        _pool.Add(executor);
        return executor;
    }

}