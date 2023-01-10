using System.IO.Compression;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Actions;

namespace Nox.Cli.Plugin.File;

public class FileUnzipArchive_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/unzip-archive@v1",
            Author = "Jan Schutte",
            Description = "Unzip an archive to a destination folder.",
            
            Inputs =
            {
                ["archive-path"] = new NoxActionInput {
                    Id = "zip-archive-path",
                    Description = "The path to the zip archive to be unzipped.",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["destination-path"] = new NoxActionInput {
                    Id = "destination-path",
                    Description = "The path where the zip contents must be unzipped to.",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["delete-archive"] = new NoxActionInput {
                    Id = "delete-archive",
                    Description = "Indicate whether the zip archive must be deleted after the unzip has completed.",
                    Default = false,
                    IsRequired = false
                },
            },
            
        };
    }

    private string? _archivePath;
    private string _destinationPath;
    private bool? _deleteArchive;

    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        _archivePath = inputs.Value<string>("archive-path");
        _destinationPath = inputs.Value<string>("destination-path");
        _deleteArchive = inputs.ValueOrDefault<bool>("delete-archive", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        try
        {
            if (string.IsNullOrEmpty(_archivePath) || string.IsNullOrEmpty(_archivePath))
            {
                ctx.SetErrorMessage("The File unzip-archive action was not initialized");
            }
            else
            {
                try
                {
                    var zipArchiveFullPath = Path.GetFullPath(_archivePath);
                    UnzipFile(_archivePath, _destinationPath);
                    
                    if (_deleteArchive.Value)
                    {
                        System.IO.File.Delete(zipArchiveFullPath);
                    }
                    ctx.SetState(ActionState.Success);
                }
                catch (Exception ex)
                {
                    ctx.SetErrorMessage(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
    
    private static void UnzipFile(string zipFilePath, string destinationRoot)
    {
        var extractFolder = Path.GetFullPath(destinationRoot);
        if (Directory.Exists(extractFolder)) Directory.Delete(extractFolder);
        const int THRESHOLD_ENTRIES = 10000;
        const int THRESHOLD_SIZE = 10 * 1024 * 1024; // 10 MB
        const double THRESHOLD_RATIO = 10;
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

            using (var outputStream = System.IO.File.Open(destinationFullPath, FileMode.CreateNew))
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
                    outputStream.Write(buffer);
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
    }
}