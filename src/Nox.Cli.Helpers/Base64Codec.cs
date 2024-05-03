namespace Nox.Cli.Helpers;

public static class Base64Codec
{
    public static string ToBase64(this string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes);
    }
    
    public static string FromBase64(this string input)
    {
        var bytes = Convert.FromBase64String(input);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}