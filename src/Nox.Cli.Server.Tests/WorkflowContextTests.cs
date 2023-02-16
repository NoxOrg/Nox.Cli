using Microsoft.Extensions.Caching.Memory;
using Nox.Cli.Abstractions;
using Nox.Cli.Server.Cache;
using Nox.Cli.Server.Services;
using NUnit.Framework.Constraints;

namespace Nox.Cli.Server.Tests;

public class WorkflowContextTests
{
    [OneTimeSetUp]
    public void Setup()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ServerCache(memCache);
        var manifest = TestHelper.GetValidManifest();
    }
    
    [Test]
    public async Task Can_Execute_a_simple_task()
    {
        var manifest = TestHelper.GetValidManifest();
        var workflowId = Guid.NewGuid();
        var context = new WorkflowContext(workflowId, manifest);
        var action = TestHelper.GetPingAction();
        var result = await context.ExecuteTask(action);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ActionState.Success));
            Assert.That(result.StateName, Is.EqualTo("Success"));
        });
    }

    [Test]
    public async Task Must_get_error_state_if_any_variables_are_unresolved()
    {
        var manifest = TestHelper.GetValidManifest();
        var workflowId = Guid.NewGuid();
        var context = new WorkflowContext(workflowId, manifest);
        var action = TestHelper.GetUninitializedPingAction();
        var result = await context.ExecuteTask(action);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ActionState.Error));
            Assert.That(result.StateName, Is.EqualTo("Error"));
            Assert.That(result.ErrorMessage, Does.StartWith("Some variables are unresolved"));
        });
    }
    
    [Test]
    public async Task Can_Execute_a_task_with_a_server_secret()
    {
        var manifest = TestHelper.GetValidManifest();
        var workflowId = Guid.NewGuid();
        var context = new WorkflowContext(workflowId, manifest);
        var action = TestHelper.GetSecretAction();
        var result = await context.ExecuteTask(action);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ActionState.Success));
            Assert.That(result.StateName, Is.EqualTo("Success"));
        });
    }

    [Test]
    public async Task Can_execute_another_task_on_the_same_context()
    {
        var manifest = TestHelper.GetValidManifest();
        var workflowId = Guid.NewGuid();
        var context = new WorkflowContext(workflowId, manifest);
        var action = TestHelper.GetFirstAction();
        var result = await context.ExecuteTask(action);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ActionState.Success));
            Assert.That(result.StateName, Is.EqualTo("Success"));
        });
        action = TestHelper.GetSecondAction();
        result = await context.ExecuteTask(action);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ActionState.Success));
            Assert.That(result.StateName, Is.EqualTo("Success"));
        });
    }
}