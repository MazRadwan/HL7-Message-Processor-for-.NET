namespace HL7Processor.Application.DTOs;

public class SystemHealthDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DatabaseHealthDto Database { get; set; } = new();
    public MemoryHealthDto Memory { get; set; } = new();
    public ProcessingHealthDto Processing { get; set; } = new();
}

public class DatabaseHealthDto
{
    public bool IsConnected { get; set; }
    public int ConnectionCount { get; set; }
    public double ResponseTimeMs { get; set; }
}

public class MemoryHealthDto
{
    public long UsedMemoryBytes { get; set; }
    public long TotalMemoryBytes { get; set; }
    public double UsagePercentage { get; set; }
}

public class ProcessingHealthDto
{
    public int QueueLength { get; set; }
    public int ProcessingRate { get; set; }
    public int ErrorRate { get; set; }
}