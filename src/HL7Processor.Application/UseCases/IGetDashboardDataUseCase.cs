using HL7Processor.Application.DTOs;

namespace HL7Processor.Application.UseCases;

public interface IGetDashboardDataUseCase
{
    Task<DashboardDataDto> ExecuteAsync();
    Task<List<ThroughputPointDto>> GetThroughputLastHourAsync(int intervalMinutes = 5);
}