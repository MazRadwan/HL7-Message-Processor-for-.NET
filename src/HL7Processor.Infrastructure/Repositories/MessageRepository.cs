using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace HL7Processor.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly HL7DbContext _db;

    public MessageRepository(HL7DbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(HL7MessageEntity message, CancellationToken token = default)
    {
        await _db.Messages.AddAsync(message, token);
    }

    public Task<HL7MessageEntity?> GetAsync(Guid id, CancellationToken token = default)
    {
        return _db.Messages
            .Include(m => m.Segments)
            .ThenInclude(s => s.Fields)
            .FirstOrDefaultAsync(m => m.Id == id, token);
    }

    public Task SaveChangesAsync(CancellationToken token = default) => _db.SaveChangesAsync(token);
} 