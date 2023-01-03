namespace Nox.Cli.Actions;

public class NoxActionInput
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public object Default { get; set; } = string.Empty;
    public string DeprecationMessage { get; set; } = string.Empty;
}




