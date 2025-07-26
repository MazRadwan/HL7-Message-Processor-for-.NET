using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IGetParserMetricsUseCase
{
    Task<ParserMetricDto> RecordParsingMetricAsync(string messageType, string? delimiter, int segments, int fields, int components, int parseTimeMs, long? memoryUsed = null);
    Task<ParserPerformanceStatsDto> GetPerformanceStatsAsync(DateTime? fromDate = null);
    Task<List<ParserMetricDto>> GetRecentMetricsAsync(int limit = 100);
    Task<Dictionary<string, double>> GetAverageParseTimesByMessageTypeAsync();
}