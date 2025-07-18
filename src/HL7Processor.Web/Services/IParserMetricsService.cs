using HL7Processor.Infrastructure.Entities;

namespace HL7Processor.Web.Services;

public interface IParserMetricsService
{
    Task<ParserMetric> RecordParsingMetricAsync(string messageType, string? delimiter, int segments, int fields, int components, int parseTimeMs, long? memoryUsed = null);
    Task<ParserPerformanceStats> GetPerformanceStatsAsync(DateTime? fromDate = null);
    Task<List<ParserMetric>> GetRecentMetricsAsync(int limit = 100);
    Task<Dictionary<string, double>> GetAverageParseTimesByMessageTypeAsync();
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