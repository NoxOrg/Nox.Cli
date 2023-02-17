namespace Nox.Cli.Plugin.YamlMaker.JsonSchema;

internal class JsonSchema
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "object";
    public object? Default { get; set; } = null;
    public string[] Required { get; set; } = new string[0];
    public Dictionary<string,JsonSchema> Properties { get; set; } = null!;
    public JsonSchema Items { get; set; } = null!;
    public OneOfEntry[] OneOf { get; set; } = null!;
}


internal class OneOfEntry
{
    public string Const { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}