using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using HL7Processor.Core.Exceptions;

namespace HL7Processor.Core.Communication.MLLP;

/// <summary>
/// Lightweight asynchronous MLLP server for receiving HL7 v2 messages over TCP/IP.
/// </summary>
public sealed class MLLPServer : IAsyncDisposable
{
    private readonly ILogger<MLLPServer> _logger;
    private readonly TcpListener _listener;
    private readonly List<Task> _clientTasks = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _isRunning;

    public MLLPServerOptions Options { get; }

    public event EventHandler<MLLPMessageEventArgs>? MessageReceived;
    public event EventHandler<MLLPAcknowledgmentEventArgs>? AcknowledgmentSent;
    public event EventHandler<TcpClient>? ClientConnected;
    public event EventHandler<TcpClient>? ClientDisconnected;
    public event EventHandler<MLLPErrorEventArgs>? ErrorOccurred;

    public MLLPServer(ILogger<MLLPServer> logger, MLLPServerOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options ?? new MLLPServerOptions();
        _listener = new TcpListener(Options.IPAddress, Options.Port);
    }

    /// <summary>
    /// Start listening for incoming MLLP connections.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _listener.Start();
        _isRunning = true;

        _logger.LogInformation("MLLP Server listening on {Address}:{Port}", Options.IPAddress, Options.Port);

        try
        {
            while (!_cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

                ClientConnected?.Invoke(this, client);
                _logger.LogDebug("Accepted client {Endpoint}", client.Client.RemoteEndPoint);

                var task = HandleClientAsync(client, _cts.Token);
                lock (_clientTasks) { _clientTasks.Add(task); }

                // Fire-and-forget with cleanup
                _ = task.ContinueWith(t =>
                {
                    ClientDisconnected?.Invoke(this, client);
                    lock (_clientTasks) { _clientTasks.Remove(t); }
                    client.Dispose();

                    if (t.Exception != null)
                    {
                        _logger.LogError(t.Exception.Flatten(), "Client handler faulted");
                    }
                }, TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MLLP Server listener encountered an error");
            ErrorOccurred?.Invoke(this, new MLLPErrorEventArgs("Server listener error", ex));
        }
    }

    /// <summary>
    /// Stop the server and disconnect all clients.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _cts.Cancel();
        _listener.Stop();

        Task[] tasks;
        lock (_clientTasks) { tasks = _clientTasks.ToArray(); }
        await Task.WhenAll(tasks).ConfigureAwait(false);

        _isRunning = false;
        _logger.LogInformation("MLLP Server on {Port} stopped", Options.Port);
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        await using var networkStream = client.GetStream();
        var buffer = new byte[Options.BufferSize];
        var receivedData = new List<byte>(Options.BufferSize);

        while (!token.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from client {Endpoint}", client.Client.RemoteEndPoint);
                ErrorOccurred?.Invoke(this, new MLLPErrorEventArgs("Read error", ex));
                break;
            }

            if (read == 0)
            {
                // Client closed connection
                break;
            }

            receivedData.AddRange(buffer[..read]);

            // Process as many complete messages as are in the buffer
            bool foundMessage;
            do
            {
                foundMessage = false;
                var result = MLLPProtocol.ExtractMessage(receivedData.ToArray(), receivedData.Count);
                if (result.IsComplete && result.MessageBytes != null)
                {
                    foundMessage = true;
                    // Remove processed bytes
                    receivedData.RemoveRange(0, result.BytesConsumed);

                    var hl7Message = MLLPProtocol.UnwrapMessage(result.MessageBytes);
                    var messageId = Guid.NewGuid().ToString();
                    try
                    {
                        MessageReceived?.Invoke(this, new MLLPMessageEventArgs(hl7Message, messageId));
                    }
                    catch (Exception evtEx)
                    {
                        _logger.LogWarning(evtEx, "MessageReceived handler threw an exception");
                    }

                    // Always send ACK for now (can be configurable)
                    var ackBytes = MLLPProtocol.CreateAcknowledgment(messageId, AcknowledgmentCode.ApplicationAccept);
                    try
                    {
                        await networkStream.WriteAsync(ackBytes, token).ConfigureAwait(false);
                        AcknowledgmentSent?.Invoke(this, new MLLPAcknowledgmentEventArgs(
                            MLLPProtocol.ParseAcknowledgment(ackBytes)));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send ACK to {Endpoint}", client.Client.RemoteEndPoint);
                    }
                }
            } while (foundMessage && !token.IsCancellationRequested);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts.Dispose();
    }
}

public class MLLPServerOptions
{
    public IPAddress IPAddress { get; init; } = IPAddress.Any;
    public int Port { get; init; } = 2575; // typical MLLP port
    public int BufferSize { get; init; } = 8192;
}