using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Caching;
using Nox.Cli.Configuration;
using Nox.Cli.PersonalAccessToken;
using Nox.Cli.Plugin.AzDevOps;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;
namespace Plugin.AzDevOps.Tests;
public class GroupTests: IClassFixture<DevOpsIntegrationFixture>
{
    private readonly DevOpsIntegrationFixture _fixture;
    public GroupTests(DevOpsIntegrationFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Theory]
    [InlineData("NOX_PROJECTS_ALL", true)]
    [InlineData("NOX_PROJECT_DOESNOTEXIST", false)]
    public async Task Can_verify_whether_an_aad_group_exists_or_not(string groupName, bool result)
    {
        var wfConfig = new WorkflowConfiguration();
        var sln = _fixture.ServiceProvider.GetRequiredService<NoxSolution>();
        var orgResolver = _fixture.ServiceProvider.GetRequiredService<IOrgSecretResolver>();
        var cacheManager = _fixture.ServiceProvider.GetRequiredService<INoxCliCacheManager>();
        var lteConfig = _fixture.ServiceProvider.GetRequiredService<LocalTaskExecutorConfiguration>();
        var secretsResolver = _fixture.ServiceProvider.GetRequiredService<INoxSecretsResolver>();
        var tokenCache = _fixture.ServiceProvider.GetRequiredService<IPersistedTokenCache>();
        var accessToken = await CredentialHelper.GetAzureDevOpsAccessToken();
        var patProvider = new AzDevOpsPatProvider(tokenCache, "iwgplc");
        var pat = await patProvider.GetPat(accessToken!);
        
        var plugin = new AzDevOpsVerifyAadGroup_v1();
        var inputs = new Dictionary<string, object>
        {
            {"server", "https://dev.azure.com/iwgplc"},
            {"personal-access-token", pat},
            {"project-id", "d6aee400-9659-4dec-a309-673518d4cc30"},
            {"aad-group-name", groupName}
        };
        await plugin.BeginAsync(inputs);
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheManager, lteConfig, secretsResolver, null!);
        var pluginOutput = await plugin.ProcessAsync(ctx);
        Assert.Equal(result, pluginOutput["is-found"]);
    }
}