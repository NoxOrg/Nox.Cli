using Azure.Core;
using Microsoft.Identity.Client;

namespace Nox.Cli.Authentication;

public interface IAuthenticator
{
    Task<NoxUserIdentity?> SignIn();
    Task<string?> GetServerToken();
}