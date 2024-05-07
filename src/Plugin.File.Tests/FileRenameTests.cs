using Moq;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Plugin.File;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using Xunit;

namespace Plugin.File.Tests;

public class FileRenameTests
{
    [Fact]
    public async Task Can_Rename_a_File()
    {
        var path = "./files/rename";
        //Copy the sample file for the test
        System.IO.File.Delete(Path.Combine(path, "Sample-Before.txt"));
        System.IO.File.Delete(Path.Combine(path, "Sample-After.txt"));
        System.IO.File.Copy(Path.Combine(path, "Sample.txt"), Path.Combine(path, "Sample-Before.txt"));
        
        var plugin = new FileRename_v1();
        var inputs = new Dictionary<string, object>
        {
            {"source-path", "./files/rename/Sample-Before.txt"},
            {"new-name", "Sample-After.txt"}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver);
        await plugin.ProcessAsync(ctx);
        Assert.True(System.IO.File.Exists(Path.Combine(path, "Sample-After.txt")));
        Assert.False(System.IO.File.Exists(Path.Combine(path, "Sample-Before.txt")));
    }
}