using Microsoft.AspNetCore.DataProtection;
using Nox.Cli.Abstractions.Secrets;
using Nox.Solution.Constants;
using Nox.Types;
using DateTime = System.DateTime;
using File = System.IO.File;

namespace Nox.Cli.Variables.Secrets;

public class PersistedSecretStore: IPersistedSecretStoreEx
{
    private readonly IDataProtector _protector;
    private const string ProtectorPurpose = "nox-secrets";

    public PersistedSecretStore(
        IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    public void Save(string key, string secret)
    {
        var path = WellKnownPaths.SecretsCachePath;
     
        Directory.CreateDirectory(path);
        
        var keyNuid = Nuid.From(key).ToHex();
        
        path = Path.Combine(path, $".{keyNuid}");

        File.WriteAllText(path, _protector.Protect(secret));
    }

    public string? Load(string key, TimeSpan? validFor = null)
    {
        validFor ??= new TimeSpan(0, 10, 0);

        var keyNuid = Nuid.From(key).ToHex();
        
        var path = Path.Combine(WellKnownPaths.SecretsCachePath, $".{keyNuid}");
        
        if (!File.Exists(path))
        {
            return null;
        }

        var fileInfo = new FileInfo(path);
        
        if (fileInfo.CreationTime.Add(validFor.Value) < DateTime.Now)
        {
            File.Delete(path);
            return null;
        }

        var content = File.ReadAllText(path);
        return _protector.Unprotect(content);
    }

#if NET8_0    
    public Task SaveAsync(string key, string secret)
    {
        var path = WellKnownPaths.SecretsCachePath;
     
        Directory.CreateDirectory(path);
        
        var keyNuid = Nuid.From(key).ToHex();
        
        path = Path.Combine(path, $".{keyNuid}");
        
        return File.WriteAllTextAsync(path, _protector.Protect(secret));
    }

    public async Task<string?> LoadAsync(string key, TimeSpan? validFor = null)
    {
        validFor ??= new TimeSpan(0, 10, 0);

        var keyNuid = Nuid.From(key).ToHex();
        
        var path = Path.Combine(WellKnownPaths.SecretsCachePath, $".{keyNuid}");
        
        if (!File.Exists(path))
        {
            return null;
        }

        var fileInfo = new FileInfo(path);
        
        if (fileInfo.CreationTime.Add(validFor.Value) < DateTime.Now)
        {
            File.Delete(path);
            return null;
        }

        var content = await File.ReadAllTextAsync(path);
        return _protector.Unprotect(content);
    }
#endif
}