using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Configuration;

namespace Nox.Cli.Secrets.Tests;

public static class SecretHelpers
{
    public static ISecretsValidForConfiguration GetSecretConfig()
    {
        return new SecretsValidForConfiguration
        {
            Days = 0,
            Hours = 0,
            Minutes = 0,
            Seconds = 1
        };
    }
}