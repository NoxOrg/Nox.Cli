namespace Nox.Cli.Abstractions.Configuration;

public interface ICliAuthConfiguration
{
    string provider { get; set; }
    string Authority { get; set; }
    string TenantId { get; set; }
}