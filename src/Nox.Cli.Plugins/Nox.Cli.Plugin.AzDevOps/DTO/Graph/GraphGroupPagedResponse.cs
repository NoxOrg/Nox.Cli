namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class GraphGroupPagedResponse
{
    public string? ContinuationToken { get; set; }
    public int Count { get; set; }
    public List<GraphGroupResult>? Value { get; set; }
}