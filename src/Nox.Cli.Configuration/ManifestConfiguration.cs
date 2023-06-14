using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class ManifestConfiguration: IManifestConfiguration
{
    public List<ICliCommandConfiguration>? CliCommands { get; set; } = null;
    public ICliAuthConfiguration? Authentication { get; set; }

    public ILocalTaskExecutorConfiguration? LocalTaskExecutor { get; set; } = null;

    public IRemoteTaskExecutorConfiguration? RemoteTaskExecutor { get; set; } = null;
    }