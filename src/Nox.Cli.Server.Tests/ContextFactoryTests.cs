using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Abstractions;
using Nox.Cli.Server.Cache;
using Nox.Cli.Server.Services;

namespace Nox.Cli.Server.Tests;

public class ContextFactoryTests
{
    private IWorkflowContextFactory TestContextFactory;
    private readonly Guid TestWorkflowId = new Guid("7A3813B8-567E-4EF4-86CC-F87CEC390AB1");

    [OneTimeSetUp]
    public void Setup()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ServerCache(memCache);
        var manifest = TestHelper.GetValidManifest();
        TestContextFactory = new WorkflowContextFactory(cache, manifest);
    }
    
    [Test]
    public void Can_Create_New_Context_From_Factory()
    {
        var context = TestContextFactory.NewInstance(TestWorkflowId);
        Assert.That(context, Is.Not.Null);
        Assert.That(context.WorkflowId, Is.EqualTo(TestWorkflowId));
        Assert.That(context.State, Is.EqualTo(ActionState.NotStarted));
    }

    [Test]
    public void Can_Get_Existing_Context_From_Factory()
    {
        var context = TestContextFactory.NewInstance(TestWorkflowId);
        Assert.That(context, Is.Not.Null);
        Assert.That(context.WorkflowId, Is.EqualTo(TestWorkflowId));
        Assert.That(context.State, Is.EqualTo(ActionState.NotStarted));
        var instanceId = context.InstanceId;
        var otherContext = TestContextFactory.GetInstance(TestWorkflowId);
        Assert.That(otherContext, Is.Not.Null);
        Assert.That(otherContext.InstanceId, Is.EqualTo(instanceId));
    }
    
}