using Nox.Cli.Abstractions;
using Nox.Solution;

namespace Nox.Cli.Commands;

using Helpers;
using Microsoft.Extensions.Configuration;
using Nox.Cli.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public class DynamicCommand : NoxCliCommand<DynamicCommand.Settings>
{
    private readonly INoxWorkflowExecutor _executor;

    public DynamicCommand(
        INoxWorkflowExecutor executor,
        IAnsiConsole console, 
        IConsoleWriter consoleWriter,
        NoxSolution solution)
        : base(console, consoleWriter, solution)
    {
        _executor = executor;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path")]
        public string DesignFolderPath { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await base.ExecuteAsync(context, settings);
        
        var workflow = (WorkflowConfiguration)context.Data!;

        return await _executor.Execute(workflow, context.Remaining) ? 0 : 1;
    }

}

