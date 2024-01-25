namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class AuthorizeAgentPoolQueueResource
{
    public string Type { get; set; } = "queue";
    public string? Id { get; set; }
}