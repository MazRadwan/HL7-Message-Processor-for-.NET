using HL7Processor.Core.Communication.Queue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DeadLetterController : ControllerBase
{
    private readonly IMessageQueue _queue;

    public DeadLetterController(IMessageQueue queue)
    {
        _queue = queue;
    }

    [HttpPost("requeue")] // Requeue latest message from DLQ to primary queue
    public async Task<IActionResult> Requeue(string queueName, CancellationToken token)
    {
        var payload = await _queue.ReceiveFromDeadLetterAsync(queueName, token);
        if (payload is null)
            return NotFound();

        await _queue.PublishAsync(queueName, payload, token);
        return Ok();
    }
} 