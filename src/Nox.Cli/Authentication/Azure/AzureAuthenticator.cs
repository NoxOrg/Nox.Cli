using System.IdentityModel.Tokens.Jwt;
using Azure.Identity;
using Microsoft.Identity.Client;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Exceptions;

namespace Nox.Cli.Authentication.Azure;

public class AzureAuthenticator: IAuthenticator
{
    private IPublicClientApplication? _application;
    private IPersistedTokenCache _persistedToken;
    private string? _serverScope;
    private ICliAuthConfiguration? _authConfiguration;

    public AzureAuthenticator(
        IPersistedTokenCache persistedToken,
        ICliAuthConfiguration? authConfiguration = null,
        IRemoteTaskExecutorConfiguration? apiConfiguration = null)
    {
        _persistedToken = persistedToken;
        _authConfiguration = authConfiguration;
        if (authConfiguration != null && apiConfiguration != null)
        {
            _application = PublicClientApplicationBuilder
                .Create(apiConfiguration.ApplicationId)
                .WithRedirectUri("http://localhost")
                .WithTenantId(authConfiguration.TenantId)
                .Build();
            _serverScope = $"api://{apiConfiguration.ApplicationId}/access_as_user";
        }
    }

    public async Task<string?> GetServerToken()
    {
        string? result = null;
        try
        {
            var token = await _persistedToken.LoadAsync("my-nox-server-token");

            if (IsTokenValid(token))
            {
                result = token;
            }
            else
            {
                var apiAuthResult = await GetApiToken();
                if (apiAuthResult != null)
                {
                    result = apiAuthResult.AccessToken;
                    await _persistedToken.SaveAsync("my-nox-server-token", result);
                }
            }
        }
        catch
        {
            await SignIn();
            var apiAuthResult = await GetApiToken();
            if (apiAuthResult != null)
            {
                result = apiAuthResult.AccessToken;
                await _persistedToken.SaveAsync("my-nox-server-token", result);
            }
            
        }

        return result;
    }

    public async Task<NoxUserIdentity?> SignIn()
    {
        if (_authConfiguration == null) throw new NoxCliException("Cannot authenticate if authentication has not been configured in manifest!");
        var result = new NoxUserIdentity();
        
        var scopes = new[]
        {
            "https://graph.microsoft.com/user.read"
        };

        if (_application != null) //use MSAL
        {
            AuthenticationResult? authResult = null;
            try
            {
                var accounts = await _application!.GetAccountsAsync();
                // Attempt to get a token from the cache (or refresh it silently if needed)
                authResult = await (_application as PublicClientApplication)?.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync()!;
            }
            catch (MsalUiRequiredException)
            {
                authResult = await _application!.AcquireTokenInteractive(scopes)
                    .WithExtraScopesToConsent(new[] { _serverScope })
                    .ExecuteAsync();
            }
            
            if (authResult == null) return result;
            
            result.UserPrincipalName = authResult.Account.Username;
            result.TenantId = authResult.TenantId;
        }
        else //Use basic credentials
        {
            var credential = new InteractiveBrowserCredential();
            var authRecord = await credential.AuthenticateAsync();
            result.TenantId = authRecord.TenantId;
            result.UserPrincipalName = authRecord.Username;
        }

        return result;

    }

    private async Task<AuthenticationResult?> GetApiToken()
    {
        var accounts = await _application!.GetAccountsAsync();
        var authResult = await _application.AcquireTokenSilent(new[] { _serverScope }, accounts.FirstOrDefault()).ExecuteAsync();
        return authResult;
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