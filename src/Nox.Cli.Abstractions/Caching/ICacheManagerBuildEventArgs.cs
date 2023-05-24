namespace Nox.Cli.Abstractions.Caching;

public interface ICacheManagerBuildEventArgs
{
    string Message { get; }
    string SpectreMessage { get; }
}