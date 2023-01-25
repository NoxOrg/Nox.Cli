namespace Nox.Cli.Server.Services;

public interface ITaskExecutorFactory
{
    ITaskExecutor GetInstance(Guid? id = null);
    void DisposeInstance(Guid id);
}