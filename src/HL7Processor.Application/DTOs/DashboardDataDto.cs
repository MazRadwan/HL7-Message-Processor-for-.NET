namespace HL7Processor.Application.DTOs;

public class DashboardDataDto
{
    public int TotalMessages { get; set; }
    public int ProcessedToday { get; set; }
    public int PendingMessages { get; set; }
    public int ErrorsToday { get; set; }
    public List<RecentMessageDto> RecentMessages { get; set; } = new();
}

public class RecentMessageDto
{
    public string MessageType { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ThroughputPointDto
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}