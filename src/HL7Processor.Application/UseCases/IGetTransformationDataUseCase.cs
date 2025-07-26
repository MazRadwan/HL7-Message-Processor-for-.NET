using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IGetTransformationDataUseCase
{
    Task<List<TransformationRuleDto>> GetTransformationRulesAsync();
    Task<TransformationRuleDto?> GetTransformationRuleByIdAsync(Guid id);
    Task<List<TransformationHistoryDto>> GetTransformationHistoryAsync(int limit = 100);
    Task<TransformationHistoryDto?> GetTransformationHistoryByIdAsync(Guid id);
}