using HL7Processor.Core.Services;
using HL7Processor.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace HL7Processor.Infrastructure.Services;

public class ArchivedMessageService : IArchivedMessageService
{
    private readonly HL7DbContext _context;

    public ArchivedMessageService(HL7DbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ArchivedMessageDto>> GetArchivedMessagesAsync(int page = 1, int pageSize = 10)
    {
        var total = await _context.ArchivedMessages.CountAsync();
        
        var messages = await _context.ArchivedMessages
            .OrderByDescending(a => a.ArchivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArchivedMessageDto
            {
                Id = a.Id,
                OriginalMessageId = a.OriginalMessageId,
                MessageType = a.MessageType,
                Version = a.Version,
                OriginalTimestamp = a.OriginalTimestamp,
                ArchivedAt = a.ArchivedAt
            })
            .ToListAsync();

        return new PagedResult<ArchivedMessageDto>(messages, total, page, pageSize);
    }

    public async Task<int> GetArchivedMessageCountAsync()
    {
        return await _context.ArchivedMessages.CountAsync();
    }
}