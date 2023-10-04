using Nox.Cli.Abstractions;
using Nox.Cli.Actions;

namespace Nox.Cli;

public static class JobExtensions
{
    public static INoxJob Clone(this INoxJob source, string id)
    {
        var steps = source.Steps;
        foreach (var step in steps)
        {
            
        }
        
        var result = new NoxJob
        {
            Id = source.Id + id,
            Name = source.Name,
            Steps = steps,
            Display = source.Display
        };

        return result;
    } 
}