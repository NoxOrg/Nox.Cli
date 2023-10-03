using System.Collections;
using System.Text.Json.Serialization;

namespace Nox.Cli.Plugin.Console.JsonSchema;


internal class JsonSchemaRaw
{
    public string? Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;

    public List<JsonSchemaOneOfElement>? OneOf { get; set; }

    public List<string>? Required { get; set; } = new();

    public Dictionary<string, JsonSchemaRaw>? Properties { get; set; }

    public JsonSchemaRaw? Items { get; set; }
    
    public JsonSchemaRaw[]? AnyOf { get; set; }

    public string? Type { get; set; }

    public List<string>? Enum { get; set; }
}


