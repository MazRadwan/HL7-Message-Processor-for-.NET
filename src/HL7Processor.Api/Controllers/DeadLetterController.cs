using HL7Processor.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DeadLetterController : ControllerBase
{
    private readonly IRequeueMessageUseCase _requeueMessageUseCase;

    public DeadLetterController(IRequeueMessageUseCase requeueMessageUseCase)
    {
        _requeueMessageUseCase = requeueMessageUseCase;
    }

    [HttpPost("requeue")] // Requeue latest message from DLQ to primary queue
    public async Task<IActionResult> Requeue(string queueName, CancellationToken token)
    {
        var success = await _requeueMessageUseCase.ExecuteAsync(queueName, token);
        
        if (!success)
            return NotFound();

        return Ok();
    }
} 