namespace Nox.Cli.Plugin.AzDevOps.Helpers;

public static class FileExtensionHelper
{
    public static bool IsBinaryFile(string fileExtension)
    {
        switch (fileExtension.ToLower())
        {
            case ".zip":
            case ".tgz":
                return true;
            default:
                return false;
        }
    }
}