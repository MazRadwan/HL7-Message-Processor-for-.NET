using HL7Processor.Infrastructure.Entities;
using HL7Processor.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ArchivedMessagesController : ControllerBase
{
    private readonly HL7Processor.Infrastructure.HL7DbContext _db;

    public ArchivedMessagesController(HL7Processor.Infrastructure.HL7DbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult List(int page = 1, int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;

        var total = _db.ArchivedMessages.Count();
        var items = _db.ArchivedMessages
            .OrderByDescending(a => a.ArchivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<HL7ArchivedMessageEntity>(items, total, page, pageSize);
        return Ok(result);
    }
} 