using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;

namespace Nox.Cli.Caching;

public class NoxCliCache : INoxCliCache
{
    private string _username = string.Empty;
    
    private string _upn = string.Empty;

    private string _tid = string.Empty;

    private Uri? _remoteUri;

    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                IsChanged = true;
            }
        }
    }

    public string UserPrincipalName
    {
        get => _upn;
        set
        {
            if (_upn != value)
            {
                _upn = value;
                IsChanged = true;
            }
        }
    }

    public Uri? RemoteUri
    {
        get => _remoteUri;
        set
        {
            if (_remoteUri != value)
            {
                _remoteUri = value;
                IsChanged = true;
            }
        }
    }

    public string TenantId
    {
        get => _tid;
        set
        {
            if (_tid != value)
            {
                _tid = value;
                IsChanged = true;
            }
        }
    }

    private DateTimeOffset _expires;
    
    public DateTimeOffset Expires
    {
        get => _expires;
        set
        {
            if (_expires != value)
            {
                _expires = value;
                IsChanged = true;
            }
        }
    }

    private List<RemoteFileInfo> _workflowInfo = new();
    
    public bool IsExpired
    {
        get => Expires < DateTime.Now;
    }

    public List<RemoteFileInfo> WorkflowInfo
    {
        get => _workflowInfo;
        set
        {
            var intersect = _workflowInfo.Intersect(value);
            var count = intersect.ToList().Count;
            if (count != value.Count || count != _workflowInfo.Count)
            {
                _workflowInfo = value;
                IsChanged = true;
            }
        }
    }

    private List<RemoteFileInfo> _templateInfo = new();

    public List<RemoteFileInfo> TemplateInfo
    {
        get => _templateInfo;
        set
        {
            var intersect = _templateInfo.Intersect(value);
            var count = intersect.ToList().Count;
            if (count != value.Count || count != _templateInfo.Count)
            {
                _templateInfo = value;
                IsChanged = true;
            }
        }
    }

    public bool IsChanged { get; private set; }
    public void AcceptChanges() => IsChanged = false;

    public NoxCliCache()
    {
        
    }
    
    public NoxCliCache(Uri remoteUri, string cachePath, string cacheFile)
    {
        _remoteUri = remoteUri;
        Expires = new DateTimeOffset(DateTime.Now.AddDays(7));
    }

    public void ClearChanges()
    {
        IsChanged = false;
    }
}

