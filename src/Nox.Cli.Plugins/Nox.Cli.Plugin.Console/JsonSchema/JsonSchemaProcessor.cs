namespace Nox.Cli.Plugin.Console.JsonSchema;

public class JsonSchemaProcessor
{
    internal JsonSchema Process(JsonSchemaRaw sourceSchema)
    {
        return ProcessNode(sourceSchema);
    }

    private JsonSchema ProcessNode(JsonSchemaRaw sourceNode)
    {
        var result = new JsonSchema
        {
            Title = sourceNode.Title,
            Description = sourceNode.Description
        };

        //Determine the SchemaType
        if (!string.IsNullOrWhiteSpace(sourceNode.Type))
        {
            result.SchemaType = new JsonSchemaType
            {
                DataType = sourceNode.Type.ToSchemaType(),
                Enum = sourceNode.Enum
            };
        }

        if (sourceNode.OneOf != null)
        {
            var firstType = sourceNode.OneOf.FirstOrDefault(oo => oo.TypeName!.ToString() != "null");
            if (firstType != null)
            {
                result.SchemaType = new JsonSchemaType()
                {
                    DataType = firstType.TypeName!.ToString().ToSchemaType(),
                    Enum = firstType.Enum
                };
            }
        }

        if (sourceNode.Properties != null)
        {
            result.Properties = new Dictionary<string, JsonSchema>();
            foreach (var prop in sourceNode.Properties)
            {
                result.Properties.Add(prop.Key, ProcessNode(prop.Value));
            }
        } else if (sourceNode.AnyOf != null)
        {
            var itemNode = sourceNode.AnyOf.FirstOrDefault(ao => ao.Properties != null && !ao.Properties.ContainsKey("$ref"));
            if (itemNode != null)
            {
                result = ProcessNode(itemNode);
            }
        } else if (sourceNode.Items != null)
        {
            if (sourceNode.Items.AnyOf != null)
            {
                result.Item = ProcessNode(sourceNode.Items.AnyOf![0]);
            } else if (sourceNode.Items.OneOf?[0].Enum != null)
            {
                if (result.SchemaType!.DataType == SchemaDataType.Array)
                {
                    result.SchemaType!.DataType = SchemaDataType.EnumList;
                }
                else
                {
                    result.SchemaType!.DataType = SchemaDataType.Enum;
                }
                
                result.SchemaType.Enum = sourceNode.Items.OneOf[0].Enum; 
            }
        }


        if (sourceNode is { Required: not null, Properties: not null })
        {
            foreach (var requiredProp in sourceNode.Required)
            {
                var prop = result.Properties![requiredProp];
                prop.IsRequired = true;
            }
        }
        
        return result;
    }
}