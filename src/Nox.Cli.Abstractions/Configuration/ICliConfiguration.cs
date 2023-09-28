namespace Nox.Cli.Abstractions.Configuration;

public interface ICliConfiguration
{
    string Branch { get; set; }
    string Command { get; set; }
    string? CommandAlias { get; set; }
    string? Description { get; set; }
    List<string[]>? Examples { get; set; }
    
}