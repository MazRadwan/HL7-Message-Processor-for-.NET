using HL7Processor.Core.Models;
using HL7Processor.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using System.Collections.Concurrent;

namespace HL7Processor.Core.Transformation;

public class TransformationEngine
{
    private readonly ILogger<TransformationEngine> _logger;
    private readonly RuleEngine _ruleEngine;
    private readonly ObjectMapper _objectMapper;

    public TransformationEngine(ILogger<TransformationEngine> logger, RuleEngine ruleEngine, ObjectMapper objectMapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _objectMapper = objectMapper ?? throw new ArgumentNullException(nameof(objectMapper));
    }

    public Dictionary<string, object> TransformMessage(HL7Message message, FieldMappingConfiguration mappingConfig)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        _logger.LogDebug("Starting message transformation for {MessageId} using config {ConfigName}", 
            message.Id, mappingConfig.Name);

        var result = new Dictionary<string, object>();
        var delimiters = HL7ParsingUtils.ExtractDelimitersFromMessage(message);

        foreach (var mapping in mappingConfig.Mappings)
        {
            try
            {
                ProcessFieldMapping(message, mapping, result, delimiters);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process mapping {SourceField} -> {TargetField}", 
                    mapping.SourceField, mapping.TargetField);
            }
        }

        // Apply custom rules
        result = _ruleEngine.ApplyCustomRules(result, mappingConfig.CustomRules);

        _logger.LogDebug("Message transformation completed for {MessageId}, {FieldCount} fields transformed", 
            message.Id, result.Count);

        return result;
    }

    public Dictionary<string, object> TransformSegment(HL7Segment segment, FieldMappingConfiguration mappingConfig)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        _logger.LogDebug("Starting segment transformation for {SegmentType}", segment.Type);

        var result = new Dictionary<string, object>();
        var delimiters = new HL7Delimiters(); // Use default delimiters for segment-only transformation

        var relevantMappings = mappingConfig.Mappings
            .Where(m => m.SourceField.StartsWith(segment.Type, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var mapping in relevantMappings)
        {
            try
            {
                ProcessSegmentFieldMapping(segment, mapping, result, delimiters);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process segment mapping {SourceField} -> {TargetField}", 
                    mapping.SourceField, mapping.TargetField);
            }
        }

        _logger.LogDebug("Segment transformation completed for {SegmentType}, {FieldCount} fields transformed", 
            segment.Type, result.Count);

        return result;
    }

    public List<Dictionary<string, object>> TransformMessages(IEnumerable<HL7Message> messages, FieldMappingConfiguration mappingConfig)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        var messageList = messages.ToList();
        _logger.LogDebug("Starting batch transformation of {MessageCount} messages", messageList.Count);

        var results = new List<Dictionary<string, object>>(messageList.Count);

        foreach (var message in messageList)
        {
            try
            {
                var transformed = TransformMessage(message, mappingConfig);
                results.Add(transformed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transform message {MessageId}", message.Id);
                // Add empty result to maintain index alignment
                results.Add(new Dictionary<string, object>());
            }
        }

        _logger.LogInformation("Batch transformation completed: {SuccessCount}/{TotalCount} messages", 
            results.Count(r => r.Any()), messageList.Count);

        return results;
    }

    public IQueryable<Dictionary<string, object>> TransformToQueryable(IEnumerable<HL7Message> messages, FieldMappingConfiguration mappingConfig)
    {
        var transformed = TransformMessages(messages, mappingConfig);
        return transformed.AsQueryable();
    }

    public T TransformMessage<T>(HL7Message message, FieldMappingConfiguration mappingConfig) where T : new()
    {
        var dictionary = TransformMessage(message, mappingConfig);
        return _objectMapper.MapDictionaryToObject<T>(dictionary);
    }

    public IEnumerable<Dictionary<string, object>> TransformMessagesStreaming(IEnumerable<HL7Message> messages, FieldMappingConfiguration mappingConfig)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        foreach (var message in messages)
        {
            Dictionary<string, object>? result = null;
            try
            {
                result = TransformMessage(message, mappingConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transform message {MessageId} in streaming mode", message.Id);
                result = new Dictionary<string, object>();
            }

            yield return result;
        }
    }

    private void ProcessFieldMapping(HL7Message message, FieldMapping mapping, Dictionary<string, object> result, HL7Delimiters delimiters)
    {
        // Check conditions first
        if (!_ruleEngine.EvaluateConditions(mapping.Conditions, message, result))
        {
            return;
        }

        var value = ExtractFieldValue(message, mapping.SourceField, delimiters);

        // Apply default value if field is missing
        if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(mapping.DefaultValue))
        {
            value = mapping.DefaultValue;
        }

        // Skip if required field is missing
        if (mapping.IsRequired && string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("Required field {SourceField} is missing for message {MessageId}", 
                mapping.SourceField, message.Id);
            return;
        }

        // Apply transformation function
        if (!string.IsNullOrEmpty(mapping.TransformFunction))
        {
            value = ApplyTransformFunction(value, mapping.TransformFunction);
        }

        // Convert data type
        var convertedValue = ConvertDataType(value, mapping.DataType);

        result[mapping.TargetField] = convertedValue;
    }

    private void ProcessSegmentFieldMapping(HL7Segment segment, FieldMapping mapping, Dictionary<string, object> result, HL7Delimiters delimiters)
    {
        var value = ExtractSegmentFieldValue(segment, mapping.SourceField, delimiters);

        if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(mapping.DefaultValue))
        {
            value = mapping.DefaultValue;
        }

        if (mapping.IsRequired && string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("Required field {SourceField} is missing for segment {SegmentType}", 
                mapping.SourceField, segment.Type);
            return;
        }

        if (!string.IsNullOrEmpty(mapping.TransformFunction))
        {
            value = ApplyTransformFunction(value, mapping.TransformFunction);
        }

        var convertedValue = ConvertDataType(value, mapping.DataType);
        result[mapping.TargetField] = convertedValue;
    }

    private string ExtractFieldValue(HL7Message message, string sourceField, HL7Delimiters delimiters)
    {
        // Handle special fields
        if (sourceField.Equals("MessageType", StringComparison.OrdinalIgnoreCase))
        {
            return message.MessageType.ToString();
        }
        if (sourceField.Equals("MessageId", StringComparison.OrdinalIgnoreCase))
        {
            return message.Id;
        }
        if (sourceField.Equals("Timestamp", StringComparison.OrdinalIgnoreCase))
        {
            return message.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        // Parse segment-field notation (e.g., "PID-3")
        var parts = sourceField.Split('-');
        if (parts.Length >= 2)
        {
            var segmentType = parts[0];
            if (int.TryParse(parts[1], out var fieldNumber))
            {
                var segment = message.GetSegment(segmentType);
                if (segment != null)
                {
                    return ExtractSegmentFieldValue(segment, sourceField, delimiters);
                }
            }
        }

        return string.Empty;
    }

    private string ExtractSegmentFieldValue(HL7Segment segment, string sourceField, HL7Delimiters delimiters)
    {
        var parts = sourceField.Split('-');
        if (parts.Length >= 2 && int.TryParse(parts[1], out var fieldNumber))
        {
            var fieldValue = HL7ParsingUtils.ExtractFieldFromSegment(segment.RawData, fieldNumber, delimiters);

            // Handle component and sub-component extraction
            if (parts.Length >= 3 && int.TryParse(parts[2], out var componentNumber))
            {
                fieldValue = HL7ParsingUtils.GetComponentFromField(fieldValue, componentNumber, delimiters);

                if (parts.Length >= 4 && int.TryParse(parts[3], out var subComponentNumber))
                {
                    fieldValue = HL7ParsingUtils.GetSubComponentFromComponent(fieldValue, subComponentNumber, delimiters);
                }
            }

            return HL7ParsingUtils.UnescapeHL7String(fieldValue, delimiters);
        }

        return string.Empty;
    }

    private string ApplyTransformFunction(string value, string function)
    {
        return function.ToLowerInvariant() switch
        {
            "uppercase" => value.ToUpperInvariant(),
            "lowercase" => value.ToLowerInvariant(),
            "trim" => value.Trim(),
            "phone" => FormatPhoneNumber(value),
            "date" => FormatDateValue(value),
            "name" => FormatPersonName(value),
            _ => value
        };
    }

    private string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
        {
            return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }
        return phone;
    }

    private string FormatDateValue(string date)
    {
        var parsed = HL7ParsingUtils.ParseHL7Date(date);
        return parsed?.ToString("yyyy-MM-dd") ?? date;
    }

    private string FormatPersonName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        // Convert "LAST^FIRST^MIDDLE" to "First Middle Last"
        var parts = name.Split('^');
        if (parts.Length >= 2)
        {
            var lastName = parts[0].Trim();
            var firstName = parts[1].Trim();
            var middleName = parts.Length > 2 ? parts[2].Trim() : "";
            
            var fullName = $"{firstName}";
            if (!string.IsNullOrEmpty(middleName))
            {
                fullName += $" {middleName}";
            }
            fullName += $" {lastName}";
            
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fullName.ToLowerInvariant());
        }
        
        return name;
    }

    private object ConvertDataType(string value, string dataType)
    {
        if (string.IsNullOrEmpty(value)) return value;

        try
        {
            return dataType.ToLowerInvariant() switch
            {
                "int" or "integer" => int.Parse(value),
                "long" => long.Parse(value),
                "decimal" or "double" => decimal.Parse(value),
                "bool" or "boolean" => bool.Parse(value),
                "datetime" => HL7ParsingUtils.ParseHL7DateTime(value) ?? DateTime.MinValue,
                "date" => HL7ParsingUtils.ParseHL7Date(value) ?? DateTime.MinValue,
                "time" => HL7ParsingUtils.ParseHL7Time(value) ?? TimeSpan.Zero,
                _ => value
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert value '{Value}' to type '{DataType}'", value, dataType);
            return value;
        }
    }
}