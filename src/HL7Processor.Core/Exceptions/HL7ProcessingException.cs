namespace HL7Processor.Core.Exceptions;

public class HL7ProcessingException : Exception
{
    public string? MessageType { get; }
    public string? SegmentName { get; }
    public int? FieldNumber { get; }
    public string? OriginalMessage { get; }

    public HL7ProcessingException(string message) : base(message)
    {
    }

    public HL7ProcessingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public HL7ProcessingException(string message, string? messageType, string? segmentName = null, int? fieldNumber = null, string? originalMessage = null) 
        : base(message)
    {
        MessageType = messageType;
        SegmentName = segmentName;
        FieldNumber = fieldNumber;
        OriginalMessage = originalMessage;
    }

    public HL7ProcessingException(string message, Exception innerException, string? messageType, string? segmentName = null, int? fieldNumber = null, string? originalMessage = null) 
        : base(message, innerException)
    {
        MessageType = messageType;
        SegmentName = segmentName;
        FieldNumber = fieldNumber;
        OriginalMessage = originalMessage;
    }

    public override string ToString()
    {
        var result = base.ToString();
        
        if (!string.IsNullOrEmpty(MessageType))
            result += $"\nMessage Type: {MessageType}";
        
        if (!string.IsNullOrEmpty(SegmentName))
            result += $"\nSegment: {SegmentName}";
        
        if (FieldNumber.HasValue)
            result += $"\nField: {FieldNumber}";
        
        if (!string.IsNullOrEmpty(OriginalMessage))
            result += $"\nOriginal Message: {OriginalMessage[..Math.Min(100, OriginalMessage.Length)]}...";
        
        return result;
    }
}