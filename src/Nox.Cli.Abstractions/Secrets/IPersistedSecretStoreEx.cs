using Nox.Secrets.Abstractions;
namespace Nox.Cli.Abstractions.Secrets;
public interface IPersistedSecretStoreEx: IPersistedSecretStore
{
#if NET8_0    
    Task SaveAsync(string key, string secret);
    Task<string?> LoadAsync(string key, TimeSpan? validFor = null);
#endif
}