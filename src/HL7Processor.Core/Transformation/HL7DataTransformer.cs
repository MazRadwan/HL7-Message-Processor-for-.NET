using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;
using HL7Processor.Core.Exceptions;
using HL7Processor.Core.Transformation.Converters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Processor.Core.Transformation;

public class HL7DataTransformer
{
    private readonly ILogger<HL7DataTransformer> _logger;
    private readonly TransformationEngine _transformationEngine;
    private readonly JsonConverter _jsonConverter;
    private readonly XmlConverter _xmlConverter;

    public HL7DataTransformer(
        ILogger<HL7DataTransformer> logger,
        TransformationEngine transformationEngine,
        JsonConverter jsonConverter,
        XmlConverter xmlConverter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
        _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
        _xmlConverter = xmlConverter ?? throw new ArgumentNullException(nameof(xmlConverter));
    }

    // Convenience constructor for simple scenarios/tests
    public HL7DataTransformer(ILogger<HL7DataTransformer> logger)
        : this(logger,
            new TransformationEngine(NullLogger<TransformationEngine>.Instance,
                new RuleEngine(NullLogger<RuleEngine>.Instance),
                new ObjectMapper(NullLogger<ObjectMapper>.Instance)),
            new JsonConverter(NullLogger<JsonConverter>.Instance, new ObjectMapper(NullLogger<ObjectMapper>.Instance)),
            new XmlConverter(NullLogger<XmlConverter>.Instance))
    {
    }

    public Dictionary<string, object> TransformMessage(HL7Message message, FieldMappingConfiguration mappingConfig)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        try
        {
            _logger.LogDebug("Starting transformation of message {MessageId} with config {ConfigName}", 
                message.Id, mappingConfig.Name);

            var result = _transformationEngine.TransformMessage(message, mappingConfig);

            _logger.LogInformation("Successfully transformed message {MessageId}: {FieldCount} fields mapped", 
                message.Id, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transform HL7 message {MessageId}: {Error}", 
                message.Id, ex.Message);
            throw new HL7ProcessingException($"Message transformation failed: {ex.Message}", ex, 
                message.MessageType.ToString(), originalMessage: message.RawMessage);
        }
    }

    public T TransformMessage<T>(HL7Message message, FieldMappingConfiguration mappingConfig) where T : new()
    {
        return _transformationEngine.TransformMessage<T>(message, mappingConfig);
    }

    public List<Dictionary<string, object>> TransformMessages(IEnumerable<HL7Message> messages, 
        FieldMappingConfiguration mappingConfig)
    {
        var messageList = messages.ToList();
        _logger.LogDebug("Starting batch transformation of {MessageCount} messages", messageList.Count);

        var result = _transformationEngine.TransformMessages(messageList, mappingConfig);

        _logger.LogInformation("Batch transformation completed: {SuccessCount}/{TotalCount} messages processed", 
            result.Count(r => r.Any()), messageList.Count);

        return result;
    }

    public IEnumerable<Dictionary<string, object>> TransformMessagesStreaming(IEnumerable<HL7Message> messages, 
        FieldMappingConfiguration mappingConfig)
    {
        _logger.LogDebug("Starting streaming transformation");
        return _transformationEngine.TransformMessagesStreaming(messages, mappingConfig);
    }

    public Dictionary<string, object> TransformSegment(HL7Segment segment, FieldMappingConfiguration mappingConfig)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        _logger.LogDebug("Transforming segment {SegmentType}", segment.Type);
        
        var result = _transformationEngine.TransformSegment(segment, mappingConfig);
        
        _logger.LogDebug("Segment transformation completed: {FieldCount} fields mapped", result.Count);
        
        return result;
    }

    public IQueryable<Dictionary<string, object>> TransformToQueryable(IEnumerable<HL7Message> messages, 
        FieldMappingConfiguration mappingConfig)
    {
        return _transformationEngine.TransformToQueryable(messages, mappingConfig);
    }

    public Dictionary<string, object> ApplyFieldLevelTransformations(Dictionary<string, object> data, 
        FieldMappingConfiguration mappingConfig)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        _logger.LogDebug("Applying field-level transformations to {FieldCount} fields", data.Count);

        var customRules = mappingConfig.CustomRules
            .Where(r => r.IsActive && r.RuleType == "field_transform")
            .ToList();

        if (customRules.Count == 0)
        {
            _logger.LogDebug("No field-level transformation rules found");
            return data;
        }

        var ruleEngine = new RuleEngine(NullLogger<RuleEngine>.Instance);
        var result = ruleEngine.ApplyCustomRules(data, customRules);

        _logger.LogDebug("Field-level transformations completed: {RuleCount} rules applied", customRules.Count);

        return result;
    }

    // JSON Conversion Methods
    public string ConvertToJson(HL7Message message, bool prettyPrint = true, bool includeMetadata = true)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        _logger.LogDebug("Converting HL7 message {MessageId} to JSON", message.Id);
        
        var json = _jsonConverter.ConvertHL7ToJson(message, prettyPrint, includeMetadata);
        
        _logger.LogDebug("JSON conversion completed for message {MessageId}: {Length} characters", 
            message.Id, json.Length);
        
        return json;
    }

    public string ConvertToFlatJson(HL7Message message, bool prettyPrint = true)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        _logger.LogDebug("Converting HL7 message {MessageId} to flat JSON", message.Id);
        
        var json = _jsonConverter.ConvertHL7ToFlatJson(message, prettyPrint);
        
        _logger.LogDebug("Flat JSON conversion completed for message {MessageId}: {Length} characters", 
            message.Id, json.Length);
        
        return json;
    }

    public HL7Message ConvertFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        _logger.LogDebug("Converting JSON to HL7 message ({Length} characters)", json.Length);
        
        var message = _jsonConverter.ConvertJsonToHL7(json);
        
        _logger.LogDebug("JSON to HL7 conversion completed: message {MessageId}", message.Id);
        
        return message;
    }

    public HL7Message ConvertFromFlatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        _logger.LogDebug("Converting flat JSON to HL7 message ({Length} characters)", json.Length);
        
        var message = _jsonConverter.ConvertFlatJsonToHL7(json);
        
        _logger.LogDebug("Flat JSON to HL7 conversion completed: message {MessageId}", message.Id);
        
        return message;
    }

    // XML Conversion Methods
    public string ConvertToXml(HL7Message message, bool prettyPrint = true, bool includeMetadata = true)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        _logger.LogDebug("Converting HL7 message {MessageId} to XML", message.Id);
        
        var xml = _xmlConverter.ConvertHL7ToXml(message, prettyPrint, includeMetadata);
        
        _logger.LogDebug("XML conversion completed for message {MessageId}: {Length} characters", 
            message.Id, xml.Length);
        
        return xml;
    }

    public HL7Message ConvertFromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException("XML cannot be null or empty", nameof(xml));

        _logger.LogDebug("Converting XML to HL7 message ({Length} characters)", xml.Length);
        
        var message = _xmlConverter.ConvertXmlToHL7(xml);
        
        _logger.LogDebug("XML to HL7 conversion completed: message {MessageId}", message.Id);
        
        return message;
    }

    public string ConvertToClinicalDocument(HL7Message message, string templateId = "2.16.840.1.113883.10.20.22.1.1")
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        _logger.LogDebug("Converting HL7 message {MessageId} to Clinical Document", message.Id);
        
        var clinicalDoc = _xmlConverter.ConvertHL7ToClinicalDocument(message, templateId);
        
        _logger.LogDebug("Clinical Document conversion completed for message {MessageId}: {Length} characters", 
            message.Id, clinicalDoc.Length);
        
        return clinicalDoc;
    }

    // Object Mapping Methods
    public T ConvertJsonToObject<T>(string json) where T : new()
    {
        return _jsonConverter.ConvertJsonToObject<T>(json);
    }

    public string ConvertObjectToJson<T>(T obj) where T : class
    {
        return _jsonConverter.ConvertObjectToJson(obj);
    }

    // Batch Processing Methods
    public List<string> ConvertMessagesToJson(IEnumerable<HL7Message> messages, bool prettyPrint = true, bool includeMetadata = true)
    {
        var messageList = messages.ToList();
        _logger.LogDebug("Converting {MessageCount} HL7 messages to JSON", messageList.Count);

        var results = new List<string>();
        var successCount = 0;

        foreach (var message in messageList)
        {
            try
            {
                var json = _jsonConverter.ConvertHL7ToJson(message, prettyPrint, includeMetadata);
                results.Add(json);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert message {MessageId} to JSON", message.Id);
                results.Add(string.Empty);
            }
        }

        _logger.LogInformation("Batch JSON conversion completed: {SuccessCount}/{TotalCount} messages converted", 
            successCount, messageList.Count);

        return results;
    }

    public List<HL7Message> ConvertJsonsToMessages(IEnumerable<string> jsons)
    {
        var jsonList = jsons.ToList();
        _logger.LogDebug("Converting {JsonCount} JSON strings to HL7 messages", jsonList.Count);

        var results = new List<HL7Message>();
        var successCount = 0;

        foreach (var json in jsonList)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var message = _jsonConverter.ConvertJsonToHL7(json);
                    results.Add(message);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert JSON to HL7 message");
            }
        }

        _logger.LogInformation("Batch HL7 conversion completed: {SuccessCount}/{TotalCount} messages converted", 
            successCount, jsonList.Count);

        return results;
    }

    // Validation and Reporting
    public ValidationResult ValidateTransformation(HL7Message originalMessage, Dictionary<string, object> transformedData, FieldMappingConfiguration mappingConfig)
    {
        if (originalMessage == null) throw new ArgumentNullException(nameof(originalMessage));
        if (transformedData == null) throw new ArgumentNullException(nameof(transformedData));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        _logger.LogDebug("Validating transformation for message {MessageId}", originalMessage.Id);

        var validator = new DataIntegrityValidator(NullLogger<DataIntegrityValidator>.Instance);
        var result = validator.ValidateTransformedData(transformedData, originalMessage, mappingConfig);

        _logger.LogDebug("Transformation validation completed for message {MessageId}: Score={Score}", 
            originalMessage.Id, result.IntegrityScore);

        return new ValidationResult
        {
            IsValid = result.IsValid,
            Errors = result.Errors,
            Warnings = result.Warnings,
            Metadata = result.Metadata
        };
    }

    public Dictionary<string, object> GetTransformationStatistics(List<Dictionary<string, object>> transformedData)
    {
        if (transformedData == null) throw new ArgumentNullException(nameof(transformedData));

        _logger.LogDebug("Calculating transformation statistics for {RecordCount} records", transformedData.Count);

        var stats = new Dictionary<string, object>
        {
            ["TotalRecords"] = transformedData.Count,
            ["NonEmptyRecords"] = transformedData.Count(r => r.Any()),
            ["EmptyRecords"] = transformedData.Count(r => !r.Any()),
            ["AverageFieldCount"] = transformedData.Where(r => r.Any()).Average(r => r.Count),
            ["MaxFieldCount"] = transformedData.Where(r => r.Any()).DefaultIfEmpty().Max(r => r?.Count ?? 0),
            ["MinFieldCount"] = transformedData.Where(r => r.Any()).DefaultIfEmpty().Min(r => r?.Count ?? 0),
            ["UniqueFieldNames"] = transformedData.SelectMany(r => r.Keys).Distinct().Count(),
            ["MostCommonFields"] = transformedData
                .SelectMany(r => r.Keys)
                .GroupBy(k => k)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        _logger.LogDebug("Transformation statistics calculated: {TotalRecords} records, {UniqueFields} unique fields", 
            stats["TotalRecords"], stats["UniqueFieldNames"]);

        return stats;
    }
}