namespace Nox.Cli.Variables.Secrets;

public interface IPersistedSecretStore
{
    void Save(string key, string secret);

    string? Load(string key, TimeSpan? validFor = null);
    
#if NET8_0    
    Task SaveAsync(string key, string secret);
    Task<string?> LoadAsync(string key, TimeSpan? validFor = null);
#endif
}