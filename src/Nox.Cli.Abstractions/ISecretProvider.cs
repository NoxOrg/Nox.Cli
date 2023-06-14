namespace Nox.Cli.Abstractions;

public interface ISecretProvider
{
    Task<IList<KeyValuePair<string, string>>?> GetSecretsFromVault(string[] keys);
}