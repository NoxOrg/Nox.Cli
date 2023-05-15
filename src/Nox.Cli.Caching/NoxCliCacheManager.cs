using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Configuration;
using Nox.Core.Constants;
using Nox.Core.Exceptions;
using Nox.Core.Helpers;
using Nox.Utilities.Configuration;
using Nox.Utilities.Secrets;
using RestSharp;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Nox.Cli.Caching;

public class NoxCliCacheManager: INoxCliCacheManager
{
    private INoxCliCache? _cache;
    private string _cachePath;
    private string _cacheFile;
    private string _workflowCachePath;
    private string _templateCachePath;
    private string _localWorkflowPath;
    private string _tenantId;
    private string _workflowUrl;
    private string _templateUrl;
    private bool _isServer;
    private readonly string _remoteUrl;
    private List<string> _buildLog;
    private IManifestConfiguration? _manifest;
    private List<IWorkflowConfiguration>? _workflows;

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
        _tenantId = tenantId;
    }

    internal void AddBuildEventHandler(EventHandler<ICacheManagerBuildEventArgs> handler)
    {
        BuildEvent += handler;
    }

    public bool IsOnline {
        get
        {
            if (string.IsNullOrEmpty(_remoteUrl)) return false;
            var ping = new Ping();
            try
            {
                var uri = new Uri(_remoteUrl);
                var reply = ping.Send(uri.Host, 3000);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            catch { 
                // Ignore
            }
            return false;
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

    public List<IWorkflowConfiguration>? Workflows
    {
        get => _workflows;
    }

    public List<string> BuildLog
    {
        get => _buildLog;
    }
    
    public event EventHandler<ICacheManagerBuildEventArgs>? BuildEvent;
    
    public void RefreshTemplate(string name)
    {
        var templateInfo = _cache!.TemplateInfo.FirstOrDefault(ti => ti.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (templateInfo == null) throw new NoxCliException($"Unable to find template {name} in the template cache.");
        if (!_isServer && IsOnline)
        {
            var client = new RestClient(_templateUrl);
            var infoRequest = new RestRequest("Info") { Method = Method.Post };
            infoRequest.AddHeader("Accept", "application/json");
            infoRequest.AddJsonBody($"{{\"FilePath\": \"{name}\"}}");
            var fileInfo = JsonConvert.DeserializeObject<RemoteFileInfo>(client.Execute(infoRequest).Content!);
            if (fileInfo!.Size != templateInfo.Size || fileInfo.ShaChecksum != templateInfo.ShaChecksum)
            {
                var fileContent = GetOnlineTemplate(name);
                File.WriteAllText(Path.Combine(_templateCachePath, name), fileContent);
            }
        }
    }

    public NoxCliCacheManager(string remoteUrl)
    {
        _buildLog = new List<string>();
        _remoteUrl = remoteUrl;
        _cachePath = WellKnownPaths.CachePath;
        _workflowCachePath = WellKnownPaths.WorkflowsCachePath;
        _templateCachePath = WellKnownPaths.TemplatesCachePath;
        _tenantId = "";
        _workflowUrl = "";
        _templateUrl = "";
        Directory.CreateDirectory(_cachePath);
        _cacheFile = WellKnownPaths.CacheFile;
        _localWorkflowPath = ".";
    }

    internal INoxCliCacheManager Build()
    {
        _buildLog = new List<string>();
        GetOrCreateCache();
        Dictionary<string,string> yamlFiles = new(StringComparer.OrdinalIgnoreCase);
        if (_isServer)
        {
            _cache!.TenantId = _tenantId;
            SetRemoteUrls();
            GetOnlineWorkflowsAndManifest(yamlFiles);
            GetOnlineTemplates();
        }
        else
        {
            GetCredentialsFromAzureToken();
            if (IsOnline)
            {
                GetOnlineWorkflowsAndManifest(yamlFiles);
                GetOnlineTemplates();
                GetLocalWorkflowsAndManifest(yamlFiles);
            }
        }
        
        var deserializer = BuildDeserializer();
        ResolveManifest(deserializer, yamlFiles);
        ResolveWorkflows(deserializer, yamlFiles);
        
        Save();
        return this;
    }

    private void GetOrCreateCache()
    {
        _cache = new NoxCliCache(_remoteUrl, _cachePath, _cacheFile);

        if (!File.Exists(_cacheFile) || _isServer) return;
        Load();
    }

    private void GetCredentialsFromAzureToken()
    {
        RaiseBuildEvent("[bold mediumpurple3_1]Checking your credentials...[/]");
        var auth = AzureSecretProvider.GetCredentialFromCacheOrBrowser().Result;
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
            _cache.UserPrincipalName = auth.AuthenticationRecord.Username;
            _tenantId = auth.AuthenticationRecord.TenantId;
            _cache.TenantId = _tenantId;
            SetRemoteUrls();
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
    }
    
    private void GetOnlineWorkflowsAndManifest(IDictionary<string, string> yamlFiles)
    {
        try
        {
            var client = new RestClient(_workflowUrl);

            var request = new RestRequest() { Method = Method.Get };

            request.AddHeader("Accept", "application/json");

            // Get list of files on server
            var onlineFilesJson = client.Execute(request);

            if (onlineFilesJson.Content == null) return;

            if (onlineFilesJson.ResponseStatus == ResponseStatus.Error)
            {
                throw new Exception($"GetOnlineWorkflowsAndManifest:-> {onlineFilesJson.ErrorException?.Message}");
            }

            var onlineFiles = JsonSerializer.Deserialize<List<RemoteFileInfo>>(onlineFilesJson.Content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Read and cache the entries

            Directory.CreateDirectory(_workflowCachePath);

            var existingCacheList = Directory
                .GetFiles(_workflowCachePath, FileExtension.WorflowDefinition)
                .Select(f => (new FileInfo(f)).Name).ToHashSet();

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

                    File.WriteAllText(Path.Combine(_workflowCachePath, file.Name), yaml);
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

            _cache!.WorkflowInfo = onlineFiles;

            foreach (var entry in yamlFiles)
            {
                yamlFiles[entry.Key] = YamlHelper.ResolveYamlReferences(Path.Combine(_workflowCachePath, entry.Key));
            }
        }
        catch (Exception ex)
        {
            throw new NoxCliException("Unable to retrieve online manifest and workflows!", ex);
        }
    }
    
    private void GetLocalWorkflowsAndManifest(Dictionary<string, string> yamlFiles)
    {
        var files = FindWorkflowsAndManifest(_localWorkflowPath);

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
            .WithTypeMapping<IActionConfiguration, ActionConfiguration>()
            .WithTypeMapping<ICliConfiguration, CliConfiguration>()
            .WithTypeMapping<IStepConfiguration, StepConfiguration>()
            .WithTypeMapping<ICliCommandConfiguration, CliCommandConfiguration>()
            .WithTypeMapping<ILocalTaskExecutorConfiguration, LocalTaskExecutorConfiguration>()
            .WithTypeMapping<ISecretsConfiguration, SecretsConfiguration>()
            .WithTypeMapping<ISecretProviderConfiguration, SecretProviderConfiguration>()
            .WithTypeMapping<ISecretsValidForConfiguration, SecretsValidForConfiguration>()
            .WithTypeMapping<ICliAuthConfiguration, CliAuthConfiguration>()
            .WithTypeMapping<IRemoteTaskExecutorConfiguration, RemoteTaskExecutorConfiguration>()
            .Build();
    }
    
    private void ResolveManifest(IDeserializer deserializer, Dictionary<string, string> yamlFiles)
    {
        _manifest = yamlFiles
            .Where(kv => kv.Key.EndsWith(".cli.nox.yaml"))
            .Select(kv => deserializer.Deserialize<ManifestConfiguration>(kv.Value))
            .FirstOrDefault();
    }
    
    private void ResolveWorkflows(IDeserializer deserializer, Dictionary<string, string> yamlFiles)
    {
        _workflows = new List<IWorkflowConfiguration>();
        foreach (var yaml in yamlFiles.Where(kv => kv.Key.EndsWith(FileExtension.WorflowDefinition.TrimStart('*'))))
        {
            try
            {
                _workflows.Add(deserializer.Deserialize<WorkflowConfiguration>(yaml.Value));
            }
            catch (Exception ex)
            {
                throw new NoxYamlException($"Unable to deserialize workflow {yaml.Key}. {ex.Message}");
            }
        }
    }
    
    private string[] FindWorkflowsAndManifest(string searchPath = "")
    {
        var searchPatterns = new string[] { FileExtension.WorflowDefinition, "*.cli.nox.yaml" };

        var path = string.IsNullOrEmpty(searchPath) 
            ? new DirectoryInfo(Directory.GetCurrentDirectory())
            : new DirectoryInfo(searchPath);

        // current or supplied folder
        var files = GetFilesWithSearchPatterns(path, searchPatterns, SearchOption.TopDirectoryOnly);

        while (!files.Any())
        {
            // root
            if (path == null || path.Parent == null)
            {
                files = Array.Empty<FileInfo>();
                break;
            }

            // Find special NOX folder
            if (Directory.GetDirectories(path.FullName, @".nox").Any())
            {
                path = new DirectoryInfo(Path.Combine(path.FullName,".nox"));
                files = GetFilesWithSearchPatterns(path, searchPatterns, SearchOption.AllDirectories);
                break;
            }

            // Stop in project root, after checking all sub-directories
            if (Directory.GetDirectories(path.FullName, @".git").Any())
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
        foreach (var pattern in searchPatterns) 
        {
            files.AddRange( path.GetFiles(pattern, searchOption) );
        }
        return files.ToArray();
    }
    
    private void GetOnlineTemplates()
    {
        try
        {
            var client = new RestClient(_templateUrl);

            var fileListRequest = new RestRequest() { Method = Method.Get };

            fileListRequest.AddHeader("Accept", "application/json");

            // Get list of files on server
            var onlineFilesJson = client.Execute(fileListRequest);

            if (onlineFilesJson.Content == null) return;

            if (onlineFilesJson.ResponseStatus == ResponseStatus.Error)
            {
                throw new NoxCliException($"GetOnlineTemplates:-> {onlineFilesJson.ErrorException?.Message}");
            }

            var onlineFiles = JsonSerializer.Deserialize<List<RemoteFileInfo>>(onlineFilesJson.Content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

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

                    var fileContent = GetOnlineTemplate(file.Name);
                    var templateFullPath = Path.Combine(_templateCachePath, file.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(templateFullPath)!);
                    File.WriteAllText(templateFullPath, fileContent);
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

    private string GetOnlineTemplate(string name)
    {
        var client = new RestClient(_templateUrl);
        var fileRequest = new RestRequest() { Method = Method.Post };
        fileRequest.AddHeader("Accept", "application/json");
        fileRequest.AddJsonBody($"{{\"FilePath\": \"{name}\"}}");
        var fileContent = client.Execute(fileRequest).Content;

        if (fileContent == null) throw new Exception($"Couldn't download template {name}");
        return fileContent;
    }

    private void SetRemoteUrls()
    {
        if (string.IsNullOrEmpty(_tenantId)) throw new NoxCliException("Tenant Id has not been set!");
        _workflowUrl = $"{_remoteUrl}/workflows/{_tenantId}";
        _templateUrl = $"{_remoteUrl}/templates/{_tenantId}";
    }
    
}