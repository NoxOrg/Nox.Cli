namespace Nox.Cli.Authentication;

public interface IBasicAuthenticator
{
    Task<NoxUserIdentity?> SignIn();
}