using Microsoft.AspNetCore.DataProtection;

namespace Nox.Cli.Authentication;

public class PersistedAccessToken
{
    private readonly IDataProtectionProvider _provider;
    
    
    
    public static string AccessTokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".nox_cli_access_token"
        );

    public Task SaveAsync(string accessToken)
    {
        return File.WriteAllTextAsync(AccessTokenPath, accessToken);
    }

    public Task<string> LoadAsync()
    {
        return File.ReadAllTextAsync(AccessTokenPath);
    }
}