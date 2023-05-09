using System.ComponentModel;
using Nox.Cli.Abstractions.Caching;

namespace Nox.Cli.Abstractions;

public interface INoxCliCache: IChangeTracking
{
    string Tid { get; set; }
    
    DateTimeOffset Expires { get; set; }
    
    List<RemoteFileInfo> WorkflowInfo { get; set; }
    
    List<RemoteFileInfo> TemplateInfo { get; set; }
    
    string CacheFile { get; }

    void Save();

    void Load(string cacheFile);
}