using Nox.Cli.Abstractions;
using Nox.Cli.Actions;

namespace Nox.Cli;

public static class JobExtensions
{
    public static INoxJob Clone(this INoxJob source, string id)
    {
        var result = new NoxJob
        {
            Id = source.Id + id,
            Name = source.Name,
            Steps = source.Steps,
            Display = source.Display
        };

        return result;
    } 
}