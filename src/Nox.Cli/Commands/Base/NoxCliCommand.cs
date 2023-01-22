namespace Nox.Cli.Commands;

using Helpers;
using Microsoft.Extensions.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public abstract class NoxCliCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    protected readonly IAnsiConsole _console;
    protected readonly IConsoleWriter _consoleWriter;
    protected readonly INoxConfiguration _noxConfiguration;
    protected readonly IConfiguration _configuration;

    public NoxCliCommand(IAnsiConsole console, IConsoleWriter consoleWriter,
        INoxConfiguration noxConfiguration, IConfiguration configuration)
    {
        _console = console;
        _consoleWriter = consoleWriter;
        _noxConfiguration = noxConfiguration;
        _configuration = configuration;
    }

    public override Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {

        _console.WriteLine();
        _consoleWriter.WriteInfo($"Design folder:");
        _console.WriteLine(_configuration["NoxCli:DesignFolder"]!);

        if (string.IsNullOrEmpty(_noxConfiguration.Name))
        {
            return Task.FromResult(0);
        }

        if (_noxConfiguration.Team is null
            || _noxConfiguration.Team.Developers is null
            || _noxConfiguration.Team.Developers.Count == 0)
        {
            throw new Exception($"The nox definition contains no 'Developers' in the 'Team' section. This section is required.");
        }

        _console.WriteLine();
        _consoleWriter.WriteHelpText("Reading: Project definition...");

        _console.WriteLine();
        _consoleWriter.WriteInfo($"Project:");
        _console.WriteLine(_noxConfiguration.Name);

        return Task.FromResult(0);
    }
}