namespace Nox.Cli.Commands;

using Spectre.Console.Cli;
using Nox.Cli.Services;
using Spectre.Console;

public class VersionCommand : AsyncCommand<VersionCommand.Settings>
{
    private readonly IAnsiConsole _console;

    public VersionCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var installedVersion = VersionChecker.GetInstalledNoxCliVersion();
        
        _console.WriteLine(installedVersion);

        return Task.FromResult(0);
    }

}