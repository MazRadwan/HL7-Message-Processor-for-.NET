using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using HL7Processor.Web.Models;

namespace HL7Processor.Web.Services;

public class ParserMetricsService : IParserMetricsService
{
    private readonly IGetParserMetricsUseCase _getParserMetricsUseCase;
    private readonly ILogger<ParserMetricsService> _logger;

    public ParserMetricsService(IGetParserMetricsUseCase getParserMetricsUseCase, ILogger<ParserMetricsService> logger)
    {
        _getParserMetricsUseCase = getParserMetricsUseCase;
        _logger = logger;
    }

    public async Task<Models.ParserMetric> RecordParsingMetricAsync(string messageType, string? delimiter, int segments, int fields, int components, int parseTimeMs, long? memoryUsed = null)
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var metricDto = await _getParserMetricsUseCase.RecordParsingMetricAsync(messageType, delimiter, segments, fields, components, parseTimeMs, memoryUsed);
            
            // Map Application DTO back to Web model
            return new Models.ParserMetric
            {
                Id = metricDto.Id,
                MessageType = metricDto.MessageType,
                DelimiterDetected = metricDto.DelimiterDetected,
                SegmentCount = metricDto.SegmentCount,
                FieldCount = metricDto.FieldCount,
                ComponentCount = metricDto.ComponentCount,
                ParseTimeMs = metricDto.ParseTimeMs,
                MemoryUsedBytes = metricDto.MemoryUsedBytes,
                CreatedAt = metricDto.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording parser metric");
            throw;
        }
    }

    public async Task<Models.ParserPerformanceStats> GetPerformanceStatsAsync(DateTime? fromDate = null)
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var statsDto = await _getParserMetricsUseCase.GetPerformanceStatsAsync(fromDate);
            
            // Map Application DTO to Web layer model
            return new Models.ParserPerformanceStats
            {
                TotalMessages = statsDto.TotalMessages,
                AverageParseTimeMs = statsDto.AverageParseTimeMs,
                MedianParseTimeMs = statsDto.MedianParseTimeMs,
                FastestParseTimeMs = statsDto.FastestParseTimeMs,
                SlowestParseTimeMs = statsDto.SlowestParseTimeMs,
                AverageMemoryUsedBytes = statsDto.AverageMemoryUsedBytes,
                DelimiterDistribution = statsDto.DelimiterDistribution,
                MessageTypePerformance = statsDto.MessageTypePerformance,
                MessageTypeCount = statsDto.MessageTypeCount,
                HourlyTrends = statsDto.HourlyTrends.Select(dto => new Models.PerformanceTrend
                {
                    Hour = dto.Hour,
                    MessageCount = dto.MessageCount,
                    AverageParseTimeMs = dto.AverageParseTimeMs,
                    AverageMemoryBytes = dto.AverageMemoryBytes
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance stats");
            return new Models.ParserPerformanceStats();
        }
    }

    public async Task<List<Models.ParserMetric>> GetRecentMetricsAsync(int limit = 100)
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var metricDtos = await _getParserMetricsUseCase.GetRecentMetricsAsync(limit);
            
            // Map Application DTOs to Web models
            return metricDtos.Select(dto => new Models.ParserMetric
            {
                Id = dto.Id,
                MessageType = dto.MessageType,
                DelimiterDetected = dto.DelimiterDetected,
                SegmentCount = dto.SegmentCount,
                FieldCount = dto.FieldCount,
                ComponentCount = dto.ComponentCount,
                ParseTimeMs = dto.ParseTimeMs,
                MemoryUsedBytes = dto.MemoryUsedBytes,
                CreatedAt = dto.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent metrics");
            return new List<Models.ParserMetric>();
        }
    }

    public async Task<Dictionary<string, double>> GetAverageParseTimesByMessageTypeAsync()
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            return await _getParserMetricsUseCase.GetAverageParseTimesByMessageTypeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting average parse times");
            return new Dictionary<string, double>();
        }
    }
}