namespace Nox.Cli.Authentication;

public interface IAuthenticator
{
    Task<NoxUserIdentity?> SignIn();
    Task<string?> GetServerToken();
}