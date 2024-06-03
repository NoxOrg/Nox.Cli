using Moq;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Plugins.Powershell;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;

namespace Plugin.Powershell.Tests;

public class ScriptTests
{
    [Fact]
    public async Task Can_perform_a_git_pull()
    {
        Directory.SetCurrentDirectory("/home/jan/Projects/IWG/Fno.MultiDeploy");
        var plugin = new PowershellScript_v1();
        var inputs = new Dictionary<string, object>
        {
            {"script", "git pull --rebase iwgplc main"},
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
        Console.WriteLine(ctx.CurrentAction!.ErrorMessage!);
        Assert.Equal(ActionState.Success, ctx.State);
    }
}