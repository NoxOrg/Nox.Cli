namespace Nox.Cli.Secrets;

public static class SecretKeyHelper
{
    public static string ToAzureSecretKey(this string source)
    {
        return source.Replace('_', '-').ToLower();
    }
}