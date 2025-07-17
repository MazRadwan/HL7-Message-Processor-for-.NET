using HL7Processor.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HL7Processor.Web.Hubs;

public class SystemHub : Hub
{
    private readonly ISystemHealthService _healthService;
    private readonly ILogger<SystemHub> _logger;

    public SystemHub(ISystemHealthService healthService, ILogger<SystemHub> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    public async Task RequestHealthStatus()
    {
        try
        {
            var health = await _healthService.GetSystemHealthAsync();
            await Clients.Caller.SendAsync("SystemHealthUpdate", health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending health status to client {ConnectionId}", Context.ConnectionId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to SystemHub", Context.ConnectionId);
        await base.OnConnectedAsync();
        
        // Send initial health status
        await RequestHealthStatus();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from SystemHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}