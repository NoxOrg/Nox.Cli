namespace Nox.Cli.PersonalAccessToken;

public class AzDevOpsPat
{
    public Guid? AuthorizationId { get; set; }
    public string? DisplayName { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Scope { get; set; }
    public DateTime? ValidFrom { get; set; }
    public string? Token { get; set; }
}