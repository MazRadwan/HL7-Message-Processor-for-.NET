using HL7Processor.Core.Communication.Queue;
using HL7Processor.Core.Communication.MLLP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class MessageController : ControllerBase
{
    private readonly IMessageQueue _queue;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IMessageQueue queue, ILogger<MessageController> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] string hl7Message, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
            return BadRequest("Message cannot be empty");

        await _queue.PublishAsync("hl7_in", System.Text.Encoding.UTF8.GetBytes(hl7Message), token);
        _logger.LogInformation("Received HL7 message via API, queued for processing");
        return Accepted();
    }
} 