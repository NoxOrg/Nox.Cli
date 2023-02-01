using Azure.Core;
using Microsoft.Identity.Client;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Authentication;

public interface IAuthenticator
{
    void Configure(IServerConfiguration config); 
    Task<NoxUserIdentity?> SignIn();
    Task<string?> GetServerToken();
}