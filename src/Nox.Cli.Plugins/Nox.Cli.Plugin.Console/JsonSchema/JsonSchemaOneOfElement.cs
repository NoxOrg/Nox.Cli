using System.Text.Json.Serialization;

namespace Nox.Cli.Plugin.Console.JsonSchema;

public class JsonSchemaOneOfElement
{
    [JsonPropertyName("type")]
    public string? TypeName { get; set; }

    public List<string>? Enum { get; set; }
}