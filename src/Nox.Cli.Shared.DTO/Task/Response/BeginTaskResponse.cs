namespace Nox.Cli.Shared.DTO.Workflow;

public class BeginTaskResponse
{
    public Guid TaskExecutorId { get; set; }
    public bool Success { get; set; } = false;
    public Exception Error { get; set; } = null!;
}