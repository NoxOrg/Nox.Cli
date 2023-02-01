namespace Nox.Cli.Configuration;

public class ManifestConfiguration
{
    public SecretsConfiguration Secrets { get; set; } = new();

    public List<BranchesConfiguration>? Branches { get; set; } = null;

    public ServerConfiguration? Server { get; set; } = null;
}