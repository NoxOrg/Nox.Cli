using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Abstractions.Caching;

namespace Nox.Cli.Caching;

public class NoxCliCacheBuilder
{
    private readonly NoxCliCacheManager _manager;

    public NoxCliCacheBuilder(string remoteUrl, bool forceOffline, IPersistedTokenCache? tokenCache = null)
    {
        _manager = new NoxCliCacheManager(remoteUrl, forceOffline, tokenCache);
    }

    public NoxCliCacheBuilder ForServer()
    {
        _manager.ForServer();
        return this;
    }

    public NoxCliCacheBuilder WithCachePath(string cachePath)
    {
        _manager.UseCachePath(cachePath);
        return this;
    }

    public NoxCliCacheBuilder WithCacheFile(string cacheFile)
    {
        _manager.UseCacheFile(cacheFile);
        return this;
    }

    public NoxCliCacheBuilder WithLocalWorkflowPath(string localWorkflowPath)
    {
        _manager.UseLocalWorkflowPath(localWorkflowPath);
        return this;
    }

    public NoxCliCacheBuilder WithBuildEventHandler(EventHandler<ICacheManagerBuildEventArgs> buildEventHandler)
    {
        _manager.AddBuildEventHandler(buildEventHandler);
        return this;
    }
    
    public NoxCliCacheBuilder WithTenantId(string tenantId)
    {
        _manager.UseTenantId(tenantId);
        return this;
    }

    public INoxCliCacheManager Build() => _manager.Build();
}