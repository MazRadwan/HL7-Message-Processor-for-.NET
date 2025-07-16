using Microsoft.Extensions.Logging;

namespace HL7Processor.Core.Logging;

public class HL7Logger<T> : IHL7Logger<T>
{
    private readonly ILogger<T> _logger;

    public HL7Logger(ILogger<T> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _logger.Log(logLevel, eventId, state, exception, formatter);

    public void LogHL7Message(string messageType, string message, LogLevel logLevel = LogLevel.Information)
    {
        using var scope = BeginScope(new Dictionary<string, object>
        {
            ["MessageType"] = messageType,
            ["MessageLength"] = message.Length,
            ["Timestamp"] = DateTime.UtcNow
        });

        _logger.Log(logLevel, "HL7 Message [{MessageType}] - Length: {MessageLength}", messageType, message.Length);
    }

    public void LogHL7Error(string operation, Exception exception, string? additionalInfo = null)
    {
        using var scope = BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["ErrorType"] = exception.GetType().Name,
            ["Timestamp"] = DateTime.UtcNow
        });

        var message = !string.IsNullOrEmpty(additionalInfo) 
            ? $"HL7 Error in {operation}: {additionalInfo}" 
            : $"HL7 Error in {operation}";

        _logger.LogError(exception, message);
    }

    public void LogHL7Performance(string operation, TimeSpan duration, Dictionary<string, object>? properties = null)
    {
        var scopeData = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["Duration"] = duration.TotalMilliseconds,
            ["Timestamp"] = DateTime.UtcNow
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                scopeData[prop.Key] = prop.Value;
            }
        }

        using var scope = BeginScope(scopeData);
        _logger.LogInformation("HL7 Performance [{Operation}] - Duration: {Duration}ms", operation, duration.TotalMilliseconds);
    }
}