namespace Nox.Cli.Plugin.AzDevOps.Exceptions;

[Serializable]
public class DevOpsClientException: Exception
{
    public DevOpsClientException(string message): base(message)
    {
        
    }

    public DevOpsClientException(string message, Exception innerException): base(message, innerException)
    {
        
    }
}