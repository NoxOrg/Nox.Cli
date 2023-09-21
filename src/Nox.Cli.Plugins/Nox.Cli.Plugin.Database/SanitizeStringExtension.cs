using System.Text.RegularExpressions;

namespace Nox.Cli.Plugin.Database;

public static class SanitizeStringExtension
{
    private static readonly Regex _rgx = new ("[^a-zA-Z0-9_-]");

    public static string Sanitize(this string input)
    {
        return _rgx.Replace(input, string.Empty);
    }
}
