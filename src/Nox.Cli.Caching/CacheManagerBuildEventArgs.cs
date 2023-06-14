using Nox.Cli.Abstractions.Caching;

namespace Nox.Cli.Caching;

public class CacheManagerBuildEventArgs: EventArgs, ICacheManagerBuildEventArgs
{
    public CacheManagerBuildEventArgs(string message, string spectreMessage)
    {
        Message = message;
        SpectreMessage = spectreMessage;
    }

    public string Message { get; private set; }
    public string SpectreMessage { get; private set; }
}