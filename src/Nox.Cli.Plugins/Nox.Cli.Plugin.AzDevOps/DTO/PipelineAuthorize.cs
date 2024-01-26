namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class PipelineAuthorize
{
    public int Id { get; set; }
    public bool Authorized { get; set; }
    public PipelineAuthorizeBy? AuthorizedBy { get; set; }
    public string? AuthorizedOn { get; set; }
}