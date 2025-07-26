using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IAuthenticateUserUseCase
{
    Task<LoginResponse> ExecuteAsync(LoginRequest request);
}