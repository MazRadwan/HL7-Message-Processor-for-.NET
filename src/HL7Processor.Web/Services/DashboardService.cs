using HL7Processor.Infrastructure;
using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace HL7Processor.Web.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
}

public class DashboardService : IDashboardService
{
    private readonly HL7DbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(HL7DbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        try
        {
            var today = DateTime.Today;
            var data = new DashboardData();

            // Get total message count
            data.TotalMessages = await _context.Messages.CountAsync();

            // Get messages processed today
            data.ProcessedToday = await _context.Messages
                .CountAsync(m => m.Timestamp.Date == today && m.ProcessingStatus == "Processed");

            // Get pending messages
            data.PendingMessages = await _context.Messages
                .CountAsync(m => m.ProcessingStatus == "Pending" || m.ProcessingStatus == "Processing");

            // Get errors today
            data.ErrorsToday = await _context.Messages
                .CountAsync(m => m.Timestamp.Date == today && m.ProcessingStatus == "Error");

            // Get recent messages
            data.RecentMessages = await _context.Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(10)
                .Select(m => new RecentMessage
                {
                    MessageType = m.MessageType ?? "Unknown",
                    PatientId = m.PatientId ?? "N/A",
                    Status = m.ProcessingStatus ?? "Unknown",
                    Timestamp = m.Timestamp
                })
                .ToListAsync();

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return new DashboardData();
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