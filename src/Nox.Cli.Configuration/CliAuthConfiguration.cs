using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class CliAuthConfiguration: ICliAuthConfiguration
{
    public string provider { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}