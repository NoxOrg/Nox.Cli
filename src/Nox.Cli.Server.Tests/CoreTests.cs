using Microsoft.Extensions.Caching.Memory;
using Microsoft.OData.ModelBuilder;
using Nox.Cli.Abstractions;
using Nox.Cli.Configuration;
using Nox.Cli.Server.Cache;
using Nox.Cli.Server.Services;

namespace Nox.Cli.Server.Tests;

public class CoreTests
{
    private ITaskExecutor TestExecutor;
    private Guid TestWorkflowId = Guid.NewGuid();
    
    [OneTimeSetUp]
    public void Setup()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var workflowCache = new WorkflowCache(memCache);
        TestExecutor = new TaskExecutor(TestWorkflowId, workflowCache);
    }
    
    [Test]
    public async Task Can_Resolve_a_Task_Action()
    {
        var beginResult = await TestExecutor.BeginAsync(TestWorkflowId, TestHelper.GetPingConfig(), TestHelper.GetPingInputs());
        Assert.That(beginResult.Success, Is.True);
        Assert.That(beginResult.TaskExecutorId, Is.InstanceOf<Guid>());
        //var result = await TestExecutor.ExecuteAsync();
    }

    [Test]
    public async Task Must_Get_Error_Response_If_Not_Able_To_Resolve_Action()
    {
        var beginResult = await TestExecutor.BeginAsync(TestWorkflowId, TestHelper.GetInvalidConfig(), TestHelper.GetPingInputs());
        Assert.That(beginResult.Success, Is.False);
        Assert.That(beginResult.Error, Is.Not.Null);
        Assert.That(beginResult.Error, Is.InstanceOf<System.Exception>());
        Assert.That(beginResult.Error.Message.EndsWith("uses test/invalid@v1 which was not found"));
    }
}