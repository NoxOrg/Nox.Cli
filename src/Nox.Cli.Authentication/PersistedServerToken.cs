using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.DataProtection;

namespace Nox.Cli.Authentication;

public class PersistedServerToken
{
    private readonly IDataProtectionProvider _provider;
    private const string ProtectorPurpose = "nox-cli-server-token";

    public PersistedServerToken(
        IDataProtectionProvider provider)
    {
        _provider = provider;
    }

    public static string CliTokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".my-nox-cli-token");
    
    public static string ServerTokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".my-nox-server-token");

    public Task SaveAsync(string accessToken, NoxTokenType tokenType)
    {
        var protector = _provider.CreateProtector(ProtectorPurpose);
        var path = CliTokenPath;
        if (tokenType == NoxTokenType.ServerToken) path = ServerTokenPath;
        return File.WriteAllTextAsync(path, protector.Protect(accessToken));
    }

    public async Task<string?> LoadAsync(NoxTokenType tokenType)
    {
        var protector = _provider.CreateProtector(ProtectorPurpose);
        var path = CliTokenPath;
        if (tokenType == NoxTokenType.ServerToken) path = ServerTokenPath;
        var content = await File.ReadAllTextAsync(path);
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