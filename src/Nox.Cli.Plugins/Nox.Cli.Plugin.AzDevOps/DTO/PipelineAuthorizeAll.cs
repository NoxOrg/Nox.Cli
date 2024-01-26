namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class PipelineAuthorizeAll
{
    public bool Authorized { get; set; }
    public PipelineAuthorizeBy? AuthorizedBy { get; set; }
    public string? AuthorizedOn { get; set; }
}