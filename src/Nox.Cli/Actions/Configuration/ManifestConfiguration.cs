namespace Nox.Cli.Actions.Configuration;

public class ManifestConfiguration
{
    public SecretsConfiguration Secrets { get; set; } = new();

    public List<BranchesConfiguration>? Branches { get; set; } = null;
    
}