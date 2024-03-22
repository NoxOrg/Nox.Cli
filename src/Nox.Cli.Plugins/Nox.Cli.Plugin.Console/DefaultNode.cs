namespace Nox.Cli.Plugin.Console;

public class DefaultNode
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public List<DefaultNode> Children { get; set; } = new();
}