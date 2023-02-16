namespace Nox.Cli.Server.Abstractions;

public class ServerVariable
{
    public string FullName { get; set; }
    public string ShortName { get; set; }
    public object? Value { get; set; }
}