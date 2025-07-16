using HL7Processor.Infrastructure.Retention;

namespace HL7Processor.Api.Services;

public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<DataRetentionBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public DataRetentionBackgroundService(IServiceProvider provider, ILogger<DataRetentionBackgroundService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDataRetentionService>();
                await service.ApplyRetentionPolicyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data retention job failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
} 