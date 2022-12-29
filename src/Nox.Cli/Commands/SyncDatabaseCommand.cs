namespace Nox.Cli.Commands;

using Helpers;
using Microsoft.Extensions.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public class SyncDatabaseCommand : NoxCliCommand<SyncDatabaseCommand.Settings>
{
    public SyncDatabaseCommand(IAnsiConsole console, IConsoleWriter consoleWriter, 
        INoxConfiguration noxConfiguration, IConfiguration configuration) 
        : base(console, consoleWriter, noxConfiguration, configuration) {}

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path")]
        public string DesignFolderPath { get; set; } = null!;
    }

    public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await base.ExecuteAsync(context, settings);

        _console.WriteLine("not yet implemented, but coming soon...");

        return 0;
    }
}