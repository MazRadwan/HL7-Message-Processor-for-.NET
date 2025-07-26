using HL7Processor.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HL7Processor.Infrastructure;

namespace HL7Processor.Infrastructure.UseCases;

public class DeleteTransformationRuleUseCase : IDeleteTransformationRuleUseCase
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<DeleteTransformationRuleUseCase> _logger;

    public DeleteTransformationRuleUseCase(IDbContextFactory<HL7DbContext> contextFactory, ILogger<DeleteTransformationRuleUseCase> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(Guid id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.TransformationRules.FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null)
            return false;

        context.TransformationRules.Remove(entity);
        await context.SaveChangesAsync();

        _logger.LogInformation("Deleted transformation rule {RuleId}", id);
        return true;
    }
} 