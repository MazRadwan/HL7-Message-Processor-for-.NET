using HL7Processor.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ArchivedMessagesController : ControllerBase
{
    private readonly IGetArchivedMessagesUseCase _getArchivedMessagesUseCase;

    public ArchivedMessagesController(IGetArchivedMessagesUseCase getArchivedMessagesUseCase)
    {
        _getArchivedMessagesUseCase = getArchivedMessagesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20)
    {
        var result = await _getArchivedMessagesUseCase.ExecuteAsync(page, pageSize);
        return Ok(result);
    }
} 