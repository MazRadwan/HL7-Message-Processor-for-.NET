namespace HL7Processor.Web.Models;

public class ParserMetric
{
    public Guid Id { get; set; }
    public string? MessageType { get; set; }
    public string? DelimiterDetected { get; set; }
    public int? SegmentCount { get; set; }
    public int? FieldCount { get; set; }
    public int? ComponentCount { get; set; }
    public int ParseTimeMs { get; set; }
    public long? MemoryUsedBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ParserPerformanceStats
{
    public int TotalMessages { get; set; }
    public double AverageParseTimeMs { get; set; }
    public double MedianParseTimeMs { get; set; }
    public int FastestParseTimeMs { get; set; }
    public int SlowestParseTimeMs { get; set; }
    public long AverageMemoryUsedBytes { get; set; }
    public Dictionary<string, int> DelimiterDistribution { get; set; } = new();
    public Dictionary<string, double> MessageTypePerformance { get; set; } = new();
    public Dictionary<string, int> MessageTypeCount { get; set; } = new();
    public List<PerformanceTrend> HourlyTrends { get; set; } = new();
}

public class PerformanceTrend
{
    public DateTime Hour { get; set; }
    public int MessageCount { get; set; }
    public double AverageParseTimeMs { get; set; }
    public long AverageMemoryBytes { get; set; }
}