using System.Text.RegularExpressions;

namespace Nox.Cli.Plugin.Console;

public class DefaultsProcessor
{
    private readonly Regex _defaultItemRegex = new(@"(\w+)(\[\d*\])?", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    private DefaultNode? _result;

    public DefaultNode? Result => _result;

    public void Process(KeyValuePair<string, object> defaultEntry)
    {
        var matches = _defaultItemRegex.Matches(defaultEntry.Key);//.Where(m => !string.IsNullOrWhiteSpace(m.Value)).ToList();
        if (matches.Any())
        {
            Process(matches, defaultEntry.Value.ToString()!);
        }
    }
    
    private void Process(MatchCollection matches, string value)
    {
        DefaultNode? node = null;
        var lastMatch = matches.Last();

        foreach (Match match in matches)
        {
            if (node == null)
            {
                node = FindNode(_result, match.Groups[1].Value);    
            }
            else
            {
                var foundNode = FindNode(node, match.Groups[1].Value);
                if (foundNode == null)
                {
                    foundNode = AddChild(node, match.Groups[1].Value, null);
                }
            
                node = foundNode;
            }

            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                var child = FindNode(node, match.Groups[2].Value);
                if (child == null)
                {
                    child = AddChild(node!, match.Groups[2].Value, null);
                }

                node = child;
            }
            
            if (match == lastMatch)
            {
                node!.Value = value;
            }    
        }
    }

    private DefaultNode? FindNode(DefaultNode? nodeToSearch, string key)
    {
        if (nodeToSearch == null)
        {
            _result = new DefaultNode
            {
                Key = key
            };
            return _result;
        }
        if (nodeToSearch.Key == key) return nodeToSearch;
        if (nodeToSearch.Children.Count != 0)
        {
            foreach (var child in nodeToSearch.Children!)
            {
                if (child.Key == key) return child;
            }    
        }
        return null;
    }

    private DefaultNode AddChild(DefaultNode node, string key, string? value)
    {
        var child = new DefaultNode
        {
            Key = key,
            Value = value
        };
        node.Children.Add(child);
        return child;
    }
}