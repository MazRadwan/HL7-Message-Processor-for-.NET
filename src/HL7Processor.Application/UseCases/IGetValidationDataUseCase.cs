using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IGetValidationDataUseCase
{
    Task<ValidationResultDto> ValidateMessageAsync(string hl7Message, string validationLevel);
    Task<List<ValidationResultDto>> GetRecentValidationResultsAsync(int limit = 50);
    Task<ValidationResultDto?> GetValidationResultByIdAsync(Guid id);
}