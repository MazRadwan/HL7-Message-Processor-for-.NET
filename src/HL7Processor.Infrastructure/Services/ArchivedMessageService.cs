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

    public async Task<PagedResult<object>> GetArchivedMessagesAsync(int page = 1, int pageSize = 10)
    {
        var total = await _context.ArchivedMessages.CountAsync();
        
        var messages = await _context.ArchivedMessages
            .OrderByDescending(a => a.ArchivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<object>
        {
            Items = messages.Cast<object>().ToList(),
            TotalItems = total,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task<int> GetArchivedMessageCountAsync()
    {
        return await _context.ArchivedMessages.CountAsync();
    }
}