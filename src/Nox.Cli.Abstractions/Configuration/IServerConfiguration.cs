namespace Nox.Cli.Abstractions.Configuration;

public interface IServerConfiguration
{
    string Url { get; set; }
    string ServerApplicationId { get; set; }
    string TenantId { get; set; }
}