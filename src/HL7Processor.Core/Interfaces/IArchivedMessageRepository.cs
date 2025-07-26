using HL7Processor.Core.Models;

namespace HL7Processor.Core.Interfaces;

public interface IArchivedMessageRepository
{
    Task<ArchivedMessage?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ArchivedMessage>> GetPagedAsync(int page, int pageSize);
    Task<int> GetCountAsync();
    Task<IReadOnlyList<ArchivedMessage>> GetByMessageTypeAsync(string messageType);
    Task<IReadOnlyList<ArchivedMessage>> GetOlderThanAsync(DateTime threshold);
    Task AddAsync(ArchivedMessage archivedMessage);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid originalMessageId);
}