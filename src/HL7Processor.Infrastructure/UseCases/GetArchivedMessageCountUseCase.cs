using HL7Processor.Application.UseCases;
using HL7Processor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Infrastructure.UseCases;

public class GetArchivedMessageCountUseCase : IGetArchivedMessageCountUseCase
{
    private readonly IArchivedMessageRepository _repository;
    private readonly ILogger<GetArchivedMessageCountUseCase> _logger;

    public GetArchivedMessageCountUseCase(IArchivedMessageRepository repository, ILogger<GetArchivedMessageCountUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            _logger.LogDebug("Getting archived message count");
            return await _repository.GetCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archived message count");
            return 0;
        }
    }
}