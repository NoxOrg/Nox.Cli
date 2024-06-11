using Moq;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Plugins.Powershell;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;

namespace Plugin.Powershell.Tests;

public class PwshTests
{
    [Fact]
    public async Task Can_copy_a_File_if_destination_folder_does_not_exist()
    {
        Directory.SetCurrentDirectory("./files/no-git");
        var plugin = new PowershellScript_v1();
        var inputs = new Dictionary<string, object>
        {
            {"script", "git init -b main && git add . && git commit -m \"Initial Commit\""}
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
        Assert.True(Directory.Exists(".git"));
        FileHelpers.PurgeFolderRecursive(".git", false);
    }
}