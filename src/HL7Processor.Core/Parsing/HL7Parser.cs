using HL7Processor.Core.Models;
using HL7Processor.Core.Exceptions;
using HL7Processor.Core.Validation;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Core.Parsing;

public class HL7Parser
{
    private readonly ILogger<HL7Parser> _logger;
    private readonly HL7MessageValidator _validator;

    public HL7Parser(ILogger<HL7Parser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = new HL7MessageValidator();
    }

    public HL7Message Parse(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            throw new HL7ProcessingException("Raw message cannot be null or empty");
        }

        try
        {
            var message = new HL7Message
            {
                RawMessage = rawMessage,
                Timestamp = DateTime.UtcNow
            };

            // Split message into segments
            var segments = SplitMessageIntoSegments(rawMessage);
            
            // Parse each segment
            foreach (var segmentData in segments)
            {
                if (!string.IsNullOrWhiteSpace(segmentData))
                {
                    var segment = ParseSegment(segmentData);
                    message.AddSegment(segment);
                }
            }

            // Extract message header information
            ExtractMessageHeaderInfo(message);

            // Identify message type
            message.MessageType = IdentifyMessageType(message);

            // Validate the parsed message
            if (!_validator.ValidateMessage(message))
            {
                _logger.LogWarning("Message validation failed for message {MessageId}: {Errors}", 
                    message.Id, string.Join(", ", message.ValidationErrors));
            }

            _logger.LogInformation("Successfully parsed HL7 message {MessageId} of type {MessageType}", 
                message.Id, message.MessageType);

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse HL7 message: {Message}", ex.Message);
            throw new HL7ProcessingException($"Failed to parse HL7 message: {ex.Message}", ex, messageType: null, originalMessage: rawMessage);
        }
    }

    public bool TryParse(string rawMessage, out HL7Message? message)
    {
        try
        {
            message = Parse(rawMessage);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse HL7 message: {Message}", ex.Message);
            message = null;
            return false;
        }
    }

    private string[] SplitMessageIntoSegments(string rawMessage)
    {
        // HL7 messages use \r (carriage return) as segment separator
        // Some systems might use \n or \r\n
        var separators = new[] { "\r\n", "\r", "\n" };
        var segments = rawMessage.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length == 0)
        {
            throw new HL7ProcessingException("Message contains no segments");
        }

        return segments;
    }

    private HL7Segment ParseSegment(string segmentData)
    {
        if (string.IsNullOrWhiteSpace(segmentData))
        {
            throw new HL7ProcessingException("Segment data cannot be empty");
        }

        // Extract segment type (first 3 characters)
        if (segmentData.Length < 3)
        {
            throw new HL7ProcessingException($"Segment data is too short: {segmentData}");
        }

        var segmentType = segmentData.Substring(0, 3);
        
        // Validate segment type
        if (!segmentType.All(char.IsLetterOrDigit))
        {
            throw new HL7ProcessingException($"Invalid segment type: {segmentType}");
        }

        var segment = new HL7Segment(segmentType, segmentData);
        
        _logger.LogDebug("Parsed segment {SegmentType} with {FieldCount} fields", 
            segmentType, segment.Fields.Count);

        return segment;
    }

    private void ExtractMessageHeaderInfo(HL7Message message)
    {
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment == null)
        {
            message.AddValidationError("MSH segment is required");
            return;
        }

        try
        {
            // MSH-3: Sending Application
            message.SendingApplication = mshSegment.GetFieldValue(3);

            // MSH-4: Sending Facility
            message.SendingFacility = mshSegment.GetFieldValue(4);

            // MSH-5: Receiving Application
            message.ReceivingApplication = mshSegment.GetFieldValue(5);

            // MSH-6: Receiving Facility
            message.ReceivingFacility = mshSegment.GetFieldValue(6);

            // MSH-10: Message Control ID
            message.MessageControlId = mshSegment.GetFieldValue(10);

            // MSH-11: Processing ID
            message.ProcessingId = mshSegment.GetFieldValue(11);

            // MSH-12: Version ID
            var versionId = mshSegment.GetFieldValue(12);
            if (!string.IsNullOrEmpty(versionId))
            {
                message.Version = versionId;
            }

            // MSH-7: Date/Time of Message
            var timestampField = mshSegment.GetFieldValue(7);
            if (!string.IsNullOrEmpty(timestampField))
            {
                if (TryParseHL7DateTime(timestampField, out var timestamp))
                {
                    message.Timestamp = timestamp;
                }
                else
                {
                    message.AddValidationError($"Invalid timestamp format in MSH-7: {timestampField}");
                }
            }
        }
        catch (Exception ex)
        {
            message.AddValidationError($"Error extracting header information: {ex.Message}");
        }
    }

    private HL7MessageType IdentifyMessageType(HL7Message message)
    {
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment == null)
        {
            return HL7MessageType.Unknown;
        }

        // MSH-9: Message Type
        var messageTypeField = mshSegment.GetFieldValue(9);
        if (string.IsNullOrEmpty(messageTypeField))
        {
            return HL7MessageType.Unknown;
        }

        return HL7MessageTypeExtensions.FromMessageTypeCode(messageTypeField);
    }

    private bool TryParseHL7DateTime(string hl7DateTime, out DateTime dateTime)
    {
        dateTime = default;

        if (string.IsNullOrEmpty(hl7DateTime))
        {
            return false;
        }

        // HL7 datetime format: YYYYMMDDHHMMSS[.SSSS][+/-ZZZZ]
        // We'll handle the basic format: YYYYMMDDHHMMSS
        var cleanDateTime = hl7DateTime.Split('+')[0].Split('-')[0]; // Remove timezone
        
        if (cleanDateTime.Length < 8)
        {
            return false;
        }

        try
        {
            var year = int.Parse(cleanDateTime.Substring(0, 4));
            var month = int.Parse(cleanDateTime.Substring(4, 2));
            var day = int.Parse(cleanDateTime.Substring(6, 2));

            var hour = 0;
            var minute = 0;
            var second = 0;

            if (cleanDateTime.Length >= 10)
            {
                hour = int.Parse(cleanDateTime.Substring(8, 2));
            }

            if (cleanDateTime.Length >= 12)
            {
                minute = int.Parse(cleanDateTime.Substring(10, 2));
            }

            if (cleanDateTime.Length >= 14)
            {
                second = int.Parse(cleanDateTime.Substring(12, 2));
            }

            dateTime = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string SerializeToHL7(HL7Message message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var segments = new List<string>();
        
        foreach (var segment in message.Segments.OrderBy(s => s.SequenceNumber))
        {
            segment.RebuildRawData();
            segments.Add(segment.RawData);
        }

        return string.Join("\r", segments);
    }
}