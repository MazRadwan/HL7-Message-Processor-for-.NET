using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IGetArchivedMessagesUseCase
{
    Task<PagedResult<ArchivedMessageDto>> ExecuteAsync(int page = 1, int pageSize = 10);
}