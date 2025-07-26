using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using HL7Processor.Infrastructure;

namespace HL7Processor.Infrastructure.UseCases;

public class GetTransformationStatsUseCase : IGetTransformationStatsUseCase
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;

    public GetTransformationStatsUseCase(IDbContextFactory<HL7DbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<TransformationStatsDto> ExecuteAsync(DateTime? fromDate = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TransformationHistories.Include(h => h.Rule).AsQueryable();
        if (fromDate.HasValue)
        {
            query = query.Where(h => h.CreatedAt >= fromDate.Value);
        }

        var total = await query.CountAsync();
        var successful = await query.CountAsync(h => h.Success);
        var failed = total - successful;
        var avgTime = total > 0 ? await query.AverageAsync(h => (double)h.TransformationTimeMs) : 0;

        var byFormat = await query
            .GroupBy(h => h.Rule!.SourceFormat + "->" + h.Rule.TargetFormat)
            .Select(g => new { Format = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.Format, v => v.Count);

        var byRule = await query
            .GroupBy(h => h.Rule!.Name)
            .Select(g => new { Rule = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.Rule, v => v.Count);

        var dailyTrends = await query
            .GroupBy(h => h.CreatedAt.Date)
            .Select(g => new TransformationTrendDto
            {
                Date = g.Key,
                TransformationCount = g.Count(),
                AverageTimeMs = g.Average(x => (double)x.TransformationTimeMs),
                SuccessCount = g.Count(x => x.Success),
                FailureCount = g.Count(x => !x.Success)
            }).ToListAsync();

        return new TransformationStatsDto
        {
            TotalTransformations = total,
            SuccessfulTransformations = successful,
            FailedTransformations = failed,
            AverageTransformationTimeMs = avgTime,
            TransformationsByFormat = byFormat,
            TransformationsByRule = byRule,
            DailyTrends = dailyTrends
        };
    }
} 