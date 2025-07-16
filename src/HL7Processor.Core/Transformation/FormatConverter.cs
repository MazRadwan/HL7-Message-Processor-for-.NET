using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;
using HL7Processor.Core.Transformation.Converters;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Core.Transformation;

/// <summary>
/// Simplified format converter that delegates to specialized converters
/// </summary>
public class FormatConverter
{
    private readonly ILogger<FormatConverter> _logger;
    private readonly JsonConverter _jsonConverter;
    private readonly XmlConverter _xmlConverter;

    public FormatConverter(ILogger<FormatConverter> logger, JsonConverter jsonConverter, XmlConverter xmlConverter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
        _xmlConverter = xmlConverter ?? throw new ArgumentNullException(nameof(xmlConverter));
    }

    // JSON Conversion Methods
    public string ConvertHL7ToJson(HL7Message message, bool indented = true, bool includeMetadata = true)
    {
        _logger.LogDebug("Converting HL7 message {MessageId} to JSON", message.Id);
        return _jsonConverter.ConvertHL7ToJson(message, indented, includeMetadata);
    }

    public string ConvertHL7ToFlatJson(HL7Message message, bool includeMetadata = false)
    {
        _logger.LogDebug("Converting HL7 message {MessageId} to flat JSON", message.Id);
        return _jsonConverter.ConvertHL7ToFlatJson(message, true);
    }

    public HL7Message ConvertJsonToHL7(string json)
    {
        _logger.LogDebug("Converting JSON to HL7 message ({Length} characters)", json.Length);
        return _jsonConverter.ConvertJsonToHL7(json);
    }

    public HL7Message ConvertFlatJsonToHL7(string json)
    {
        _logger.LogDebug("Converting flat JSON to HL7 message ({Length} characters)", json.Length);
        return _jsonConverter.ConvertFlatJsonToHL7(json);
    }

    // XML Conversion Methods
    public string ConvertHL7ToXml(HL7Message message, bool indented = true, bool includeMetadata = true)
    {
        _logger.LogDebug("Converting HL7 message {MessageId} to XML", message.Id);
        return _xmlConverter.ConvertHL7ToXml(message, indented, includeMetadata);
    }

    public HL7Message ConvertXmlToHL7(string xml)
    {
        _logger.LogDebug("Converting XML to HL7 message ({Length} characters)", xml.Length);
        return _xmlConverter.ConvertXmlToHL7(xml);
    }

    public string ConvertHL7ToClinicalDocument(HL7Message message, string templateId = "2.16.840.1.113883.10.20.22.1.1")
    {
        _logger.LogDebug("Converting HL7 message {MessageId} to Clinical Document", message.Id);
        return _xmlConverter.ConvertHL7ToClinicalDocument(message, templateId);
    }

    // CSV Conversion Methods
    public string ConvertHL7ToCsv(HL7Message message, string delimiter = ",", bool includeHeaders = true)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        _logger.LogDebug("Converting HL7 message {MessageId} to CSV", message.Id);

        try
        {
            var csv = new StringBuilder();
            
            // Add headers if requested
            if (includeHeaders)
            {
                var headers = new List<string> { "SegmentType", "FieldNumber", "FieldValue" };
                csv.AppendLine(string.Join(delimiter, headers));
            }

            // Add data rows
            foreach (var segment in message.Segments)
            {
                for (int i = 1; i < segment.Fields.Count; i++)
                {
                    var fieldValue = segment.GetFieldValue(i);
                    if (!string.IsNullOrEmpty(fieldValue))
                    {
                        var escapedValue = EscapeCsvValue(fieldValue, delimiter);
                        csv.AppendLine($"{segment.Type}{delimiter}{i}{delimiter}{escapedValue}");
                    }
                }
            }

            var result = csv.ToString();
            _logger.LogDebug("CSV conversion completed for message {MessageId}: {Length} characters", 
                message.Id, result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert HL7 message {MessageId} to CSV", message.Id);
            throw;
        }
    }

    public List<HL7Message> ConvertCsvToHL7(string csv, string delimiter = ",", bool hasHeaders = true)
    {
        if (string.IsNullOrWhiteSpace(csv)) throw new ArgumentException("CSV cannot be null or empty", nameof(csv));

        _logger.LogDebug("Converting CSV to HL7 messages ({Length} characters)", csv.Length);

        try
        {
            var messages = new List<HL7Message>();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var startIndex = hasHeaders ? 1 : 0;

            var currentMessage = new HL7Message();
            var messageData = new Dictionary<string, List<(int fieldNumber, string value)>>();

            for (int i = startIndex; i < lines.Length; i++)
            {
                var parts = ParseCsvLine(lines[i], delimiter);
                if (parts.Length >= 3)
                {
                    var segmentType = parts[0];
                    if (int.TryParse(parts[1], out var fieldNumber))
                    {
                        var fieldValue = parts[2];

                        if (!messageData.TryGetValue(segmentType, out var fields))
                        {
                            fields = new List<(int, string)>();
                            messageData[segmentType] = fields;
                        }

                        fields.Add((fieldNumber, fieldValue));
                    }
                }
            }

            // Build message from collected data
            if (messageData.Any())
            {
                foreach (var segmentData in messageData)
                {
                    var segment = new HL7Segment(segmentData.Key, string.Empty);
                    foreach (var (fieldNumber, value) in segmentData.Value)
                    {
                        segment.SetFieldValue(fieldNumber, value);
                    }
                    segment.RebuildRawData();
                    currentMessage.AddSegment(segment);
                }

                currentMessage.RawMessage = string.Join("\r", currentMessage.Segments.Select(s => s.RawData));
                messages.Add(currentMessage);
            }

            _logger.LogDebug("CSV to HL7 conversion completed: {MessageCount} messages created", messages.Count);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert CSV to HL7 messages");
            throw;
        }
    }

    // Object Conversion Methods
    public T ConvertJsonToObject<T>(string json) where T : new()
    {
        return _jsonConverter.ConvertJsonToObject<T>(json);
    }

    public string ConvertObjectToJson<T>(T obj) where T : class
    {
        return _jsonConverter.ConvertObjectToJson(obj);
    }

    // Batch Conversion Methods
    public List<string> ConvertMessagesToFormat(IEnumerable<HL7Message> messages, string format, bool includeMetadata = true)
    {
        var messageList = messages.ToList();
        _logger.LogDebug("Converting {MessageCount} HL7 messages to {Format}", messageList.Count, format);

        var results = new List<string>();
        var successCount = 0;

        foreach (var message in messageList)
        {
            try
            {
                var converted = format.ToLowerInvariant() switch
                {
                    "json" => ConvertHL7ToJson(message, true, includeMetadata),
                    "xml" => ConvertHL7ToXml(message, true, includeMetadata),
                    "csv" => ConvertHL7ToCsv(message),
                    "flatjson" => ConvertHL7ToFlatJson(message, includeMetadata),
                    _ => throw new ArgumentException($"Unsupported format: {format}")
                };

                results.Add(converted);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert message {MessageId} to {Format}", message.Id, format);
                results.Add(string.Empty);
            }
        }

        _logger.LogInformation("Batch {Format} conversion completed: {SuccessCount}/{TotalCount} messages converted", 
            format, successCount, messageList.Count);

        return results;
    }

    public List<HL7Message> ConvertFromFormat(IEnumerable<string> data, string format)
    {
        var dataList = data.ToList();
        _logger.LogDebug("Converting {DataCount} {Format} strings to HL7 messages", dataList.Count, format);

        var results = new List<HL7Message>();
        var successCount = 0;

        foreach (var item in dataList)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var message = format.ToLowerInvariant() switch
                    {
                        "json" => ConvertJsonToHL7(item),
                        "xml" => ConvertXmlToHL7(item),
                        "flatjson" => ConvertFlatJsonToHL7(item),
                        _ => throw new ArgumentException($"Unsupported format: {format}")
                    };

                    results.Add(message);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert {Format} to HL7 message", format);
            }
        }

        _logger.LogInformation("Batch HL7 conversion completed: {SuccessCount}/{TotalCount} messages converted", 
            successCount, dataList.Count);

        return results;
    }

    // Format Validation Methods
    public ValidationResult ValidateFormat(string data, string format)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            switch (format.ToLowerInvariant())
            {
                case "json":
                    System.Text.Json.JsonDocument.Parse(data);
                    break;
                case "xml":
                    System.Xml.Linq.XDocument.Parse(data);
                    break;
                case "csv":
                    // Basic CSV validation
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        result.AddError("CSV data cannot be empty");
                    }
                    break;
                default:
                    result.AddError($"Unsupported format: {format}");
                    break;
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Invalid {format} format: {ex.Message}");
        }

        return result;
    }

    public FormatDetectionResult DetectFormat(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return new FormatDetectionResult { DetectedFormat = "unknown", Confidence = 0.0 };
        }

        var trimmed = data.Trim();

        // JSON detection
        if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || 
            (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(data);
                return new FormatDetectionResult { DetectedFormat = "json", Confidence = 0.95 };
            }
            catch { }
        }

        // XML detection
        if (trimmed.StartsWith("<") && trimmed.EndsWith(">"))
        {
            try
            {
                System.Xml.Linq.XDocument.Parse(data);
                return new FormatDetectionResult { DetectedFormat = "xml", Confidence = 0.95 };
            }
            catch { }
        }

        // HL7 detection
        if (trimmed.StartsWith("MSH|"))
        {
            return new FormatDetectionResult { DetectedFormat = "hl7", Confidence = 0.98 };
        }

        // CSV detection (basic heuristic)
        var lines = trimmed.Split('\n');
        if (lines.Length > 1)
        {
            var firstLine = lines[0];
            var commaCount = firstLine.Count(c => c == ',');
            var semicolonCount = firstLine.Count(c => c == ';');
            var tabCount = firstLine.Count(c => c == '\t');

            if (commaCount > 0 || semicolonCount > 0 || tabCount > 0)
            {
                var confidence = Math.Min(0.8, (commaCount + semicolonCount + tabCount) / 10.0);
                return new FormatDetectionResult { DetectedFormat = "csv", Confidence = confidence };
            }
        }

        return new FormatDetectionResult { DetectedFormat = "unknown", Confidence = 0.0 };
    }

    // Utility Methods
    private string EscapeCsvValue(string value, string delimiter)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsEscaping = value.Contains(delimiter) || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
        
        if (needsEscaping)
        {
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return value;
    }

    private string[] ParseCsvLine(string line, string delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (!inQuotes && line.Substring(i).StartsWith(delimiter))
            {
                values.Add(current.ToString());
                current.Clear();
                i += delimiter.Length - 1;
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values.ToArray();
    }
}

public class FormatDetectionResult
{
    public string DetectedFormat { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}