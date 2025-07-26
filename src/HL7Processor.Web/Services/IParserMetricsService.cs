using HL7Processor.Web.Models;

namespace HL7Processor.Web.Services;

public interface IParserMetricsService
{
    Task<Models.ParserMetric> RecordParsingMetricAsync(string messageType, string? delimiter, int segments, int fields, int components, int parseTimeMs, long? memoryUsed = null);
    Task<Models.ParserPerformanceStats> GetPerformanceStatsAsync(DateTime? fromDate = null);
    Task<List<Models.ParserMetric>> GetRecentMetricsAsync(int limit = 100);
    Task<Dictionary<string, double>> GetAverageParseTimesByMessageTypeAsync();
}