using System.IdentityModel.Tokens.Jwt;
using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Nox.Cli.Authentication;

public class Authenticator: IAuthenticator
{
    private IPublicClientApplication _application;

    public Authenticator()
    {
        _application = PublicClientApplicationBuilder
            .Create("750b96e1-e772-48f8-b6b3-84bac1961d9b")
            .WithRedirectUri("http://localhost")
            .WithTenantId("88155c28-f750-4013-91d3-8347ddb3daa7")
            .Build();
    }

    public async Task<string?> GetServerToken()
    {
        try
        {
            var apiAuthResult = await GetApiToken();
            return apiAuthResult.AccessToken;
        }
        catch
        {
            await SignIn();
            var apiAuthResult = await GetApiToken();
            return apiAuthResult.AccessToken;
        }
    }

    public async Task<NoxUserIdentity?> SignIn()
    {
        var result = new NoxUserIdentity();
        AuthenticationResult authResult = null;

        var scopes = new[]
        {
            "https://graph.microsoft.com/user.read"
        };

        try
        {
            var accounts = await _application.GetAccountsAsync();
            // Attempt to get a token from the cache (or refresh it silently if needed)
            authResult = await (_application as PublicClientApplication)
                .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            authResult = await _application.AcquireTokenInteractive(scopes)
                .WithExtraScopesToConsent(new[] { "api://750b96e1-e772-48f8-b6b3-84bac1961d9b/access_as_user" })
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
        var accounts = await _application.GetAccountsAsync();
        var authResult = await _application.AcquireTokenSilent(new[] { "api://750b96e1-e772-48f8-b6b3-84bac1961d9b/access_as_user" }, accounts.FirstOrDefault()).ExecuteAsync();
        return authResult;
    }
}