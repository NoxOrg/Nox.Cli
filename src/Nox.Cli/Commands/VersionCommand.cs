namespace Nox.Cli.Commands;

using Spectre.Console.Cli;
using Nox.Cli.Helpers;
using Nox.Cli.Services;

public class VersionCommand : AsyncCommand<VersionCommand.Settings>
{
    private readonly IConsoleWriter _consoleWriter;

    public VersionCommand(IConsoleWriter consoleWriter)
    {
        _consoleWriter = consoleWriter;
    }

    public class Settings : CommandSettings
    {
    }

    public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var installedVersion = VersionChecker.GetInstalledNoxCliVersion();
        
        _consoleWriter.WriteInfo(installedVersion);

        await Task.Delay(1);

        return 0;
    }

}