using System.ComponentModel;

namespace Nox.Cli.Abstractions.Caching;

public interface INoxCliCache: IChangeTracking
{
    string Username { get; set; }
    string UserPrincipalName { get; set; }
    string TenantId { get; set; }
    
    Uri? RemoteUri { get; set; }
    
    DateTimeOffset Expires { get; set; }
    
    List<RemoteFileInfo> WorkflowInfo { get; set; }
    
    List<RemoteFileInfo> TemplateInfo { get; set; }

    void ClearChanges();
}