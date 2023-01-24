using System.Diagnostics;
using System.Reflection;

namespace Nox.Cli.Shared.DTO.Health.Response;

public class HealthResponseDto
{
    public string Name { get; } = "Nox Cli Server";
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset ServerTime { get; set; } = DateTimeOffset.Now;
}