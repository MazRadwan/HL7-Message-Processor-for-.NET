namespace HL7Processor.Application.UseCases;

public interface IDeleteTransformationRuleUseCase
{
    Task<bool> ExecuteAsync(Guid id);
} 