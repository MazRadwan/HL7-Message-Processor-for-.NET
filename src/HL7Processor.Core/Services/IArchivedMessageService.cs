using HL7Processor.Core.Models;

namespace HL7Processor.Core.Services;

public interface IArchivedMessageService
{
    Task<PagedResult<ArchivedMessageDto>> GetArchivedMessagesAsync(int page = 1, int pageSize = 10);
    Task<int> GetArchivedMessageCountAsync();
}