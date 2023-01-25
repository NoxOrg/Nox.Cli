using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Actions;

namespace Nox.Cli.Server.Tests;

public class TaskExecutorTests
{
    [Test]
    public async Task Can_Execute_a_task()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var executor = new TaskExecutor(cache);
        var wfId = Guid.NewGuid();
        var action = new NoxAction
        {
            
        };
        await executor.ExecuteAsync(wfId, action);
    }
}