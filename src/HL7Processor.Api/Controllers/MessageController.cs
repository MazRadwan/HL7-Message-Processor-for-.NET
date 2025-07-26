using HL7Processor.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class MessageController : ControllerBase
{
    private readonly ISubmitMessageUseCase _submitMessageUseCase;

    public MessageController(ISubmitMessageUseCase submitMessageUseCase)
    {
        _submitMessageUseCase = submitMessageUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] string hl7Message, CancellationToken token)
    {
        var success = await _submitMessageUseCase.ExecuteAsync(hl7Message, token);
        
        if (!success)
            return BadRequest("Message cannot be empty");

        return Accepted();
    }
} 