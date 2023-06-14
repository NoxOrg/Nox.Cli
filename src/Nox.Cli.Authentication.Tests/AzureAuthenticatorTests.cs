
using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Authentication.Azure;
using Nox.Cli.Configuration;

namespace Nox.Cli.Authentication.Tests;

public class AuthenticatorTests
{
    private IServiceCollection _services;
    
    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        _services.AddAzureAuthentication();
    }

    [Test]
    public void Must_throw_if_authentication_not_configured()
    {
        var provider = _services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthenticator>();
        Assert.ThrowsAsync<NoxCliException>(async () => await auth.SignIn());
    }
    
    [Test]
    public async Task Can_sign_into_azure_without_api()
    {
        var authConfig = GetAzureAuthConfig();
        _services.AddSingleton<ICliAuthConfiguration>(authConfig);
        var provider = _services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthenticator>();
        
        var noxUser = await auth.SignIn();
        Assert.That(noxUser, Is.Not.Null);
        Assert.That(noxUser.TenantId, Is.Not.Empty);
        Assert.That(noxUser.UserPrincipalName, Is.Not.Empty);
    }
    
    [Test]
    public async Task Can_sign_into_azure_with_api()
    {
        var authConfig = GetAzureAuthConfig();
        var apiConfig = GetApiConfig();
        
        _services.AddSingleton<ICliAuthConfiguration>(authConfig);
        _services.AddSingleton<IRemoteTaskExecutorConfiguration>(apiConfig);
        var provider = _services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthenticator>();
        
        var noxUser = await auth.SignIn();
        Assert.That(noxUser, Is.Not.Null);
        Assert.That(noxUser.TenantId, Is.Not.Empty);
        Assert.That(noxUser.UserPrincipalName, Is.Not.Empty);
    }

    [Test]
    public async Task Can_Get_a_Server_Token()
    {
        var authConfig = GetAzureAuthConfig();
        var apiConfig = GetApiConfig();
        
        _services.AddSingleton<ICliAuthConfiguration>(authConfig);
        _services.AddSingleton<IRemoteTaskExecutorConfiguration>(apiConfig);
        var provider = _services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthenticator>();

        var token = await auth.GetServerToken();
        Assert.That(token, Is.Not.Empty);
    }
    
    private ICliAuthConfiguration GetAzureAuthConfig()
    {
        return new CliAuthConfiguration
        {
            provider = "azure",
            TenantId = "88155c28-f750-4013-91d3-8347ddb3daa7"
        };
    }

    private IRemoteTaskExecutorConfiguration GetApiConfig()
    {
        return new RemoteTaskExecutorConfiguration
        {
            Url = "http://localhost:8000",
            ApplicationId = "750b96e1-e772-48f8-b6b3-84bac1961d9b"
        };
    }
}