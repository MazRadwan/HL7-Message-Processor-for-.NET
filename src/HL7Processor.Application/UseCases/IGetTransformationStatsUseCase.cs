using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IGetTransformationStatsUseCase
{
    Task<TransformationStatsDto> ExecuteAsync(DateTime? fromDate = null);
} 