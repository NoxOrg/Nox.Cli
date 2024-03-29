namespace Nox.Cli.Variables.Secrets;

public interface ISecretProvider
{
    int Precedence { get; }
    Task<IList<KeyValuePair<string, string>>?> GetSecretsAsync(string[] keys);
    IList<KeyValuePair<string, string>>? GetSecrets(string[] keys);
}