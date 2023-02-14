namespace Nox.Cli.Abstractions.Configuration;

public interface ILocalTaskExecutorConfiguration
{
    List<ISecretConfiguration>? Secrets { get; set; }
}