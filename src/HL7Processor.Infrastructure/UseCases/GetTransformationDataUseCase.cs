using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Infrastructure.UseCases;

public class GetTransformationDataUseCase : IGetTransformationDataUseCase
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<GetTransformationDataUseCase> _logger;

    public GetTransformationDataUseCase(IDbContextFactory<HL7DbContext> contextFactory, ILogger<GetTransformationDataUseCase> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<TransformationRuleDto>> GetTransformationRulesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var rules = await context.TransformationRules
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rules.Select(MapRuleToDto).ToList();
    }

    public async Task<TransformationRuleDto?> GetTransformationRuleByIdAsync(Guid id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var rule = await context.TransformationRules
            .FirstOrDefaultAsync(r => r.Id == id);

        return rule != null ? MapRuleToDto(rule) : null;
    }

    public async Task<List<TransformationHistoryDto>> GetTransformationHistoryAsync(int limit = 100)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var history = await context.TransformationHistories
            .Include(h => h.Rule)
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return history.Select(MapHistoryToDto).ToList();
    }

    public async Task<TransformationHistoryDto?> GetTransformationHistoryByIdAsync(Guid id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var history = await context.TransformationHistories
            .Include(h => h.Rule)
            .FirstOrDefaultAsync(h => h.Id == id);

        return history != null ? MapHistoryToDto(history) : null;
    }

    private static TransformationRuleDto MapRuleToDto(TransformationRule entity)
    {
        return new TransformationRuleDto
        {
            Id = entity.Id,
            RuleName = entity.Name,
            SourcePath = entity.SourceFormat,
            TargetPath = entity.TargetFormat,
            TransformationType = entity.SourceFormat + "->" + entity.TargetFormat,
            TransformationExpression = entity.RuleDefinition,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.ModifiedAt
        };
    }

    private static TransformationHistoryDto MapHistoryToDto(TransformationHistory entity)
    {
        return new TransformationHistoryDto
        {
            Id = entity.Id,
            MessageId = entity.SourceMessageId ?? Guid.Empty,
            RuleId = entity.RuleId,
            RuleName = entity.Rule?.Name ?? "Unknown",
            SourceFormat = entity.Rule?.SourceFormat ?? "Unknown",
            TargetFormat = entity.Rule?.TargetFormat ?? "Unknown",
            SourceData = null,
            TransformedData = entity.OutputData,
            IsSuccessful = entity.Success,
            ErrorMessage = entity.ErrorMessage,
            ProcessingTimeMs = entity.TransformationTimeMs,
            CreatedAt = entity.CreatedAt
        };
    }
}