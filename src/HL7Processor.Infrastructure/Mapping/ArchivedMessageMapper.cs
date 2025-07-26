using HL7Processor.Application.DTOs;
using HL7Processor.Application.Interfaces;
using HL7Processor.Core.Models;
using HL7Processor.Infrastructure.Entities;

namespace HL7Processor.Infrastructure.Mapping;

public class ArchivedMessageMapper : IArchivedMessageMapper
{
    public ArchivedMessageDto ToDto(ArchivedMessage entity)
    {
        return new ArchivedMessageDto
        {
            Id = entity.Id,
            OriginalMessageId = entity.OriginalMessageId,
            MessageType = entity.MessageType,
            Version = entity.Version,
            OriginalTimestamp = entity.OriginalTimestamp,
            ArchivedAt = entity.ArchivedAt
        };
    }

    public ArchivedMessage ToEntity(ArchivedMessageDto dto)
    {
        return new ArchivedMessage(
            dto.Id,
            dto.OriginalMessageId,
            dto.MessageType,
            dto.Version,
            dto.OriginalTimestamp,
            dto.ArchivedAt);
    }

    public HL7ArchivedMessageEntity ToEntityFrameworkEntity(ArchivedMessage entity)
    {
        return new HL7ArchivedMessageEntity
        {
            Id = entity.Id,
            OriginalMessageId = entity.OriginalMessageId,
            MessageType = entity.MessageType,
            Version = entity.Version,
            OriginalTimestamp = entity.OriginalTimestamp,
            ArchivedAt = entity.ArchivedAt
        };
    }

    public ArchivedMessage FromEntityFrameworkEntity(HL7ArchivedMessageEntity efEntity)
    {
        return new ArchivedMessage(
            efEntity.Id,
            efEntity.OriginalMessageId,
            efEntity.MessageType,
            efEntity.Version,
            efEntity.OriginalTimestamp,
            efEntity.ArchivedAt);
    }
}