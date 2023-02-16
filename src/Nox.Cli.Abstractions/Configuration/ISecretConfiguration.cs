namespace Nox.Cli.Abstractions.Configuration;

public interface ISecretConfiguration
{
    string Provider { get; set; }
    string Url { get; set; }
    ISecretValidForConfiguration? ValidFor { get; set; }
    
}