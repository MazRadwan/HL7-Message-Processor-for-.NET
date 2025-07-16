using System.ComponentModel.DataAnnotations;
using HL7Processor.Core.Models;
using HL7Processor.Core.Exceptions;

namespace HL7Processor.Core.Validation;

public class HL7MessageValidator
{
    private readonly List<string> _validationErrors = new();

    public bool ValidateMessage(HL7Message message)
    {
        _validationErrors.Clear();
        
        // Basic model validation using Data Annotations
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(message, context, results, true))
        {
            _validationErrors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
        }

        // Custom HL7-specific validation
        ValidateHL7Structure(message);
        ValidateMessageHeader(message);
        ValidateSegments(message);
        ValidateMessageType(message);

        // Update message validation state
        if (_validationErrors.Count > 0)
        {
            message.ValidationErrors.AddRange(_validationErrors);
            message.IsValid = false;
            return false;
        }

        message.ClearValidationErrors();
        return true;
    }

    public List<string> GetValidationErrors()
    {
        return new List<string>(_validationErrors);
    }

    public void ThrowIfInvalid(HL7Message message)
    {
        if (!ValidateMessage(message))
        {
            throw new HL7ValidationException(
                $"HL7 message validation failed for message type {message.MessageType}", 
                _validationErrors,
                message.MessageType.ToString(),
                originalMessage: message.RawMessage
            );
        }
    }

    private void ValidateHL7Structure(HL7Message message)
    {
        if (string.IsNullOrEmpty(message.RawMessage))
        {
            _validationErrors.Add("Raw message cannot be empty");
            return;
        }

        // Check for minimum required characters
        if (message.RawMessage.Length < 10)
        {
            _validationErrors.Add("Message is too short to be a valid HL7 message");
        }

        // Check for proper line endings
        if (!message.RawMessage.Contains('\r') && !message.RawMessage.Contains('\n'))
        {
            _validationErrors.Add("Message does not contain proper line endings");
        }

        // Check for field separator
        if (!message.RawMessage.Contains('|'))
        {
            _validationErrors.Add("Message does not contain field separator '|'");
        }
    }

    private void ValidateMessageHeader(HL7Message message)
    {
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment == null)
        {
            _validationErrors.Add("MSH (Message Header) segment is required");
            return;
        }

        // MSH segment should have at least 11 fields
        if (mshSegment.Fields.Count < 11)
        {
            _validationErrors.Add("MSH segment must have at least 11 fields");
        }

        // Validate required MSH fields
        ValidateRequiredField(mshSegment, 1, "Field Separator");
        ValidateRequiredField(mshSegment, 2, "Encoding Characters");
        ValidateRequiredField(mshSegment, 3, "Sending Application");
        ValidateRequiredField(mshSegment, 5, "Receiving Application");
        ValidateRequiredField(mshSegment, 7, "Date/Time of Message");
        ValidateRequiredField(mshSegment, 9, "Message Type");
        ValidateRequiredField(mshSegment, 10, "Message Control ID");
        ValidateRequiredField(mshSegment, 11, "Processing ID");
        ValidateRequiredField(mshSegment, 12, "Version ID");
    }

    private void ValidateRequiredField(HL7Segment segment, int fieldNumber, string fieldName)
    {
        var fieldValue = segment.GetFieldValue(fieldNumber);
        if (string.IsNullOrEmpty(fieldValue))
        {
            _validationErrors.Add($"{segment.Type} field {fieldNumber} ({fieldName}) is required");
        }
    }

    private void ValidateSegments(HL7Message message)
    {
        if (message.Segments.Count == 0)
        {
            _validationErrors.Add("Message must contain at least one segment");
            return;
        }

        // Validate each segment
        foreach (var segment in message.Segments)
        {
            ValidateSegment(segment);
        }

        // Check for duplicate segments that shouldn't be duplicated
        var segmentCounts = message.Segments.GroupBy(s => s.Type).ToDictionary(g => g.Key, g => g.Count());
        
        foreach (var kvp in segmentCounts)
        {
            if (kvp.Key == "MSH" && kvp.Value > 1)
            {
                _validationErrors.Add("MSH segment should appear only once");
            }
        }
    }

    private void ValidateSegment(HL7Segment segment)
    {
        if (string.IsNullOrEmpty(segment.Type))
        {
            _validationErrors.Add("Segment type cannot be empty");
            return;
        }

        if (segment.Type.Length != 3)
        {
            _validationErrors.Add($"Segment type '{segment.Type}' must be exactly 3 characters");
        }

        if (!segment.Type.All(char.IsLetterOrDigit))
        {
            _validationErrors.Add($"Segment type '{segment.Type}' must contain only letters and digits");
        }

        // Validate segment-specific rules
        switch (segment.Type.ToUpperInvariant())
        {
            case "MSH":
                ValidateMSHSegment(segment);
                break;
            case "PID":
                ValidatePIDSegment(segment);
                break;
            case "EVN":
                ValidateEVNSegment(segment);
                break;
        }
    }

    private void ValidateMSHSegment(HL7Segment segment)
    {
        // MSH-1 should be field separator
        var fieldSeparator = segment.GetFieldValue(1);
        if (fieldSeparator != "|")
        {
            _validationErrors.Add("MSH-1 (Field Separator) must be '|'");
        }

        // MSH-2 should be encoding characters
        var encodingChars = segment.GetFieldValue(2);
        if (string.IsNullOrEmpty(encodingChars) || encodingChars.Length != 4)
        {
            _validationErrors.Add("MSH-2 (Encoding Characters) must be exactly 4 characters");
        }
    }

    private void ValidatePIDSegment(HL7Segment segment)
    {
        // PID-3 (Patient ID) is required
        ValidateRequiredField(segment, 3, "Patient ID");
        
        // PID-5 (Patient Name) is required
        ValidateRequiredField(segment, 5, "Patient Name");
    }

    private void ValidateEVNSegment(HL7Segment segment)
    {
        // EVN-1 (Event Type Code) is required
        ValidateRequiredField(segment, 1, "Event Type Code");
        
        // EVN-2 (Recorded Date/Time) is required
        ValidateRequiredField(segment, 2, "Recorded Date/Time");
    }

    private void ValidateMessageType(HL7Message message)
    {
        if (message.MessageType == HL7MessageType.Unknown)
        {
            _validationErrors.Add("Message type is unknown or not supported");
        }

        var mshSegment = message.GetSegment("MSH");
        if (mshSegment != null)
        {
            var messageTypeField = mshSegment.GetFieldValue(9);
            if (!string.IsNullOrEmpty(messageTypeField))
            {
                var parsedMessageType = HL7MessageTypeExtensions.FromMessageTypeCode(messageTypeField);
                if (parsedMessageType != message.MessageType)
                {
                    _validationErrors.Add($"Message type mismatch: Header indicates {messageTypeField}, but message type is {message.MessageType}");
                }
            }
        }
    }
}