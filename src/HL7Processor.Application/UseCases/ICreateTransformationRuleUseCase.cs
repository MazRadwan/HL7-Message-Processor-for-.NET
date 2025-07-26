using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface ICreateTransformationRuleUseCase
{
    Task<TransformationRuleDto> ExecuteAsync(TransformationRuleDto ruleDto);
} 