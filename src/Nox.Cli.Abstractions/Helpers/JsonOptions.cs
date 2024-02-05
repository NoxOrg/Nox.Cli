using System.Text.Json;

namespace Nox.Cli.Abstractions.Helpers;

public static class JsonOptions
{
    public static JsonSerializerOptions Instance { get; } = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}