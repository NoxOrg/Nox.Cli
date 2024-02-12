namespace Nox.Cli.Plugin.AzDevOps.DTO;

public class IdentityPickerResult
{
    public string? QueryToken { get; set; }
    public List<IdentityPickerIdentity>? Identities { get; set; }
}