namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class IdentityPickerRequest
{
    public string? Query { get; set; }
    public List<string>? IdentityTypes { get; set; }
    public List<string>? OperationScopes { get; set; }
    public IdentityPickerOptions? Options { get; set; }
    public List<string>? Properties { get; set; }
}