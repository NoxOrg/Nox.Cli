namespace Nox.Cli.Abstractions;

public interface INoxWorkflowCancellationToken
{
    string Reason { get;  }
}