using HL7Processor.Core.Interfaces;

namespace HL7Processor.Application.UseCases;

public class GetArchivedMessageCountUseCase : IGetArchivedMessageCountUseCase
{
    private readonly IArchivedMessageRepository _repository;

    public GetArchivedMessageCountUseCase(IArchivedMessageRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> ExecuteAsync()
    {
        return await _repository.GetCountAsync();
    }
}