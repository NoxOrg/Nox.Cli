using Nox.Cli.Helpers;

namespace Nox.Cli.Commands;

using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public abstract class NoxCliCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    protected readonly IAnsiConsole _console;
    protected readonly IConsoleWriter _consoleWriter;
    protected readonly Solution.Solution _solution;
    protected readonly IConfiguration _configuration;

    public NoxCliCommand(IAnsiConsole console, IConsoleWriter consoleWriter,
        Solution.Solution solution, IConfiguration configuration)
    {
        _console = console;
        _consoleWriter = consoleWriter;
        _solution = solution;
        _configuration = configuration;
    }

    public override Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        _console.WriteLine();
        _consoleWriter.WriteInfo($"Design folder:");
        _console.WriteLine(_configuration["NoxCli:DesignFolder"]!);

        if (string.IsNullOrEmpty(_solution.Name))
        {
            return Task.FromResult(0);
        }

        if (_solution.Team is null
            || _solution.Team is null
            || _solution.Team.Count == 0)
        {
            throw new Exception($"The nox definition contains no members in the 'Team' section. This section is required.");
        }

        _console.WriteLine();
        _consoleWriter.WriteHelpText("Reading: Project definition...");

        _console.WriteLine();
        _consoleWriter.WriteInfo($"Project:");
        _console.WriteLine(_solution.Name);

        return Task.FromResult(0);
    }
}