using System.Text.Json.Serialization;

namespace Nox.Cli.Plugin.Console.JsonSchema;


internal class JsonSchema
{
    public string? Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    
    [Newtonsoft.Json.JsonConverter(typeof(JsonSchemaTypeConverter))]    
    public JsonSchemaType? Type { get; set; }
    
    public object? Default { get; set; } = null;
    
    public List<string>? Required { get; set; } = new();
    
    public Dictionary<string, JsonSchema>? Properties { get; set; } = null!;
    
    public JsonSchema? Items { get; set; } = null!;
    
    public JsonSchema[]? AnyOf { get; set; }

    public List<string>? Enum { get; set; }
}

internal class JsonSchemaType
{
    public string? TypeName { get; set; } = "object";
    public object? DefaultValue { get; set; } = null;
}
