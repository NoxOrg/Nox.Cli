namespace Nox.Cli.Server.Services;

public interface ITaskExecutorFactory
{
    ITaskExecutor NewInstance(Guid workflowId);
    ITaskExecutor GetInstance(Guid id);
    void DisposeInstance(Guid id);
}