namespace Nox.Cli.Commands;

using Helpers;
using Microsoft.Extensions.Configuration;
using Nox.Cli.Actions;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO.Abstractions;

public class DynamicCommand : NoxCliCommand<DynamicCommand.Settings>
{
    public DynamicCommand(IAnsiConsole console, IConsoleWriter consoleWriter, 
        INoxConfiguration noxConfiguration, IConfiguration configuration, IFileSystem fileSystem) 
        : base(console, consoleWriter, noxConfiguration, configuration) {}

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path")]
        public string DesignFolderPath { get; set; } = null!;
    }

    public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await base.ExecuteAsync(context, settings);

        var yaml = (string)context.Data!;

        return await NoxWorkflowExecutor.Execute(yaml, _configuration, _noxConfiguration, _console) ? 0 : 1;
    }

}

