using Microsoft.Extensions.Caching.Memory;
using Microsoft.OData.ModelBuilder;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Server.Abstractions;
using Nox.Cli.Server.Cache;
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
        var manifest = new ManifestConfiguration
        {
            RemoteTaskExecutor = new RemoteTaskExecutorConfiguration
            {
                ApplicationId = Guid.NewGuid().ToString(),
                Url = "http://localhost:8000",
                Secrets = new List<ISecretConfiguration>
                {
                    new SecretConfiguration
                    {
                        Url = "https://nox-3D6394A1E5840C21.vault.azure.net/",
                        Provider = "azure-keyvault"
                    }
                }
            }
        };
        TestContext = new WorkflowContext(TestWorkflowId, manifest);
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
        Assert.That(result.ErrorMessage!.EndsWith("uses test/invalid@v1 which was not found"));
    }
}