namespace Nox.Cli.Abstractions.Caching;

public interface IPersistedTokenCache
{
    Task SaveAsync(string tokenName, string token);
    Task<string?> LoadAsync(string tokenName);
}