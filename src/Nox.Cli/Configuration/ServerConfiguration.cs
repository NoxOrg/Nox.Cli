using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class ServerConfiguration: IServerConfiguration
{
    public string Url { get; set; } = string.Empty;
    public string ServerApplicationId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}