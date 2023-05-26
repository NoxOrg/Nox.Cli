using Microsoft.AspNetCore.DataProtection;
using Nox.Cli.Abstractions.Caching;

namespace Nox.Cli.Caching;

public class PersistedTokenCache: IPersistedTokenCache
{
    private readonly IDataProtectionProvider _provider;
    private const string ProtectorPurpose = "nox-token";

    public PersistedTokenCache(
        IDataProtectionProvider provider)
    {
        _provider = provider;
    }

    public Task SaveAsync(string tokenName, string token)
    {
        var protector = _provider.CreateProtector(ProtectorPurpose);
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{tokenName}");
        return File.WriteAllTextAsync(path, protector.Protect(token));
    }

    public async Task<string?> LoadAsync(string tokenName)
    {
        var protector = _provider.CreateProtector(ProtectorPurpose);
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{tokenName}");
        if (!File.Exists(path)) return null;
        var content = await File.ReadAllTextAsync(path);
        return protector.Unprotect(content);
    }
}