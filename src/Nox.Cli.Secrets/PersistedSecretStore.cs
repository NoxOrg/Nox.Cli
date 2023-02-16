using Microsoft.AspNetCore.DataProtection;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Secrets;

public class PersistedSecretStore: IPersistedSecretStore
{
    private readonly IDataProtector _protector;
    private readonly TimeSpan _secretTtl;
    private const string ProtectorPurpose = "nox-cli-secrets";

    public PersistedSecretStore(
        IDataProtectionProvider provider,
        ISecretValidForConfiguration config)
    {
        _protector = provider.CreateProtector(ProtectorPurpose);
        _secretTtl = new TimeSpan(config.Days ?? 0, config.Hours ?? 0, config.Minutes ?? 0, config.Seconds ?? 0);
    }

    public Task SaveAsync(string key, string secret)
    {
        //TODO replace this with xxhash
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{key}");
        return File.WriteAllTextAsync(path, _protector.Protect(secret));
    }

    public async Task<string?> LoadAsync(string key)
    {
        //TODO maybe replace this with xxhash
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{key}");
        if (!File.Exists(path)) return null;
        //Check if the secret has expired
        var fileInfo = new FileInfo(path);
        if (fileInfo.CreationTime.Add(_secretTtl) < DateTime.Now)
        {
            File.Delete(path);
            return null;
        }
        var content = await File.ReadAllTextAsync(path);
        return _protector.Unprotect(content);
    }
}