using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HL7Processor.Infrastructure.UseCases;

public class GetSystemHealthUseCase : IGetSystemHealthUseCase
{
    private readonly HL7DbContext _context;
    private readonly ILogger<GetSystemHealthUseCase> _logger;

    public GetSystemHealthUseCase(HL7DbContext context, ILogger<GetSystemHealthUseCase> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Test database connectivity
            var canConnect = await TestDatabaseConnectionAsync();
            stopwatch.Stop();

            // Get memory usage
            var process = Process.GetCurrentProcess();
            var memoryUsed = process.WorkingSet64;
            var totalMemory = GC.GetTotalMemory(false);

            // Get basic processing metrics
            var pendingMessages = await GetPendingMessageCountAsync();
            var recentErrorCount = await GetRecentErrorCountAsync();

            var health = new SystemHealthDto
            {
                Status = canConnect ? "Healthy" : "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Database = new DatabaseHealthDto
                {
                    IsConnected = canConnect,
                    ConnectionCount = 1,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                },
                Memory = new MemoryHealthDto
                {
                    UsedMemoryBytes = memoryUsed,
                    TotalMemoryBytes = totalMemory,
                    UsagePercentage = (double)totalMemory / memoryUsed * 100
                },
                Processing = new ProcessingHealthDto
                {
                    QueueLength = pendingMessages,
                    ProcessingRate = 0,
                    ErrorRate = recentErrorCount
                }
            };

            _logger.LogDebug("System health check completed: {Status}", health.Status);
            
            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing system health check");
            
            return new SystemHealthDto
            {
                Status = "Error",
                Timestamp = DateTime.UtcNow,
                Database = new DatabaseHealthDto { IsConnected = false },
                Memory = new MemoryHealthDto(),
                Processing = new ProcessingHealthDto()
            };
        }
    }

    private async Task<bool> TestDatabaseConnectionAsync()
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection test failed");
            return false;
        }
    }

    private async Task<int> GetPendingMessageCountAsync()
    {
        try
        {
            return await _context.Messages
                .CountAsync(m => m.ProcessingStatus == "Pending" || m.ProcessingStatus == "Processing");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get pending message count");
            return 0;
        }
    }

    private async Task<int> GetRecentErrorCountAsync()
    {
        try
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            return await _context.Messages
                .CountAsync(m => m.ProcessingStatus == "Error" && m.Timestamp >= oneHourAgo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get recent error count");
            return 0;
        }
    }
}