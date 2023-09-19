namespace Nox.Cli.Helpers;

using Spectre.Console;

public interface IConsoleWriter
{
    void WriteInfo(string key, string message);
    void WriteHelpText(string key, string message);
}

public class ConsoleWriter : IConsoleWriter
{
    private readonly IAnsiConsole _console;

    public ConsoleWriter(IAnsiConsole console)
    {
        _console = console;
    }

    public void WriteInfo(string key, string message)
    {
        _console.MarkupLine($"[bold mediumpurple3_1]{key}: [/]{message.EscapeMarkup()}");
    }

    public void WriteHelpText(string key, string message)
    {
        _console.MarkupLine($"[bold seagreen1]{key}: [/]{message.EscapeMarkup()}");
    }

}