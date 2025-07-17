using HL7Processor.Infrastructure;
using HL7Processor.Web.Components;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HL7Processor.Web.Services;

public interface ISystemHealthService
{
    Task<SystemHealthIndicator.SystemHealth> GetSystemHealthAsync();
}

public class SystemHealthService : ISystemHealthService
{
    private readonly HL7DbContext _context;
    private readonly ILogger<SystemHealthService> _logger;

    public SystemHealthService(HL7DbContext context, ILogger<SystemHealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SystemHealthIndicator.SystemHealth> GetSystemHealthAsync()
    {
        var health = new SystemHealthIndicator.SystemHealth();

        try
        {
            // Check database connectivity
            health.DatabaseConnected = await CheckDatabaseConnectivityAsync();

            // Get queue length (pending messages)
            health.QueueLength = await GetQueueLengthAsync();

            // Check system resources
            health.CpuUsage = GetCpuUsage();
            health.MemoryUsage = GetMemoryUsage();

            // SignalR connectivity (always true if we're getting this call)
            health.SignalRConnected = true;

            // Determine overall status
            health.OverallStatus = DetermineOverallStatus(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            health.OverallStatus = SystemHealthIndicator.SystemStatus.Critical;
        }

        return health;
    }

    private async Task<bool> CheckDatabaseConnectivityAsync()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> GetQueueLengthAsync()
    {
        try
        {
            return await _context.Messages
                .CountAsync(m => m.ProcessingStatus == "Pending" || m.ProcessingStatus == "Processing");
        }
        catch
        {
            return -1;
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100;
        }
        catch
        {
            return -1;
        }
    }

    private double GetMemoryUsage()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.WorkingSet64 / (1024.0 * 1024.0); // MB
        }
        catch
        {
            return -1;
        }
    }

    private SystemHealthIndicator.SystemStatus DetermineOverallStatus(SystemHealthIndicator.SystemHealth health)
    {
        if (!health.DatabaseConnected)
            return SystemHealthIndicator.SystemStatus.Critical;

        if (health.QueueLength > 1000 || health.CpuUsage > 80 || health.MemoryUsage > 500)
            return SystemHealthIndicator.SystemStatus.Warning;

        return SystemHealthIndicator.SystemStatus.Healthy;
    }
}