using HL7Processor.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ArchivedMessagesController : ControllerBase
{
    private readonly IArchivedMessageService _archivedMessageService;

    public ArchivedMessagesController(IArchivedMessageService archivedMessageService)
    {
        _archivedMessageService = archivedMessageService;
    }

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;

        var result = await _archivedMessageService.GetArchivedMessagesAsync(page, pageSize);
        return Ok(result);
    }
} 