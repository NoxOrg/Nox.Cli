namespace Nox.Cli.Abstractions;

public interface INoxWorkflow
{
    public IDictionary<string, INoxJob> Jobs { get; set; }
}