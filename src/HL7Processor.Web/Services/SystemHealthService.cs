using HL7Processor.Application.UseCases;
using HL7Processor.Web.Components;
using System.Diagnostics;

namespace HL7Processor.Web.Services;

public interface ISystemHealthService
{
    Task<SystemHealthIndicator.SystemHealth> GetSystemHealthAsync();
}

public class SystemHealthService : ISystemHealthService
{
    private readonly IGetSystemHealthUseCase _getSystemHealthUseCase;
    private readonly ILogger<SystemHealthService> _logger;

    public SystemHealthService(IGetSystemHealthUseCase getSystemHealthUseCase, ILogger<SystemHealthService> logger)
    {
        _getSystemHealthUseCase = getSystemHealthUseCase;
        _logger = logger;
    }

    public async Task<SystemHealthIndicator.SystemHealth> GetSystemHealthAsync()
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var healthDto = await _getSystemHealthUseCase.GetSystemHealthAsync();
            
            // Map Application DTO to Web layer model and add Web-specific health checks
            var health = new SystemHealthIndicator.SystemHealth
            {
                DatabaseConnected = healthDto.Database.IsConnected,
                QueueLength = healthDto.Processing.QueueLength,
                MemoryUsage = healthDto.Memory.UsedMemoryBytes / (1024.0 * 1024.0), // Convert to MB
                CpuUsage = GetCpuUsage(), // Keep this local since it's Web-specific
                SignalRConnected = true // Always true if we're getting this call
            };

            // Determine overall status
            health.OverallStatus = DetermineOverallStatus(health);
            
            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return new SystemHealthIndicator.SystemHealth
            {
                OverallStatus = SystemHealthIndicator.SystemStatus.Critical
            };
        }
    }

    // Removed - now handled by Application layer Use Case

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