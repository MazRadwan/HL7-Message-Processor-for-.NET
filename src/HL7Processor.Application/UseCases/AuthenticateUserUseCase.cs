using HL7Processor.Application.DTOs;
using HL7Processor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Application.UseCases;

public class AuthenticateUserUseCase : IAuthenticateUserUseCase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticateUserUseCase> _logger;

    public AuthenticateUserUseCase(ITokenService tokenService, ILogger<AuthenticateUserUseCase> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with missing username or password");
            return LoginResponse.Failed("Username and password are required");
        }

        // TODO: Replace with real user validation / Identity store
        if (request.Username == "admin" && request.Password == "password")
        {
            var roles = new[] { "Admin" };
            var token = _tokenService.GenerateToken(request.Username, roles);
            
            _logger.LogInformation("Successful login for user: {Username}", request.Username);
            return LoginResponse.Successful(token, request.Username, roles);
        }

        _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
        return LoginResponse.Failed("Invalid username or password");
    }
}