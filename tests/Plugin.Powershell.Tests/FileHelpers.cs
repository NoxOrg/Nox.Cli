namespace Plugin.Powershell.Tests;

public static class FileHelpers
{
    public static void PurgeFolderRecursive(string path, bool includeRoot)
    {
        if (Directory.Exists(path))
        {
            PurgeFolder(path);
            var di = new DirectoryInfo(path);

            foreach (var dir in di.GetDirectories())
            {
                PurgeFolder(dir.FullName);
                dir.Delete(true);
            }

            if (includeRoot)
            {
                Directory.Delete(path);
            }
        }
    }

    public static void PurgeFolder(string path)
    {
        if (Directory.Exists(path))
        {
            var di = new DirectoryInfo(path);

            foreach (var file in di.GetFiles())
            {
                file.Delete();
            }
        }
    }
}