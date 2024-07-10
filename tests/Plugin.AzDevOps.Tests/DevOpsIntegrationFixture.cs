using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nox;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Caching;
using Nox.Cli.Configuration;
using Nox.Cli.PersonalAccessToken;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;
namespace Plugin.AzDevOps.Tests;

public class DevOpsIntegrationFixture
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IServiceCollection? Services { get; private set; }
    public DevOpsIntegrationFixture()
    {
        Services = new ServiceCollection();
        Services.AddSingleton(Mock.Of<NoxSolution>());
        Services.AddOrgSecretResolver();
        Services.AddSingleton(Mock.Of<LocalTaskExecutorConfiguration>());
        Services.AddPersistedSecretStore();
        Services.AddSingleton<INoxSecretsResolver, NoxSecretsResolver>();
        Services.AddSingleton<AzDevOpsPatProvider>();
        Services.AddNoxTokenCache();
        var onlineCacheUrl = "https://noxorg.dev";
        var persistedTokenCache = Services.BuildServiceProvider().GetRequiredService<IPersistedTokenCache>();
        var cacheBuilder = new NoxCliCacheBuilder(onlineCacheUrl, false, persistedTokenCache);
        var cacheManager = cacheBuilder.Build();
        Services.AddNoxCliCacheManager(cacheManager);
        ServiceProvider = Services.BuildServiceProvider();
    }
}