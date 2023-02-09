namespace Nox.Cli.Abstractions.Configuration;

public interface IManifestConfiguration
{
    List<ICliCommandConfiguration>? CliCommands { get; set; }
    
    ICliAuthConfiguration? Authentication { get; set; }
    ILocalTaskExecutorConfiguration? LocalTaskExecutor { get; set; }
    IRemoteTaskExecutorConfiguration? RemoteTaskExecutor { get; set; }
}