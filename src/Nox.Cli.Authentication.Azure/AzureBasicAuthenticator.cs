using Azure.Identity;

namespace Nox.Cli.Authentication.Azure;

public class AzureBasicAuthenticator: IBasicAuthenticator
{
    public async Task<NoxUserIdentity?> SignIn()
    {
        var result = new NoxUserIdentity();
        
        var scopes = new[]
        {
            "https://graph.microsoft.com/.default"
        };

        var credential = new InteractiveBrowserCredential();
        var authRecord = await credential.AuthenticateAsync();
        result.TenantId = authRecord.TenantId;
        result.UserPrincipalName = authRecord.Username;

        return result;

    }
}