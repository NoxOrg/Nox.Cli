namespace Nox.Cli.Plugin.Console.JsonSchema;

public static class JsonSchemaSimpleTypeResolver
{
    public static SchemaDataType ToSchemaType(this string? typeName)
    {
        return typeName?.ToLower() switch
        {
            "object" => SchemaDataType.Object,
            "array" => SchemaDataType.Array,
            "boolean" => SchemaDataType.Boolean,
            "integer" => SchemaDataType.Integer,
            _ => SchemaDataType.String
        };
    }
}