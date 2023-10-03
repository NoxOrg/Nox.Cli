namespace Nox.Cli.Plugin.Console.JsonSchema;

public class JsonSchema
{
    public string? Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public JsonSchemaType? SchemaType { get; set; }
    public Dictionary<string, JsonSchema>? Properties { get; set; }

    public JsonSchema? Item { get; set; }
    
    public bool IsRequired { get; set; }
}