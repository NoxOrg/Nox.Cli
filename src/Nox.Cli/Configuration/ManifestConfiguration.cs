namespace Nox.Cli.Configuration;

public class ManifestConfiguration
{
    public SecretsConfiguration Secrets { get; set; } = new();

    public List<BranchesConfiguration>? Branches { get; set; } = null;

    public string ServerUrl { get; set; } = string.Empty;
}