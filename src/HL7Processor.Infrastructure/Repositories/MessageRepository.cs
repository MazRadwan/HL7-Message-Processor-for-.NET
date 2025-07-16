using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

    public async Task<PagedResult<HL7MessageEntity>> SearchAsync(string? messageType, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken token = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 25;

        var query = _db.Messages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(messageType))
            query = query.Where(m => m.MessageType == messageType);
        if (fromUtc.HasValue)
            query = query.Where(m => m.Timestamp >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(m => m.Timestamp <= toUtc.Value);

        var total = await query.CountAsync(token);
        var items = await query
            .OrderByDescending(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.Segments)
            .ThenInclude(s => s.Fields)
            .ToListAsync(token);

        return new PagedResult<HL7MessageEntity>(items, total, page, pageSize);
    }

    public Task<int> CountAsync(string? messageType = null, CancellationToken token = default)
    {
        return string.IsNullOrWhiteSpace(messageType)
            ? _db.Messages.CountAsync(token)
            : _db.Messages.CountAsync(m => m.MessageType == messageType, token);
    }

    public Task SaveChangesAsync(CancellationToken token = default) => _db.SaveChangesAsync(token);
} 