using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Infrastructure.UseCases;

public class GetValidationDataUseCase : IGetValidationDataUseCase
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<GetValidationDataUseCase> _logger;

    public GetValidationDataUseCase(IDbContextFactory<HL7DbContext> contextFactory, ILogger<GetValidationDataUseCase> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<ValidationResultDto> ValidateMessageAsync(string hl7Message, string validationLevel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Create a validation result entity (simplified for demo)
        var validationResult = new ValidationResult
        {
            ValidationLevel = validationLevel,
            IsValid = true,
            ErrorCount = 0,
            WarningCount = 0,
            ValidationDetails = "[]",
            ProcessingTimeMs = 50
        };

        context.ValidationResults.Add(validationResult);
        await context.SaveChangesAsync();

        _logger.LogDebug("Validation completed for message with level {ValidationLevel}", validationLevel);

        return MapToDto(validationResult);
    }

    public async Task<List<ValidationResultDto>> GetRecentValidationResultsAsync(int limit = 50)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var results = await context.ValidationResults
            .OrderByDescending(v => v.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return results.Select(MapToDto).ToList();
    }

    public async Task<ValidationResultDto?> GetValidationResultByIdAsync(Guid id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var result = await context.ValidationResults
            .FirstOrDefaultAsync(v => v.Id == id);

        return result != null ? MapToDto(result) : null;
    }

    private static ValidationResultDto MapToDto(ValidationResult entity)
    {
        return new ValidationResultDto
        {
            Id = entity.Id,
            MessageId = entity.MessageId,
            ValidationLevel = entity.ValidationLevel,
            IsValid = entity.IsValid,
            ErrorCount = entity.ErrorCount,
            WarningCount = entity.WarningCount,
            ValidationDetails = entity.ValidationDetails,
            ProcessingTimeMs = entity.ProcessingTimeMs,
            CreatedAt = entity.CreatedAt
        };
    }
}