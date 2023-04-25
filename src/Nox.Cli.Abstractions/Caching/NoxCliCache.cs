using System.ComponentModel;
using System.Text.Json;

namespace Nox.Cli.Abstractions.Caching;

public class NoxCliCache : IChangeTracking
{
    private string _upn = null!;
    public string Upn
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

    private string _tid = null!;
    public string Tid
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

    public string CacheFile { get; private set; } = string.Empty;

    public bool IsChanged { get; private set; }
    public void AcceptChanges() => IsChanged = false;
    public NoxCliCache() { }

    public NoxCliCache(string cacheFile)
    {
        CacheFile = cacheFile;
    }

    public void Save()
    {
        if (IsChanged)
        {
            File.WriteAllText(CacheFile, JsonSerializer.Serialize(this));
            IsChanged = false;
        }
    }

    public static NoxCliCache Load(string cacheFile)
    {
        var cache = JsonSerializer.Deserialize<NoxCliCache>(File.ReadAllText(cacheFile))!;
        cache.CacheFile = cacheFile;
        cache.IsChanged = false;
        return cache;
    }

}

