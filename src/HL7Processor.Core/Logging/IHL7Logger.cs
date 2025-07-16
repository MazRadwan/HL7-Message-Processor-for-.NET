using Microsoft.Extensions.Logging;

namespace HL7Processor.Core.Logging;

public interface IHL7Logger<T> : ILogger<T>
{
    void LogHL7Message(string messageType, string message, LogLevel logLevel = LogLevel.Information);
    void LogHL7Error(string operation, Exception exception, string? additionalInfo = null);
    void LogHL7Performance(string operation, TimeSpan duration, Dictionary<string, object>? properties = null);
}