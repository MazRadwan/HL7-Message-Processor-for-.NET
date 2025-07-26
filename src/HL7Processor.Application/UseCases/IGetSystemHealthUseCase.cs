using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IGetSystemHealthUseCase
{
    Task<SystemHealthDto> GetSystemHealthAsync();
}