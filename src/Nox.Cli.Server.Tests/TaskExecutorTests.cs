using Microsoft.Extensions.Caching.Memory;
using Microsoft.OData.ModelBuilder;
using Nox.Cli.Configuration;

namespace Nox.Cli.Server.Tests;

public class TaskExecutorTests
{
    [Test]
    public async Task Can_Execute_a_task()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var executor = new TaskExecutor(cache);
        var wfId = Guid.NewGuid();
        var inputs = new Dictionary<string, object>();
        var config = new Configuration.ActionConfiguration();
        await executor.BeginAsync(wfId, config, inputs);
        var result = await executor.ExecuteAsync();
        
    }
}