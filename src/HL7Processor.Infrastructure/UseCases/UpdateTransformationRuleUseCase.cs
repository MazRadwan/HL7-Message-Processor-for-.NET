using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HL7Processor.Infrastructure;

namespace HL7Processor.Infrastructure.UseCases;

public class UpdateTransformationRuleUseCase : IUpdateTransformationRuleUseCase
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<UpdateTransformationRuleUseCase> _logger;

    public UpdateTransformationRuleUseCase(IDbContextFactory<HL7DbContext> contextFactory, ILogger<UpdateTransformationRuleUseCase> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<TransformationRuleDto> ExecuteAsync(TransformationRuleDto ruleDto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.TransformationRules.FirstOrDefaultAsync(r => r.Id == ruleDto.Id);
        if (entity == null)
        {
            throw new ArgumentException($"Transformation rule {ruleDto.Id} not found");
        }

        entity.Name = ruleDto.RuleName;
        entity.SourceFormat = ruleDto.SourcePath;
        entity.TargetFormat = ruleDto.TargetPath;
        entity.RuleDefinition = ruleDto.TransformationExpression ?? string.Empty;
        entity.IsActive = ruleDto.IsActive;
        entity.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated transformation rule {RuleId}", entity.Id);

        ruleDto.UpdatedAt = entity.ModifiedAt;
        return ruleDto;
    }
} 