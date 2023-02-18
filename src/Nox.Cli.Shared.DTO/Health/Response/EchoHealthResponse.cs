namespace Nox.Cli.Shared.DTO.Health;

public class EchoHealthResponse
{
    public string Name { get; } = "Nox Cli Server";
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset ServerTime { get; set; } = DateTimeOffset.Now;
}