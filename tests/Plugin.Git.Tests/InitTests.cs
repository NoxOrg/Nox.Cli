using Moq;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Plugin.Git;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using TestHelpers;

namespace Plugin.Git.Tests;

public class InitTests
{
    [Fact]
    public async Task Can_initialize_a_local_git_repo()
    {
        var folder = "./files/repo";
        var gitFolder = Path.Combine(folder, ".git");
        FileHelpers.PurgeFolderRecursive(gitFolder, true);
        var plugin = new GitInit_v1();
        var inputs = new Dictionary<string, object>
        {
            {"path", folder},
            {"branch-name", "test"}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver);
        ctx.SetCurrentActionForTests();
        await plugin.ProcessAsync(ctx);
        Assert.Equal(ActionState.Success, ctx.CurrentAction!.State);
        Assert.True(Directory.Exists(gitFolder));
        FileHelpers.PurgeFolderRecursive(gitFolder, true);
    }
    
    [Fact]
    public async Task Must_get_warning_if_initializing_more_than_once()
    {
        var folder = "./files/repo";
        var gitFolder = Path.Combine(folder, ".git");
        FileHelpers.PurgeFolderRecursive(gitFolder, true);
        var plugin = new GitInit_v1();
        var inputs = new Dictionary<string, object>
        {
            {"path", folder},
            {"branch-name", "test"}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver);
        ctx.SetCurrentActionForTests();
        await plugin.ProcessAsync(ctx);
        Assert.Equal(ActionState.Success, ctx.CurrentAction!.State);
        Assert.True(Directory.Exists(gitFolder));
        await plugin.ProcessAsync(ctx);
        Assert.Equal(ActionState.Error, ctx.CurrentAction.State);
        Assert.StartsWith("warning: re-init: ignored", ctx.CurrentAction.ErrorMessage);
        FileHelpers.PurgeFolderRecursive(gitFolder, true);
    }
    
    [Fact]
    public async Task Must_not_get_warning_if_initializing_more_than_once_and_suppress_warnings_on()
    {
        var folder = "./files/repo";
        var gitFolder = Path.Combine(folder, ".git");
        FileHelpers.PurgeFolderRecursive(gitFolder, true);
        var plugin = new GitInit_v1();
        var inputs = new Dictionary<string, object>
        {
            {"path", folder},
            {"branch-name", "test"},
            {"suppress-warnings", true}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver);
        ctx.SetCurrentActionForTests();
        await plugin.ProcessAsync(ctx);
        Assert.Equal(ActionState.Success, ctx.CurrentAction!.State);
        Assert.True(Directory.Exists(gitFolder));
        await plugin.ProcessAsync(ctx);
        Assert.Equal(ActionState.Success, ctx.CurrentAction.State);
        FileHelpers.PurgeFolderRecursive(gitFolder, true);
    }
    
}