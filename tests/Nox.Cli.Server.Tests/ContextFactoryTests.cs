using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Caching;
using Nox.Cli.Server.Abstractions;
using Nox.Cli.Server.Caching;
using Nox.Cli.Server.Services;

namespace Nox.Cli.Server.Tests;

[NonParallelizable]
public class ContextFactoryTests
{
    private IWorkflowContextFactory TestContextFactory;
    private readonly Guid TestWorkflowId = new Guid("7A3813B8-567E-4EF4-86CC-F87CEC390AB1");

    [OneTimeSetUp]
    public void Setup()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ServerCache(memCache);
        var cacheManager = new NoxCliCacheBuilder("")
            .WithCacheFile("./files/NoxCliCache.json")
            .Build();
        
        TestContextFactory = new WorkflowContextFactory(cache, cacheManager, null);
    }
    
    [Test]
    public async Task Can_Execute_a_second_task_using_an_existing_context()
    {
        var manifest = TestHelper.GetValidManifest();
        var context = TestContextFactory.NewInstance(TestWorkflowId);
        Assert.That(context, Is.Not.Null);
        Assert.That(context.WorkflowId, Is.EqualTo(TestWorkflowId));
        Assert.That(context.State, Is.EqualTo(ActionState.NotStarted));
        var instanceId = context.InstanceId;
        var action = TestHelper.GetFirstAction();
        var result = await context.ExecuteTask(action);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ActionState.Success));
            Assert.That(result.StateName, Is.EqualTo("Success"));
        });
        
        var otherContext = TestContextFactory.GetInstance(TestWorkflowId);
        Assert.That(otherContext, Is.Not.Null);
        Assert.That(otherContext.InstanceId, Is.EqualTo(instanceId));
        action = TestHelper.GetSecondAction();
        result = await otherContext.ExecuteTask(action);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ActionState.Success));
            Assert.That(result.StateName, Is.EqualTo("Success"));
            Assert.That(result.Outputs, Is.Not.Null);
            if (result.Outputs != null)
            {
                Assert.That(result.Outputs.Count, Is.EqualTo(1));
                Assert.That(result.Outputs["my-result"], Is.EqualTo("0"));
            }
        });
    }
    
}