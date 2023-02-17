namespace Nox.Cli.Helpers;

using Spectre.Console;

public interface IConsoleWriter
{
    void WriteInfo(string message);
    void WriteError(string message);
    void WriteWarning(string message);
    void WriteHelpHeader(string message);
    void WriteHelpText(string message);
    void WriteLogMessage(string message);
    void WriteRuler(string message);
}

public class ConsoleWriter : IConsoleWriter
{
    private readonly IAnsiConsole _console;

    public ConsoleWriter(IAnsiConsole console)
    {
        _console = console;
    }

    public void WriteInfo(string message)
    {
        _console.MarkupLine($"[bold mediumpurple3_1]{message.EscapeMarkup()}[/]");
    }

    public void WriteError(string message)
    {
        _console.MarkupLine($"[bold indianred1]ERROR: {message.EscapeMarkup()}[/]");
    }

    public void WriteWarning(string message)
    {
        _console.MarkupLine($"[bold olive]WARNING: {message.EscapeMarkup()}[/]");
    }

    public void WriteHelpHeader(string message)
    {
        _console.MarkupLine($"[bold olive]{message.EscapeMarkup()}[/]");
    }

    public void WriteHelpText(string message)
    {
        _console.MarkupLine($"[bold seagreen1]{message.EscapeMarkup()}[/]");
    }

    public void WriteLogMessage(string message)
    {
        _console.MarkupLine($"[grey]{message}.[/]");
    }

    public void WriteRuler(string message)
    {
        _console.Write(new Rule($"[bold mediumpurple3_1]{message.EscapeMarkup()}[/]") { Justification = Justify.Left });
    }

}