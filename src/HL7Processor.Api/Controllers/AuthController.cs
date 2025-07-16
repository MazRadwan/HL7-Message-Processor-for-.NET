using HL7Processor.Api.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;
    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // NOTE: placeholder – replace with real user validation / Identity store
        if (request.Username == "admin" && request.Password == "password")
        {
            var token = _tokenService.GenerateToken(request.Username, new[] { "Admin" });
            return Ok(new { access_token = token });
        }
        return Unauthorized();
    }
} 