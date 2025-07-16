using System.Net.Sockets;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Core.Communication.Connection;

/// <summary>
/// Simple in-memory manager for tracking active <see cref="TcpClient"/> connections. Provides
/// idle-timeout enforcement and graceful shutdown of all open sockets.
/// </summary>
public sealed class ConnectionManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ClientEntry> _clients = new();
    private readonly ILogger<ConnectionManager> _logger;
    private readonly TimeSpan _idleTimeout;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _sweeperTask;

    public ConnectionManager(ILogger<ConnectionManager> logger, TimeSpan? idleTimeout = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _idleTimeout = idleTimeout ?? TimeSpan.FromMinutes(5);
        _sweeperTask = Task.Run(SweeperLoopAsync);
    }

    public bool Register(string id, TcpClient client)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        if (client == null) throw new ArgumentNullException(nameof(client));

        var entry = new ClientEntry(client);
        if (_clients.TryAdd(id, entry))
        {
            _logger.LogDebug("Registered connection {Id} from {Endpoint}", id, client.Client.RemoteEndPoint);
            return true;
        }
        return false;
    }

    public bool UpdateActivity(string id)
    {
        if (_clients.TryGetValue(id, out var entry))
        {
            entry.Touch();
            return true;
        }
        return false;
    }

    public bool Unregister(string id)
    {
        if (_clients.TryRemove(id, out var entry))
        {
            entry.Dispose();
            _logger.LogDebug("Unregistered connection {Id}", id);
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<string> ActiveConnectionIds => _clients.Keys.ToList();

    private async Task SweeperLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token).ConfigureAwait(false);
                var now = DateTime.UtcNow;

                foreach (var kvp in _clients)
                {
                    var idle = now - kvp.Value.LastActivity;
                    if (idle > _idleTimeout)
                    {
                        _logger.LogDebug("Connection {Id} idle for {Seconds}s – closing", kvp.Key, idle.TotalSeconds);
                        Unregister(kvp.Key);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConnectionManager sweeper loop error");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _sweeperTask.ConfigureAwait(false);

        foreach (var id in _clients.Keys)
        {
            Unregister(id);
        }
        _cts.Dispose();
    }

    private sealed class ClientEntry : IDisposable
    {
        private readonly TcpClient _client;
        public DateTime LastActivity { get; private set; }

        public ClientEntry(TcpClient client)
        {
            _client = client;
            LastActivity = DateTime.UtcNow;
        }

        public void Touch() => LastActivity = DateTime.UtcNow;

        public void Dispose()
        {
            try
            {
                _client.Close();
                _client.Dispose();
            }
            catch { /* ignored */ }
        }
    }
} 