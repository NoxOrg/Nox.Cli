using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

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

    public async Task<string?> LoadAsync()
    {
        var protector = _provider.CreateProtector("nox-cli-server-token");
        var content = await File.ReadAllTextAsync(ServerTokenPath);
        var token = protector.Unprotect(content);
        return IsTokenValid(token) ? token : null;
    }

    private bool IsTokenValid(string? token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var tokenExp = jwtToken.Claims.First(claim => claim.Type.Equals("exp")).Value;
        var ticks = long.Parse(tokenExp);
        var tokenExpDate = DateTimeOffset.FromUnixTimeSeconds(ticks);
        var now = DateTime.Now.ToUniversalTime();
        return tokenExpDate>= now;
    }

}