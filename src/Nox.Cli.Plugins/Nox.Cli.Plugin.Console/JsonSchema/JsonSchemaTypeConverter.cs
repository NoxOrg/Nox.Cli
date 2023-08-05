using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nox.Cli.Plugin.Console.JsonSchema;

internal class JsonSchemaTypeConverter : JsonConverter<JsonSchemaType?>
{
    public override JsonSchemaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new JsonSchemaType();

        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                result.Type = GetSchemaTypeFromString(reader.GetString());
                break;
            case JsonTokenType.StartArray:
                reader.Read();
                result.Type = GetSchemaTypeFromString(reader.GetString());
                reader.Read();
                result.Default = reader.GetString()!;
                reader.Read();
                break;
        }
        
        return result;
    }

    public override void Write(Utf8JsonWriter writer, JsonSchemaType? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private SchemaType GetSchemaTypeFromString(string? source)
    {
        return source?.ToLower() switch
        {
            "object" => SchemaType.Object,
            "array" => SchemaType.Array,
            "boolean" => SchemaType.Boolean,
            "integer" => SchemaType.Integer,
            _ => SchemaType.String
        };
    }
}