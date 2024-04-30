using Moq;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Plugin.Core;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;

namespace Plugin.Core.Tests;

public class SnakeNameTests
{
    [Theory]
    [InlineData("Hello.World", "hello_world")]
    [InlineData("HelloWorld", "helloworld")]
    [InlineData("HELLO.World", "hello_world")]
    public async Task Can_Convert_To_Snake_Name(string sourceValue, string expectedValue)
    {
        var plugin = new CoreToSnakeCase_v1();
        var inputs = new Dictionary<string, object>
        {
            {"source-string", sourceValue}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver);
        var result = await plugin.ProcessAsync(ctx);
        Assert.Single(result);
        Assert.Equal(expectedValue, result["result"]);
        await plugin.EndAsync();
    }
}