using System.IdentityModel.Tokens.Jwt;
using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Authentication;

public class Authenticator: IAuthenticator
{
    private IPublicClientApplication? _application;
    private readonly PersistedServerToken _serverToken;
    private string? _serverScope;

    public Authenticator(
        PersistedServerToken serverToken)
    {
        _serverToken = serverToken;
    }

    public void Configure(IServerConfiguration config)
    {
        _application = PublicClientApplicationBuilder
            .Create(config.ServerApplicationId)
            .WithRedirectUri("http://localhost")
            .WithTenantId(config.TenantId)
            .Build();
        _serverScope = $"api://{config.ServerApplicationId}/access_as_user";
    }

    public async Task<string?> GetServerToken()
    {
        string? result = null;
        try
        {
            var persistedToken = await _serverToken.LoadAsync();
            if (string.IsNullOrEmpty(persistedToken))
            {
                var apiAuthResult = await GetApiToken();
                if (apiAuthResult != null)
                {
                    result = apiAuthResult.AccessToken;
                    await _serverToken.SaveAsync(result);    
                }
            }
            else
            {
                result = persistedToken;
            }
        }
        catch
        {
            await SignIn();
            var apiAuthResult = await GetApiToken();
            if (apiAuthResult != null)
            {
                result = apiAuthResult.AccessToken;
                await _serverToken.SaveAsync(result);    
            }
            
        }

        return result;
    }

    public async Task<NoxUserIdentity?> SignIn()
    {
        var result = new NoxUserIdentity();
        AuthenticationResult? authResult = null;

        var scopes = new[]
        {
            "https://graph.microsoft.com/user.read"
        };

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

        var nameClaim = authResult.ClaimsPrincipal.Claims.FirstOrDefault(c => c.Type == "name");
        string? username = null;
        if (nameClaim != null) username = nameClaim.Value;

        if (authResult == null) return result;
        result.UserPrincipalName = authResult.Account.Username;
        result.UserName = username;
        result.TenantId = authResult.TenantId;

        return result;

    }

    private async Task<AuthenticationResult?> GetApiToken()
    {
        var accounts = await _application!.GetAccountsAsync();
        var authResult = await _application.AcquireTokenSilent(new[] { _serverScope }, accounts.FirstOrDefault()).ExecuteAsync();
        return authResult;
    }
}