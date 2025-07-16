using HL7Processor.Infrastructure.Entities;

namespace HL7Processor.Infrastructure.Repositories;

public interface IMessageRepository
{
    Task AddAsync(HL7MessageEntity message, CancellationToken token = default);
    Task<HL7MessageEntity?> GetAsync(Guid id, CancellationToken token = default);
    Task SaveChangesAsync(CancellationToken token = default);
} 