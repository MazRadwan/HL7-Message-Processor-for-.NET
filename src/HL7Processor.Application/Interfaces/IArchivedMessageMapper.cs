using HL7Processor.Application.DTOs;
using HL7Processor.Core.Models;

namespace HL7Processor.Application.Interfaces;

public interface IArchivedMessageMapper
{
    ArchivedMessageDto ToDto(ArchivedMessage entity);
    ArchivedMessage ToEntity(ArchivedMessageDto dto);
}