namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class AuthorizeAgentPoolQueueAllPipelines
{
    public bool Authorized { get; set; }
    public AuthorizeAgentPoolQueueAuthorizedBy? AuthorizedBy { get; set; }
    public string? AuthorizedOn { get; set; }
}