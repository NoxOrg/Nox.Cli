namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class AuthorizeAgentPoolQueueResponse
{
    public AuthorizeAgentPoolQueueResource? Resource { get; set; }
    public List<AuthorizeAgentPoolQueuePipeline>? Pipelines { get; set; } = new();
    public AuthorizeAgentPoolQueueAllPipelines? AllPipelines { get; set; }
}