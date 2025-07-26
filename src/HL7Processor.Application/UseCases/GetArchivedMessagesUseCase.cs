using HL7Processor.Application.DTOs;
using HL7Processor.Application.Interfaces;
using HL7Processor.Core.Interfaces;
using HL7Processor.Core.Models;

namespace HL7Processor.Application.UseCases;

public class GetArchivedMessagesUseCase : IGetArchivedMessagesUseCase
{
    private readonly IArchivedMessageRepository _repository;
    private readonly IArchivedMessageMapper _mapper;

    public GetArchivedMessagesUseCase(
        IArchivedMessageRepository repository,
        IArchivedMessageMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ArchivedMessageDto>> ExecuteAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;

        var archivedMessages = await _repository.GetPagedAsync(page, pageSize);
        var totalCount = await _repository.GetCountAsync();

        var dtos = archivedMessages.Select(_mapper.ToDto).ToList();
        
        return new PagedResult<ArchivedMessageDto>(dtos, totalCount, page, pageSize);
    }
}