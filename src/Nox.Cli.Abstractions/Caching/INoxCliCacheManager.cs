using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Abstractions.Caching;

public interface INoxCliCacheManager
{
    bool IsOnline { get; }
    bool IsExpired { get; }
    
    INoxCliCache? Cache { get; }
    
    IManifestConfiguration? Manifest { get; }

    List<IWorkflowConfiguration>? Workflows { get; }
    
    List<string> BuildLog { get; }
    
    IPersistedTokenCache? TokenCache { get; }

    event EventHandler<ICacheManagerBuildEventArgs> BuildEvent;

    void RefreshTemplate(string name);
}