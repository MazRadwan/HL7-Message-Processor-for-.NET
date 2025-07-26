using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HL7Processor.Infrastructure;

namespace HL7Processor.Infrastructure.UseCases;

public class CreateTransformationRuleUseCase : ICreateTransformationRuleUseCase
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<CreateTransformationRuleUseCase> _logger;

    public CreateTransformationRuleUseCase(IDbContextFactory<HL7DbContext> contextFactory, ILogger<CreateTransformationRuleUseCase> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<TransformationRuleDto> ExecuteAsync(TransformationRuleDto ruleDto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var entity = new TransformationRule
        {
            Id = Guid.NewGuid(),
            Name = ruleDto.RuleName,
            SourceFormat = ruleDto.SourcePath,
            TargetFormat = ruleDto.TargetPath,
            RuleDefinition = ruleDto.TransformationExpression ?? string.Empty,
            IsActive = ruleDto.IsActive,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await context.TransformationRules.AddAsync(entity);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created transformation rule {RuleId}", entity.Id);

        ruleDto.Id = entity.Id;
        ruleDto.CreatedAt = entity.CreatedAt;
        ruleDto.UpdatedAt = entity.ModifiedAt;
        return ruleDto;
    }
} 