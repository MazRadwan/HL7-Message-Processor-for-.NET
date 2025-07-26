using HL7Processor.Application.DTOs;
using HL7Processor.Application.Interfaces;
using HL7Processor.Application.UseCases;
using HL7Processor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Infrastructure.UseCases;

public class GetArchivedMessagesUseCase : IGetArchivedMessagesUseCase
{
    private readonly IArchivedMessageRepository _repository;
    private readonly IArchivedMessageMapper _mapper;
    private readonly ILogger<GetArchivedMessagesUseCase> _logger;

    public GetArchivedMessagesUseCase(
        IArchivedMessageRepository repository, 
        IArchivedMessageMapper mapper,
        ILogger<GetArchivedMessagesUseCase> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ArchivedMessageDto>> ExecuteAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            _logger.LogDebug("Getting archived messages - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            
            // Get paged entities from repository
            var entities = await _repository.GetPagedAsync(page, pageSize);
            var totalCount = await _repository.GetCountAsync();
            
            // Map to DTOs
            var dtos = entities.Select(entity => _mapper.ToDto(entity)).ToList();
            
            return new PagedResult<ArchivedMessageDto>(dtos, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archived messages");
            return new PagedResult<ArchivedMessageDto>(new List<ArchivedMessageDto>(), 0, page, pageSize);
        }
    }
}