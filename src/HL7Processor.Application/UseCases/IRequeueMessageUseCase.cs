namespace HL7Processor.Application.UseCases;

public interface IRequeueMessageUseCase
{
    Task<bool> ExecuteAsync(string queueName, CancellationToken cancellationToken = default);
}