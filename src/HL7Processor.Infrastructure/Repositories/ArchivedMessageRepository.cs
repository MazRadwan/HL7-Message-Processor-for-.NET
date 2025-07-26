using HL7Processor.Core.Interfaces;
using HL7Processor.Core.Models;
using HL7Processor.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace HL7Processor.Infrastructure.Repositories;

public class ArchivedMessageRepository : IArchivedMessageRepository
{
    private readonly HL7DbContext _context;
    private readonly ArchivedMessageMapper _mapper;

    public ArchivedMessageRepository(HL7DbContext context, ArchivedMessageMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ArchivedMessage?> GetByIdAsync(Guid id)
    {
        var entity = await _context.ArchivedMessages.FindAsync(id);
        return entity == null ? null : _mapper.FromEntityFrameworkEntity(entity);
    }

    public async Task<IReadOnlyList<ArchivedMessage>> GetPagedAsync(int page, int pageSize)
    {
        var entities = await _context.ArchivedMessages
            .OrderByDescending(a => a.ArchivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return entities.Select(_mapper.FromEntityFrameworkEntity).ToList();
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.ArchivedMessages.CountAsync();
    }

    public async Task<IReadOnlyList<ArchivedMessage>> GetByMessageTypeAsync(string messageType)
    {
        var entities = await _context.ArchivedMessages
            .Where(a => a.MessageType == messageType)
            .OrderByDescending(a => a.ArchivedAt)
            .ToListAsync();

        return entities.Select(_mapper.FromEntityFrameworkEntity).ToList();
    }

    public async Task<IReadOnlyList<ArchivedMessage>> GetOlderThanAsync(DateTime threshold)
    {
        var entities = await _context.ArchivedMessages
            .Where(a => a.ArchivedAt < threshold)
            .OrderBy(a => a.ArchivedAt)
            .ToListAsync();

        return entities.Select(_mapper.FromEntityFrameworkEntity).ToList();
    }

    public async Task AddAsync(ArchivedMessage archivedMessage)
    {
        var entity = _mapper.ToEntityFrameworkEntity(archivedMessage);
        await _context.ArchivedMessages.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.ArchivedMessages.FindAsync(id);
        if (entity != null)
        {
            _context.ArchivedMessages.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid originalMessageId)
    {
        return await _context.ArchivedMessages
            .AnyAsync(a => a.OriginalMessageId == originalMessageId);
    }
}