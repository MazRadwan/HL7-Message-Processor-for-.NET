namespace HL7Processor.Application.UseCases;

public interface ISubmitMessageUseCase
{
    Task<bool> ExecuteAsync(string hl7Message, CancellationToken cancellationToken = default);
}