using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class SecretsValidForConfiguration: ISecretsValidForConfiguration
{
    public int? Days { get; set; }
    public int? Hours { get; set; }
    public int? Minutes { get; set; }
    public int? Seconds { get; set; }
}