namespace Nox.Cli.Plugin.Console;

public static class DefaultValueExtensions
{
    public static char ToYesNo(this bool value)
    {
        return value ? 'y' : 'n';
    }
}