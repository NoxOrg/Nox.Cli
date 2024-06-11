using Moq;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Plugin.File;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using TestHelpers;
using Xunit;

namespace Plugin.File.Tests;

public class CopyFolderTests
{
    [Fact]
    public async Task Can_copy_a_File_if_destination_folder_does_not_exist()
    {
        var path = "./files/copy-folder";
        //Ensure the target folder does not exist
        FileHelpers.PurgeFolderRecursive(Path.Combine(path, "target"), true);
        
        var plugin = new FileCopyFolder_v1();
        var inputs = new Dictionary<string, object>
        {
            {"source-path", Path.Combine(path, "source")},
            {"target-path", Path.Combine(path, "target")},
            {"is-recursive", true}
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
        Assert.True(System.IO.File.Exists(Path.Combine(path, "target/Sample.txt")));
        Assert.True(System.IO.File.Exists(Path.Combine(path, "target/child/Sample.txt")));
    }
}