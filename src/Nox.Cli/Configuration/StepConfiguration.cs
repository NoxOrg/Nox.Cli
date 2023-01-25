using Nox.Cli.Abstractions;

namespace Nox.Cli.Configuration;

public class StepConfiguration
{
    public List<IActionConfiguration> Steps { get; set; } = new();
}

