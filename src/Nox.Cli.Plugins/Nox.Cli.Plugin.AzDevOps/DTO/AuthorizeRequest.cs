namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class AuthorizeRequest
{
    public Resource? Resource { get; set; }
    public List<PipelineAuthorize>? Pipelines { get; set; } = new();
    public PipelineAuthorizeAll? AllPipelines { get; set; }
}