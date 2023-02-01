using Microsoft.AspNetCore.DataProtection;

namespace Nox.Cli.Authentication;

public class PersistedServerToken
{
    private readonly IDataProtectionProvider _provider;

    public PersistedServerToken(
        IDataProtectionProvider provider)
    {
        _provider = provider;
    }

    public static string ServerTokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".my-cli-server-token");

    public Task SaveAsync(string accessToken)
    {
        var protector = _provider.CreateProtector("nox-cli-server-token");
        return File.WriteAllTextAsync(ServerTokenPath, protector.Protect(accessToken));
    }

    public async Task<string> LoadAsync()
    {
        var protector = _provider.CreateProtector("nox-cli-server-token");
        var content = await File.ReadAllTextAsync(ServerTokenPath);
        return protector.Unprotect(content);
    } 

}