namespace Nox.Cli.Commands;

using Spectre.Console.Cli;
using Nox.Cli.Helpers;
using Nox.Cli.Services;
using Spectre.Console;

public class LogoutCommand : AsyncCommand<LogoutCommand.Settings>
{
    private readonly IAnsiConsole _console;

    public LogoutCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var cacheFile = Cli.ConfiguratorExtensions.CacheFile;
        var cacheFolder = Cli.ConfiguratorExtensions.CachePath;

        // Rather be safe than sorry.
        // Make sure our Cache file exists before removing the folder ;)

        if (File.Exists(cacheFile))
        {
            _console.MarkupLine($"{Emoji.Known.GreenCircle} Logging out...");
            _console.MarkupLine($"{Emoji.Known.GreenCircle} Clearing the cache at {cacheFolder}...");
            Directory.Delete(cacheFolder, true);
            _console.MarkupLine($"{Emoji.Known.GreenCircle} Done.");
        }

        else if(Directory.Exists(cacheFolder)) 
        { 
            _console.MarkupLine($"{Emoji.Known.BlueCircle} Cache file {cacheFile} not found");
            _console.MarkupLine($"{Emoji.Known.BlueCircle} You are loghged out but may still have workflows cached");
            _console.MarkupLine($"{Emoji.Known.BlueCircle} Remove these manually at {cacheFolder}");
        }

        else
        {
            _console.MarkupLine($"{Emoji.Known.BlueCircle} You are already logged out");

        }

        _console.WriteLine();

        return Task.FromResult(0);
    }

}