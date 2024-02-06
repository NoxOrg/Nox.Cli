using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Configuration;

namespace Nox.Cli.Abstractions.Caching;

public interface INoxCliCacheManager
{
    bool IsOnline { get; }
    bool IsExpired { get; }
    
    INoxCliCache? Cache { get; }
    
    IManifestConfiguration? Manifest { get; }

    List<WorkflowConfiguration>? Workflows { get; }
    
    List<string> BuildLog { get; }
    
    IPersistedTokenCache? TokenCache { get; }

    event EventHandler<ICacheManagerBuildEventArgs> BuildEvent;

    void RefreshTemplate(string name);
}