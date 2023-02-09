using Azure.Core;
using Microsoft.Identity.Client;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Authentication;

public interface IAuthenticator
{
    Task<NoxUserIdentity?> SignIn();
    Task<string?> GetServerToken();
}