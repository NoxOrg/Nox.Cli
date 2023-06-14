using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class StepConfiguration : IStepConfiguration
{
    public List<IActionConfiguration> Steps { get; set; } = new();
}

