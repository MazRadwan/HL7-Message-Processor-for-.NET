using HL7Processor.Application.DTOs;
using HL7Processor.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace HL7Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticateUserUseCase _authenticateUserUseCase;

    public AuthController(IAuthenticateUserUseCase authenticateUserUseCase)
    {
        _authenticateUserUseCase = authenticateUserUseCase;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authenticateUserUseCase.ExecuteAsync(request);
        
        if (!response.Success)
            return Unauthorized(new { error = response.ErrorMessage });

        return Ok(new { access_token = response.AccessToken });
    }
} 