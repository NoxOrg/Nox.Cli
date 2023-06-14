namespace Nox.Cli.Abstractions.Configuration;

public interface ICliCommandConfiguration
{
    string Name { get; set; }
    string Description { get; set; }
}