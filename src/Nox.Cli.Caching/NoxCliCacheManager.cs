using System.Net;
using System.Text.Json;
using Newtonsoft.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Constants;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Configuration;
using Nox.Cli.Helpers;
using Nox.Yaml;
using RestSharp;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using IDeserializer = YamlDotNet.Serialization.IDeserializer;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Nox.Cli.Caching;

public class NoxCliCacheManager: INoxCliCacheManager
{
    private NoxCliCache? _cache;
    private string _cachePath;
    private string _cacheFile;
    private string _workflowCachePath;
    private string _templateCachePath;
    private string _localWorkflowPath;
    private bool _forceOffline;
    private bool _isServer;
    private readonly Uri _remoteUri;
    private Uri? _workflowUri;
    private List<string> _buildLog;
    private IManifestConfiguration? _manifest;
    private List<WorkflowConfiguration>? _workflows;
    private IPersistedTokenCache? _tokenCache;
    private IDeserializer _deserializer;
    private string? _tenantId;

    public NoxCliCacheManager(string? remoteUrl, bool forceOffline, IPersistedTokenCache? tokenCache = null)
    {
        _buildLog = new List<string>();
        if (string.IsNullOrEmpty(remoteUrl))
        {
            _remoteUri = new Uri("https://noxorg.dev");
        }
        else
        {
            _remoteUri = new Uri(remoteUrl);
        }

        _forceOffline = forceOffline;
        _cachePath = WellKnownPaths.CachePath;
        _workflowCachePath = WellKnownPaths.WorkflowsCachePath;
        _templateCachePath = WellKnownPaths.TemplatesCachePath;
        Directory.CreateDirectory(_cachePath);
        _cacheFile = WellKnownPaths.CacheFile;
        _localWorkflowPath = ".";
        _tokenCache = tokenCache;
        _deserializer = BuildDeserializer();
    }

    internal void ForServer()
    {
        _isServer = true;
    }

    internal void UseCachePath(string cachePath)
    {
        _cachePath = Path.GetFullPath(cachePath);
        _workflowCachePath = Path.Combine(_cachePath, "workflows");
        _templateCachePath = Path.Combine(_cachePath, "templates");
    }

    internal void UseCacheFile(string cacheFile)
    {
        _cacheFile = Path.GetFullPath(cacheFile);
    }

    internal void UseLocalWorkflowPath(string localWorkflowPath)
    {
        _localWorkflowPath = localWorkflowPath;
    }

    internal void UseTenantId(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId)) throw new NoxCliException("Tenant has not been configured!");
        SetTenantId(tenantId);
    }

    internal void AddBuildEventHandler(EventHandler<ICacheManagerBuildEventArgs> handler)
    {
        BuildEvent += handler;
    }

    public bool IsOnline {
        get
        {
            if (_forceOffline) return false;
            if (_remoteUri.Host == "localhost") return true;
            return PingHelper.ServicePing(_remoteUri.Host);
        }
    }

    public bool IsExpired
    {
        get
        {
            var now = new DateTimeOffset(DateTime.UtcNow);
            return _cache!.Expires < now;
        }
    }

    public INoxCliCache? Cache
    {
        get => _cache;
    }

    public IManifestConfiguration? Manifest
    {
        get => _manifest;
    }

    public List<WorkflowConfiguration>? Workflows
    {
        get => _workflows;
    }

    public List<string> BuildLog
    {
        get => _buildLog;
    }

    public IPersistedTokenCache? TokenCache => _tokenCache;

    public event EventHandler<ICacheManagerBuildEventArgs>? BuildEvent;
    
    public void RefreshTemplate(string name)
    {
        var templateInfo = _cache!.TemplateInfo.FirstOrDefault(ti => ti.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (IsOnline)
        {
            var client = new RestClient(GetRemoteUri($"/templateInfo/{name}"));
            var infoRequest = new RestRequest() { Method = Method.Get };
            infoRequest.AddHeader("Accept", "application/json");
            var fileInfo = JsonConvert.DeserializeObject<RemoteFileInfo>(client.Execute(infoRequest).Content!);
            
            if (fileInfo == null) throw new NoxCliException($"Unable to locate the template in the online cache.");
            
            if (templateInfo == null)
            {
                var fileContent = GetOnlineTemplate(name);
                File.WriteAllText(Path.Combine(_templateCachePath, name), fileContent);
                _cache.TemplateInfo.Add(fileInfo!);
            }
            else
            {
                if (fileInfo!.Size != templateInfo.Size || fileInfo.ShaChecksum != templateInfo.ShaChecksum)
                {
                    var fileContent = GetOnlineTemplate(name);
                    File.WriteAllText(Path.Combine(_templateCachePath, name), fileContent);
                }
            }
        }
    }

    internal INoxCliCacheManager Build()
    {
        _buildLog = new List<string>();
        GetOrCreateCache();
        Dictionary<string,string> yamlFiles = new(StringComparer.OrdinalIgnoreCase);
        if (_isServer)
        {
            if (string.IsNullOrEmpty(_cache!.TenantId)) throw new NoxCliException("Tenant has not been configured!");            

            GetOnlineWorkflowsAndManifest(yamlFiles);
            
            GetOnlineTemplates();
        }
        else
        {
            GetCredentialsFromAzureToken();
            RaiseBuildEvent($"Connecting to remote workflow cache server at: {_remoteUri}.");
            if (IsOnline)
            {
                GetOnlineWorkflowsAndManifest(yamlFiles);
                GetOnlineTemplates();
            }
            else
            {
                RaiseBuildEvent($"{Emoji.Known.ExclamationQuestionMark} [bold yellow]Unable to connect to remote workflow cache server at: {_remoteUri}.\nReverting to local cache.[/]");
                GetLocalWorkflowsAndManifest(yamlFiles);
            }
        }

        BuildDeserializer();
        ResolveManifest(yamlFiles);
        ResolveWorkflows(yamlFiles);
        Save();
        return this;
    }

    internal void GetOrCreateCache()
    {
        if (_remoteUri == null) throw new NoxCliException("Remote Uri has not been set!");
        _cache = new NoxCliCache(_remoteUri, _cachePath, _cacheFile);
        _cache.TenantId = _tenantId!;

        if (!File.Exists(_cacheFile) || _isServer) return;
        Load();
    }

    private void GetCredentialsFromAzureToken()
    {
        RaiseBuildEvent("[bold mediumpurple3_1]Checking your credentials...[/]");
        var auth = CredentialHelper.GetCredentialFromCacheOrBrowser().Result;
        if (_cache != null)
        {
            if (auth.AuthenticationRecord.Username == null)
            {
                RaiseBuildEvent($"{Emoji.Known.ExclamationQuestionMark} User principal name (UPN) not detected. Continuing without login.");
                return;
            }
    
            if (auth.AuthenticationRecord.TenantId == null)
            {
                RaiseBuildEvent($"{Emoji.Known.ExclamationQuestionMark} Tenant Id (TId) not detected. Continuing without login.");
                return;
            }

            _cache.Username = GetUsernameFromUpn(auth.AuthenticationRecord.Username);
            _cache.UserPrincipalName = auth.AuthenticationRecord.Username;
            SetTenantId(auth.AuthenticationRecord.TenantId);
            
            Save();
            RaiseBuildEvent($"{Emoji.Known.GreenCircle} Logged in as {auth.AuthenticationRecord.Username} on tenant {auth.AuthenticationRecord.TenantId}");
        }
    }

    private void Save()
    {
        if (_cache!.IsChanged)
        {
            File.WriteAllText(_cacheFile, JsonSerializer.Serialize(_cache));
            _cache.ClearChanges();
        }
    }

    private void Load()
    {
        _cache = JsonSerializer.Deserialize<NoxCliCache>(File.ReadAllText(_cacheFile))!;
        _cache.ClearChanges();
        ValidateLocalCache();
    }

    private void ValidateLocalCache()
    {
        var workflows = new List<RemoteFileInfo>();
        foreach (var item in _cache!.WorkflowInfo)
        {
            if (File.Exists(Path.Combine(_workflowCachePath, item.Name)))
            {
                workflows.Add(item);
            }
        }

        _cache.WorkflowInfo = workflows;

        var templates = new List<RemoteFileInfo>();
        foreach (var item in _cache!.TemplateInfo)
        {
            if (File.Exists(Path.Combine(_templateCachePath, item.Name)))
            {
                templates.Add(item);
            }
        }

        _cache.TemplateInfo = templates;
    }
    
    internal void GetOnlineWorkflowsAndManifest(IDictionary<string, string> yamlFiles)
    {
        try
        {
            var client = new RestClient(GetRemoteUri("/scripts"));

            var request = new RestRequest() { Method = Method.Get };

            request.AddHeader("Accept", "application/json");

            // Get list of files on server
            var onlineFilesJson = client.Execute(request);

            if (onlineFilesJson.Content == null) return;

            if (onlineFilesJson.ResponseStatus == ResponseStatus.Error)
            {
                throw new Exception($"GetOnlineWorkflowsAndManifest:-> {onlineFilesJson.ErrorException?.Message}");
            }

            var onlineFiles = JsonSerializer.Deserialize<List<RemoteFileInfo>>(onlineFilesJson.Content,  JsonOptions.Instance);

            // Read and cache the entries

            Directory.CreateDirectory(_workflowCachePath);

            var existingCacheList = Directory
                .GetFiles(_workflowCachePath, FileExtension.WorkflowDefinition)
                .Select(f => (new FileInfo(f)).Name).ToHashSet();

            var hasRefreshed = false;

            foreach (var file in onlineFiles!)
            {
                string? yaml = null;

                if (_cache!.WorkflowInfo == null
                    || !_cache.WorkflowInfo.Any(i => i.Name == file.Name)
                    || !_cache.WorkflowInfo.Any(i => i.Name == file.Name && i.ShaChecksum == file.ShaChecksum))
                {

                    request.Resource = file.Name;

                    yaml = client.Execute(request).Content;

                    if (yaml == null) throw new NoxCliException($"Couldn't download workflow {file.Name}");

                    var scriptFullPath = Path.Combine(_workflowCachePath, file.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(scriptFullPath)!);
                    File.WriteAllText(scriptFullPath, yaml);
                    hasRefreshed = true;
                }
                else
                {
                    yaml = File.ReadAllText(Path.Combine(_workflowCachePath, file.Name));
                }

                yamlFiles[file.Name] = yaml;

                if (existingCacheList.Contains(file.Name))
                {
                    existingCacheList.Remove(file.Name);
                }
            }

            foreach (var orphanEntry in existingCacheList)
            {
                File.Delete(Path.Combine(_workflowCachePath, orphanEntry));
            }
            
            
            
            
            // foreach (var entry in yamlFiles)
            // {
            //     yamlFiles[entry.Key] = YamlHelper.ResolveYamlReferences(Path.Combine(_workflowCachePath, entry.Key));
            // }

            _cache!.WorkflowInfo = onlineFiles;
            if (hasRefreshed) RaiseBuildEvent($"[bold yellow]Workflow cache successfully updated from remote.[/]");

        }
        catch (Exception ex)
        {
            throw new NoxCliException("Unable to retrieve online manifest and workflows!", ex);
        }
    }
    
    private void GetLocalWorkflowsAndManifest(Dictionary<string, string> yamlFiles)
    {
        var files = FindWorkflowsAndManifest(_workflowCachePath);

        var overriddenFiles = new List<string>(files.Length);

        foreach (var file in files)
        {
            var yaml = File.ReadAllText(file);

            var fileInfo = new FileInfo(file);

            if (yamlFiles.ContainsKey(fileInfo.Name))
            {
                var overriddenPath = fileInfo.DirectoryName;
                var overriddenFile = fileInfo.Name;
                var pathMarkup = $"[underline]{overriddenPath.EscapeMarkup()}[/]";
                if (overriddenPath != null && !overriddenFiles.Contains(pathMarkup))
                {
                    overriddenFiles.Add(pathMarkup);
                }
                overriddenFiles.Add(overriddenFile.Substring(0,overriddenFile.IndexOf('.')).EscapeMarkup());
            }

            yaml = YamlHelper.ResolveYamlReferences(file);
            yamlFiles[fileInfo.Name] = yaml;
        }
        if (overriddenFiles.Count > 0)
        {
            RaiseBuildEvent($"[bold yellow]Warning: Local files {string.Join(',', overriddenFiles.Skip(1).ToArray())} in local folder {overriddenFiles.First()} overrides remote workflow(s) with the same name[/]");
        }

    }

    private IDeserializer BuildDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            //.WithTypeMapping<IActionConfiguration, ActionConfiguration>()
            //.WithTypeMapping<ICliConfiguration, CliConfiguration>()
            //.WithTypeMapping<IJobConfiguration, JobConfiguration>()
            .WithTypeMapping<ICliCommandConfiguration, CliCommandConfiguration>()
            .WithTypeMapping<ILocalTaskExecutorConfiguration, LocalTaskExecutorConfiguration>()
            .WithTypeMapping<ISecretsConfiguration, SecretsConfiguration>()
            .WithTypeMapping<ISecretProviderConfiguration, SecretProviderConfiguration>()
            .WithTypeMapping<ISecretsValidForConfiguration, SecretsValidForConfiguration>()
            .WithTypeMapping<ICliAuthConfiguration, CliAuthConfiguration>()
            .WithTypeMapping<IRemoteTaskExecutorConfiguration, RemoteTaskExecutorConfiguration>()
            .Build();
    }
    
    internal void ResolveManifest(Dictionary<string, string> yamlFiles)
    {
        _manifest = yamlFiles
            .Where(kv => kv.Key.EndsWith(".cli.nox.yaml"))
            .Select(kv => _deserializer.Deserialize<ManifestConfiguration>(kv.Value))
            .FirstOrDefault();
    }
    
    internal void ResolveWorkflows(Dictionary<string, string> yamlFiles)
    {
        _workflows = new List<WorkflowConfiguration>();
        var workflowFiles = yamlFiles.Where(f => f.Key.EndsWith("workflow.nox.yaml", StringComparison.OrdinalIgnoreCase));
        foreach (var workflowFile in workflowFiles)
        {
            var reader = new YamlConfigurationReader<WorkflowConfiguration>()
                .WithFile(Path.Combine(_workflowCachePath, workflowFile.Key));
            try
            {
                var workflow = reader.Read();
                _workflows.Add(workflow);
            }
            catch(Exception ex)
            {
                throw new NoxCliException($"Unable to parse workflow file {workflowFile.Key}", ex);
            }
        }
    }
    
    private string[] FindWorkflowsAndManifest(string searchPath = "")
    {
        var searchPatterns = new string[] { FileExtension.WorkflowDefinition, "*.cli.nox.yaml" };

        var path = string.IsNullOrEmpty(searchPath) 
            ? new DirectoryInfo(Directory.GetCurrentDirectory())
            : new DirectoryInfo(searchPath);

        // current or supplied folder
        var files = GetFilesWithSearchPatterns(path, searchPatterns, SearchOption.TopDirectoryOnly);

        while (files.Length == 0)
        {
            // root
            if (path == null || path.Parent == null)
            {
                files = Array.Empty<FileInfo>();
                break;
            }

            // Find special NOX folder
            if (Directory.GetDirectories(path.FullName, @".nox").Length != 0)
            {
                path = new DirectoryInfo(Path.Combine(path.FullName,".nox"));
                files = GetFilesWithSearchPatterns(path, searchPatterns, SearchOption.AllDirectories);
                break;
            }

            // Stop in project root, after checking all sub-directories
            if (Directory.GetDirectories(path.FullName, @".git").Length != 0)
            {
                files = GetFilesWithSearchPatterns(path, searchPatterns, SearchOption.AllDirectories); ;
                break;
            }

            path = path.Parent;

            files = GetFilesWithSearchPatterns(path, searchPatterns, SearchOption.TopDirectoryOnly); ;
        }

        return files.Select(f => f.FullName).ToArray();
    }

    private FileInfo[] GetFilesWithSearchPatterns(DirectoryInfo path, string[] searchPatterns, SearchOption searchOption)
    {
        var files = new List<FileInfo>();
        if (path.Exists)
        {
            foreach (var pattern in searchPatterns) 
            {
                files.AddRange( path.GetFiles(pattern, searchOption) );
            }    
        }
        return files.ToArray();
    }
    
    internal void GetOnlineTemplates()
    {
        try
        {
            var hasRefreshed = false;
            var client = new RestClient(GetRemoteUri("/templates"));

            var fileListRequest = new RestRequest() { Method = Method.Get };

            fileListRequest.AddHeader("Accept", "application/json");

            // Get list of files on server
            var onlineFilesJson = client.Execute(fileListRequest);

            if (onlineFilesJson.Content == null) return;

            if (onlineFilesJson.ResponseStatus == ResponseStatus.Error)
            {
                throw new NoxCliException($"GetOnlineTemplates:-> {onlineFilesJson.ErrorException?.Message}");
            }

            if (onlineFilesJson.StatusCode != HttpStatusCode.OK) return;

            var onlineFiles = JsonSerializer.Deserialize<List<RemoteFileInfo>>(onlineFilesJson.Content,  JsonOptions.Instance);

            // Read and cache the entries

            Directory.CreateDirectory(_templateCachePath);

            var existingTemplateList = TraverseDirectory(_templateCachePath);

            var existingCacheList = existingTemplateList.Select(f => new FileInfo(f).Name).ToHashSet();

            var fileRequest = new RestRequest() { Method = Method.Post };

            fileRequest.AddHeader("Accept", "application/json");


            foreach (var file in onlineFiles!)
            {
                if (_cache!.TemplateInfo == null
                    || !_cache.TemplateInfo.Any(i => i.Name == file.Name)
                    || !_cache.TemplateInfo.Any(i => i.Name == file.Name && i.ShaChecksum == file.ShaChecksum))
                {
                    try
                    {
                        var fileContent = GetOnlineTemplate(file.Name);
                        var templateFullPath = Path.Combine(_templateCachePath, file.Name);
                        Directory.CreateDirectory(Path.GetDirectoryName(templateFullPath)!);
                        File.WriteAllText(templateFullPath, fileContent);
                    }
                    catch
                    {
                        //ignore here so that cli client/server can at least start
                    }
                    
                    hasRefreshed = true;
                }

                if (existingCacheList.Contains(file.Name))
                {
                    existingCacheList.Remove(file.Name);
                }
            }

            foreach (var orphanEntry in existingCacheList)
            {
                File.Delete(Path.Combine(_templateCachePath, orphanEntry));
            }

            _cache!.TemplateInfo = onlineFiles;
            Save();
            if (hasRefreshed) RaiseBuildEvent($"[bold yellow]Template cache successfully updated from remote.[/]");
        }
        catch (Exception ex)
        {
            throw new NoxCliException("Unable to retrieve online templates.", ex);
        }
    }
    
    private List<string> TraverseDirectory(string directory)
    {
        var result = new List<string>();
        foreach (var file in Directory.GetFiles(directory))
        {
            result.Add(file);
        }

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            result.AddRange(TraverseDirectory(subDirectory));
        }

        return result;
    }

    private void RaiseBuildEvent(string message)
    {
        var plainMessage = message.RemoveMarkup();
        _buildLog.Add(plainMessage);
        BuildEvent?.Invoke(this, new CacheManagerBuildEventArgs(plainMessage, message));
    }

    internal string GetOnlineTemplate(string name)
    {
        using var client = new RestClient(GetRemoteUri($"/templates/{name}"));
        var fileRequest = new RestRequest() { Method = Method.Get };
        fileRequest.AddHeader("Accept", "application/json");
        var fileContent = client.Execute(fileRequest).Content;
        if (fileContent == null) throw new Exception($"Couldn't download template {name}");
        return fileContent;
    }

    private string GetUsernameFromUpn(string upn)
    {
        var result = upn;
        if (upn.Contains('@'))
        {
            result = upn.Substring(0, upn.IndexOf('@'));
        }

        result = result.Replace('.', ' ');
        return result;
    }

    private void SetTenantId(string tenantId)
    {
        _tenantId = tenantId;
        _workflowUri = new Uri(_remoteUri, $"workflows/{tenantId}");
    }

    private Uri GetRemoteUri(string path)
    {
        var uriBuilder = new UriBuilder(_workflowUri!);
        uriBuilder.Path += path;
        return uriBuilder.Uri;
    }
    
}