using Nox.Cli.Abstractions;

namespace Nox.Cli.Actions;

public class NoxWorkflowCancellationToken: INoxWorkflowCancellationToken
{
    public string Reason { get; internal set; } = string.Empty;
}