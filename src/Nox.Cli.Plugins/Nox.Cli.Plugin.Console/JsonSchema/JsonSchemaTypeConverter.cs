using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nox.Cli.Plugin.Console.JsonSchema;

internal class JsonSchemaTypeConverter : JsonConverter<JsonSchemaType>
{
    public override JsonSchemaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new JsonSchemaType();

        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                result.TypeName = reader.GetString();
                break;
            case JsonTokenType.StartArray:
                reader.Read();
                result.TypeName = reader.GetString();
                reader.Read();
                result.DefaultValue = reader.GetString()!;
                reader.Read();
                break;
        }
        
        return result;
    }

    public override void Write(Utf8JsonWriter writer, JsonSchemaType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}