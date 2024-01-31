using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileDeleteFiles_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/delete-files@v1",
            Author = "Jan Schutte",
            Description = "Delete files in a folder using a search pattern.",

            Inputs =
            {
                ["folder"] = new NoxActionInput {
                    Id = "folder",
                    Description = "The folder in which to delete the files",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["search-pattern"] = new NoxActionInput {
                    Id = "search-pattern",
                    Description = "The search-pattern to use to determine which files to delete",
                    Default = string.Empty,
                    IsRequired = true
                }
                
            }
        };
    }

    private string? _folder;
    private string? _searchPattern;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _folder = inputs.Value<string>("folder");
        _searchPattern = inputs.Value<string>("search-pattern");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrWhiteSpace(_folder) ||
            string.IsNullOrWhiteSpace(_searchPattern))
        {
            ctx.SetErrorMessage("The File delete-files action was not initialized");
        }
        else
        {
            try
            {
                var dir = new DirectoryInfo(_folder);
                foreach (var file in dir.EnumerateFiles(_searchPattern))
                {
                    file.Delete();
                }
                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }
        
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}