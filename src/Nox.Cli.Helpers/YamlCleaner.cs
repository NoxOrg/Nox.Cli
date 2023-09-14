using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Nox.Cli.Helpers;

public static class YamlCleaner
{
    public static StringBuilder RemoveEmptyNodes(StringBuilder sourceYaml)
    {
        var input = new StringReader(sourceYaml.ToString());
        var yaml = new YamlStream();
        yaml.Load(input);
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        RemoveEmptyChildren(root);
        var yamlDoc = new YamlDocument(root);
        var yamlStream = new YamlStream(yamlDoc);
        var sb = new StringBuilder();
        var writer = new StringWriter(sb);
        yamlStream.Save(writer, false);
        writer.Flush();
        writer.Close();
        return sb;
    }

    private static void RemoveEmptyChildren(YamlMappingNode node)
    {
        foreach (var child in node.Children.Reverse())
        {
            if (child.Value.NodeType == YamlNodeType.Mapping)
            {
                var childNode = (YamlMappingNode)child.Value;
                RemoveEmptyChildren(childNode);
            }

            if (child.Value.NodeType == YamlNodeType.Scalar && string.IsNullOrWhiteSpace(((YamlScalarNode)child.Value).Value))
            {
                node.Children.Remove(child);
            }
            
            if (child.Value.NodeType == YamlNodeType.Mapping && !((YamlMappingNode)child.Value).Children.Any())
            {
                node.Children.Remove(child);
            }
        }
    }
}