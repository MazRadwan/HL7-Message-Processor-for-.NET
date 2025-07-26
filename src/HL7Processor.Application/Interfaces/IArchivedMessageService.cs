using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.Interfaces;

public interface IArchivedMessageService
{
    Task<PagedResult<ArchivedMessageDto>> GetArchivedMessagesAsync(int page = 1, int pageSize = 10);
    Task<int> GetArchivedMessageCountAsync();
}