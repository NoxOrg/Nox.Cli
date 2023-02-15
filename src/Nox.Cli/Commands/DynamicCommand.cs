using Nox.Cli.Abstractions;
using Nox.Cli.Authentication;
using Nox.Cli.Server.Integration;

namespace Nox.Cli.Commands;

using Helpers;
using Microsoft.Extensions.Configuration;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public class DynamicCommand : NoxCliCommand<DynamicCommand.Settings>
{
    private readonly INoxWorkflowExecutor _executor;

    public DynamicCommand(
        INoxWorkflowExecutor executor,
        IAnsiConsole console, 
        IConsoleWriter consoleWriter,
        INoxConfiguration noxConfiguration, 
        IConfiguration configuration)
        : base(console, consoleWriter, noxConfiguration, configuration)
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

        return await _executor.Execute(workflow) ? 0 : 1;
    }

}

