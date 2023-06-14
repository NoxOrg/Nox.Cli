using System.Runtime.Serialization;

namespace Nox.Cli.Abstractions.Exceptions;

[Serializable]
public class NoxCliException: Exception, INoxCliException
{
    public NoxCliException(string message): base(message)
    {
        
    }

    public NoxCliException(string message, Exception innerException): base(message, innerException)
    {
        
    }
}