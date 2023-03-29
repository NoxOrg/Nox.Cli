using System.IO.Compression;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsDownloadRepoBranch_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/download-repo-branch@v1",
            Author = "Jan Schutte",
            Description = "Download a branch from an Azure Devops repository",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["repository-id"] = new NoxActionInput {
                    Id = "repository-id",
                    Description = "The Id (Guid) of the devops repository. Normally the output from 'azdevops/get-repo@v1'",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["branch-name"] = new NoxActionInput { 
                    Id = "branch-name", 
                    Description = "The name of the branch to clone, defaults to 'main'",
                    Default = "main",
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["local-repository-path"] = new NoxActionOutput {
                    Id = "local-repository-path",
                    Description = "The path to the locally downloaded repository",
                },
                ["download-date-time"] = new NoxActionOutput {
                    Id = "download-date-time",
                    Description = "The date time this repo was downloaded. This is typically the reference-date-time input of azdevops/merge-folder@v1",
                },
            }
        };
    }

    private GitHttpClient? _gitClient;
    private Guid? _repoId;
    private string? _branchName;
    private bool _isServerContext;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
        _repoId = inputs.Value<Guid>("repository-id");
        _branchName = inputs.ValueOrDefault<string>("branch-name", this);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || _repoId == null || _repoId == Guid.Empty || string.IsNullOrEmpty(_branchName))
        {
            ctx.SetErrorMessage("The devops download-repo-branch action was not initialized");
        }
        else
        {
            try
            {
                var repoId = Guid.NewGuid();
                var repoPath = Path.Combine(Path.GetTempPath(), "nox", "repositories");
                var zipFilePath = await DownloadRepo(repoId, repoPath);
                if (!string.IsNullOrEmpty(zipFilePath))
                {
                    var extractFolder = UnzipRepo(repoId, zipFilePath, repoPath);
                    File.Delete(zipFilePath);
                    outputs["local-repository-path"] = extractFolder;
                    outputs["download-date-time"] = DateTime.Now;
                    ctx.SetState(ActionState.Success);    
                }
                else
                {
                    ctx.SetErrorMessage("Unable to download remote repository branch to local zip file.");
                }               
                
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }

        return outputs;
    }

    public Task EndAsync()
    {
        if (!_isServerContext) _gitClient?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<string> DownloadRepo(Guid repoId, string downloadFolder)
    {
        var result = "";
        var branch = new GitVersionDescriptor
        {
            Version = _branchName,
            VersionType = GitVersionType.Branch
        };
        var item = await _gitClient!.GetItemAsync(_repoId!.Value, "/", recursionLevel: VersionControlRecursionType.None, versionDescriptor: branch);
        var zipStream = await _gitClient.GetTreeZipAsync(_repoId!.Value, item.ObjectId);
        Directory.CreateDirectory(downloadFolder);
        result = Path.Combine(downloadFolder, $"{repoId}.zip");
        if (File.Exists(result)) File.Delete(result);
        var fileStream = File.Create(result);
        await zipStream.CopyToAsync(fileStream);
        fileStream.Close();
        return result;
    }

    private static string UnzipRepo(Guid repoId, string zipFilePath, string destinationRoot)
    {
        var extractFolder = Path.Combine(destinationRoot, repoId.ToString());
        if (Directory.Exists(extractFolder)) Directory.Delete(extractFolder);
        var THRESHOLD_ENTRIES = 10000;
        var THRESHOLD_SIZE = 10 * 1024 * 1024; // 10 MB
        double THRESHOLD_RATIO = 10;
        var totalSizeArchive = 0;
        var totalEntryArchive = 0;

        var destinationDirectoryFullPath = Path.GetFullPath(extractFolder);
        using var zipToOpen = new FileStream(zipFilePath, FileMode.Open);
        using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            totalEntryArchive ++;
            
            if (entry.Length == 0) continue;
            
            var destinationPath = Path.Combine(destinationDirectoryFullPath, entry.FullName);
            var destinationFullPath = Path.GetFullPath(destinationPath);
            if (!destinationFullPath.StartsWith(destinationDirectoryFullPath))
            {
                throw new NoxCliException("Attempting to extract archive entry outside destination directory");
            }

            
            var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
            Directory.CreateDirectory(destinationDirectory!);

            using (var outputStream = File.Open(destinationFullPath, FileMode.CreateNew))
            using (var inputStream = entry.Open())
            {
                var buffer = new byte[1024];
                var totalSizeEntry = 0;
                var numBytesRead = 0;

                do
                {
                    numBytesRead = inputStream.Read(buffer, 0, 1024);
                    totalSizeEntry += numBytesRead;
                    totalSizeArchive += numBytesRead;
                    var compressionRatio = (double)totalSizeEntry / entry.CompressedLength;

                    if(compressionRatio > THRESHOLD_RATIO)
                    {
                        throw new NoxCliException("Ratio between compressed and uncompressed data is highly suspicious, looks like a Zip Bomb Attack");
                    }
                    outputStream.Write(buffer, 0, numBytesRead);
                }
                while (numBytesRead > 0);
                outputStream.Flush();
                outputStream.Close();
            }

            if(totalSizeArchive > THRESHOLD_SIZE) {
                throw new NoxCliException("The uncompressed data size is too much for the application resource capacity");
            }

            if(totalEntryArchive > THRESHOLD_ENTRIES) {
                throw new NoxCliException("Too many entries in this archive, can lead to inodes exhaustion of the system");
            }
        }
        
        return extractFolder;
    }
}

