using Nox.Cli.Abstractions.Exceptions;
using Nox.Core.Exceptions;
using Octopus.CoreParsers.Hcl;

namespace Nox.Cli.Plugin.Hcl;

public static class HclHelpers
{
    public static bool ValueExists(HclElement template, string valuePath)
    {
        var pathValues = valuePath.Split('/');
        if (pathValues.Length == 0) throw new NoxException("Node path is invalid!");
        var foundNode = template.Child;
        if (foundNode == null) throw new NoxCliException("HCL template contains no children.");
        for (var i = 0; i < pathValues.Length; i++)
        {
            try
            {
                if (foundNode == null) throw new NoxCliException($"$Unable to find node {pathValues[i - 1]}");
                if (i == pathValues.Length - 1)
                {
                    foundNode = FindChildProcessedValue(foundNode, pathValues[i]);
                }
                else
                {
                    foundNode = FindChildNode(foundNode, pathValues[i]);
                }

            }
            catch
            {
                foundNode = null;
                break;
            }
        }

        return foundNode != null;
    }

    public static void AddValue(HclElement template, string path, string value)
    {
        var pathValues = path.Split('/');
        var foundNode = template.Child;
        if (foundNode == null) throw new NoxCliException("HCL template contains no children.");
        for (var i = 0; i < pathValues.Length; i++)
        {
            try
            {
                if (foundNode == null) throw new NoxCliException($"$Unable to find node {pathValues[i - 1]}");
                foundNode = FindChildNode(foundNode, pathValues[i]);

            }
            catch (Exception ex)
            {
                throw new NoxException(ex.Message);
            }
        }

        var newElement = new HclUnquotedExpressionElement
        {
            Value = value
        };
        var newNode = foundNode!.Children.Append(newElement);
        foundNode.Children = newNode;
    }
    
    private static HclElement? FindChildNode(HclElement parentNode, string name)
    {
        return parentNode.Children.First(c => c.Name == name);
    }
    
    private static HclElement? FindChildProcessedValue(HclElement parentNode, string value)
    {
        return parentNode.Children.First(c => c.ProcessedValue == value);
    }
}