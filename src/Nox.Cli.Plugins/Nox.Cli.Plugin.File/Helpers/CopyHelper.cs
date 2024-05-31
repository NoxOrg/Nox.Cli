namespace Nox.Cli.Plugin.File.Helpers;

public static class CopyHelper
{
    public static bool CopyFile(string sourcePath, string targetPath, bool isOverwrite)
    {
        if (System.IO.File.Exists(targetPath))
        {
            if (isOverwrite)
            {
                var createDate = System.IO.File.GetCreationTime(targetPath);
                System.IO.File.Delete(targetPath);    
                System.IO.File.Copy(sourcePath, targetPath);
                System.IO.File.SetCreationTime(targetPath, createDate);        
            }

            return false;
        }

        var targetFolder = Path.GetDirectoryName(targetPath);
        Directory.CreateDirectory(targetFolder!);
        System.IO.File.Copy(sourcePath, targetPath);
        return true;
    }
}