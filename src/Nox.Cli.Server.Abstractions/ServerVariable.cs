namespace Nox.Cli.Server.Abstractions;

public class ServerVariable
{
    public string FullName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public object? Value { get; set; }
}