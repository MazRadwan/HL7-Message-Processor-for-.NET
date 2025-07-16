namespace HL7Processor.Core.Exceptions;

public class HL7CommunicationException : HL7ProcessingException
{
    public string? RemoteEndPoint { get; }
    public int? Port { get; }
    public string? Protocol { get; }

    public HL7CommunicationException(string message) : base(message)
    {
    }

    public HL7CommunicationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public HL7CommunicationException(string message, string? remoteEndPoint, int? port = null, string? protocol = null) 
        : base(message)
    {
        RemoteEndPoint = remoteEndPoint;
        Port = port;
        Protocol = protocol;
    }

    public HL7CommunicationException(string message, Exception innerException, string? remoteEndPoint, int? port = null, string? protocol = null) 
        : base(message, innerException)
    {
        RemoteEndPoint = remoteEndPoint;
        Port = port;
        Protocol = protocol;
    }

    public override string ToString()
    {
        var result = base.ToString();
        
        if (!string.IsNullOrEmpty(RemoteEndPoint))
            result += $"\nRemote Endpoint: {RemoteEndPoint}";
        
        if (Port.HasValue)
            result += $"\nPort: {Port}";
        
        if (!string.IsNullOrEmpty(Protocol))
            result += $"\nProtocol: {Protocol}";
        
        return result;
    }
}