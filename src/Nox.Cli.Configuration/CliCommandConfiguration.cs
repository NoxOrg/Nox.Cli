using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class CliCommandConfiguration: ICliCommandConfiguration
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

