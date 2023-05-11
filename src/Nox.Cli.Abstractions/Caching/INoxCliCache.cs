using System.ComponentModel;

namespace Nox.Cli.Abstractions.Caching;

public interface INoxCliCache: IChangeTracking
{
    string UserPrincipalName { get; set; }
    string TenantId { get; set; }
    
    string RemoteUrl { get; set; }
    
    DateTimeOffset Expires { get; set; }
    
    List<RemoteFileInfo> WorkflowInfo { get; set; }
    
    List<RemoteFileInfo> TemplateInfo { get; set; }

    void ClearChanges();
}