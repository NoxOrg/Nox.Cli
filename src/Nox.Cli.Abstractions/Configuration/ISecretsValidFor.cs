namespace Nox.Cli.Abstractions.Configuration;

public interface ISecretsValidForConfiguration
{
    public int? Days { get; set; }
    public int? Hours { get; set; }
    public int? Minutes { get; set; }
    public int? Seconds { get; set; }
}