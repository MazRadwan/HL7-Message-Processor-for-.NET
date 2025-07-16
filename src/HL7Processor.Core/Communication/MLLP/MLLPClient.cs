using HL7Processor.Core.Models;
using HL7Processor.Core.Communication.MLLP;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;

namespace HL7Processor.Core.Communication.MLLP;

/// <summary>
/// MLLP client for sending HL7 messages over TCP/IP
/// </summary>
public class MLLPClient : IDisposable
{
    private readonly ILogger<MLLPClient> _logger;
    private readonly MLLPClientOptions _options;
    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public bool IsConnected => _tcpClient?.Connected == true;
    public string RemoteEndpoint => _options.Host + ":" + _options.Port;

    public event EventHandler<MLLPMessageEventArgs>? MessageSent;
    public event EventHandler<MLLPAcknowledgmentEventArgs>? AcknowledgmentReceived;
    public event EventHandler<MLLPErrorEventArgs>? ErrorOccurred;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public MLLPClient(ILogger<MLLPClient> logger, MLLPClientOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionSemaphore = new SemaphoreSlim(1, 1);
        _cancellationTokenSource = new CancellationTokenSource();

        ValidateOptions();
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                _logger.LogDebug("Already connected to {Endpoint}", RemoteEndpoint);
                return true;
            }

            _logger.LogDebug("Connecting to MLLP server at {Endpoint}", RemoteEndpoint);

            _tcpClient = new TcpClient();
            _tcpClient.ReceiveTimeout = _options.ReceiveTimeoutMs;
            _tcpClient.SendTimeout = _options.SendTimeoutMs;

            using var timeoutCts = new CancellationTokenSource(_options.ConnectionTimeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await _tcpClient.ConnectAsync(_options.Host, _options.Port, combinedCts.Token);
            _networkStream = _tcpClient.GetStream();

            _logger.LogInformation("Successfully connected to MLLP server at {Endpoint}", RemoteEndpoint);
            Connected?.Invoke(this, EventArgs.Empty);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MLLP server at {Endpoint}", RemoteEndpoint);
            await CleanupConnectionAsync();
            ErrorOccurred?.Invoke(this, new MLLPErrorEventArgs($"Connection failed: {ex.Message}", ex));
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<MLLPResponse> SendMessageAsync(string hl7Message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(hl7Message))
            throw new ArgumentException("HL7 message cannot be null or empty", nameof(hl7Message));

        if (!IsConnected)
        {
            if (_options.AutoReconnect)
            {
                var connected = await ConnectAsync(cancellationToken);
                if (!connected)
                {
                    return new MLLPResponse
                    {
                        Success = false,
                        ErrorMessage = "Unable to establish connection"
                    };
                }
            }
            else
            {
                throw new InvalidOperationException("Not connected to MLLP server");
            }
        }

        try
        {
            var messageId = ExtractMessageControlId(hl7Message);
            _logger.LogDebug("Sending HL7 message {MessageId} to {Endpoint}", messageId, RemoteEndpoint);

            // Wrap message with MLLP framing
            var mllpMessage = MLLPProtocol.WrapMessage(hl7Message);

            // Send message
            await _networkStream!.WriteAsync(mllpMessage, cancellationToken);
            await _networkStream.FlushAsync(cancellationToken);

            MessageSent?.Invoke(this, new MLLPMessageEventArgs(hl7Message, messageId));

            // Wait for acknowledgment if required
            MLLPAcknowledgment? acknowledgment = null;
            if (_options.WaitForAcknowledgment)
            {
                acknowledgment = await ReceiveAcknowledgmentAsync(cancellationToken);
                if (acknowledgment != null)
                {
                    AcknowledgmentReceived?.Invoke(this, new MLLPAcknowledgmentEventArgs(acknowledgment));
                }
            }

            _logger.LogDebug("Successfully sent HL7 message {MessageId}", messageId);

            return new MLLPResponse
            {
                Success = true,
                MessageId = messageId,
                Acknowledgment = acknowledgment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HL7 message to {Endpoint}", RemoteEndpoint);
            ErrorOccurred?.Invoke(this, new MLLPErrorEventArgs($"Send failed: {ex.Message}", ex));

            if (_options.AutoReconnect)
            {
                await CleanupConnectionAsync();
            }

            return new MLLPResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<MLLPResponse>> SendMessagesAsync(IEnumerable<string> hl7Messages, CancellationToken cancellationToken = default)
    {
        var messages = hl7Messages.ToList();
        var responses = new List<MLLPResponse>();

        _logger.LogDebug("Sending batch of {MessageCount} HL7 messages to {Endpoint}", messages.Count, RemoteEndpoint);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var response = await SendMessageAsync(message, cancellationToken);
            responses.Add(response);

            if (!response.Success && !_options.ContinueOnError)
            {
                _logger.LogWarning("Stopping batch send due to error: {Error}", response.ErrorMessage);
                break;
            }

            // Add delay between messages if configured
            if (_options.MessageDelayMs > 0 && messages.IndexOf(message) < messages.Count - 1)
            {
                await Task.Delay(_options.MessageDelayMs, cancellationToken);
            }
        }

        var successCount = responses.Count(r => r.Success);
        _logger.LogInformation("Batch send completed: {SuccessCount}/{TotalCount} messages sent successfully", 
            successCount, messages.Count);

        return responses;
    }

    public async Task DisconnectAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            if (!IsConnected)
            {
                _logger.LogDebug("Already disconnected from {Endpoint}", RemoteEndpoint);
                return;
            }

            _logger.LogDebug("Disconnecting from MLLP server at {Endpoint}", RemoteEndpoint);
            await CleanupConnectionAsync();
            _logger.LogInformation("Disconnected from MLLP server at {Endpoint}", RemoteEndpoint);
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task<MLLPAcknowledgment?> ReceiveAcknowledgmentAsync(CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[_options.ReceiveBufferSize];
            var receivedData = new List<byte>();

            using var timeoutCts = new CancellationTokenSource(_options.AcknowledgmentTimeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            while (!combinedCts.Token.IsCancellationRequested)
            {
                var bytesRead = await _networkStream!.ReadAsync(buffer, 0, buffer.Length, combinedCts.Token);
                
                if (bytesRead == 0)
                {
                    _logger.LogWarning("Connection closed while waiting for acknowledgment");
                    return null;
                }

                receivedData.AddRange(buffer.Take(bytesRead));

                if (MLLPProtocol.IsCompleteMessage(receivedData.ToArray(), receivedData.Count))
                {
                    var messageResult = MLLPProtocol.ExtractMessage(receivedData.ToArray(), receivedData.Count);
                    if (messageResult.IsComplete && messageResult.MessageBytes != null)
                    {
                        return MLLPProtocol.ParseAcknowledgment(messageResult.MessageBytes);
                    }
                }
            }

            _logger.LogWarning("Timeout waiting for acknowledgment from {Endpoint}", RemoteEndpoint);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving acknowledgment from {Endpoint}", RemoteEndpoint);
            return null;
        }
    }

    private async Task CleanupConnectionAsync()
    {
        try
        {
            _networkStream?.Close();
            _tcpClient?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error during connection cleanup");
        }
        finally
        {
            _networkStream?.Dispose();
            _networkStream = null;
            _tcpClient?.Dispose();
            _tcpClient = null;
        }
    }

    private string ExtractMessageControlId(string hl7Message)
    {
        try
        {
            var mshSegment = hl7Message.Split('\r')[0];
            var fields = mshSegment.Split('|');
            return fields.Length > 9 ? fields[9] : Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrEmpty(_options.Host))
            throw new ArgumentException("Host cannot be null or empty");

        if (_options.Port <= 0 || _options.Port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535");

        if (_options.ConnectionTimeoutMs <= 0)
            throw new ArgumentException("Connection timeout must be positive");

        if (_options.SendTimeoutMs <= 0)
            throw new ArgumentException("Send timeout must be positive");

        if (_options.ReceiveTimeoutMs <= 0)
            throw new ArgumentException("Receive timeout must be positive");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource.Cancel();
        CleanupConnectionAsync().GetAwaiter().GetResult();
        _connectionSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}

public class MLLPClientOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 2575;
    public int ConnectionTimeoutMs { get; set; } = 30000;
    public int SendTimeoutMs { get; set; } = 30000;
    public int ReceiveTimeoutMs { get; set; } = 30000;
    public int AcknowledgmentTimeoutMs { get; set; } = 10000;
    public int ReceiveBufferSize { get; set; } = 8192;
    public int MessageDelayMs { get; set; } = 0;
    public bool WaitForAcknowledgment { get; set; } = true;
    public bool AutoReconnect { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
}

public class MLLPResponse
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public MLLPAcknowledgment? Acknowledgment { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class MLLPMessageEventArgs : EventArgs
{
    public string Message { get; }
    public string MessageId { get; }
    public DateTime Timestamp { get; }

    public MLLPMessageEventArgs(string message, string messageId)
    {
        Message = message;
        MessageId = messageId;
        Timestamp = DateTime.UtcNow;
    }
}

public class MLLPAcknowledgmentEventArgs : EventArgs
{
    public MLLPAcknowledgment Acknowledgment { get; }
    public DateTime Timestamp { get; }

    public MLLPAcknowledgmentEventArgs(MLLPAcknowledgment acknowledgment)
    {
        Acknowledgment = acknowledgment;
        Timestamp = DateTime.UtcNow;
    }
}

public class MLLPErrorEventArgs : EventArgs
{
    public string Message { get; }
    public Exception? Exception { get; }
    public DateTime Timestamp { get; }

    public MLLPErrorEventArgs(string message, Exception? exception = null)
    {
        Message = message;
        Exception = exception;
        Timestamp = DateTime.UtcNow;
    }
}