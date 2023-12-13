using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Caching;
using Nox.Cli.Configuration;
using Nox.Cli.Server.Caching;
using Nox.Cli.Server.Services;

namespace Nox.Cli.Server.Tests;

public class CoreTests
{
    private INoxWorkflowContext TestContext;
    private Guid TestWorkflowId = Guid.NewGuid();
    
    [OneTimeSetUp]
    public void Setup()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var workflowCache = new ServerCache(memCache);
        var cacheManager = new NoxCliCacheBuilder("")
            .WithCacheFile("./files/NoxCliCache.json")
            .Build();
        
        TestContext = new WorkflowContext(TestWorkflowId, cacheManager);
    }
    
    [Test]
    public async Task Can_Resolve_a_Task_Action()
    {
        var result = await TestContext.ExecuteTask(TestHelper.GetPingAction());
        Assert.That(result.State, Is.EqualTo(ActionState.Success));
    }

    [Test]
    public async Task Must_Get_Error_Response_If_Not_Able_To_Resolve_Action()
    {
        var result = await TestContext.ExecuteTask(TestHelper.GetInvalidAction());
        Assert.That(result.State, Is.EqualTo(ActionState.Error));
        Assert.That(result.ErrorMessage!.StartsWith("Some variables are unresolved"));
    }
}