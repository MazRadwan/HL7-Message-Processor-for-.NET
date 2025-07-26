namespace HL7Processor.Application.UseCases;

public interface IGetArchivedMessageCountUseCase
{
    Task<int> ExecuteAsync();
}