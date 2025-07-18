using HL7Processor.Infrastructure;
using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace HL7Processor.Web.Services;

public class ParserMetricsService : IParserMetricsService
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<ParserMetricsService> _logger;

    public ParserMetricsService(IDbContextFactory<HL7DbContext> contextFactory, ILogger<ParserMetricsService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<ParserMetric> RecordParsingMetricAsync(string messageType, string? delimiter, int segments, int fields, int components, int parseTimeMs, long? memoryUsed = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var metric = new ParserMetric
        {
            MessageType = messageType,
            DelimiterDetected = delimiter,
            SegmentCount = segments,
            FieldCount = fields,
            ComponentCount = components,
            ParseTimeMs = parseTimeMs,
            MemoryUsedBytes = memoryUsed
        };

        context.ParserMetrics.Add(metric);
        await context.SaveChangesAsync();

        _logger.LogDebug("Recorded parser metric for {MessageType}: {ParseTimeMs}ms, {Segments} segments", 
            messageType, parseTimeMs, segments);

        return metric;
    }

    public async Task<ParserPerformanceStats> GetPerformanceStatsAsync(DateTime? fromDate = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-7);
        
        var metrics = await context.ParserMetrics
            .Where(m => m.CreatedAt >= startDate)
            .ToListAsync();

        if (!metrics.Any())
        {
            return new ParserPerformanceStats();
        }

        var parseTimes = metrics.Select(m => m.ParseTimeMs).OrderBy(t => t).ToList();
        var memoryUsages = metrics.Where(m => m.MemoryUsedBytes.HasValue).Select(m => m.MemoryUsedBytes!.Value).ToList();

        var stats = new ParserPerformanceStats
        {
            TotalMessages = metrics.Count,
            AverageParseTimeMs = parseTimes.Average(),
            MedianParseTimeMs = GetMedian(parseTimes),
            FastestParseTimeMs = parseTimes.First(),
            SlowestParseTimeMs = parseTimes.Last(),
            AverageMemoryUsedBytes = memoryUsages.Any() ? (long)memoryUsages.Average() : 0,
            
            DelimiterDistribution = metrics
                .Where(m => !string.IsNullOrEmpty(m.DelimiterDetected))
                .GroupBy(m => m.DelimiterDetected!)
                .ToDictionary(g => g.Key, g => g.Count()),
                
            MessageTypePerformance = metrics
                .Where(m => !string.IsNullOrEmpty(m.MessageType))
                .GroupBy(m => m.MessageType!)
                .ToDictionary(g => g.Key, g => g.Average(m => m.ParseTimeMs)),
                
            MessageTypeCount = metrics
                .Where(m => !string.IsNullOrEmpty(m.MessageType))
                .GroupBy(m => m.MessageType!)
                .ToDictionary(g => g.Key, g => g.Count()),
                
            HourlyTrends = metrics
                .GroupBy(m => new DateTime(m.CreatedAt.Year, m.CreatedAt.Month, m.CreatedAt.Day, m.CreatedAt.Hour, 0, 0))
                .Select(g => new PerformanceTrend
                {
                    Hour = g.Key,
                    MessageCount = g.Count(),
                    AverageParseTimeMs = g.Average(m => m.ParseTimeMs),
                    AverageMemoryBytes = g.Where(m => m.MemoryUsedBytes.HasValue).Any() 
                        ? (long)g.Where(m => m.MemoryUsedBytes.HasValue).Average(m => m.MemoryUsedBytes!.Value)
                        : 0
                })
                .OrderBy(t => t.Hour)
                .ToList()
        };

        return stats;
    }

    public async Task<List<ParserMetric>> GetRecentMetricsAsync(int limit = 100)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.ParserMetrics
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Dictionary<string, double>> GetAverageParseTimesByMessageTypeAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.ParserMetrics
            .Where(m => !string.IsNullOrEmpty(m.MessageType))
            .GroupBy(m => m.MessageType!)
            .ToDictionaryAsync(g => g.Key, g => g.Average(m => (double)m.ParseTimeMs));
    }

    private static double GetMedian(List<int> sortedValues)
    {
        if (!sortedValues.Any()) return 0;
        
        int count = sortedValues.Count;
        if (count % 2 == 0)
        {
            return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
        }
        else
        {
            return sortedValues[count / 2];
        }
    }
}