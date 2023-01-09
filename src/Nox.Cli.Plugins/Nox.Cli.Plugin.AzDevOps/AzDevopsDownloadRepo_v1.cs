using System.IO.Compression;
using Nox.Cli.Actions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsDownloadRepo_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/download-repo@v1",
            Author = "Jan Schutte",
            Description = "Download an Azure Devops repository",

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
            }
        };
    }

    private GitHttpClient? _gitClient;
    private Guid? _repoId;
    private string? _branchName;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
        _repoId = inputs.Value<Guid>("repository-id");
        _branchName = inputs.ValueOrDefault<string>("branch-name", this);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || _repoId == null || _repoId == Guid.Empty || string.IsNullOrEmpty(_branchName))
        {
            ctx.SetErrorMessage("The devops clone-repo action was not initialized");
        }
        else
        {
            try
            {
                var repoId = Guid.NewGuid();
                var repoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nox", "repositories");
                var zipFilePath = await DownloadRepo(repoId, repoPath);
                if (!string.IsNullOrEmpty(zipFilePath))
                {
                    var extractFolder = UnzipRepo(repoId, zipFilePath, repoPath);
                    File.Delete(zipFilePath);
                    outputs["local-repository-path"] = extractFolder;
                    ctx.SetState(ActionState.Success);    
                }
                else
                {
                    ctx.SetErrorMessage("Unable to download remote repository to local zip file.");
                }               
                
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }

        return outputs;
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
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

    private string UnzipRepo(Guid repoId, string zipFilePath, string destinationPath)
    {
        var extractFolder = Path.Combine(destinationPath, repoId.ToString());
        if (Directory.Exists(extractFolder)) Directory.Delete(extractFolder);
        ZipFile.ExtractToDirectory(zipFilePath, extractFolder);
        return extractFolder;
    }
}

