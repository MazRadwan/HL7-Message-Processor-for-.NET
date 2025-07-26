using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;

namespace HL7Processor.Web.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
    Task<List<ThroughputPoint>> GetThroughputLastHourAsync(int intervalMinutes = 5);
}

public class DashboardService : IDashboardService
{
    private readonly IGetDashboardDataUseCase _getDashboardDataUseCase;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IGetDashboardDataUseCase getDashboardDataUseCase, ILogger<DashboardService> logger)
    {
        _getDashboardDataUseCase = getDashboardDataUseCase;
        _logger = logger;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var dashboardDto = await _getDashboardDataUseCase.ExecuteAsync();
            
            // Map Application DTO to Web layer model (maintain same interface)
            return new DashboardData
            {
                TotalMessages = dashboardDto.TotalMessages,
                ProcessedToday = dashboardDto.ProcessedToday,
                PendingMessages = dashboardDto.PendingMessages,
                ErrorsToday = dashboardDto.ErrorsToday,
                RecentMessages = dashboardDto.RecentMessages.Select(dto => new RecentMessage
                {
                    MessageType = dto.MessageType,
                    PatientId = dto.PatientId,
                    Status = dto.Status,
                    Timestamp = dto.Timestamp
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return new DashboardData();
        }
    }

    public async Task<List<ThroughputPoint>> GetThroughputLastHourAsync(int intervalMinutes = 5)
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var throughputDtos = await _getDashboardDataUseCase.GetThroughputLastHourAsync(intervalMinutes);
            
            // Map Application DTOs to Web layer models (maintain same interface)
            return throughputDtos.Select(dto => new ThroughputPoint
            {
                Timestamp = dto.Timestamp,
                Count = dto.Count
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting throughput data");
            return new List<ThroughputPoint>();
        }
    }
}

public class DashboardData
{
    public int TotalMessages { get; set; }
    public int ProcessedToday { get; set; }
    public int PendingMessages { get; set; }
    public int ErrorsToday { get; set; }
    public List<RecentMessage> RecentMessages { get; set; } = new();
}

public class RecentMessage
{
    public string MessageType { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ThroughputPoint
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}