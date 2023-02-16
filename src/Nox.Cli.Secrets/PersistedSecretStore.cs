using Microsoft.AspNetCore.DataProtection;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Secrets;

public class PersistedSecretStore: IPersistedSecretStore
{
    private readonly IDataProtector _protector;
    private const string ProtectorPurpose = "nox-cli-secrets";

    public PersistedSecretStore(
        IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    public Task SaveAsync(string key, string secret)
    {
        //TODO replace this with xxhash
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{key}");
        return File.WriteAllTextAsync(path, _protector.Protect(secret));
    }

    public async Task<string?> LoadAsync(string key, TimeSpan ttl)
    {
        //TODO maybe replace this with xxhash
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{key}");
        if (!File.Exists(path)) return null;
        //Check if the secret has expired
        var fileInfo = new FileInfo(path);
        if (fileInfo.CreationTime.Add(ttl) < DateTime.Now)
        {
            File.Delete(path);
            return null;
        }
        var content = await File.ReadAllTextAsync(path);
        return _protector.Unprotect(content);
    }
}