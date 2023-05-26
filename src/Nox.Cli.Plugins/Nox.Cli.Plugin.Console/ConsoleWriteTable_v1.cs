using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Spectre.Console;

namespace Nox.Cli.Plugin.Console;

public class ConsoleWriteTable_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "console/write-table@v1",
            Author = "Jan Schutte",
            Description = "Outputs a table of key value pairs to the console",
            RequiresConsole = true,

            Inputs =
            {
                ["lines"] = new NoxActionInput {
                    Id = "lines",
                    Description = "The list of key value pairs to write to the console",
                    Default = new Dictionary<string, string>(),
                    IsRequired = true
                }
            }
        };
    }

    private Dictionary<string, string>? _lines;
    
    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _lines = inputs.Value<Dictionary<string, string>>("lines");
        return Task.CompletedTask;

    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        if (ctx.IsServer) throw new NoxCliException("This action cannot be executed on a server. remove the run-at-server attribute for this step in your Nox workflow.");
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_lines == null ||
            _lines.Count == 0)
        {
            ctx.SetErrorMessage("The write-table action was not initialized");
        }
        else 
        {
            try
            {
                var table = new Table();
                table.AddColumn("Property");
                table.AddColumn("Value");
                foreach (var line in _lines)
                {
                    table.AddRow(line.Key, $"[yellow]{line.Value}[/]");
                }
                AnsiConsole.Write(table);
                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage( ex.Message );
            }
        }

        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}