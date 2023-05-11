using Nox.Cli.Abstractions.Caching;

namespace Nox.Cli.Caching;

public class NoxCliCacheBuilder
{
    private readonly NoxCliCacheManager _manager;
    
    public NoxCliCacheBuilder(string remoteUrl)
    {
        _manager = new NoxCliCacheManager(remoteUrl);
    }

    public NoxCliCacheBuilder WithCustomSecurity()
    {
        _manager.UseCustomSecurity();
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
    
    public NoxCliCacheBuilder WithTentantId(string tenantId)
    {
        _manager.UseTenantId(tenantId);
        return this;
    }

    public INoxCliCacheManager Build() => _manager.Build();
}