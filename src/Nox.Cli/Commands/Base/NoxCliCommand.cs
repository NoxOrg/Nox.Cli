using Nox.Cli.Helpers;
using Nox.Solution;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nox.Cli.Commands.Base;

public abstract class NoxCliCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    protected readonly IAnsiConsole _console;
    protected readonly IConsoleWriter _consoleWriter;
    protected readonly NoxSolution _solution;

    public NoxCliCommand(IAnsiConsole console, IConsoleWriter consoleWriter,
        NoxSolution solution)
    {
        _console = console;
        _consoleWriter = consoleWriter;
        _solution = solution;
    }

    public override Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {

        if (string.IsNullOrEmpty(_solution.Name))
        {
            return Task.FromResult(0);
        }

        if (_solution.Team is null
            || _solution.Team.Count == 0)
        {
            throw new Exception($"The nox definition contains no members in the 'Team' section. This section is required.");
        }

        _console.WriteLine();
        _consoleWriter.WriteHelpText("Reading","Project definition...");

        _console.WriteLine();
        _consoleWriter.WriteInfo("Project", _solution.Name);

        return Task.FromResult(0);
    }
}