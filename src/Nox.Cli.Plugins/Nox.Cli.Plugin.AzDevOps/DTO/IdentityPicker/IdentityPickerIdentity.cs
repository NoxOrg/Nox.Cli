namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class IdentityPickerIdentity
{
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? OriginDirectory { get; set; }
    public string? OriginId { get; set; }
    public string? LocalDirectory { get; set; }
    public string? LocalId { get; set; }
    public string? DisplayName { get; set; }
    public string? ScopeName { get; set; }
    public string? SubjectDescriptor { get; set; }
}